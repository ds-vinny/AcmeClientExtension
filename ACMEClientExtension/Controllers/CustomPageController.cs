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
        private readonly ILogger _logger;
        private readonly ILoggerProvider _loggerProvider;
        private readonly IAssociateService _associateService;
        private readonly ICurrentUser _currentUser;

        public CustomPageController(IAssociateService associateService, ICurrentUser currentUser, ILoggerProvider loggerProvider)
        {
            _loggerProvider = loggerProvider ?? throw new ArgumentNullException(nameof(loggerProvider));
            _logger = _loggerProvider.CreateLogger("CustomPageLogger");
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

            // See "MORE INFORMATION ABOUT LOGGING" comment below for more information about Extension Logging
            _logger.LogInformation($"The Hello World Page was visited and displayed the following name: '{personsName}'.");

            return View(model);
        }

        // **********************************************************
        // ************* MORE INFORMATION ABOUT LOGGING *************
        // **********************************************************
        //
        // **** | How to view the Log | ****
        // - Execute the following query in the SQL Manager. https://<clientid>.corpadmin.directscale.com/Corporate/Reports/SqlViewer
        //      SELECT * FROM ExtensionLog L
        //      WHERE L.eventName = 'CustomPageLogger'
        //
        // **** | Example of the log record | ****
        //  recordnumber	last_modified	        logLevel	eventId	eventName	        message
        //  4383	        9/22/2022 7:33:43 AM	2	        0	    CustomPageLogger	The Hello World Page was visited and displayed the following name: 'Sam'.
        //
        // **** | ExtensionLog Table Schema | ****
        // Column           DataType        Nullable
        // recordnumber     int             true
        // last_modified    datetime        true
        // logLevel         int             true
        // eventId          int             true
        // eventName        varchar(25)	    false
        // message          varchar(255)	true
        // scope            varchar(255)	false
        //
        // **** | NOTES | ****
        // - Messages longer than 255 Characters are not supported.
        //      - If you need messages longer than 255 characters then you will need to split the string and call the log function multiple times.
        // - Logging is asynchronous
        //      - Logs may not be in exact chronilogical order because they are written using async operations.

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
