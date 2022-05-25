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

        public async override Task<int> FormatCurrency(double amount)
        {
            amount = amount * 100;
            return await Task.FromResult((int)amount);
        }
    }
}
