using DiskAnalyzer.Core;

namespace DiskAnalyzer.Model
{
    public class TopItem
    {
        public string Name { get; set; }

        public string Path { get; set; }

        public long Size { get; set; }

        public string SizeCaption => Size.FormatSize();

        public long CountFiles { get; set; }

        public string CountFilesCaption => CountFiles.FormatNumber();
    }
}