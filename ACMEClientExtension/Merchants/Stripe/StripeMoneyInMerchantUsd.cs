using ACMEClientExtension.Merchants.Stripe.Interfaces;
using DirectScale.Disco.Extension;
using DirectScale.Disco.Extension.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ACMEClientExtension.Merchants.Stripe
{
    public class StripeMoneyInMerchantUsd : StripeMoneyInMerchant
    {
        public StripeMoneyInMerchantUsd(IAssociateService associateService, IStripeService stripeService) : base(associateService, stripeService)
        {
        }

        /// <summary>
        /// Formats currency into the representation stripe expects.
        /// </summary>
        /// <param name="amount">The amount passed in from the order payment</param>
        /// <remarks>Stripe expects currency amounts as an integer instead of a decimal. For example $12.25 USD must be sent as the integer 1225</remarks>
        public async override Task<int> FormatCurrency(double amount)
        {
            amount = amount * 100;
            return await Task.FromResult((int)amount);
        }
    }
}
