using System;

namespace LinkupSharp.Security
{
    public interface ISessionRepository
    {
        SessionContext Get(string id);
        void Add(SessionContext session);
        void Remove(SessionContext session);
    }
}
