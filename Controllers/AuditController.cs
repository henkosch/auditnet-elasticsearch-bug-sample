using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Sample.Controllers
{
    [ApiController]
    [Route("api/audit")] 
    public class AuditController : ControllerBase
    {
        [HttpPost("test1")]
        public async Task<ActionResult> Test1Audit([FromBody]string input)
        {
            await Task.Delay(10);

            return Ok(new {
                Success = true,
                Input = input
            });
        }

        [HttpPost("test2")]
        public async Task<ActionResult> Test2Audit([FromBody]Param input)
        {
            await Task.Delay(10);

            return Ok("asd");
        }
    }

    public class Param
    {
        public int A { get; set; }
        public bool B { get; set; }
    }
}
