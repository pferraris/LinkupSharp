#region License
/*
 * The MIT License (MIT)
 *
 * Copyright (c) 2017 Pablo Ferraris
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
*/
#endregion License

using System.Collections.Generic;
using System.Linq;

namespace LinkupSharp.Security
{
    public class MemorySessionRepository : ISessionRepository
    {
        private Dictionary<string, Session> sessions;

        public MemorySessionRepository()
        {
            sessions = new Dictionary<string, Session>();
        }

        public bool Contains(string token)
        {
            return sessions.ContainsKey(token);
        }

        public Session Get(string token)
        {
            if (Contains(token))
                return sessions[token];
            return null;
        }

        public IEnumerable<Session> Get(Id id)
        {
            return sessions.Values.Where(x => x.Id == id).ToArray();
        }

        public void Add(Session session)
        {
            lock (sessions)
                sessions[session.Token] = session;
        }

        public void Remove(Session session)
        {
            lock (sessions)
                if (sessions.ContainsKey(session.Token))
                    sessions.Remove(session.Token);
        }
    }
}
