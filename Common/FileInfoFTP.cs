using System;

namespace Common
{

    public class FileInfoFTP
    {
        public byte[] Data { get; set; }
        public string Name { get; set; }
        public FileInfoFTP(byte[] Data, string Name)
        {
            this.Data = Data;
            this.Name = Name;
        }
    }
}