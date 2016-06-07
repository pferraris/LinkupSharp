using LinkupSharp.Channels;
using LinkupSharp.Serializers;
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

        [HttpPost]
        [Route("")]
        public IHttpActionResult Post([FromBody]ListenerDefinition definition)
        {
            try
            {
                if (Management.Server.Listeners.Any(x => x.Endpoint.Equals(definition.Endpoint, StringComparison.InvariantCultureIgnoreCase)))
                    return BadRequest("Endpoint in use yet");
                var type = DependencyHelper.GetClasses<IChannelListener>().FirstOrDefault(x => x.Name.Replace("`1", "").Equals(definition.Type, StringComparison.InvariantCultureIgnoreCase));
                if (type == null)
                    return BadRequest("Listener type not found");
                var genericType = type.MakeGenericType(typeof(JsonPacketSerializer));
                var listener = Activator.CreateInstance(genericType) as IChannelListener;
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
