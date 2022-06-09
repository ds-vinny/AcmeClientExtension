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
        private const string _accountNumber = "AccountNumber";

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
                catch (Exception e) // do not let external calls stop the loop because some may have processed successfully.
                {
                    commissionPaymentResults[i].Status = CommissionPaymentStatus.Failed;
                    commissionPaymentResults[i].TransactionNumber = "transaction number from external system if applicable";
                    commissionPaymentResults[i].ErrorMessage = e.Message;
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

            if (!accountProvisioned) // Accounts are automatically provisioned by the DS System prior calling PayCommissions() if ProvisionAccount() is implemented.
            {
                throw new Exception($"No Account is created for AssociateID: {commissionPayment.AssociateId} for MerchantID: {commissionPayment.MerchantId}");
                // If desired, a call to the MoneyOutService.SetActiveOnFileMerchant() could be made here to try to provision the account again.
            }

            var payAssociate = "Success"; // Call out to third party merchant

            return await Task.FromResult(payAssociate);
        }

        public async override Task<Dictionary<string, string>> ProvisionAccount(int associateId)
        {          
            string accountNumber = await CreateAssociateAccount(associateId);
            var associateCustomValues = new Dictionary<string, string>()
            {
                { _accountNumber, accountNumber }
            };

            return await Task.FromResult(associateCustomValues);
        }

        private async Task<string> CreateAssociateAccount(int associateId)
        {
            var accountNumber = Guid.NewGuid().ToString(); // call out to third party merchant API to create and get an Account Number

            return await Task.FromResult(accountNumber);
        }

    }
}
