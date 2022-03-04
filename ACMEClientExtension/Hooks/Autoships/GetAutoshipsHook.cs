using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DirectScale.Disco.Extension.Hooks;
using DirectScale.Disco.Extension.Hooks.Autoships;

namespace ACMEClientExtension.Hooks.Autoships
{
    public class GetAutoshipsHook : IHook<DirectScale.Disco.Extension.Hooks.Autoships.GetAutoshipsHookRequest, GetAutoshipsHookResponse>
    {
        public async Task<GetAutoshipsHookResponse> Invoke(DirectScale.Disco.Extension.Hooks.Autoships.GetAutoshipsHookRequest request, Func<DirectScale.Disco.Extension.Hooks.Autoships.GetAutoshipsHookRequest, Task<GetAutoshipsHookResponse>> func)
        {
            // This is an Example of an After Hook
            // Execute the DirectScale callback method first
            GetAutoshipsHookResponse result = await func(request);

            // Then update the result of the DirectScale Callback method so that the Address is in ALL CAPS
            foreach (var autoship in result.Autoships)
            {
                autoship.ShipAddress.AddressLine1 = autoship.ShipAddress.AddressLine1.ToUpper();
                autoship.ShipAddress.AddressLine2 = autoship.ShipAddress.AddressLine2.ToUpper();
                autoship.ShipAddress.City = autoship.ShipAddress.City.ToUpper();
                autoship.ShipAddress.CountryCode = autoship.ShipAddress.CountryCode.ToUpper();
                autoship.ShipAddress.State = autoship.ShipAddress.State.ToUpper();
            }

            // Return the modified result
            return result;
        }
    }
}
