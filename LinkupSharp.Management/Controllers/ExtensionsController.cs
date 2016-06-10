using System.Web.Http;

namespace LinkupSharp.Management.Controllers
{
    [RoutePrefix("extensions")]
    public class ExtensionsController : ApiControllerBase
    {
        [HttpGet]
        [Route("")]
        public IHttpActionResult Get()
        {
            return Ok();
        }

        [HttpGet]
        [Route("")]
        public IHttpActionResult Post()
        {
            return Ok();
        }

        [HttpGet]
        [Route("")]
        public IHttpActionResult Put()
        {
            return Ok();
        }

        [HttpGet]
        [Route("")]
        public IHttpActionResult Delete()
        {
            return Ok();
        }
    }
}
