using System;
using LinkupSharp.Modules;
using Microsoft.Owin.Hosting;
using Owin;
using System.Web.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace LinkupSharp.Management
{
    public class LinkupManagementModule : IServerModule
    {
        private string endpoint;
        private IDisposable host;

        internal ConnectionManager Server { get; private set; }

        public LinkupManagementModule(string endpoint)
        {
            this.endpoint = endpoint;
            host = WebApp.Start(endpoint, app =>
            {
                var config = new HttpConfiguration();
                config.Formatters.JsonFormatter.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                config.Formatters.JsonFormatter.SerializerSettings.Formatting = Formatting.Indented;
                config.Formatters.JsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                config.Properties.TryAdd("LinkupManagementModule", this);
                config.MapHttpAttributeRoutes();
                app.UseWebApi(config);
            });
        }

        public void OnAdded(ConnectionManager manager)
        {
            OnRemoved(manager);
            Server = manager;
            Server.ClientConnected += Manager_ClientConnected;
            Server.ClientDisconnected += Manager_ClientDisconnected;
        }

        public void OnRemoved(ConnectionManager manager)
        {
            if (Server != null)
            {
                Server.ClientConnected -= Manager_ClientConnected;
                Server.ClientDisconnected -= Manager_ClientDisconnected;
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
