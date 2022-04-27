using DirectScale.Disco.Extension.EventModels;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ACMEClientExtension.Controllers.WebHooks
{
    [Route("api/webhooks/Order")]
    [ApiController]
    public class OrderWebHooks2Controller : ControllerBase
    {
        private const int DistributorAssociateType = 1;
        private const int December = 12;


        /// <summary>
        /// This is a duplicate of the OrderWebHooks2Controller.CreateOrder API.
        /// This implement directly calls the Directscale Extension API instead of using the library to do it.
        /// </summary>
        [HttpPost("CreateOrder2")]
        public async Task<ActionResult> CreateOrderWebHook()
        {
            // *******|  WARNING READ BEST PRACTICES!!!  |*******
            // ALTHOUGH THIS CODE SEEMS FUNCTIONAL IT DOES NOT ACCOUNT FOR 
            // - DUPLICATE EVENTS
            //   or
            // - RETURN A 200 QUICKLY BEFORE DOING PROCESSING. 
            //   (IF A CALL TIMES OUT THEN IT WILL BE SENT AGAIN)

            // The following code is an example of how a Webhook can utilize DirectScale Services
            // Both the Associate Service and Coupon Service will be used in this example
            // This code implemented below adds business logic to the DirectScale system
            // by giving any Distributor Type Associate an automatic 10 dollars off
            // their next order during january for every order they placed in December.

            string expectedAuthKey = Environment.GetEnvironmentVariable("AuthKey"); // The API Key used to Authorize that a request came from Directscale
            string apiKey = HttpContext.Request.Headers["Authorization"];

            if (apiKey != "Bearer " + expectedAuthKey)
            {
                return Forbid();
            }

            Stream requestBodyStream = HttpContext.Request.Body;
            StreamReader sr = new StreamReader(requestBodyStream);
            string jsonBody = sr.ReadToEnd();
            CreateOrderEvent request = Newtonsoft.Json.JsonConvert.DeserializeObject<CreateOrderEvent>(jsonBody);

            string corpAdminUrl = "https://acme.corpadmin.directscalestage.com";

            string getAssociateEndpoint = corpAdminUrl + "/api/extension/services/AssociateService/v1/GetAssociate";
            Associate associate = Post<Associate>(getAssociateEndpoint, JsonConvert.SerializeObject(new GetAssociateRequest(request.CustomerId)), null, null);

            if (associate.AssociateBaseType == DistributorAssociateType && DateTime.Now.Month == December)
            {
                Coupon oneTimeUseAssociateCoupon = new Coupon()
                {
                    BackOfficeIds = new string[] { associate.BackOfficeId },
                    BeginDate = new DateTime(year: DateTime.Now.Year + 1, month: 1, day: 1, hour: 0, minute: 0, second: 0),
                    EndDate = new DateTime(year: DateTime.Now.Year + 1, month: 1, day: 31, hour: 23, minute: 59, second: 59),
                    Recurring = false,
                    CouponType = CouponType.OrderDiscount,
                    AmountType = AmountType.Amount,
                    Discount = 10d,
                };

                string saveCouponEndpoint = corpAdminUrl + "/api/extension/services/CouponService/v1/SaveCoupon";
                var a = Post<SaveCouponResponse>(saveCouponEndpoint, JsonConvert.SerializeObject(new SaveCouponRequest(oneTimeUseAssociateCoupon)), null, null);

            }

            return Ok();
        }


        public TResult Post<TResult>(string endpoint, string jsonBody, string callbackToken, string user)
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

                return JsonConvert.DeserializeObject<TResult>(responseBody);
            }
        }

        public class GetAssociateRequest
        {
            public GetAssociateRequest(int associateId)
            {
                this.associateId = associateId;
            }
            public int associateId { get; set; }
        }

        public class Associate
        {
            public int AssociateBaseType { get; set; }
            public string BackOfficeId { get; set; }
            // There are many other fields for a Associate that are not included for brevity
        }

        public class SaveCouponRequest
        {
            public SaveCouponRequest(Coupon coupon)
            {
                couponInfo = coupon;
            }
            public Coupon couponInfo { get; set; }
        }

        public class SaveCouponResponse
        {
            public int CouponId { get; set; }
        }

        public class Coupon
        {
            public int CouponId { get; set; }
            public string[] BackOfficeIds { get; set; }
            public DateTime BeginDate { get; set; }
            public DateTime EndDate { get; set; }
            public bool Recurring { get; set; }
            public CouponType CouponType { get; set; }
            public AmountType AmountType { get; set; }
            public double Discount { get; set; }
        }

        public enum AmountType
        {
            Percent = 0,
            Amount = 1
        }

        public enum CouponType
        {
            ItemDiscount = 0,
            OrderDiscount = 1,
            ShippingDiscount = 2
        }
    }
}
