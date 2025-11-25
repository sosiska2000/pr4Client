using System;

namespace Common
{
    [Serializable]
    public class FileInfoFTP
    {
        public byte[] Data { get; set; }
        public string Name { get; set; }

        public FileInfoFTP(byte[] data, string name)
        {
            this.Data = data;
            this.Name = name;
        }
    }
}