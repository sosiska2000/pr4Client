namespace FTPClient
{
    public class FileItem
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Size { get; set; }
        public bool IsDirectory { get; set; }
        public string FullPath { get; set; }
    }
}