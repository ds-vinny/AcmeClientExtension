using DirectScale.Disco.Extension;
using DirectScale.Disco.Extension.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ACMEClientExtension.Merchants
{
    public class MyCommissionMerchant : CommissionMerchant
    {
        private const int _merchantId = 9001;
        private const string _merchantName = "MyCommissionMerchant";


        private const string _accountNumber = "AccountNumber";
        public IMoneyOutService _moneyOutService { get; set; }
        public MyCommissionMerchant(IMoneyOutService moneyOutService)
        {
            _moneyOutService = moneyOutService ?? throw new ArgumentNullException(nameof(moneyOutService));
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

                string payAssociateResult = "";
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

                if (payAssociateResult == "Success")
                {
                    commissionPaymentResults[i].TransactionNumber = "transaction number from external system";
                    commissionPaymentResults[i].Status = CommissionPaymentStatus.Paid;
                }
                if (payAssociateResult == "Pending")
                {
                    commissionPaymentResults[i].TransactionNumber = "transaction number from external system";
                    commissionPaymentResults[i].Status = CommissionPaymentStatus.Pending;
                }
            }

            return await Task.FromResult(commissionPaymentResults);
        }

        private async Task<string> PayAssociate(CommissionPayment commissionPayment)
        {
            string accountNumber = "";
            bool accountProvisioned = commissionPayment.MerchantCustomFields.TryGetValue(_accountNumber, out accountNumber);
            if (!accountProvisioned)
            {
                accountNumber = await ProvisionNewAccount(commissionPayment.AssociateId, commissionPayment.MerchantId, _merchantName);
            }

            var payAssociate = "Success"; // Call out to third party merchant

            return await Task.FromResult(payAssociate);
        }

        public async override Task ProvisionAccount(int associateId)
        {
            await ProvisionNewAccount(associateId, _merchantId, _merchantName);
        }

        private async Task<string> ProvisionNewAccount(int associateId, int merchantId, string merchantName)
        {
            var onFileMerchants = await _moneyOutService.GetOnFileMerchants(associateId);
            bool alreadyProvisioned = onFileMerchants.FirstOrDefault(x => x.MerchantId == merchantId)?.CustomValues?.ContainsKey(_accountNumber) ?? false;

            string accountNumber = "";
            if (!alreadyProvisioned)
            {
                accountNumber = await CreateAssociateAccount(associateId);
                var accountInfo = new OnFileMerchant()
                {
                    AssociateId = associateId,
                    CustomValues = new Dictionary<string, string>()
                    {
                        { _accountNumber, accountNumber }
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
            var accountNumber = Guid.NewGuid().ToString(); // call out to third party merchant API to create and get an Account Number

            return await Task.FromResult(accountNumber);
        }

    }
}
