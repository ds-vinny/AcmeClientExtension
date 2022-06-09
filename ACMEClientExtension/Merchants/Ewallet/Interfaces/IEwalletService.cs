using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ACMEClientExtension.Merchants.Ewallet.Interfaces
{
    public interface IEwalletService
    {
        Task<string> ProvisionAccount(int associateId, string firstName, string lastName, string email);
        Task<Models.TransactionStatus> AddToBalance(string payorId, decimal amount);
        Task<Models.TransactionStatus> RemoveFromBalance(string payorId, decimal amount);
    }
}
