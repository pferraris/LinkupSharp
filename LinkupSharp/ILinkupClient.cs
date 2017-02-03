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
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using LinkupSharp.Channels;
using LinkupSharp.Modules;
using LinkupSharp.Security;
using LinkupSharp.Security.Authentication;
using LinkupSharp.Serializers;
using System.Threading.Tasks;

namespace LinkupSharp
{
    public interface ILinkupClient : IDisposable
    {
        IChannel Channel { get; }
        Session Session { get; }
        Id Id { get; }
        bool IsConnected { get; }
        bool IsSignedIn { get; }
        IEnumerable<IClientModule> Modules { get; }

        event EventHandler<EventArgs> Connected;
        event EventHandler<DisconnectedEventArgs> Disconnected;
        event EventHandler<EventArgs> SignedIn;
        event EventHandler<EventArgs> AuthenticationFailed;
        event EventHandler<EventArgs> SignedOut;
        event EventHandler<PacketEventArgs> PacketReceived;

        void AddModule(IClientModule module);
        void RemoveModule(IClientModule module);

        Task<bool> Connect(string endpoint, X509Certificate2 certificate = null);
        Task<bool> Connect<T>(string endpoint, X509Certificate2 certificate = null) where T : IPacketSerializer, new();
        Task<bool> Connect(IChannel channel);
        Task<bool> Disconnect();
        Task<bool> SignIn(Id id);
        Task<bool> SignIn(SignIn signIn);
        Task<bool> RestoreSession(Session session);
        Task<bool> SignOut(Session session);
        Task<bool> Send(object content);
        Task<bool> Send(Packet packet);
    }
}