using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DiskAnalyzer.Core;
using ICSharpCode.TreeView;

namespace DiskAnalyzer.Model
{
    public class TreeGridFileNode : SharpTreeNode
    {
        private readonly int level;
        private readonly FileSystemNode node;
        private string fullPath;

        public TreeGridFileNode(FileSystemNode node, int level)
        {
            this.node = node;
            this.level = level;


            this.node.PropertyChanged += (n, args) =>
                                         {
                                             if (args.PropertyName == nameof(FileSystemNode.Size))
                                                 RaisePropertyChanged(nameof(SizeCaption));
                                             if (args.PropertyName == nameof(FileSystemNode.CountFiles))
                                                 RaisePropertyChanged(nameof(CountFiles));
                                             if (args.PropertyName == nameof(FileSystemNode.CountDirectories))
                                                 RaisePropertyChanged(nameof(CountDirectories));
                                         };
            this.node.CollectionChanged += (n, args) => LoadChildren();
            fullPath = this.node.GetFullPath();
            LazyLoading = node.FileType != FileType.File;
        }

        public string Name => node.Name;

        public override object Text => node.Name;

        public override object Icon => IconHelpers.GetIconDll(fullPath);

        public override object ToolTip => fullPath;

        public string SizeCaption => node.Size.FormatSize();

        public long Size => node.Size;

        public Thickness MarginPercentage => new Thickness((1 - Math.Pow(0.9, level)) * 100, 0, 0, 0);

        public double FilledWidth => ((double) node.Size / node.Parent.Size) * Math.Pow(0.9, level) * 100;

        public double FreeWidth => (1 - (double) node.Size / node.Parent.Size) * Math.Pow(0.9, level) * 100;

        public int? CountFiles => node.CountFiles > 0 ? node.CountFiles : (int?) null;
        public string CountFilesCaption => CountFiles?.FormatNumber();
        public int? CountDirectories => node.CountDirectories > 0 ? node.CountDirectories : (int?) null;
        public string CountDirectoriesCaption => CountDirectories?.FormatNumber();


        public string FullPath => fullPath;
        public string DirectoryPath => node.FileType == FileType.File ? Path.GetDirectoryName(FullPath) : FullPath;

        protected override void LoadChildren()
        {
            Children.Clear();
            foreach (var p in node.Children.OrderByDescending(r => r.FileType).ThenByDescending(r => r.Size))
                Children.Add(new TreeGridFileNode(p, level + 1));
        }

        public TreeGridFileNode GetChild(string path)
        {
            IsExpanded = true;

            var parts = IoHelpers.SplitPath(path);
            var child = Children?.Cast<TreeGridFileNode>().FirstOrDefault(n => n.Name == parts[0]);
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
            var menuItem = new MenuItem() {Header = "Show in explorer"};
            menu.Items.Add(menuItem);
            menuItem.Click += (sender, args) => Process.Start(DirectoryPath);

            menu.PlacementTarget = e.Source as UIElement;
            menu.IsOpen = true;
        }
    }
}