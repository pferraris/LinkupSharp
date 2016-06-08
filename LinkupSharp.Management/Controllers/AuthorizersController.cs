using LinkupSharp.Security.Authorization;
using System.Linq;
using System.Web.Http;

namespace LinkupSharp.Management.Controllers
{
    [RoutePrefix("authorizers")]
    public class AuthorizersController : ApiControllerBase
    {
        [HttpGet]
        [Route("")]
        public IHttpActionResult Get()
        {
            return Ok(Management.Server.Authorizers.Select(x => new
            {
                Extension = ExtensionHelper.GetAuthorizer(x.GetType())
            }).ToArray());
        }

        [HttpGet]
        [Route("available")]
        public IHttpActionResult Available()
        {
            return Ok(ExtensionHelper.Authorizers);
        }
    }
}
