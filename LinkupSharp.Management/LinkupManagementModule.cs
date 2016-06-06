using System;
using LinkupSharp.Modules;
using Microsoft.Owin.Hosting;
using Owin;
using System.Web.Http;
using System.Configuration;
using System.Collections.Generic;

namespace LinkupSharp.Management
{
    public class LinkupManagementModule : IServerModule
    {
        private string endpoint;
        private IDisposable host;

        internal ConnectionManager Manager { get; private set; }

        public LinkupManagementModule(string endpoint)
        {
            this.endpoint = endpoint;
            host = WebApp.Start(endpoint, app =>
            {
                var config = new HttpConfiguration();
                config.Properties.TryAdd("LinkupManagementModule", this);
                config.MapHttpAttributeRoutes();
                app.UseWebApi(config);
            });
        }

        public void OnAdded(ConnectionManager manager)
        {
            OnRemoved(manager);
            Manager = manager;
            Manager.ClientConnected += Manager_ClientConnected;
            Manager.ClientDisconnected += Manager_ClientDisconnected;
        }

        public void OnRemoved(ConnectionManager manager)
        {
            if (Manager != null)
            {
                Manager.ClientConnected -= Manager_ClientConnected;
                Manager.ClientDisconnected -= Manager_ClientDisconnected;
            }
        }

        private void Manager_ClientConnected(object sender, ClientConnectionEventArgs e)
        {
            
        }

        private void Manager_ClientDisconnected(object sender, ClientConnectionEventArgs e)
        {
            
        }

        public bool Process(Packet packet, ClientConnection client, ConnectionManager manager)
        {
            return false;
        }
    }
}
