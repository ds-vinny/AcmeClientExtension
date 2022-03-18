using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace ACMEClientExtension.Controllers.Hooks
{
    [Route("api/hooks/[controller]")]
    [ApiController]
    public class OrderHooksGenericController : ControllerBase
    {
        private const int DistributorAssociateType = 1;
        private const int December = 12;

        // IMPORTANT! A NOTE ABOUT HOOK BEST PRACTICES:
        // This example is to meant only to show a Client Developer how a service could be used in a hook.
        // It is best practice that hooks are only implemented for CHANGES to business logic and
        // NOT ADDITIONS to business logic like the example below.
        // This is because hooks are executed in-process (Synchronously) and add performance overhead to the DirectScale System.
        // Although the example implemented below is functional it is not the most performant.
        // The example below would be most performant by using the DirectScale Event System that functions out-of-process (Asynchrously).
        // The Event System can notify the Client Extension that an order was created and the Client Extension could then create a
        // coupon out-of-process instead of adding overhead during the order creation process.
        [HttpPost("SubmitOrderHook")]
        public ActionResult<SubmitOrderResponse> SubmitOrderHook()
        {
            string expectedAuthKey = Environment.GetEnvironmentVariable("AuthKey"); // The API Key used to Authorize that a request came from Directscale
            string apiKey = HttpContext.Request.Headers["Authorization"];

            if (apiKey != "Bearer " + expectedAuthKey)
            {
                return Forbid();
            }

            Stream requestBodyStream = HttpContext.Request.Body;
            StreamReader sr = new StreamReader(requestBodyStream);
            string jsonBody = sr.ReadToEnd();
            int associateId = Newtonsoft.Json.JsonConvert.DeserializeObject<SubmitOrderRequest>(jsonBody).Order.AssociateId;

            // Execute the DirectScale callback method first
            string corpAdminUrl = Environment.GetEnvironmentVariable("DirectScaleServiceUrl"); // The URL for the Corp Admin Application
            string submitOrderCallbackEndpoint = corpAdminUrl + "/api/extension/hooks/Orders.SubmitOrder";
            string user = HttpContext.Request.Headers["X-DirectScale-User"];
            string callbackToken = HttpContext.Request.Headers["X-DirectScale-CallbackToken"];

            SubmitOrderResponse result = Post<SubmitOrderResponse>(submitOrderCallbackEndpoint, jsonBody, callbackToken, user);

            // The following code is an example of how a hook can utilize DirectScale Services
            // Both the Associate Service and Coupon Service will be used in this example
            // This code implemented below adds business logic to the DirectScale system
            // by giving any Distributor Type Associate an automatic 10 dollars off
            // their next order during january for every order they placed in December.
            try
            {
                string getAssociateEndpoint = corpAdminUrl + "/api/extension/services/AssociateService/v1/GetAssociate";
                Associate associate = Post<Associate>(getAssociateEndpoint, JsonConvert.SerializeObject(new GetAssociateRequest(associateId)), null, null);

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
            }
            catch (Exception)
            {
                // This catch allows the order call to return successful if the coupon fails to be created.
                // At this point the order has been created and the call should return success even if the code implemented as an After Hook Fails
            }

            return Ok(result);
        }

        public TResult Post<TResult>(string endpoint, string jsonBody, string callbackToken, string user)
        {
            HttpClient client = new HttpClient();

            try
            {
                string directscaleToken = Environment.GetEnvironmentVariable("DirectScaleToken"); // The API Key for the Corp Admin Application

                using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint))
                {
                    if (user != null)
                    {
                        requestMessage.Headers.Add("X-DirectScale-User", user);
                    }
                    if (callbackToken != null)
                    {
                        requestMessage.Headers.Add("X-DirectScale-CallbackToken", callbackToken);
                    }
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
            catch (ExtensionException) { throw; }
            catch (Exception e)
            {
                throw ExtensionException.FromException(e, "Disco call", endpoint, "");
            }
        }

        public class ExceptionModel
        {
            public string ErrorId { get; set; }
            public string ErrorMessage { get; set; }
            public List<string> RemoteStackTrace { get; set; }
            public string InternalStackTrace { get; set; }
        }

        [JsonObject(MemberSerialization.OptIn)]
        public class ExtensionException : System.Exception
        {

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public ExceptionModel DiscoRemoteException { get; set; }


            public string Context { get; set; }
            public string Method { get; set; }
            public string RequestBody { get; set; }
            public override string Message => DiscoRemoteException.ErrorMessage;

            public ExtensionException()
            {
            }

            public ExtensionException(string message, Exception innerException) : base(message, innerException)
            {
                DiscoRemoteException = new ExceptionModel();
                DiscoRemoteException.ErrorMessage = message;
                DiscoRemoteException.InternalStackTrace = innerException?.StackTrace ?? Environment.StackTrace;
            }

            // If this is the first error in the stack trace, the add the errorId and callcontext.
            // Error message should be something like this:
            //      SSN required (Error Id: 1234 from Disco call validateApplicationUnhooked)
            // Not this:
            //      SSN required (Error Id: 1234 from Disco call validateApplicationUnhooked) (ErrorId: 1234: from hook call validateApplication) (ErrorId: 1234 from Disco call SubmitApplicationUnhooked) (ErrorId: 1234 from hook SubmitApplication) 
            public void AddStackTrace(string stackTrace)
            {
                DiscoRemoteException.RemoteStackTrace.Add(stackTrace);
                if (DiscoRemoteException.RemoteStackTrace.Count == 1)
                {
                    DiscoRemoteException.ErrorMessage = $"{DiscoRemoteException.ErrorMessage} (Error Id: {DiscoRemoteException.ErrorId} from {DiscoRemoteException.RemoteStackTrace[0]})";
                }
            }

            #region Static factory methods
            public static ExtensionException FromHttpResponse(string responseBody, string responseMessage, string context, string method)
            {
                string stackTrace = (method ?? context) == null ? null : $"{context}: {method}";
                ExtensionException extensionException;


                extensionException = new ExtensionException(responseMessage, null);
                try
                {
                    if (!string.IsNullOrWhiteSpace(responseBody))
                    {
                        var json = JObject.Parse(responseBody);
                        if (json[nameof(DiscoRemoteException)] != null)
                        {
                            extensionException = json.ToObject<ExtensionException>();
                        }
                    }
                }
                catch { /* default value already set */ }

                if (string.IsNullOrWhiteSpace(extensionException.DiscoRemoteException.ErrorMessage)) extensionException.DiscoRemoteException.ErrorMessage = responseMessage;
                if (stackTrace != null)
                {
                    extensionException.AddStackTrace(stackTrace);
                }

                return extensionException;
            }

            public static ExtensionException FromException(Exception e, string context, string method, string requestBody)
            {
                string stackTrace = (method ?? context) == null ? null : $"{context}: {method}";
                ExtensionException extensionException;

                try
                {
                    extensionException = FromCodeException(e);
                    if (extensionException == null) extensionException = FromWebException(e);
                    if (extensionException == null) extensionException = new ExtensionException(e.Message, e);
                }
                catch
                {
                    extensionException = new ExtensionException(e.Message, e);
                }

                if (stackTrace != null) extensionException.AddStackTrace(stackTrace);
                extensionException.Context = context;
                extensionException.Method = method;
                extensionException.RequestBody = requestBody;

                return extensionException;
            }

            private static ExtensionException FromWebException(Exception e)
            {
                WebException webException = e as WebException;
                if (webException == null && e is HttpRequestException httpRequestException) webException = httpRequestException.InnerException as WebException;
                if (webException != null)
                {
                    if (webException.Response != null)
                    {
                        string responseBody = new System.IO.StreamReader(webException.Response.GetResponseStream()).ReadToEnd();
                        var json = JObject.Parse(responseBody);
                        if (json[nameof(DiscoRemoteException)] != null)
                        {
                            return json.ToObject<ExtensionException>();
                        }
                        else
                        {
                            return new ExtensionException(e.Message, e);
                        }

                    }
                }

                return null;
            }

            private static ExtensionException FromCodeException(Exception e)
            {
                return e as ExtensionException;
            }
            #endregion

            public class ExceptionModel
            {
                [JsonProperty]
                public string ErrorId { get; set; } = Guid.NewGuid().ToString().Substring(0, 8);
                [JsonProperty]
                public string ErrorMessage { get; set; }
                [JsonProperty]
                public List<string> RemoteStackTrace { get; set; } = new List<string>();
                [JsonProperty]
                public string InternalStackTrace { get; set; }
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

        public class SubmitOrderRequest
        {
            public Order Order { get; set; }
        }

        public class SubmitOrderResponse
        {
            public int OrderNumber { get; set; }
            public NewOrderDetail Order { get; set; }
        }

        public class Order
        {
            public int AssociateId { get; set; }
            // There are more fields on an order that are not included for brevity..
        }

        public class NewOrderDetail
        {
            public NewOrderDetail()
            {
                ShipAddress = new Address();
                ShipTaxOverride = new ShippingTaxOverride();
                LineItems = new List<LineItem>();
                Custom = new CustomFields();
            }
            public int AssociateId { get; set; }
            public string ShipToName { get; set; }
            public Address ShipAddress { get; set; }
            public int ShipMethodId { get; set; }
            public int WarehouseId { get; set; }
            public string ShipPhone { get; set; }
            public int PriceGroupId { get; set; }
            public string SpecialInstructions { get; set; }
            public OrderSourceEnum OrderSource { get; set; }
            public OrderTypeEnum OrderType { get; set; }
            public int AssociateType { get; set; }
            public int StoreId { get; set; }
            public string CurrencyCode { get; set; }
            public string[] CouponCodes { get; set; }
            public List<LineItem> LineItems { get; set; }
            public ShippingTaxOverride ShipTaxOverride { get; set; }
            public CustomFields Custom { get; set; }

            public class CustomFields
            {
                public string Field1 { get; set; }
                public string Field2 { get; set; }
                public string Field3 { get; set; }
                public string Field4 { get; set; }
                public string Field5 { get; set; }
            }

            public class LineItem
            {
                public LineItem()
                {
                    Category = new ItemCategory();
                    Images = new List<ItemImage>();
                    Prices = new List<LineItemPrice>();
                    Options = new List<LineItemOption>();
                    ItemOptions = new List<ItemOption>();
                    Languages = new List<LanguageMap>();
                    OptionsMap = new List<ItemOptionKeyMap>();
                    Custom = new CustomFields();
                }

                public int ItemId { get; set; }
                public double Quantity { get; set; }

                public double Cost { get; set; }
                public bool Disabled { get; set; }
                public double ExtendedPrice { get; set; }
                public double ExtendedOriginalPrice { get; set; }
                public double ExtendedBonus { get; set; }
                public double ExtendedCV { get; set; }
                public double ExtendedQV { get; set; }
                public double ExtendedRewardPoints { get; set; }
                public double ExtendedCost { get; set; }

                public string ProductName { get; set; }
                public string Description { get; set; }
                public string Specifications { get; set; }
                public string LanguageCode { get; set; }

                public string SKU { get; set; }
                public ItemCategory Category { get; set; }
                public bool ChargeShipping { get; set; }
                public double Height { get; set; }
                public string Image { get; set; }
                public double Length { get; set; }
                public string LengthUOM { get; set; }
                public string MPN { get; set; }
                public double PackCount { get; set; }
                public int PackageGroupId { get; set; }
                public int TaxClassId { get; set; }
                public string UnitOfMeasure { get; set; }
                public string UPC { get; set; }
                public double Weight { get; set; }
                public string WeightUOM { get; set; }
                public double Width { get; set; }
                public bool FlagBirthDefects { get; set; }
                public bool HasKitGroups { get; set; }
                public bool FlagCancer { get; set; }
                public int PriceGroup { get; set; }

                public CustomFields Custom { get; set; }

                public List<ItemImage> Images { get; set; }
                public List<LineItemPrice> Prices { get; set; }
                public List<LineItemOption> Options { get; set; }
                public List<ItemOption> ItemOptions { get; set; }
                public List<LanguageMap> Languages { get; set; }
                public List<ItemOptionKeyMap> OptionsMap { get; set; }

                public bool HasOptions { get; set; }

                public double Price { get; set; }
                public string PriceCurrency { get; set; }
                public double OriginalPrice { get; set; }
                public double Bonus { get; set; }
                public double CV { get; set; }
                public double QV { get; set; }
                public double RewardPoints { get; set; }
                public int CouponsBeingUsed { get; set; }
                public int OutOfStockStatus { get; set; }

                public int[] PriceGroups { get; set; }

                public class ItemOptionKeyMap
                {
                    public string Key { get; set; }
                    public bool Checked { get; set; }
                    public int ItemId { get; set; }
                    public string Image { get; set; }
                    public string ExtSku { get; set; }
                }

                public class LanguageMap
                {
                    public string Description { get; set; }
                    public string LanguageCode { get; set; }
                    public string ProductName { get; set; }
                    public string SEOKeywords { get; set; }
                    public string Specifications { get; set; }
                }

                public class ItemOption
                {
                    public ItemOption()
                    {
                        Values = new List<ItemOptionValues>();
                    }
                    public int OptionId { get; set; }
                    public string Option { get; set; }
                    public int OptionType { get; set; }
                    public List<ItemOptionValues> Values { get; set; }

                    public class ItemOptionValues
                    {
                        public string Option { get; set; }
                        public string SkuExt { get; set; }
                    }
                }

                public class LineItemOption
                {
                    public int OptionId { get; set; }
                    public string Option { get; set; }
                }

                public class LineItemPrice
                {
                    public int GroupId { get; set; }
                    public double Price { get; set; }
                    public string PriceCurrency { get; set; }
                    public double OriginalPrice { get; set; }
                    public double Bonus { get; set; }
                    public double CV { get; set; }
                    public double QV { get; set; }
                    public double RewardPoints { get; set; }
                }

                public class ItemCategory
                {
                    public ItemCategory()
                    {
                        StoreIds = new List<int>();
                        CategoryTranslations = new List<CategoryTranslation>();
                    }

                    public int Id { get; set; }
                    public string Name { get; set; }
                    public List<CategoryTranslation> CategoryTranslations { get; set; }
                    public string Description { get; set; }
                    public double DisplayIndex { get; set; }
                    public string ImageUrl { get; set; }
                    public int ParentId { get; set; }
                    public int ProductLineId { get; set; }
                    public string ShortDescription { get; set; }
                    public List<int> StoreIds { get; set; }
                    public bool HasChildren { get; set; }

                    public class CategoryTranslation
                    {
                        public int Id { get; set; }
                        public int CategoryId { get; set; }
                        public string Description { get; set; }
                        public string Name { get; set; }
                        public string LanguageCode { get; set; }
                    }
                }

                public class ItemImage
                {
                    public string Description { get; set; }
                    public string Path { get; set; }
                }
            }

            public class Address
            {
                public int Id { get; set; }
                public string AddressLine1 { get; set; }
                public string AddressLine2 { get; set; }
                public string City { get; set; }
                public string State { get; set; }
                public string PostalCode { get; set; }
                public string CountryCode { get; set; }
            }

            public enum OrderTypeEnum
            {
                Unknown = 0, Standard = 1, Autoship = 2, Enrollment = 3, VolumeAdjustment = 4
            }

            public enum OrderSourceEnum
            {
                Unknown = 0,
                PublicApi = 1,
                V1Api = 2,
                V2Api = 3,
                CorpAdmin = 4,
                DataMigration = 5,
                ImportOrder = 6,
                Autoship = 7,
                Facade = 8,
                UnknownExtension = 9,
                PublicApiExtension = 10,
                V1ApiExtension = 11,
                V2ApiExtension = 12,
                CorpAdminExtension = 13,
                ImportOrderExtension = 14,
                AutoshipExtension = 15,
                FacadeExtension = 16
            }

            public class ShippingTaxOverride
            {
                public double Shipping { get; set; }
                public double Tax { get; set; }
                public bool HasShippingOverride { get; set; }
                public bool HasTaxOverride { get; set; }
            }
        }
    }
}
