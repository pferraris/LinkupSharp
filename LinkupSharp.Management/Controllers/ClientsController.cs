using System.Linq;
using System.Web.Http;

namespace LinkupSharp.Management.Controllers
{
    [RoutePrefix("clients")]
    public class ClientsController : ApiControllerBase
    {
        [HttpGet]
        [Route("")]
        public IHttpActionResult Get()
        {
            return Ok(Management.Server.Clients.Select(x => new
            {
                Session = x.Session,
                Channel = new
                {
                    Type = x.Channel.GetType().Name,
                    Endpoint = x.Channel.Endpoint,
                    Certificate = x.Channel.Certificate?.Subject
                }
            }).ToArray());
        }

        [HttpGet]
        [Route("anonymous")]
        public IHttpActionResult Anonymous()
        {
            return Ok(Management.Server.Anonymous.Select(x => new
            {
                Channel = new
                {
                    Type = x.Channel.GetType().Name,
                    Endpoint = x.Channel.Endpoint,
                    Certificate = x.Channel.Certificate?.Subject
                }
            }).ToArray());
        }

        [HttpPost]
        [Route("{id}")]
        public IHttpActionResult Send(string id, [FromBody]Packet content)
        {
            var client = Management.Server.Clients.FirstOrDefault(x => x.Id.Equals((Id)id));
            if (client == null)
                client = Management.Server.Anonymous.FirstOrDefault(x => x.Channel.Endpoint.Equals(id));
            if (client != null)
            {
                client.Send(content);
                return Ok();
            }
            return NotFound();
        }

        [HttpDelete]
        [Route("{id}")]
        public IHttpActionResult Disconnect(string id)
        {
            var client = Management.Server.Clients.FirstOrDefault(x => x.Id.Equals((Id)id));
            if (client == null)
                client = Management.Server.Anonymous.FirstOrDefault(x => x.Channel.Endpoint.Equals(id));
            if (client != null)
            {
                client.Disconnect();
                return Ok();
            }
            return NotFound();
        }
    }
}
