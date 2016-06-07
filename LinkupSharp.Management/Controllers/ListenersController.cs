using LinkupSharp.Channels;
using System.Linq;
using System.Web.Http;

namespace LinkupSharp.Management.Controllers
{
    [RoutePrefix("listeners")]
    public class ListenersController : ApiControllerBase
    {
        [HttpGet]
        [Route("")]
        public IHttpActionResult Get()
        {
            return Ok(Management.Server.Listeners.Select(x => new
            {
                Type = x.GetType().Name.Replace("`1", ""),
                Endpoint = x.Endpoint,
                Certificate = x.Certificate?.Subject
            }).ToArray());
        }

        [HttpGet]
        [Route("available")]
        public IHttpActionResult Available()
        {
            return Ok(DependencyHelper.GetClasses<IChannelListener>().Select(x => x.Name.Replace("`1", "")).ToArray());
        }
    }
}
