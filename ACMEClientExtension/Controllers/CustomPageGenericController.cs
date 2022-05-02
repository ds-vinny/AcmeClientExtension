using ACMEClientExtension.Models;
using ACMEClientExtension.Models.CustomPage;
using DirectScale.Disco.Extension.Middleware;
using DirectScale.Disco.Extension.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ACMEClientExtension.Controllers
{
    public class CustomPageGenericController : Controller
    {
        /// <summary>
        /// This is a duplicate of the CustomPageContoller/SecuredHelloWorld page.
        /// This has the exact same logic, but calls the directscale extension api directly instead of using directscale services.
        /// </summary>
        public async Task<IActionResult> SecuredHelloWorld([FromQuery] string pageToken, [FromQuery] string pageId, [FromQuery] int associateId)
        {
            string corpAdminUrl = Environment.GetEnvironmentVariable("DirectScaleServiceUrl"); // The URL for the Corp Admin Application

            var validatePageTokenEndpoint = corpAdminUrl + "/api/extension/pages/ValidateToken";
            var username = Post<string>(validatePageTokenEndpoint, JsonConvert.SerializeObject(new ValidateTokenRequest(pageToken, pageId)));

            var getUserEndpoint = corpAdminUrl + "/api/extension/services/UserService/v1/GetUser";
            var userInfo = Post<GetUserResponse>(getUserEndpoint, JsonConvert.SerializeObject(new GetUserRequest(username)));

            // Authorizes the page to only users that have the ViewAdministration right.
            if (userInfo.Rights.Contains("ViewAdministration") == false)
            {
                return new ForbidResult();
            }

            var model = new SecuredHelloWorldViewModel();

            // Use Directscale Service to get associate information
            var getAssociateEndpoint = corpAdminUrl + "/api/extension/services/AssociateService/v1/GetAssociate";
            var associate = Post<GetAssociateResponse>(getAssociateEndpoint, JsonConvert.SerializeObject(new GetAssociateRequest(associateId)));
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

        public class GetAssociateRequest
        {
            public GetAssociateRequest(int associateId)
            {
                AssociateId = associateId;
            }
            public int AssociateId { get; set; }
        }

        public class GetAssociateResponse
        {
            public string Name { get; set; }

            // many other properties omitted for brevity
        }

        public class GetUserRequest
        {
            public GetUserRequest(string username)
            {
                Username = username;
            }
            public string Username { get; set; }
        }

        public class GetUserResponse
        {
            public bool IsCorporate { get; set; }
            public string[] Rights { get; set; }
            public DateTime LastLogin { get; set; }
            public string PrimaryRole { get; set; }
            public string SecondaryRole { get; set; }
            public string EmailAddress { get; set; }
            public string DisplayName { get; set; }
            public string UserName { get; set; }
            public bool Authenticated { get; set; }        
        }

        public class ValidateTokenResponse
        {
            public string username { get; set; }
        }

        public class ValidateTokenRequest
        {
            public ValidateTokenRequest(string token, string pageName)
            {
                Token = token;
                PageName = pageName;
            }

            public string Token { get; set; }
            public string PageName { get; set; }
        }

        public TResult Post<TResult>(string endpoint, string jsonBody)
        {
            HttpClient client = new HttpClient();

            string directscaleToken = Environment.GetEnvironmentVariable("DirectScaleToken"); // The API Key for the Corp Admin Application

            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint))
            {
                // Add Authentication Header - { "Authentication" : "Bearer [DirectscaleTokenValue]" }
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", directscaleToken);

                // Add JSON encoded Body and Content-Type Header - { "Content-Type" : "application/json" }
                requestMessage.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                var response = client.Send(requestMessage);

                string responseBody = new StreamReader(response.Content.ReadAsStream()).ReadToEnd();

                if (response.IsSuccessStatusCode == false)
                {
                    throw ExtensionException.FromHttpResponse(responseBody, response.ToString(), "Disco call", endpoint);
                }

                return JsonConvert.DeserializeObject<TResult>(responseBody);
            }

        }
    }
}
