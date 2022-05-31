using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ACMEClientExtension.Merchants.Ewallet.Models
{
    public enum TransactionStatus
    {
        Success,
        InsufficientFunds,
        Failed
    }
}
