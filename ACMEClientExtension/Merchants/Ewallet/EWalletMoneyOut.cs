using ACMEClientExtension.Merchants.Ewallet.Interfaces;
using DirectScale.Disco.Extension;
using DirectScale.Disco.Extension.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ACMEClientExtension.Merchants.Ewallet
{
    public class EWalletMoneyOut : CommissionMerchant
    {
        public const string AccountNumber = "AccountNumber";
        public IEwalletService _ewalletService { get; set; }
        private readonly IAssociateService _associateService;
        public EWalletMoneyOut(IEwalletService ewalletService, IAssociateService associateService)
        {
            _ewalletService = ewalletService ?? throw new ArgumentNullException(nameof(ewalletService));
            _associateService = associateService ?? throw new ArgumentNullException(nameof(associateService));
        }

        public async override Task<CommissionPaymentResult[]> PayCommissions(int batchId, CommissionPayment[] payments)
        {
            var commissionPaymentResults = new CommissionPaymentResult[payments.Length];

            for (int i = 0; i < payments.Length; i++)
            {
                commissionPaymentResults[i] = new CommissionPaymentResult()
                {
                    DatePaid = DateTime.UtcNow,
                    Status = CommissionPaymentStatus.Submitted,
                    PaymentUniqueId = payments[i].PaymentUniqueId,
                };

                Models.TransactionStatus payAssociateResult = Models.TransactionStatus.Failed;
                try
                {
                    payAssociateResult = await PayAssociate(payments[i]);
                }
                catch (Exception e) // do not let external calls stop the loop because some may have processed successfully.
                {
                    commissionPaymentResults[i].Status = CommissionPaymentStatus.Failed;
                    commissionPaymentResults[i].TransactionNumber = "transaction number from external system if applicable";
                    commissionPaymentResults[i].ErrorMessage = e.Message;
                }

                if (payAssociateResult == Models.TransactionStatus.Success)
                {
                    commissionPaymentResults[i].TransactionNumber = "transaction number from external system";
                    commissionPaymentResults[i].Status = CommissionPaymentStatus.Paid;
                }
            }

            return await Task.FromResult(commissionPaymentResults);
        }

        private async Task<Models.TransactionStatus> PayAssociate(CommissionPayment commissionPayment)
        {
            string accountNumber = "";
            bool accountProvisioned = commissionPayment.MerchantCustomFields.TryGetValue(AccountNumber, out accountNumber);

            if (!accountProvisioned) // Accounts are automatically provisioned by the DS System prior calling PayCommissions() if ProvisionAccount() is implemented.
            {
                throw new Exception($"No Account is created for AssociateID: {commissionPayment.AssociateId} for MerchantID: {commissionPayment.MerchantId}");
                // If desired, a call to the MoneyOutService.SetActiveOnFileMerchant() could be made here to try to provision the account again.
            }

            var payAssociate = await _ewalletService.AddToBalance(accountNumber, commissionPayment.Total);

            return await Task.FromResult(payAssociate);
        }

        public async override Task<Dictionary<string, string>> ProvisionAccount(int associateId)
        {
            string accountNumber = await CreateAssociateAccount(associateId);

            var associateCustomValues = new Dictionary<string, string>()
            {
                { AccountNumber, accountNumber }
            };

            return await Task.FromResult(associateCustomValues);
        }

        private async Task<string> CreateAssociateAccount(int associateId)
        {
            Associate associate = await _associateService.GetAssociate(associateId);

            return await _ewalletService.ProvisionAccount(associateId, associate.DisplayFirstName, associate.DisplayLastName, associate.EmailAddress);
        }

    }
}
