using System;
using System.Collections.Generic;

namespace LinkupSharp.Management
{
    public interface IFileStorage
    {
        event EventHandler<FileEventArgs> Created;
        event EventHandler<FileEventArgs> Deleted;

        IEnumerable<string> List();
        byte[] Get(string filename);
        bool Create(string filename, byte[] content);
        bool Delete(string filename);
    }
}
