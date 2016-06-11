using System;
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

        [HttpPost]
        [Route("")]
        public IHttpActionResult Post([FromBody]AuthenticatorDefinition definition)
        {
            try
            {
                if (Management.Server.Authenticators.Any(x => x.TypeEquals(definition.Type)))
                    return BadRequest("Authenticator is added yet");
                var extension = ExtensionHelper.GetAuthenticator(definition.Type);
                if (extension == null)
                    return BadRequest("Authenticator type not found");
                var authenticator = extension.Create();
                Management.Server.AddAuthenticator(authenticator);
                return Ok();
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpDelete]
        [Route("")]
        public IHttpActionResult Delete([FromBody]AuthenticatorDefinition definition)
        {
            try
            {
                var authenticator = Management.Server.Authenticators.FirstOrDefault(x => x.TypeEquals(definition.Type));
                if (authenticator == null)
                    return BadRequest("Authenticator not found");
                Management.Server.RemoveAuthenticator(authenticator);
                return Ok();
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        public class AuthenticatorDefinition
        {
            public string Type { get; set; }
        }
    }
}
