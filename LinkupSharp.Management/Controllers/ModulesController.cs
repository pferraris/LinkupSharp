using LinkupSharp.Modules;
using System.Linq;
using System.Web.Http;

namespace LinkupSharp.Management.Controllers
{
    [RoutePrefix("modules")]
    public class ModulesController : ApiControllerBase
    {
        [HttpGet]
        [Route("")]
        public IHttpActionResult Get()
        {
            return Ok(Management.Server.Modules.Select(x => new
            {
                Type = x.GetType().Name.Replace("`1", "")
            }).ToArray());
        }

        [HttpGet]
        [Route("available")]
        public IHttpActionResult Available()
        {
            return Ok(DependencyHelper.GetClasses<IServerModule>().Select(x => x.Name.Replace("`1", "")).ToArray());
        }
    }
}
