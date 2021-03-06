﻿using System;
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
using DiskAnalyzer.ViewModel;
using ICSharpCode.TreeView;

namespace DiskAnalyzer
{
    public partial class MainWindow
    {
        private readonly IDriveService driveService;
        private readonly IFileSystemService fileSystemService;
        private readonly IStatisticsService statisticsService;

        private string currentDrive;
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
            fileSystemService.Cleanup(drive);

            var driveNode = fileSystemService.GetDrive(drive);
            fileSystemService.StartWatcher(drive);

            TreeGrid.Root = new TreeGridNodeViewModel(driveNode);
            SetStatus($"Scanning drive: {drive} ...", loader: true);

            var ts = new CancellationTokenSource();
            Task.Delay(TimeSpan.FromMilliseconds(100), ts.Token)
                .ContinueWith(t => fileSystemService.Scan(drive, ts.Token), ts.Token)
                .ContinueWith(t =>
                              {
                                  TreeGrid.Root = new TreeGridNodeViewModel(driveNode);
                                  if (ts.IsCancellationRequested)
                                      return;

                                  SetStatus("Ready", loader: false);
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

            statisticsService.CalculateAsync(node, ts.Token, SynchronizationContext.Current);

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
            var treeViewItem = sender as SharpTreeViewItem;
            if (treeViewItem == null) return;

            var fileNode = treeViewItem.Content as TreeGridNodeViewModel;
            if (fileNode == null) return;

            CalcStatistics(fileSystemService.Root.GetChild(fileNode.FullPath));
        }

        private void ListViewItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var listViewItem = sender as ListViewItem;
            if (listViewItem == null) return;

            var topItem = listViewItem.Content as StatisticsItem;
            if (topItem == null) return;

            var fileNode = ((TreeGridNodeViewModel) TreeGrid.Root).GetChild(IoHelpers.SplitPath(topItem.Path)[1]);

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