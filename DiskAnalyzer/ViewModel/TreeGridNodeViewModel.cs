﻿using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DiskAnalyzer.Core;
using DiskAnalyzer.Model;
using ICSharpCode.TreeView;

namespace DiskAnalyzer.ViewModel
{
    public class TreeGridNodeViewModel : SharpTreeNode
    {
        private static readonly Color[] colors = typeof(Colors).GetProperties()
                                                               .Where(p => p.Name.StartsWith("Dark"))
                                                               .Select(p => p.GetValue(null))
                                                               .Cast<Color>()
                                                               .ToArray();

        private readonly string fullPath;

        private readonly int level;
        private readonly IFileSystemNode node;
        private readonly SynchronizationContext synchronizationContext;

        public TreeGridNodeViewModel(IFileSystemNode node, int level = 0)
        {
            this.node = node;
            this.level = level;
            node.CollectionChanged += CollectionChangedCallback;
            node.PropertyChanged += NotifyAboutChangedProperties;
            synchronizationContext = SynchronizationContext.Current;
            fullPath = this.node.GetFullPath();
            LazyLoading = node.FileType != FileType.File;
        }


        public string Name => node.Name;

        public override object Text => node.Name;

        public override object Icon => IconHelpers.GetIconDll(FullPath);

        public override object ToolTip => FullPath;

        public string Size => node.Size.FormatSize();
        public string CountFiles => (node.CountFiles > 0 ? node.CountFiles : (int?) null)?.FormatNumber();
        public string CountDirectories => (node.CountDirectories > 0 ? node.CountDirectories : (int?) null)?.FormatNumber();

        public Thickness MarginPercentage => new Thickness((1 - Math.Pow(0.9, level)) * 100, 0, 0, 0);
        public double PercentWidth => 100 * Math.Pow(0.9, level);
        
        public double Percent => (double) node.Size / (node.Parent.Parent != null ? node.Parent.Size : node.Size) * 100;

        public double FilledPercentWidth => Percent * Math.Pow(0.9, level);

        public string FullPath => fullPath;

        public string DirectoryPath => node.FileType == FileType.File ? Path.GetDirectoryName(FullPath) : FullPath;

        public Brush Color => new SolidColorBrush(colors[level.GetHashCode() % colors.Length]);


        private void CollectionChangedCallback(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!IsExpanded) return;

            if (SynchronizationContext.Current == synchronizationContext)
                LoadChildren();
            else
                Task.Run(() => synchronizationContext.Send(v => LoadChildren(), null));
        }

        protected override void LoadChildren()
        {
            Children.Clear();
            foreach (var p in node.Children.OrderByDescending(r => r.FileType).ThenBy(r => r.Name))
                Children.Add(new TreeGridNodeViewModel(p, level + 1));
        }

        public TreeGridNodeViewModel GetChild(string path)
        {
            IsExpanded = true;

            var parts = IoHelpers.SplitPath(path);
            var child = Children?.Cast<TreeGridNodeViewModel>().FirstOrDefault(n => n.Name == parts[0]);
            if (child != null)
            {
                if (parts.Length == 1)
                    return child;

                return child.GetChild(parts[1]);
            }

            return null;
        }

        public override void ShowContextMenu(ContextMenuEventArgs e)
        {
            base.ShowContextMenu(e);
            var menu = new ContextMenu();
            var menuItem = new MenuItem {Header = "Show in explorer"};
            menu.Items.Add(menuItem);
            menuItem.Click += (sender, args) => Process.Start(DirectoryPath);

            menu.PlacementTarget = e.Source as UIElement;
            menu.IsOpen = true;
        }

        private void NotifyAboutParentChangedProperties(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == nameof(IFileSystemNode.Size))
            {
                RaisePropertyChanged(nameof(FilledPercentWidth));
            }
        }

        private void NotifyAboutChangedProperties(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == nameof(IFileSystemNode.Size))
            {
                RaisePropertyChanged(nameof(Size));
                RaisePropertyChanged(nameof(FilledPercentWidth));
            }

            if (args.PropertyName == nameof(IFileSystemNode.CountFiles))
                RaisePropertyChanged(nameof(CountFiles));
            if (args.PropertyName == nameof(IFileSystemNode.CountDirectories))
                RaisePropertyChanged(nameof(CountDirectories));
        }

        protected override void OnIsVisibleChanged()
        {
            base.OnIsVisibleChanged();
            if (IsVisible)
            {
                node.Parent.PropertyChanged += NotifyAboutParentChangedProperties;
            }
            else
            {
                node.Parent.PropertyChanged -= NotifyAboutParentChangedProperties;
            }
        }
    }
}