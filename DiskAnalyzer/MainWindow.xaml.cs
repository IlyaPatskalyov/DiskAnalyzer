using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DiskAnalyzer.Core;
using DiskAnalyzer.Model;
using DiskAnalyzer.Statistics;
using ICSharpCode.TreeView;

namespace DiskAnalyzer
{
    public partial class MainWindow
    {
        private DriveInfo currentDriveInfo;
        private FileSystemModel model;
        private CancellationTokenSource statisticsCancellationTokenSource;
        private CancellationTokenSource treeCancellationTokenSource;


        public MainWindow()
        {
            InitializeComponent();
            Progress.IsIndeterminate = true;

            DataContext = this;
            DriveInfos = new ObservableCollection<DriveInfo>();
            TopFilesBySize = new ObservableCollection<TopItem>();
            TopDirectoriesBySize = new ObservableCollection<TopItem>();
            TopDirectoriesByFilesCount = new ObservableCollection<TopItem>();
            TopExtensions = new ObservableCollection<TopItem>();
            TopMimeTypes = new ObservableCollection<TopItem>();
            TopFilesByCreationYear = new ObservableCollection<TopItem>();

            Drives.ItemsSource = DriveInfos;
            UpdateDrives();
            TreeGrid.ShowRoot = true;

            var synchronizationContext = SynchronizationContext.Current;
            var timer = new System.Timers.Timer();
            timer.Elapsed += (v, e) => synchronizationContext.Send(a => UpdateDrives(), v);
            timer.Interval = 5000;
            timer.Enabled = true;
        }


        public ObservableCollection<DriveInfo> DriveInfos { get; }
        public ObservableCollection<TopItem> TopFilesBySize { get; }
        public ObservableCollection<TopItem> TopDirectoriesBySize { get; }
        public ObservableCollection<TopItem> TopDirectoriesByFilesCount { get; }
        public ObservableCollection<TopItem> TopExtensions { get; }
        public ObservableCollection<TopItem> TopMimeTypes { get; }
        public ObservableCollection<TopItem> TopFilesByCreationYear { get; }

        private void UpdateDrives()
        {
            var newValues = DriveInfo.GetDrives().Where(d => d.IsReady).ToArray();

            var newDriveInfos = newValues.Select((drive, index) => new {Drive = drive.Name, Index = index}).ToDictionary(k => k.Drive, v => v.Index);
            var oldDriveInfos = DriveInfos.Select((drive, index) => new {Drive = drive.Name, Index = index}).ToDictionary(k => k.Drive, v => v.Index);

            foreach (var name in oldDriveInfos.Keys.Except(newDriveInfos.Keys))
            {
                DriveInfos.RemoveAt(oldDriveInfos[name]);
            }

            foreach (var name in newDriveInfos.Keys.Except(oldDriveInfos.Keys))
            {
                var index = newDriveInfos[name];
                DriveInfos.Insert(index, newValues[index]);
            }
        }

        private void Init()
        {
            var driveInfo = Drives.SelectedItem as DriveInfo;
            if (driveInfo == null) return;

            if (currentDriveInfo == driveInfo) return;

            TreeGrid.ItemsSource = null;
            CleanupStatistics();

            treeCancellationTokenSource?.Cancel();
            statisticsCancellationTokenSource?.Cancel();

            model?.StopWatcher();

            model = new FileSystemModel(driveInfo.Name, SynchronizationContext.Current);
            model.StartWatcher();

            TreeGrid.Root = new TreeGridFileNode(model);

            Progress.Visibility = Visibility.Visible;
            SetStatus($"Scanning drive: {model.Root.GetFullPath()} ...");

            var ts = new CancellationTokenSource();

            Task.Run(() => model.Refresh(ts.Token))
                .ContinueWith(t =>
                              {
                                  Progress.Visibility = Visibility.Collapsed;
                                  SetStatus("Ready");

                                  TreeGrid.Root = new TreeGridFileNode(model);
                                  CalcTop(model.Root);
                              }, ts.Token, TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());

            currentDriveInfo = driveInfo;
            treeCancellationTokenSource = ts;
        }

        private void CleanupStatistics()
        {
            TopFilesBySize.Clear();
            TopDirectoriesBySize.Clear();
            TopDirectoriesByFilesCount.Clear();
            TopExtensions.Clear();
            TopMimeTypes.Clear();
            TopFilesByCreationYear.Clear();
        }


        private void CalcTop(FileSystemNode root)
        {
            if (root == null)
                return;
            statisticsCancellationTokenSource?.Cancel();
            var ts = new CancellationTokenSource();

            Progress.Visibility = Visibility.Visible;
            SetStatus($"Analyzing: {root.GetFullPath()} ...");

            Task.WhenAll(
                    CalcStatisticsAsync(root, new TopFilesBySizeCalculator(), TopFilesBySize, ts),
                    CalcStatisticsAsync(root, new TopDirectoriesBySizeCalculator(), TopDirectoriesBySize, ts),
                    CalcStatisticsAsync(root, new TopDirectoriesByFilesCountCalculator(), TopDirectoriesByFilesCount, ts),
                    CalcStatisticsAsync(root, new TopExtensionsCalculator(), TopExtensions, ts),
                    CalcStatisticsAsync(root, new TopMimeTypesCalculator(), TopMimeTypes, ts),
                    CalcStatisticsAsync(root, new TopFilesByCreationYearCalculator(), TopFilesByCreationYear, ts))
                .ContinueWith(a =>
                              {
                                  Progress.Visibility = Visibility.Collapsed;
                                  SetStatus("Ready");
                              }, ts.Token, TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());

            statisticsCancellationTokenSource = ts;
        }

        private Task CalcStatisticsAsync(FileSystemNode root, IStatisticsCalculator calculator, ObservableCollection<TopItem> observableCollection,
                                         CancellationTokenSource ts)
        {
            observableCollection.Clear();
            return Task.Run(() => calculator.Calculate(root).ToList(), ts.Token)
                       .ContinueWith(async t => SetCollection(observableCollection, await t),
                                     TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void SetCollection<T>(ObservableCollection<T> observable, IEnumerable<T> result)
        {
            observable.Clear();
            foreach (var r in result)
            {
                observable.Add(r);
            }
        }


        internal void SetStatus(string text)
        {
            Status.Text = text;
        }

        private void ChangeDrive(object sender, SelectionChangedEventArgs e)
        {
            Init();
        }

        private void TreeGridItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (model.InProcess) return;

            var treeViewItem = sender as SharpTreeViewItem;
            if (treeViewItem == null) return;

            var fileNode = (treeViewItem.Content as TreeGridFileNode);
            if (fileNode == null) return;

            CalcTop(model.Root.Parent.GetChild(fileNode.FullPath));
        }

        private void ListViewItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var listViewItem = sender as ListViewItem;
            if (listViewItem == null) return;

            var topItem = listViewItem.Content as TopItem;
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
    }
}