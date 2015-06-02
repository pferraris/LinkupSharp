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

using LinkupSharp.Authentication;

namespace LinkupSharp.Modules
{
    public class SessionModule : ClientModule
    {
        public SessionModule()
        {
            RegisterHandler<Credentials>(HandleCredentials); // Only Server Side
            RegisterHandler<SessionContext>(HandleSessionContext); // Only Server Side
            RegisterHandler<Connected>(HandleConnected); // Only Client Side
            RegisterHandler<Authenticated>(HandleAuthenticated); // Only Client Side
            RegisterHandler<AuthenticationFailed>(HandleAuthenticationFailed); // Only Client Side
            RegisterHandler<Disconnected>(HandleDisconnected); // Both Server & Client Side
        }

        private bool HandleCredentials(Packet packet, ClientConnection client)
        {
            client.OnAuthenticationRequired(packet.GetContent() as Credentials);
            return true;
        }

        private bool HandleSessionContext(Packet packet, ClientConnection client)
        {
            client.OnRestoreSessionRequired(packet.GetContent<SessionContext>());
            return true;
        }

        private bool HandleConnected(Packet packet, ClientConnection client)
        {
            client.OnConnected();
            return true;
        }

        private bool HandleAuthenticated(Packet packet, ClientConnection client)
        {
            client.OnAuthenticated(packet.GetContent<Authenticated>().SessionContext);
            return true;
        }

        private bool HandleAuthenticationFailed(Packet packet, ClientConnection client)
        {
            client.OnAuthenticationFailed();
            return true;
        }

        private bool HandleDisconnected(Packet packet, ClientConnection client)
        {
            client.Disconnect(packet.GetContent<Disconnected>().Reason, false);
            return true;
        }
    }
}
