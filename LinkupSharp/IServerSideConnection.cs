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

using System;
using LinkupSharp.Channels;
using LinkupSharp.Security;
using LinkupSharp.Security.Authentication;

namespace LinkupSharp
{
    public interface IServerSideConnection : IDisposable
    {
        IChannel Channel { get; }
        Session Session { get; }
        Id Id { get; }
        bool IsConnected { get; }
        bool IsSignedIn { get; }

        event EventHandler<SignInEventArgs> SignInRequired;
        event EventHandler<SessionEventArgs> RestoreSessionRequired;
        event EventHandler<SessionEventArgs> SignOutRequired;
        event EventHandler<DisconnectedEventArgs> Disconnected;
        event EventHandler<PacketEventArgs> PacketReceived;

        void Connect(IChannel channel);
        void Disconnect();
        bool SetSession(Session session);
        bool CloseSession(Session session);
        void Send(object content);
        void Send(Packet packet);
    }
}