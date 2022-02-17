﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ACMEClientExtension.Models.CustomPage
{
    public class SecuredHelloWorldViewModel
    {
        public string AssociatesName { get; set; }
        public Dictionary<string, string> QueryStringParameters { get; set; }
        public Dictionary<string, string> Headers { get; set; }
    }
}
