using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DiskAnalyzer.Core;
using DiskAnalyzer.Model;
using DiskAnalyzer.Services;
using ICSharpCode.TreeView;

namespace DiskAnalyzer
{
    public partial class MainWindow
    {
        private readonly IDriveService driveService;
        private readonly IFileSystemService fileSystemService;
        private readonly IStatisticsService statisticsService;

        private string currentDrive;
        private volatile bool inProcess;
        private CancellationTokenSource statisticsCancellationTokenSource;
        private CancellationTokenSource treeCancellationTokenSource;

        public MainWindow(IDriveService driveService,
                          IFileSystemService fileSystemService,
                          IStatisticsService statisticsService)
        {
            InitializeComponent();
            this.driveService = driveService;
            this.fileSystemService = fileSystemService;
            this.statisticsService = statisticsService;

            DataContext = this;
            DriveInfos = new ObservableCollection<DriveInfo>();
            driveService.StartWatcher(SynchronizationContext.Current, DriveInfos);
        }

        public ObservableCollection<DriveInfo> DriveInfos { get; }
        public ObservableCollection<StatisticsItem> TopFilesBySize => statisticsService.GetCollection(nameof(TopFilesBySize));
        public ObservableCollection<StatisticsItem> TopDirectoriesBySize => statisticsService.GetCollection(nameof(TopDirectoriesBySize));
        public ObservableCollection<StatisticsItem> TopDirectoriesByFilesCount => statisticsService.GetCollection(nameof(TopDirectoriesByFilesCount));
        public ObservableCollection<StatisticsItem> TopExtensions => statisticsService.GetCollection(nameof(TopExtensions));
        public ObservableCollection<StatisticsItem> TopMimeTypes => statisticsService.GetCollection(nameof(TopMimeTypes));
        public ObservableCollection<StatisticsItem> TopFilesByCreationYear => statisticsService.GetCollection(nameof(TopFilesByCreationYear));

        private void Init(string drive)
        {
            var driveNode = fileSystemService.GetDrive(drive);
            fileSystemService.StartWatcher(drive);

            TreeGrid.Root = new TreeGridFileNode(driveNode);

            SetStatus($"Scanning drive: {drive} ...", loader: true);

            var ts = new CancellationTokenSource();

            Task.Run(() =>
                     {
                         try
                         {
                             inProcess = true;
                             fileSystemService.Scan(drive, ts.Token);
                         }
                         finally
                         {
                             inProcess = false;
                         }
                     })
                .ContinueWith(t =>
                              {
                                  SetStatus("Ready", loader: false);

                                  TreeGrid.Root = new TreeGridFileNode(driveNode);
                                  CalcStatistics(driveNode);
                              }, ts.Token, TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());


            treeCancellationTokenSource = ts;
        }

        private void Reset()
        {
            TreeGrid.ItemsSource = null;
            statisticsService.Cleanup();

            treeCancellationTokenSource?.Cancel();
            statisticsCancellationTokenSource?.Cancel();

            if (currentDrive != null)
                fileSystemService.StopWatcher(currentDrive);
            currentDrive = null;
        }


        private void CalcStatistics(IFileSystemNode node)
        {
            if (node == null)
                return;
            statisticsCancellationTokenSource?.Cancel();
            var ts = new CancellationTokenSource();

            SetStatus($"Analyzing: {node.GetFullPath()} ...", loader: true);

            statisticsService.CalculateAsync(node, ts.Token, SynchronizationContext.Current)
                             .ContinueWith(a => SetStatus("Ready", loader: false),
                                           ts.Token, TaskContinuationOptions.None,
                                           TaskScheduler.FromCurrentSynchronizationContext());

            statisticsCancellationTokenSource = ts;
        }

        private void SetStatus(string text, bool loader)
        {
            Progress.Visibility = loader ? Visibility.Visible : Visibility.Collapsed;
            Status.Text = text;
        }

        private void ChangeDrive(object sender, SelectionChangedEventArgs e)
        {
            var driveInfo = Drives.SelectedItem as DriveInfo;
            if (driveInfo == null) return;

            if (currentDrive == driveInfo.Name) return;

            Reset();
            Init(driveInfo.Name);

            currentDrive = driveInfo.Name;
        }

        private void TreeGridItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (inProcess) return;

            var treeViewItem = sender as SharpTreeViewItem;
            if (treeViewItem == null) return;

            var fileNode = treeViewItem.Content as TreeGridFileNode;
            if (fileNode == null) return;

            CalcStatistics(fileSystemService.Root.GetChild(fileNode.FullPath));
        }

        private void ListViewItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var listViewItem = sender as ListViewItem;
            if (listViewItem == null) return;

            var topItem = listViewItem.Content as StatisticsItem;
            if (topItem == null) return;

            var fileNode = ((TreeGridFileNode) TreeGrid.Root).GetChild(IoHelpers.SplitPath(topItem.Path)[1]);

            if (fileNode == null) return;

            var item = TreeGrid.ItemContainerGenerator.ContainerFromItem(fileNode) as ListViewItem;
            if (item == null) return;


            TreeGrid.SelectedItems.Clear();

            TreeGrid.Focus();
            item.Focus();
            item.IsSelected = true;
            Keyboard.Focus(item);
            TreeGrid.ScrollIntoView(item);
        }

        private void MainWindow_OnClosed(object sender, EventArgs e)
        {
            Reset();
            driveService.StopWatcher(DriveInfos);
        }
    }
}