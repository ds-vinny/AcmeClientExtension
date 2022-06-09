using ACMEClientExtension.Merchants.Stripe.Interfaces;
using DirectScale.Disco.Extension;
using DirectScale.Disco.Extension.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ACMEClientExtension.Merchants.Stripe
{
    public class StripeMoneyInMerchantJpy : StripeMoneyInMerchant
    {
        public StripeMoneyInMerchantJpy(IAssociateService associateService, IStripeService stripeService) : base(associateService, stripeService)
        {
        }

        public async override Task<int> FormatCurrency(double amount)
        {
            return await Task.FromResult((int)amount);
        }
    }
}
