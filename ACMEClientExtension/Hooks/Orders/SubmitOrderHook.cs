using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DirectScale.Disco.Extension;
using DirectScale.Disco.Extension.Hooks;
using DirectScale.Disco.Extension.Hooks.Orders;
using DirectScale.Disco.Extension.Services;

namespace ACMEClientExtension.Hooks.Orders
{
    public class SubmitOrderHook : IHook<SubmitOrderHookRequest, SubmitOrderHookResponse>
    {
        private const int DistributorAssociateType = 1;
        private const int December = 12;

        private readonly IAssociateService _associateService;
        private readonly ICouponService _couponService;

        // In order to use a DirectScale Service it needs to be added to the constructor following the Dependency Injection pattern shown below
        public SubmitOrderHook(IAssociateService associateService, ICouponService couponService)
        {
            _associateService = associateService ?? throw new ArgumentNullException(nameof(associateService));
            _couponService = couponService ?? throw new ArgumentNullException(nameof(couponService));
        }

        // IMPORTANT! A NOTE ABOUT HOOK BEST PRACTICES:
        // This example is to meant only to show a Client Developer how a service could be used in a hook.
        // It is best practice that hooks are only implemented for CHANGES to business logic and
        // NOT ADDITIONS to business logic like the example below.
        // This is because hooks are executed in-process (Synchronously) and add performance overhead to the DirectScale.
        // Although the example implemented below is functional it is not the most performant.
        // The example below would be most performant by using the DirectScale Event System that functions out-of-process (Asynchrously).
        // The Event System can notify the Client Extension that an order was created and the Client Extension could then create a
        // coupon out-of-process instead of adding overhead during the order creation process.
        public async Task<SubmitOrderHookResponse> Invoke(SubmitOrderHookRequest request, Func<SubmitOrderHookRequest, Task<SubmitOrderHookResponse>> func)
        {
            // Execute the DirectScale callback method first
            var result = await func(request);

            // The following code is an example of how a hook can utilize DirectScale Services
            // Both the Associate Service and Coupon Service will be used in this example
            // This code implemented below adds business logic to the DirectScale system
            // by giving any Distributor Type Associate an automatic 10 dollars off
            // their next order during january for every order they placed in December.
            var associate = await _associateService.GetAssociate(request.Order.AssociateId);

            if (associate.AssociateBaseType == DistributorAssociateType && DateTime.Now.Month == December)
            {
                Coupon oneTimeUseAssociateCoupon = new Coupon()
                {
                    BackOfficeIds = new string[] { associate.BackOfficeId },
                    BeginDate = new DateTime(year: DateTime.Now.Year + 1, month: 1, day: 1, hour: 0, minute: 0, second: 0),
                    EndDate = new DateTime(year: DateTime.Now.Year + 1, month: 1, day: 31, hour: 23, minute: 59, second: 59),
                    Recurring = false,
                    CouponType = CouponType.OrderDiscount,
                    AmountType = AmountType.Amount,
                    Discount = 10d,
                };
                
                await _couponService.SaveCoupon(oneTimeUseAssociateCoupon);
            }

            return result;
        }
    }
}
