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

using LinkupSharp.Security.Authentication;

namespace LinkupSharp.Modules
{
    public class SessionModule : ClientModule
    {
        public SessionModule()
        {
            RegisterHandler<SignIn>(HandleSignIn); // Only Server Side
            RegisterHandler<SignOut>(HandleSignOut); // Only Server Side
            RegisterHandler<RestoreSession>(HandleRestoreSession); // Only Server Side
            RegisterHandler<SignedIn>(HandleSignedIn); // Only Client Side
            RegisterHandler<SignedOut>(HandleSignedOut); // Only Client Side
            RegisterHandler<AuthenticationFailed>(HandleAuthenticationFailed); // Only Client Side
            RegisterHandler<Connected>(HandleConnected); // Only Client Side
            RegisterHandler<Disconnected>(HandleDisconnected); // Both Server & Client Side
        }

        private bool HandleSignIn(Packet packet, ClientConnection client)
        {
            client.OnSignInRequired(packet.GetContent<SignIn>().Credentials);
            return true;
        }

        private bool HandleSignOut(Packet packet, ClientConnection client)
        {
            client.OnSignOutRequired(packet.GetContent<SignOut>().Session);
            return true;
        }

        private bool HandleRestoreSession(Packet packet, ClientConnection client)
        {
            client.OnRestoreSessionRequired(packet.GetContent<RestoreSession>().Session);
            return true;
        }

        private bool HandleSignedIn(Packet packet, ClientConnection client)
        {
            client.OnSignedIn(packet.GetContent<SignedIn>().Session);
            return true;
        }

        private bool HandleSignedOut(Packet packet, ClientConnection client)
        {
            var signedOut = packet.GetContent<SignedOut>();
            client.OnSignedOut(signedOut.Session, signedOut.Current);
            return true;
        }

        private bool HandleAuthenticationFailed(Packet packet, ClientConnection client)
        {
            client.OnAuthenticationFailed();
            return true;
        }

        private bool HandleConnected(Packet packet, ClientConnection client)
        {
            client.OnConnected();
            return true;
        }

        private bool HandleDisconnected(Packet packet, ClientConnection client)
        {
            client.Disconnect(packet.GetContent<Disconnected>().Reason, false);
            return true;
        }
    }
}
