using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace ACMEClientExtension.Controllers.WebHooks
{
    [Route("api/webhooks")]
    public class DailyController : ControllerBase
    {
        [HttpPost("DailyEvent")]
        public async Task<ActionResult> DailyEvent()
        {
            // Custom code to run daily

            return await Task.FromResult(Ok());
        }
    }
}
