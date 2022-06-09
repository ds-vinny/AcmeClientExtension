using ACMEClientExtension.Merchants.Stripe.Interfaces;
using ACMEClientExtension.Models.Merchants.Stripe;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ACMEClientExtension.Controllers.Merchants
{
    public class AddCardFrame : Controller
    {
        private readonly IStripeSettings _stripeSettings;
        public AddCardFrame(IStripeSettings stripeSettings)
        {
            _stripeSettings = stripeSettings ?? throw new ArgumentNullException(nameof(stripeSettings));
        }
        [Route("Merchants/AddCardFrame/Stripe")]
        public async Task<IActionResult> Stripe(string payorId)
        {
            var addCardFrameStripe = new AddCardFrameViewModel
            {
                PayorId = payorId,
                PublicApiKey = await _stripeSettings.PublicApiKey,
                BaseUrl = Environment.GetEnvironmentVariable("ExtensionBaseURL")
            };


            return View("../Merchants/Stripe/AddCardFrame", addCardFrameStripe);
        }
    }
}
