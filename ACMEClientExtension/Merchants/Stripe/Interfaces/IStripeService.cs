using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ACMEClientExtension.Merchants.Stripe.Models;
using Stripe;

namespace ACMEClientExtension.Merchants.Stripe.Interfaces
{
    public interface IStripeService
    {
        Task<string> CreateCard(string payorId, string sourceToken);
        Task<StripeChargeResponse> ChargeCard(StripeMoneyInMerchant merchant, string payorId, double amount, string currencyCode, string cardToken, string description);
        Task<string> CreateCustomer(int associateId, string email, string name);
        Task DeleteCard(string payorId, string cardToken);
        Task<string> Refund(StripeMoneyInMerchant merchant, string chargeToken, double amount);
        Task<StripeList<Card>> GetCards(string payorId);
        Task<Customer> GetCustomer(string payorId);
    }
}
