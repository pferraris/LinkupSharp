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

using LinkupSharp.Loggers;
using LinkupSharp.Serializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace LinkupSharp.Channels
{
    public class WebClientChannel : IClientChannel
    {
        private IPacketSerializer serializer;
        private Task readingTask;
        private bool active;
        private bool serverSide;
        private Queue<Packet> pending;
        private int poolingTime;
        private Timer inactivityTimer;
        private int inactivityTime;

        internal string Address { get; private set; }
        internal string Id { get; private set; }

        static WebClientChannel()
        {
            ServicePointManager.DefaultConnectionLimit = Int32.MaxValue;
        }

        internal WebClientChannel(string id, bool serverSide)
        {
            Id = id;
            this.serverSide = true;
            serializer = new JsonPacketSerializer();
            inactivityTime = 5000;
            inactivityTimer = new Timer(InactivityCheck, null, inactivityTime, Timeout.Infinite);
            pending = new Queue<Packet>();
        }

        public WebClientChannel(string address)
        {
            HttpWebRequest.DefaultWebProxy.Credentials = CredentialCache.DefaultNetworkCredentials;
            Address = address;
            Id = Guid.NewGuid().ToString();
            serverSide = false;
            serializer = new JsonPacketSerializer();
            poolingTime = 500;
            inactivityTime = 5000;
            active = true;
            readingTask = Task.Factory.StartNew(Read);
        }

        #region Methods

        internal void DataReceived(byte[] buffer)
        {
            foreach (var packet in serializer.Deserialize(buffer))
                OnPacketReceived(packet);
        }

        internal byte[] DataPending()
        {
            inactivityTimer.Change(inactivityTime, Timeout.Infinite);
            lock (pending)
                return serializer.Serialize(pending.Dequeue());
        }

        private void Read()
        {
            while (active)
            {
                try
                {
                    var webRequest = HttpWebRequest.Create(Address) as HttpWebRequest;
                    HttpWebRequest.DefaultWebProxy.Credentials = CredentialCache.DefaultNetworkCredentials;
                    webRequest.ContentType = "text/plain";
                    webRequest.Headers.Add("ClientId", Id);
                    webRequest.Timeout = inactivityTime;
                    using (HttpWebResponse webResponse = webRequest.GetResponse() as HttpWebResponse)
                    {
                        if (webResponse.StatusCode == HttpStatusCode.OK)
                        {
                            byte[] buffer = new byte[65536];
                            int count;
                            do
                            {
                                count = webResponse.GetResponseStream().Read(buffer, 0, buffer.Length);
                                if (count > 0) DataReceived(buffer.Take(count).ToArray());
                            } while (count > 0);
                        }
                        else if (webResponse.StatusCode != HttpStatusCode.NoContent)
                            Close();
                        webResponse.Close();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Reading error.");
                }
                Thread.Sleep(poolingTime);
            }
        }

        public void Send(Packet packet)
        {
            if (serverSide)
                lock (pending)
                    pending.Enqueue(packet);
            else if (active)
            {
                try
                {
                    byte[] buffer = serializer.Serialize(packet);
                    var webRequest = HttpWebRequest.Create(Address) as HttpWebRequest;
                    webRequest.ContentType = "text/plain";
                    webRequest.Headers.Add("ClientId", Id);
                    webRequest.Method = "POST";
                    webRequest.ContentLength = buffer.Length;
                    using (var stream = webRequest.GetRequestStream())
                        stream.Write(buffer, 0, buffer.Length);
                    using (HttpWebResponse webResponse = webRequest.GetResponse() as HttpWebResponse)
                    {
                        if (webResponse.StatusCode != HttpStatusCode.OK)
                            throw new InvalidOperationException(String.Format("StatusCode: {0}", ((HttpStatusCode)webResponse.StatusCode).ToString()));
                        webResponse.Close();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Sending error.");
                    Close();
                }
            }
        }

        public void Close()
        {
            if (active)
            {
                active = false;
                try
                {
                    readingTask.Wait();
                    readingTask.Dispose();
                }
                catch { }
                try
                {
                    inactivityTimer.Change(Timeout.Infinite, Timeout.Infinite);
                    inactivityTimer.Dispose();
                }
                catch { }
                OnClosed();
            }
        }

        private void InactivityCheck(object state)
        {
            Close();
        }

        public void Dispose()
        {
            Close();
        }

        #endregion Methods

        #region Events

        public event EventHandler<PacketEventArgs> PacketReceived;
        public event EventHandler<EventArgs> Closed;

        private void OnPacketReceived(Packet packet)
        {
            if (PacketReceived != null)
                PacketReceived(this, new PacketEventArgs(packet));
        }

        private void OnClosed()
        {
            if (Closed != null)
                Closed(this, EventArgs.Empty);
        }

        #endregion Events

    }
}
