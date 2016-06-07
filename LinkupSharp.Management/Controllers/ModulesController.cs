using LinkupSharp.Modules;
using System;
using System.Linq;
using System.Web.Http;

namespace LinkupSharp.Management.Controllers
{
    [RoutePrefix("modules")]
    public class ModulesController : ApiControllerBase
    {
        [HttpGet]
        [Route("")]
        public IHttpActionResult Get()
        {
            return Ok(Management.Server.Modules.Select(x => new
            {
                Type = x.GetType().Name.Replace("`1", "")
            }).ToArray());
        }

        [HttpGet]
        [Route("available")]
        public IHttpActionResult Available()
        {
            return Ok(DependencyHelper.GetClasses<IServerModule>().Select(x => x.Name.Replace("`1", "")).ToArray());
        }

        [HttpPost]
        [Route("")]
        public IHttpActionResult Post([FromBody]string typeName)
        {
            try
            {
                if (Management.Server.Modules.Any(x => x.GetType().Name.Equals(typeName, StringComparison.InvariantCultureIgnoreCase)))
                    return BadRequest("Module is added yet");
                var type = DependencyHelper.GetClasses<IServerModule>().FirstOrDefault(x => x.Name.Equals(typeName, StringComparison.InvariantCultureIgnoreCase));
                if (type == null)
                    return BadRequest("Listener type not found");
                var module = Activator.CreateInstance(type) as IServerModule;
                Management.Server.AddModule(module);
                return Ok();
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpDelete]
        [Route("")]
        public IHttpActionResult Delete([FromBody]string typeName)
        {
            try
            {
                var module = Management.Server.Modules.FirstOrDefault(x => x.GetType().Name.Equals(typeName, StringComparison.InvariantCultureIgnoreCase));
                if (module == null)
                    return BadRequest("Module not found");
                Management.Server.RemoveModule(module);
                return Ok();
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}
