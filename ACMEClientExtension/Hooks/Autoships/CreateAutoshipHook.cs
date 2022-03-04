using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DirectScale.Disco.Extension.Hooks;
using DirectScale.Disco.Extension.Hooks.Autoships;

namespace ACMEClientExtension.Hooks.Autoships
{
    public class CreateAutoshipHook : IHook<CreateAutoshipHookRequest, CreateAutoshipHookResponse>
    {
        public async Task<CreateAutoshipHookResponse> Invoke(CreateAutoshipHookRequest request, Func<CreateAutoshipHookRequest, Task<CreateAutoshipHookResponse>> func)
        {
            // This is an Example of a Before Hook
            // The following lines adds some text to the custom field of an autoship when it is created.
            request.AutoshipInfo.Custom.Field1 = "This message was added from the CreateAutoshipHook";

            // Execute the DirectScale callback function with the modified 'request' object that was passed into the method
            return await func(request);
        }
    }
}
