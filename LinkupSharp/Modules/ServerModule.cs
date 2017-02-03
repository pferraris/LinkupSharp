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

using LinkupSharp.Security.Authentication;
using LinkupSharp.Security.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinkupSharp.Modules
{
    public abstract class ServerModule : IServerModule
    {

        #region Implements IServerPacketInterceptor

        public virtual void OnAdded(LinkupServer server) { }

        public virtual void OnRemoved(LinkupServer server) { }

        public bool Process(Packet packet, IServerSideConnection connection, LinkupServer server)
        {
            if (server == null) return false;
            foreach (var handler in PacketHandlers)
                if (packet.Is(handler.Item1))
                {
                    var attributes = handler.Item2.Method.GetCustomAttributes(typeof(AuthenticatedAttribute), true);
                    if ((!attributes.Any()) || (connection.Session != null))
                    {
                        var roles = attributes.OfType<AuthorizedAttribute>().SelectMany(x => x.Roles).ToArray();
                        if ((!roles.Any()) || (server.IsAuthorized(connection, roles)))
                            if (handler.Item2(packet, connection, server))
                                return true;
                    }
                }
            return false;
        }

        #endregion Implements IServerPacketInterceptor

        #region Handlers

        protected delegate bool PacketHandler(Packet packet, IServerSideConnection connection, LinkupServer server);
        private List<Tuple<Type, PacketHandler>> PacketHandlers = new List<Tuple<Type, PacketHandler>>();

        protected void RegisterHandler<T>(PacketHandler handler)
        {
            RegisterHandler(typeof(T), handler);
        }

        protected void RegisterHandler(Type type, PacketHandler handler)
        {
            PacketHandlers.Add(Tuple.Create(type, handler));
        }

        #endregion Handlers

    }
}
