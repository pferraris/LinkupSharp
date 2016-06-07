using System.Web.Http;

namespace LinkupSharp.Management
{
    public class ApiControllerBase : ApiController
    {
        public LinkupManagementModule Management { get { return Configuration.Properties["LinkupManagementModule"] as LinkupManagementModule; } }
    }
}
