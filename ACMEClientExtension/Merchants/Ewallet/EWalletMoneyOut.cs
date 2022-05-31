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
        public const int MerchantId = 9101;
        private const string _merchantName = "MyCommissionMerchant";


        public const string AccountNumber = "AccountNumber";
        public IMoneyOutService _moneyOutService { get; set; }
        public IEwalletService _ewalletService { get; set; }
        private readonly IAssociateService _associateService;
        public EWalletMoneyOut(IMoneyOutService moneyOutService, IEwalletService ewalletService, IAssociateService associateService)
        {
            _moneyOutService = moneyOutService ?? throw new ArgumentNullException(nameof(moneyOutService));
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
                catch (Exception) // do not let external calls stop the loop because some may have processed successfully.
                {
                    commissionPaymentResults[i].Status = CommissionPaymentStatus.Failed;
                    commissionPaymentResults[i].TransactionNumber = "transaction number from external system if applicable";
                    commissionPaymentResults[i].ErrorMessage = "Error message from external payment vendor";
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
            if (!accountProvisioned)
            {
                accountNumber = await ProvisionNewAccount(commissionPayment.AssociateId, commissionPayment.MerchantId, _merchantName);
            }

            var payAssociate = await _ewalletService.AddToBalance(accountNumber, commissionPayment.Total);

            return await Task.FromResult(payAssociate);
        }

        public async override Task ProvisionAccount(int associateId)
        {
            await ProvisionNewAccount(associateId, MerchantId, _merchantName);
        }

        private async Task<string> ProvisionNewAccount(int associateId, int merchantId, string merchantName)
        {
            var onFileMerchants = await _moneyOutService.GetOnFileMerchants(associateId);
            bool alreadyProvisioned = onFileMerchants.FirstOrDefault(x => x.MerchantId == merchantId)?.CustomValues?.ContainsKey(AccountNumber) ?? false;

            string accountNumber = "";
            if (!alreadyProvisioned)
            {
                accountNumber = await CreateAssociateAccount(associateId);
                var accountInfo = new OnFileMerchant()
                {
                    AssociateId = associateId,
                    CustomValues = new Dictionary<string, string>()
                    {
                        { AccountNumber, accountNumber }
                    },
                    MerchantId = merchantId,
                    MerchantName = merchantName
                };

                await _moneyOutService.SetActiveOnFileMerchant(accountInfo);
            }

            return await Task.FromResult(accountNumber);
        }

        private async Task<string> CreateAssociateAccount(int associateId)
        {
            Associate associate = await _associateService.GetAssociate(associateId);

            return await _ewalletService.ProvisionAccount(associateId, associate.DisplayFirstName, associate.DisplayLastName, associate.EmailAddress);
        }

    }
}
