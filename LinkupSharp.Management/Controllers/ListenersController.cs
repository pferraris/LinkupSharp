using System;
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
                Extension = ExtensionHelper.GetListener(x.GetType()),
                Endpoint = x.Endpoint,
                Certificate = x.Certificate?.Subject
            }).ToArray());
        }

        [HttpGet]
        [Route("available")]
        public IHttpActionResult Available()
        {
            return Ok(ExtensionHelper.Listeners);
        }

        [HttpPost]
        [Route("")]
        public IHttpActionResult Post([FromBody]ListenerDefinition definition)
        {
            try
            {
                if (Management.Server.Listeners.Any(x => x.Endpoint.Equals(definition.Endpoint, StringComparison.InvariantCultureIgnoreCase)))
                    return BadRequest("Endpoint in use yet");
                var extension = ExtensionHelper.GetListener(definition.Type);
                if (extension == null)
                    return BadRequest("Listener type not found");
                var listener = extension.Create();
                listener.Endpoint = definition.Endpoint;
                Management.Server.AddListener(listener);
                return Ok();
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpDelete]
        [Route("")]
        public IHttpActionResult Delete([FromBody]ListenerDefinition definition)
        {
            try
            {
                var listener = Management.Server.Listeners.FirstOrDefault(x => x.Endpoint.Equals(definition.Endpoint, StringComparison.InvariantCultureIgnoreCase));
                if (listener == null)
                    return BadRequest("Endpoint not found");
                Management.Server.RemoveListener(listener);
                return Ok();
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        public class ListenerDefinition
        {
            public string Type { get; set; }
            public string Endpoint { get; set; }
        }
    }
}
