using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ACMEClientExtension.Models.Merchants.Stripe
{
    public class AddCardFrameViewModel
    {
        public string PayorId { get; set; }
        public string PublicApiKey { get; set; }
        public string BaseUrl { get; set; }
    }
}
