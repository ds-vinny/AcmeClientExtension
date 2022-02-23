using DirectScale.Disco.Extension.Hooks.Autoships;
using DirectScale.Disco.Extension.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ACMEClientExtension.Controllers.Hooks
{
    [ExtensionAuthorize]
    [Route("api/hooks/[controller]")]
    [ApiController]
    public class AutoshipHooksController : ControllerBase
    {
        private readonly IHookCallback _hookCallback;
        public AutoshipHooksController(IHookCallback hookCallback)
        {
            _hookCallback = hookCallback ?? throw new ArgumentNullException(nameof(hookCallback));
        }

        public async Task<ActionResult<CreateAutoshipHookResponse>> CreateAutoshipHook([FromBody] CreateAutoshipHookRequest request, [FromHeader] string callbackToken)
        {
            // This is an Example of a Before Hook
            // The following lines add some text to the custom field of an autoship when it is created.
            request.AutoshipInfo.Custom.Field1 = "This message was added from the CreateAutoshipHook";

            // Execute the DirectScale callback method with the modified 'request' object that was passed into the controller
            CreateAutoshipHookResponse response = await _hookCallback.CallBack<CreateAutoshipHookRequest, CreateAutoshipHookResponse>(callbackToken, request);

            return Ok(response);
        }
    }
}
