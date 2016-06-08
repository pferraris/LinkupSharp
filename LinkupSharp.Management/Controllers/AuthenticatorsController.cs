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
                Extension = ExtensionHelper.GetAuthenticator(x.GetType())
            }).ToArray());
        }

        [HttpGet]
        [Route("available")]
        public IHttpActionResult Available()
        {
            return Ok(ExtensionHelper.Authenticators);
        }
    }
}
