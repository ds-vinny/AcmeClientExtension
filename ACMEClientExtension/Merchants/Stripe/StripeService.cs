using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ACMEClientExtension.Merchants.Stripe.Interfaces;
using ACMEClientExtension.Merchants.Stripe.Models;
using Stripe;

namespace ACMEClientExtension.Merchants.Stripe
{
    public class StripeService : IStripeService
    {
        private readonly IStripeSettings _stripeSettings;

        public StripeService(IStripeSettings stripeSettings)
        {
            _stripeSettings = stripeSettings ?? throw new ArgumentNullException(nameof(stripeSettings));
        }

        public async Task<string> CreateCard(string payorId, string sourceToken)
        {
            var cardCreateOptions = new CardCreateOptions
            {
                Source = sourceToken
            };

            Card card;
            try
            {
                var cardService = new CardService();
                card = cardService.Create(payorId, cardCreateOptions, new RequestOptions
                {
                    ApiKey = await _stripeSettings.SecretApiKey
                });
            }
            catch (Exception ex)
            {
                //LogError();
                throw;
            }

            return await Task.FromResult(card.Id);
        }

        public async Task<StripeList<Card>> GetCards(string payorId)
        {
            StripeList<Card> cards = new StripeList<Card>();
            try
            {
                var cardService = new CardService();
                cards = cardService.List(customerId: payorId, requestOptions: new RequestOptions
                {
                    ApiKey = await _stripeSettings.SecretApiKey
                });
            }
            catch (Exception ex)
            {
                //LogError();
                throw;
            }

            return await Task.FromResult(cards);
        }

        public async Task<string> CreateCustomer(int associateId, string email, string name)
        {
            var customerCreateOptions = new CustomerCreateOptions
            {
                Description = name + $" (Associate ID: {associateId})",
                Email = email,
                Name = name
            };

            Customer customer;
            try
            {
                var customerService = new CustomerService();
                customer = customerService.Create(customerCreateOptions, new RequestOptions
                {
                    ApiKey =  await _stripeSettings.SecretApiKey
                });
            }
            catch (Exception ex)
            {
                //LogError();
                throw;
            }

            return customer.Id;
        }

        public async Task<Customer> GetCustomer(string payorId)
        {
            Customer customer;
            try
            {
                var customerService = new CustomerService();
                customer = customerService.Get(payorId, requestOptions: new RequestOptions
                {
                    ApiKey =  await _stripeSettings.SecretApiKey
                });
            }
            catch (Exception ex)
            {
                //LogError();
                throw;
            }

            return customer;
        }

        public async Task DeleteCard(string payorId, string cardToken)
        {
            try
            {
                var cardService = new CardService();
                cardService.Delete(payorId, cardToken, new RequestOptions
                {
                    ApiKey = await _stripeSettings.SecretApiKey
                });
            }
            catch (Exception ex)
            {
                //LogError();
                throw;
            }
        }

        public async Task<StripeChargeResponse> ChargeCard(StripeMoneyInMerchant merchant, string payorId, double amount, string currencyCode, string cardToken, string description)
        {
            var chargeCreateOptions = new ChargeCreateOptions
            {
                Amount = await merchant.FormatCurrency(amount),
                Capture = true,
                Currency = currencyCode,
                Customer = payorId,
                Description = description,
                Source = cardToken
            };

            Charge charge;
            try
            {
                var chargeService = new ChargeService();
                charge = chargeService.Create(chargeCreateOptions, new RequestOptions
                {
                    ApiKey = await _stripeSettings.SecretApiKey
                });
            }
            catch (Exception ex)
            {
                //LogError();
                throw;
            }

            return await Task.FromResult(new StripeChargeResponse
            {
                TransactionNumber = charge.Id,
                Status = charge.Status,
                TransferId = charge.TransferId ?? string.Empty
            });
        }

        public async Task<string> Refund(StripeMoneyInMerchant merchant, string chargeToken, double amount)
        {
            var refundCreateOptions = new RefundCreateOptions
            {
                Amount = await merchant.FormatCurrency(amount),
                Charge = chargeToken,
                Reason = RefundReasons.RequestedByCustomer
            };

            Refund refund;
            try
            {
                var refundService = new RefundService();
                refund = refundService.Create(refundCreateOptions, new RequestOptions
                {
                    ApiKey = await _stripeSettings.SecretApiKey
                });
            }
            catch (Exception ex)
            {
                //LogError();
                throw;
            }

            return refund.Id;
        }
    }
}
