using System.Linq;
using System.Web.Http;

namespace LinkupSharp.Management
{
    [RoutePrefix("clients")]
    public class ClientsController : ApiController
    {
        public LinkupManagementModule Module { get { return Configuration.Properties["LinkupManagementModule"] as LinkupManagementModule; } }

        [HttpGet]
        [Route("")]
        public IHttpActionResult Get()
        {
            return Ok(Module.Manager.Clients.ToArray());
        }
    }
}
