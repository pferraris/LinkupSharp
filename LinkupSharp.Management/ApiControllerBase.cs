using System.Web.Http;

namespace LinkupSharp.Management
{
    public class ApiControllerBase : ApiController
    {
        public LinkupManagementModule Module { get { return Configuration.Properties["LinkupManagementModule"] as LinkupManagementModule; } }
    }
}
