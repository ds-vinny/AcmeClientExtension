using ACMEClientExtension.Merchants.Ewallet.Interfaces;
using DirectScale.Disco.Extension;
using DirectScale.Disco.Extension.MoneyIn.Custom.Models;
using System;
using ACMEClientExtension.Merchants.Ewallet.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using DirectScale.Disco.Extension.Services;

namespace ACMEClientExtension.Merchants.Ewallet
{
    public class EWalletMoneyIn : SinglePaymentMoneyInMerchant
    {
        public const int MerchantId = 9100;

        private readonly IEwalletService _ewalletService;
        private readonly IAssociateService _associateService;
        public EWalletMoneyIn(IEwalletService ewalletService, IAssociateService associateService)
        {
            _ewalletService = ewalletService ?? throw new ArgumentNullException(nameof(ewalletService));
            _associateService = associateService ?? throw new ArgumentNullException(nameof(associateService));
        }
        public async override Task<PaymentResponse> ChargePayment(string payerId, int orderNumber, double amount, Address billingAddress, string currencyCode)
        {
            var paymentResponse = new PaymentResponse()
            {
                Amount = amount,
                Currency = currencyCode,
                ResponseId = Guid.NewGuid().ToString(),
                Response = "Not Provided",
                TransactionNumber = Guid.NewGuid().ToString(),
                Status = PaymentStatus.NotProvided,
                AuthorizationCode = "N/A",
                Merchant = MerchantId
            };

            Models.TransactionStatus result = await _ewalletService.RemoveFromBalance(payerId, (decimal)amount);

            if (result == Models.TransactionStatus.Success)
            {
                paymentResponse.Status = PaymentStatus.Accepted;
                paymentResponse.Response = "Accepted";
            }
            else if (result == Models.TransactionStatus.InsufficientFunds)
            {
                paymentResponse.Status = PaymentStatus.Rejected;
                paymentResponse.Response = "Insufficient Funds";
            }

            return await Task.FromResult(paymentResponse);
        }

        public async override Task<ExtendedPaymentResult> RefundPayment(string payerId, int orderNumber, string currencyCode, double paymentAmount, double refundAmount, string referenceNumber, string transactionNumber, string authorizationCode)
        {
            var paymentResult = new ExtendedPaymentResult()
            {
                Amount = refundAmount,
                Currency = currencyCode,
                ResponseId = Guid.NewGuid().ToString(),
                Response = Guid.NewGuid().ToString(),
                TransactionNumber = transactionNumber,
                Status = PaymentStatus.NotProvided,
                AuthorizationCode = authorizationCode,
            };

            Models.TransactionStatus result = await _ewalletService.AddToBalance(payerId, (decimal)refundAmount);

            if (result == Models.TransactionStatus.Success)
            {
                paymentResult.Status = PaymentStatus.Accepted;
            }

            return await Task.FromResult(paymentResult);
        }

        public async override Task<string> GetNewPayerId(int associateId)
        {
            Associate associate = await _associateService.GetAssociate(associateId);

            return await _ewalletService.ProvisionAccount(associateId, associate.DisplayFirstName, associate.DisplayLastName, associate.EmailAddress);
        }
    }
}
