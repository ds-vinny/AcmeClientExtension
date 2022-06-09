using ACMEClientExtension.Merchants.Stripe;
using ACMEClientExtension.Merchants.Stripe.Interfaces;
using DirectScale.Disco.Extension.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ACMEClientExtension.Controllers.Merchants
{
    [Route("api/Merchants/[controller]")]
    [ApiController]
    public class StripeController : ControllerBase
    {
        private readonly IStripeService _stripeService;

        public StripeController(IStripeService stripeService)
        {
            _stripeService = stripeService ?? throw new ArgumentNullException(nameof(stripeService));
        }

        [Route("AddCardToCustomer")]
        [HttpPost]
        public async Task<ActionResult> AddCardToCustomer([FromBody] AddCardToCustomerRequest request)
        {
            string token = string.Empty;
            try
            {
                token = await _stripeService.CreateCard(request.PayorId, request.SourceToken);
            }
            catch (Exception)
            {
                // Log error
                throw;
            }

            var response = new AddCardToCustomerResponse()
            {
                Token = new Token()
                {
                    Id = token,
                }
            };

            return Ok(response);
        }

        public class AddCardToCustomerResponse
        {
            [JsonProperty("token")]
            public Token Token { get; set; }
        }

        public class Token
        {
            public string CardType { get; set; }
            public string Id { get; set; }
            public string Last4 { get; set; }
            public int ExpireMonth { get; set; }
            public int ExpireYear { get; set; }
        }

        public class AddCardToCustomerRequest
        {
            public string PayorId { get; set; }
            public string SourceToken { get; set; }
        }
    }
}
