using ACMEClientExtension.Merchants.Ewallet.Interfaces;
using DirectScale.Disco.Extension;
using DirectScale.Disco.Extension.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ACMEClientExtension.Merchants.Ewallet
{
    public class EwalletService : IEwalletService
    {
        public IAssociateService _associateService { get; set; }
        public EwalletService(IAssociateService associateService)
        {
            _associateService = associateService ?? throw new ArgumentNullException(nameof(associateService));
        }

        public async Task<Models.TransactionStatus> AddToBalance(string payorId, decimal amount)
        {
            var associateId = await DecodeAssociateIdFromPayorId(payorId);
            Associate associate = await _associateService.GetAssociate(associateId);

            decimal currentBalance;

            if (associate.AssociateCustom == null)
            {
                associate.AssociateCustom = new AssociateCustomFields();
            }
            if (string.IsNullOrWhiteSpace(associate.AssociateCustom.Field2))
            {
                currentBalance = 0m;
            }
            else if (decimal.TryParse(associate.AssociateCustom.Field2, out currentBalance) == false)
            {
                throw new Exception("An error occured because the current balance of the account cannot be determined.");
            }

            var newBalance = currentBalance + amount;
            associate.AssociateCustom.Field2 = newBalance.ToString();

            await _associateService.UpdateAssociate(associate);

            return Models.TransactionStatus.Success;
        }

        public async Task<string> ProvisionAccount(int associateId, string firstName, string lastName, string email)
        {
            Associate associate = await _associateService.GetAssociate(associateId);

            if (associate.AssociateCustom == null)
            {
                associate.AssociateCustom = new AssociateCustomFields();
            }
            if (associate.AssociateCustom.Field1 == null)
            {
                associate.AssociateCustom.Field1 = await EncodeAssociateId(associateId);

                await _associateService.UpdateAssociate(associate);
            }

            return associate.AssociateCustom.Field1;
        }

        private async Task<string> EncodeAssociateId(int associateId)
        {
            return await Task.FromResult($"wal_{associateId + 12391893}");
        }

        private async Task<int> DecodeAssociateIdFromPayorId(string payorId)
        {
            if (int.TryParse(payorId.Substring(4), out int encodedAssociateId))
            {
                return await Task.FromResult(encodedAssociateId - 12391893);
            }

            throw new Exception("An Error Occured");
        }

        public async Task<Models.TransactionStatus> RemoveFromBalance(string payorId, decimal amount)
        {
            var associateId = await DecodeAssociateIdFromPayorId(payorId);
            Associate associate = await _associateService.GetAssociate(associateId);

            decimal currentBalance;

            if (associate.AssociateCustom.Field2 == null)
            {
                currentBalance = 0m;
            }
            else if (decimal.TryParse(associate.AssociateCustom.Field2, out currentBalance) == false)
            {
                throw new Exception("An error occured because the current balance of the account cannot be determined.");
            }

            if (Math.Round(amount, 2) > Math.Round(currentBalance, 2))
            {
                return Models.TransactionStatus.InsufficientFunds;
            }

            var newBalance = currentBalance - amount;
            associate.AssociateCustom.Field2 = newBalance.ToString();

            await _associateService.UpdateAssociate(associate);

            return Models.TransactionStatus.Success;
        }
    }
}
