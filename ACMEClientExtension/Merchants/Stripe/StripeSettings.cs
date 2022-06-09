using ACMEClientExtension.Merchants.Stripe.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ACMEClientExtension.Merchants.Stripe
{
    public class StripeSettings : IStripeSettings
    {
        public Task<string> PublicApiKey
        {
            get
            {
                var key = Environment.GetEnvironmentVariable("StripeExtensionMerchant_PublicKey");
                return Task.FromResult(key);
            }
        }

        public Task<string> SecretApiKey
        {
            get
            {
                var key = Environment.GetEnvironmentVariable("StripeExtensionMerchant_SecretKey");
                return Task.FromResult(key);
            }
        }
    }
}
