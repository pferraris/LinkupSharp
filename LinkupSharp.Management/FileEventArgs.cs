using System;

namespace LinkupSharp.Management
{
    public class FileEventArgs : EventArgs
    {
        public string Filename { get; set; }
        public byte[] Content { get; set; }

        public FileEventArgs(string filename, byte[] content)
        {
            Filename = filename;
            Content = content;
        }
    }
}