using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LinkupSharp.Management
{
    public class FileSystemStorage : IFileStorage
    {
        private string path;

        public event EventHandler<FileEventArgs> Created;
        public event EventHandler<FileEventArgs> Deleted;

        public FileSystemStorage(string path)
        {
            this.path = path;
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        public IEnumerable<string> List()
        {
            var directory = new DirectoryInfo(path);
            return directory.EnumerateFiles("*.*")
                            .OrderBy(x => x.LastWriteTime)
                            .Select(x => x.Name)
                            .ToArray();
        }

        public byte[] Get(string filename)
        {
            var fullname = Path.Combine(path, filename);
            if (!File.Exists(fullname))
                return null;
            return File.ReadAllBytes(fullname);
        }

        public bool Create(string filename, byte[] content)
        {
            var fullname = Path.Combine(path, filename);
            if (File.Exists(fullname))
                return false;
            File.WriteAllBytes(fullname, content);
            OnCreated(filename, content);
            return true;
        }

        public bool Delete(string filename)
        {
            var fullname = Path.Combine(path, filename);
            if (!File.Exists(fullname))
                return false;
            var content = Get(filename);
            File.Delete(fullname);
            OnDeleted(filename, content);
            return true;
        }

        private void OnCreated(string filename, byte[] content)
        {
            Created?.Invoke(this, new FileEventArgs(filename, content));
        }

        private void OnDeleted(string filename, byte[] content)
        {
            Deleted?.Invoke(this, new FileEventArgs(filename, content));
        }
    }
}
