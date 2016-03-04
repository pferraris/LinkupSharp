using System.Collections.Generic;

namespace LinkupSharp.Security
{
    public class MemorySessionRepository : LinkupSharp.Security.ISessionRepository
    {
        private Dictionary<string, SessionContext> sessions;

        public MemorySessionRepository()
        {
            sessions = new Dictionary<string, SessionContext>();
        }

        public SessionContext Get(string id)
        {
            if (sessions.ContainsKey(id))
                return sessions[id];
            else
                return null;
        }

        public void Add(SessionContext session)
        {
            lock (sessions)
                if (sessions.ContainsKey(session.Id))
                    sessions[session.Id] = session;
                else
                    sessions.Add(session.Id, session);
        }

        public void Remove(SessionContext session)
        {
            lock (sessions)
                if (sessions.ContainsKey(session.Id) && (sessions[session.Id].Token == session.Token))
                    sessions.Remove(session.Id);
        }
    }
}
