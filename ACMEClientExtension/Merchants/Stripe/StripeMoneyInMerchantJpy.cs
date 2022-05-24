﻿using ACMEClientExtension.Merchants.Stripe.Interfaces;
using DirectScale.Disco.Extension;
using DirectScale.Disco.Extension.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ACMEClientExtension.Merchants.Stripe
{
    public class StripeMoneyInMerchantJpy : StripeMoneyInMerchant
    {
        private string _currencyCode = "JPY";
        public StripeMoneyInMerchantJpy(IAssociateService associateService, IStripeService stripeService) : base(associateService, stripeService)
        {
        }

        public async override Task<int> FormatCurrency(double amount)
        {
            return await Task.FromResult((int)amount);
        }

        public async override Task<ValidationResult> ValidateCurrency(string currencyCode)
        {
            bool isValid = _currencyCode.ToLower() == currencyCode.ToLower();

            var validationResult = new ValidationResult()
            {
                IsValid = isValid,
                ErrorMessage = isValid ? null : $"{currencyCode} Currency is not accepted by this merchant. Please use {_currencyCode} Currency."
            };

            return await Task.FromResult(validationResult);
        }
    }
}
