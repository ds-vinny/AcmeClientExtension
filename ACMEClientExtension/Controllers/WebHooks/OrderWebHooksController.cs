using DirectScale.Disco.Extension;
using DirectScale.Disco.Extension.EventModels;
using DirectScale.Disco.Extension.Middleware;
using DirectScale.Disco.Extension.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ACMEClientExtension.Controllers.WebHooks
{
    [ExtensionAuthorize]
    [Route("api/webhooks/Order")]
    [ApiController]
    public class OrderWebHooksController : ControllerBase
    {
        private const int DistributorAssociateType = 1;
        private const int December = 12;

        private readonly IAssociateService _associateService;
        private readonly ICouponService _couponService;

        // In order to use a DirectScale Service it needs to be added to the constructor following the Dependency Injection pattern shown below
        public OrderWebHooksController(IAssociateService associateService, ICouponService couponService)
        {
            _associateService = associateService ?? throw new ArgumentNullException(nameof(associateService));
            _couponService = couponService ?? throw new ArgumentNullException(nameof(couponService));
        }

        [HttpPost("CreateOrder")]
        public async Task<ActionResult> CreateOrderWebHook([FromBody] CreateOrderEvent request)
        {
            // *******|  WARNING READ BEST PRACTICES!!!  |*******
            // ALTHOUGH THIS CODE SEEMS FUNCTIONAL IT DOES NOT ACCOUNT FOR 
            // - DUPLICATE EVENTS
            //   or
            // - RETURN A 200 QUICKLY BEFORE DOING PROCESSING. 
            //   (IF A CALL TIMES OUT THEN IT WILL BE SENT AGAIN)

            // The following code is an example of how a Webhook can utilize DirectScale Services
            // Both the Associate Service and Coupon Service will be used in this example
            // This code implemented below adds business logic to the DirectScale system
            // by giving any Distributor Type Associate an automatic 10 dollars off
            // their next order during january for every order they placed in December.

            var associate = await _associateService.GetAssociate(request.CustomerId);

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

            return Ok();
        }
    }
}
