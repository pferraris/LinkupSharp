using LinkupSharp.Security.Authentication;
using System.Linq;
using System.Web.Http;

namespace LinkupSharp.Management.Controllers
{
    [RoutePrefix("authenticators")]
    public class AuthenticatorsController : ApiControllerBase
    {
        [HttpGet]
        [Route("")]
        public IHttpActionResult Get()
        {
            return Ok(Management.Server.Authenticators.Select(x => new
            {
                Type = x.GetType().Name.Replace("`1", "")
            }).ToArray());
        }

        [HttpGet]
        [Route("available")]
        public IHttpActionResult Available()
        {
            return Ok(DependencyHelper.GetClasses<IAuthenticator>().Select(x => x.Name.Replace("`1", "")).ToArray());
        }
    }
}
