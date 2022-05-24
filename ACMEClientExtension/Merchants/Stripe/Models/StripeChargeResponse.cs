using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ACMEClientExtension.Merchants.Stripe.Models
{
    public class StripeChargeResponse
    {
        public string TransactionNumber { get; set; }
        public string Status { get; set; }
        public string TransferId { get; set; }
    }
}
