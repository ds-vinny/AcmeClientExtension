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
        private readonly ICurrentUser _currentUser;

        public CustomPageController(ILogger<CustomPageController> logger, IAssociateService associateService, ICurrentUser currentUser)
        {
            _logger = logger;
            _associateService = associateService ?? throw new ArgumentNullException(nameof(associateService));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
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

        [ExtensionAuthorize] // This authenticates that the request is coming from DirectScale
        public async Task<IActionResult> SecuredHelloWorld([FromQuery] int associateId)
        {
            // Authorizes the page to only users that have the ViewAdministration right.
            if (_currentUser.Rights.Contains("ViewAdministration") == false)
            {
                return new ForbidResult();
            }

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
