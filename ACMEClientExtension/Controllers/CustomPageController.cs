using ACMEClientExtension.Models;
using ACMEClientExtension.Models.CustomPage;
using DirectScale.Disco.Extension.Middleware;
using DirectScale.Disco.Extension.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace ACMEClientExtension.Controllers
{
    public class CustomPageController : Controller
    {
        private readonly ILogger<CustomPageController> _logger;
        private readonly IAssociateService _associateService;

        public CustomPageController(ILogger<CustomPageController> logger, IAssociateService associateService)
        {
            _logger = logger;
            _associateService = associateService ?? throw new ArgumentNullException(nameof(associateService));
        }

        public IActionResult HelloWorld([FromQuery]string personsName)
        {
            var model = new HelloWorldViewModel();
            model.PersonsName = personsName;
            var queryStringKvps = new Dictionary<string, string>();
            var headersKvps = new Dictionary<string, string>();

            foreach (var queryKvp in HttpContext.Request.Query.Keys)
            {
                queryStringKvps.Add(queryKvp, HttpContext.Request.Query[queryKvp]);
            }
            model.QueryStringParameters = queryStringKvps;

            foreach (var headerKvp in HttpContext.Request.Headers)
            {
                headersKvps.Add(headerKvp.Key, headerKvp.Value.ToString());
            }
            model.Headers = headersKvps;

            return View(model);
        }

        // Purposely Not including an Auth Attribute yet.
        public async Task<IActionResult> SecuredHelloWorld([FromQuery] int associateId)
        {
            var model = new SecuredHelloWorldViewModel();

            // Use Directscale Service to get associate information
            var associate = await _associateService.GetAssociate(associateId);
            model.AssociatesName = associate.Name;

            var queryStringKvps = new Dictionary<string, string>();
            var headersKvps = new Dictionary<string, string>();

            foreach (var queryKvp in HttpContext.Request.Query.Keys)
            {
                queryStringKvps.Add(queryKvp, HttpContext.Request.Query[queryKvp]);
            }
            model.QueryStringParameters = queryStringKvps;

            foreach (var headerKvp in HttpContext.Request.Headers)
            {
                headersKvps.Add(headerKvp.Key, headerKvp.Value.ToString());
            }
            model.Headers = headersKvps;

            return View(model);
        }
    }
}
