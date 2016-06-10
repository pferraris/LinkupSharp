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
                Extension = ExtensionHelper.GetModule(x.GetType())
            }).Where(x => x.Extension != null).ToArray());
        }

        [HttpGet]
        [Route("available")]
        public IHttpActionResult Available()
        {
            return Ok(ExtensionHelper.Modules);
        }

        [HttpPost]
        [Route("")]
        public IHttpActionResult Post([FromBody]ModuleDefinition definition)
        {
            try
            {
                if (Management.Server.Modules.Any(x => x.GetType().Name.Equals(definition.Type, StringComparison.InvariantCultureIgnoreCase)))
                    return BadRequest("Module is added yet");
                var extension = ExtensionHelper.GetModule(definition.Type);
                if (extension == null)
                    return BadRequest("Listener type not found");
                var module = extension.Create();
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
        public IHttpActionResult Delete([FromBody]ModuleDefinition definition)
        {
            try
            {
                var module = Management.Server.Modules.FirstOrDefault(x => x.GetType().Name.Equals(definition.Type, StringComparison.InvariantCultureIgnoreCase));
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

        public class ModuleDefinition
        {
            public string Type { get; set; }
        }
    }
}
