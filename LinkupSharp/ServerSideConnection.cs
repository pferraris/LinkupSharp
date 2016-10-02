#region License
/*
 * The MIT License (MIT)
 *
 * Copyright (c) 2015 Pablo Ferraris
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

using LinkupSharp.Channels;
using LinkupSharp.Security;
using LinkupSharp.Security.Authentication;
using System;
using System.Collections.Generic;

namespace LinkupSharp
{
    public class ServerSideConnection : IServerSideConnection
    {
        private Disconnected disconnected;

        public IChannel Channel { get; private set; }
        public Session Session { get; internal set; }
        public Id Id { get { return Session?.Id; } }
        public bool IsSignedIn { get { return Session != null; } }
        public bool IsConnected { get; private set; }

        public event EventHandler<SignInEventArgs> SignInRequired;
        public event EventHandler<SessionEventArgs> RestoreSessionRequired;
        public event EventHandler<SessionEventArgs> SignOutRequired;
        public event EventHandler<DisconnectedEventArgs> Disconnected;
        public event EventHandler<PacketEventArgs> PacketReceived;

        public ServerSideConnection()
        {

            #region Packet Handlers

            RegisterHandler<SignIn>(packet =>
            {
                SignInRequired?.Invoke(this, new SignInEventArgs(packet.GetContent() as SignIn));
                return true;
            });

            RegisterHandler<SignOut>(packet =>
            {
                SignOutRequired?.Invoke(this, new SessionEventArgs(packet.GetContent<SignOut>().Session));
                return true;
            });

            RegisterHandler<RestoreSession>(packet =>
            {
                RestoreSessionRequired?.Invoke(this, new SessionEventArgs(packet.GetContent<RestoreSession>().Session));
                return true;
            });

            RegisterHandler<Disconnected>(packet =>
            {
                Disconnect(packet.GetContent<Disconnected>().Reason, false);
                return true;
            });

            #endregion Packet Handlers

        }

        public void Dispose()
        {
            Disconnect();
        }

        #region Methods

        public bool SetSession(Session session)
        {
            if (session == null) return false;
            Session = session;
            Send(new SignedIn(session));
            return true;
        }

        public bool CloseSession(Session session)
        {
            if (session.Id == Id)
            {
                if (session.Token == Session.Token)
                {
                    Session = null;
                    Send(new SignedOut(session, true));
                }
                else
                    Send(new SignedOut(session, false));
                return true;
            }
            return false;
        }

        public void Connect(IChannel channel)
        {
            if (Channel == null)
            {
                Channel = channel;
                Channel.PacketReceived += Channel_PacketReceived;
                Channel.Closed += Channel_Closed;
                try
                {
                    Channel.Open().Wait();
                    IsConnected = true;
                    Send(new Connected());
                }
                catch (Exception ex)
                {
                    Channel = null;
                    throw ex;
                }
            }
        }

        private void Disconnect(Reasons reason, bool sendNotification = true)
        {
            if (Channel != null)
            {
                disconnected = new Disconnected(reason);
                if (sendNotification) Send(disconnected);
                Channel.Close();
            }
        }

        public void Disconnect()
        {
            Disconnect(Reasons.ServerRequest);
        }

        public void Send(object content)
        {
            if (content is Packet)
                Send(content as Packet);
            else
                Send(new Packet(content));
        }

        public void Send(Packet packet)
        {
            if (Channel != null)
                Channel.Send(packet);
        }

        #endregion Methods

        #region Channel

        private void Channel_PacketReceived(object sender, PacketEventArgs e)
        {
            if (!Process(e.Packet))
                PacketReceived?.Invoke(this, new PacketEventArgs(e.Packet));
        }

        private void Channel_Closed(object sender, EventArgs e)
        {
            if (disconnected == null)
                disconnected = new Disconnected(Reasons.ConnectionLost);

            Channel.PacketReceived -= Channel_PacketReceived;
            Channel.Closed -= Channel_Closed;
            Channel = null;
            IsConnected = false;
            Disconnected?.Invoke(this, new DisconnectedEventArgs(disconnected));
        }

        #endregion Channel

        #region Packet Handlers

        private delegate bool PacketHandler(Packet packet);
        private List<Tuple<Type, PacketHandler>> packetHandlers = new List<Tuple<Type, PacketHandler>>();

        private bool Process(Packet packet)
        {
            foreach (var handler in packetHandlers)
                if (packet.Is(handler.Item1))
                    if (handler.Item2(packet))
                        return true;
            return false;
        }

        private void RegisterHandler<T>(PacketHandler handler)
        {
            RegisterHandler(typeof(T), handler);
        }

        private void RegisterHandler(Type type, PacketHandler handler)
        {
            packetHandlers.Add(Tuple.Create(type, handler));
        }

        #endregion Packet Handlers

    }
}
