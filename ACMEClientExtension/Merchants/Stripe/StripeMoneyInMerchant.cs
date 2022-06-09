using ACMEClientExtension.Merchants.Stripe.Interfaces;
using DirectScale.Disco.Extension;
using DirectScale.Disco.Extension.MoneyIn.Custom.Models;
using DirectScale.Disco.Extension.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ACMEClientExtension.Merchants.Stripe
{
    public class StripeMoneyInMerchant : SavedPaymentMoneyInMerchant
    {
        private readonly IAssociateService _associateRetrievalService;
        private readonly IStripeService _stripeService;

        public StripeMoneyInMerchant(IAssociateService associateService, IStripeService stripeService)
        {
            _associateRetrievalService = associateService ?? throw new ArgumentNullException(nameof(associateService));
            _stripeService = stripeService ?? throw new ArgumentNullException(nameof(stripeService));
        }

        public async override Task<Payment[]> GetExternalPayments(string payerId)
        {
            var customer = await _stripeService.GetCustomer(payerId);
            var cards = await _stripeService.GetCards(payerId);
            var payments = new List<Payment>(cards.Count());

            foreach (var card in cards)
            {
                var payment = new Payment()
                {
                    Last4 = card.Last4, 
                    BillingInfo = new PaymentMethodBillingInfo()
                    {
                        Address = new Address()
                        {
                            AddressLine1 = card.AddressLine1,
                            AddressLine2 = card.AddressLine2,
                            City = card.AddressCity,
                            CountryCode = card.AddressCountry,
                            PostalCode = card.AddressZip,
                            State = card.AddressState
                        },
                        EmailAddress = customer.Email,
                        FullName = customer.Name,
                        PhoneNumber = customer.Phone
                    },
                    CanDelete = true,
                    CardType = card.Brand,
                    ExpireMonth = (int)card.ExpMonth,
                    ExpireYear = (int)card.ExpYear,
                    ProfileType = OnFileProfile.Unknown,
                    Token = card.Id
                };
                payments.Add(payment);
            }

            return await Task.FromResult(payments.ToArray());
        }

        /// <summary>
        /// An override to make it possible to return the lowest-common-denominator of the currency in question for Stripe processing 
        /// </summary>
        /// <param name="amount">The amount passed in from the order payment</param>
        /// <returns>The amount passed in by default, or an overridden amount that is the amount formatted to how stripe expects.</returns>
        /// <remarks>Stripe expects currency amounts as an integer instead of a decimal. For example $12.25 USD must be sent as the integer 1225</remarks>
        public async virtual Task<int> FormatCurrency(double amount)
        {
            return await Task.FromResult((int)amount);
        }

        public async override Task<AddPaymentFrameData> GetSavePaymentFrame(string payorId, int? associateId, string languageCode, string countryCode, Region region)
        {
            var addPay = new AddPaymentFrameData
            {
                IFrameWidth = 500,
                IFrameHeight = 530,
                IFrameURL = $"{Environment.GetEnvironmentVariable("ExtensionBaseURL")}/Merchants/AddCardFrame/Stripe?payorId={payorId}"
            };

            return await Task.FromResult(addPay);
        }

        public async override Task<ExtendedPaymentResult> ChargePayment(string payerId, Payment payment, double amount, string currencyCode, int orderNumber)
        {
            var chargeResult = await _stripeService.ChargeCard(this, payerId, amount, currencyCode, payment.Token, "");

            var paymentResult = new ExtendedPaymentResult()
            {
                Amount = amount,
                Currency = currencyCode,
                ResponseId = chargeResult.TransferId,
                Response = chargeResult.TransferId,
                TransactionNumber = chargeResult.TransactionNumber,
                Status = PaymentStatus.Rejected, 
                AuthorizationCode = "N/A",
            };


            if (chargeResult.Status == "succeeded")
            {
                paymentResult.Status = PaymentStatus.Accepted;
            }
            else
            {
                throw new Exception("Failed to charge Credit Card");
            }

            return await Task.FromResult(paymentResult);
        }

        public async override Task<ExtendedPaymentResult> RefundPayment(string payerId, int orderNumber, string currencyCode, double paymentAmount, double refundAmount, string referenceNumber, string transactionNumber, string authorizationCode)
        {
            var refundId = await _stripeService.Refund(this, transactionNumber, refundAmount);
            return await Task.FromResult(new ExtendedPaymentResult
            {
                Amount = refundAmount * -1.0,
                AuthorizationCode = authorizationCode,
                Currency = currencyCode,
                ResponseId = refundId,
                Response = refundId,
                TransactionNumber = refundId,
                Status = PaymentStatus.Accepted
            });
        }

        public async Task<string> AddCardToCustomer(string payorId, string sourceToken)
        {
            try
            {
                return await _stripeService.CreateCard(payorId, sourceToken);
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to Save Card to Customer '{payorId}': {ex.Message}");
            }
        }

        public async override Task<string> GetNewPayerId(int associateId)
        {
            var associate = await _associateRetrievalService.GetAssociate(associateId);

            if (associate == null)
            {
                throw new ArgumentOutOfRangeException($"AssociateID: {associateId} does not exist and a PayerId cannot be created with Stripe.");
            }

            return await _stripeService.CreateCustomer(associateId, associate?.EmailAddress, associate?.Name);
        }

        public async override Task DeletePayment(string payerId, string paymentMethodId)
        {
            await _stripeService.DeleteCard(payerId, paymentMethodId);
        }
    }
}
