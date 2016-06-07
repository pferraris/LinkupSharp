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
                Type = x.GetType().Name.Replace("`1", "")
            }).ToArray());
        }

        [HttpGet]
        [Route("available")]
        public IHttpActionResult Available()
        {
            return Ok(DependencyHelper.GetClasses<IAuthorizer>().Select(x => x.Name.Replace("`1", "")).ToArray());
        }
    }
}
