using System.Linq;
using System.Web.Http;

namespace LinkupSharp.Management.Controllers
{
    [RoutePrefix("clients")]
    public class ClientsController : ApiControllerBase
    {
        [HttpGet]
        [Route("")]
        public IHttpActionResult Get()
        {
            return Ok(Module.Manager.Clients.Select(x => new
            {
                Session = x.Session,
                Channel = new
                {
                    Type = x.Channel.GetType().Name.Replace("`1", ""),
                    Endpoint = x.Channel.Endpoint,
                    Certificate = x.Channel.Certificate?.Subject
                }
            }).ToArray());
        }
    }
}
