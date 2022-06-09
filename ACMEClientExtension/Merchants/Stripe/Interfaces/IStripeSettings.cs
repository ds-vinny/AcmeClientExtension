using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ACMEClientExtension.Merchants.Stripe.Interfaces
{
    public interface IStripeSettings
    {
        Task<string> PublicApiKey { get; }
        Task<string> SecretApiKey { get; }
    }
}
