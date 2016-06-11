using LinkupSharp.Security.Authorization;
using System;
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

        [HttpPost]
        [Route("")]
        public IHttpActionResult Post([FromBody]AuthorizerDefinition definition)
        {
            try
            {
                if (Management.Server.Authorizers.Any(x => x.TypeEquals(definition.Type)))
                    return BadRequest("Authorizer is added yet");
                var extension = ExtensionHelper.GetAuthorizer(definition.Type);
                if (extension == null)
                    return BadRequest("Authorizer type not found");
                var authorizer = extension.Create();
                Management.Server.AddAuthorizer(authorizer);
                return Ok();
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpDelete]
        [Route("")]
        public IHttpActionResult Delete([FromBody]AuthorizerDefinition definition)
        {
            try
            {
                var authorizer = Management.Server.Authorizers.FirstOrDefault(x => x.TypeEquals(definition.Type));
                if (authorizer == null)
                    return BadRequest("Authorizer not found");
                Management.Server.RemoveAuthorizer(authorizer);
                return Ok();
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        public class AuthorizerDefinition
        {
            public string Type { get; set; }
        }
    }
}
