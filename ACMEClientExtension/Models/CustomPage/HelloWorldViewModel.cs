using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IntegrationClientExtension.Models.CustomPage
{
    public class HelloWorldViewModel
    {
        public string PersonsName { get; set; }
        public Dictionary<string, string> QueryStringParameters { get; set; }
        public Dictionary<string, string> Headers { get; set; }
    }
}
