using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;

namespace ACMEClientExtension.Controllers.Hooks
{
    [Route("api/hooks/AutoshipHooksGeneric")]
    [ApiController]
    public class AutoshipHooksGenericController : ControllerBase
    {
        [HttpPost("CreateAutoshipHook")]
        public ActionResult<CreateAutoshipResponse> CreateAutoshipHook()
        {
            // This is an Example of a Before Hook
            string expectedAuthKey = Environment.GetEnvironmentVariable("AuthKey"); // The API Key used to Authorize that a request came from Directscale
            string apiKey = HttpContext.Request.Headers["Authorization"];

            if (apiKey != "Bearer " + expectedAuthKey)
            {
                return Forbid();
            }

            Stream requestBodyStream = HttpContext.Request.Body;
            StreamReader sr = new StreamReader(requestBodyStream);
            var s = sr.ReadToEnd();
            CreateAutoshipRequest requestBody = JsonConvert.DeserializeObject<CreateAutoshipRequest>(s);

            // The following lines add some text to the custom field of an autoship when it is created.
            requestBody.AutoshipInfo.Custom.Field1 = "This message was added from the CreateAutoshipHook";

            // Execute the DirectScale callback API with the modified 'request' object that was passed into the controller as well as the one-time callbackToken
            string corpAdminUrl = Environment.GetEnvironmentVariable("DirectScaleServiceUrl"); // The URL for the Corp Admin Application
            string createAutoshipCallbackEndpoint = corpAdminUrl + "/api/extension/hooks/Autoships.CreateAutoship";
            string user = HttpContext.Request.Headers["X-DirectScale-User"];
            string callbackToken = HttpContext.Request.Headers["X-DirectScale-CallbackToken"];

            CreateAutoshipResponse response = Post<CreateAutoshipResponse>(createAutoshipCallbackEndpoint, requestBody, callbackToken, user);

            return Ok(response);
        }

        [HttpPost("GetAutoshipsHook")]
        public ActionResult<GetAutoshipsResponse> GetAutoshipsHook()
        {
            // This is an Example of an After Hook
            string expectedAuthKey = Environment.GetEnvironmentVariable("AuthKey"); // The API Key used to Authorize that a request came from Directscale
            string apiKey = HttpContext.Request.Headers["Authorization"];

            if (apiKey != "Bearer " + expectedAuthKey)
            {
                return Forbid();
            }

            Stream requestBodyStream = HttpContext.Request.Body;
            StreamReader sr = new StreamReader(requestBodyStream);
            GetAutoshipsRequest requestBody = JsonConvert.DeserializeObject<GetAutoshipsRequest>(sr.ReadToEnd());

            // Execute the DirectScale callback method first
            string corpAdminUrl = Environment.GetEnvironmentVariable("DirectScaleServiceUrl"); // The URL for the Corp Admin Application
            string getAutoshipsCallbackEndpoint = corpAdminUrl + "/api/extension/hooks/Autoships.GetAutoships";
            string user = HttpContext.Request.Headers["X-DirectScale-User"];
            string callbackToken = HttpContext.Request.Headers["X-DirectScale-CallbackToken"];

            GetAutoshipsResponse result = Post<GetAutoshipsResponse>(getAutoshipsCallbackEndpoint, requestBody, callbackToken, user);

            // Then update the result of the DirectScale Callback method so that the Address is in ALL CAPS
            foreach (var autoship in result.Autoships)
            {
                autoship.ShipAddress.AddressLine1 = autoship.ShipAddress.AddressLine1.ToUpper();
                autoship.ShipAddress.AddressLine2 = autoship.ShipAddress.AddressLine2.ToUpper();
                autoship.ShipAddress.City = autoship.ShipAddress.City.ToUpper();
                autoship.ShipAddress.CountryCode = autoship.ShipAddress.CountryCode.ToUpper();
                autoship.ShipAddress.State = autoship.ShipAddress.State.ToUpper();
            }

            // Return the modified result
            return Ok(result);
        }

        public TResult Post<TResult>(string endpoint, object body, string callbackToken, string user)
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
                    requestMessage.Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");

                    HttpResponseMessage response = client.Send(requestMessage);

                    string responseBody = new StreamReader(response.Content.ReadAsStream()).ReadToEnd();
                    
                    if (response.IsSuccessStatusCode == false)
                    {
                        throw new HttpRequestException(responseBody, null, response.StatusCode);
                    }
                    
                    return JsonConvert.DeserializeObject<TResult>(responseBody);
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message, e);
            }
        }
    }

    public class CreateAutoshipRequest
    {
        public CreateAutoshipRequest()
        {
            AutoshipInfo = new Autoship();
        }
        public Autoship AutoshipInfo { get; set; }
    }

    public class CreateAutoshipResponse
    {
        public int AutoshipId { get; set; }
    }

    public class Autoship
    {
        public Autoship()
        {
            ShipAddress = new Address();
            Custom = new CustomFields();
            LineItems = new List<LineItem>();
            Custom = new CustomFields();
        }

        public int AutoshipId { get; set; }
        public int AssociateId { get; set; }
        public Address ShipAddress { get; set; }

        public DateTime StartDate { get; set; }
        public Frequency Frequency { get; set; }


        public DateTime LastProcessDate { get; set; }
        public DateTime NextProcessDate { get; set; }

        public double LastChargeAmount { get; set; }

        public string Status { get; set; }
        public int ShipMethodId { get; set; }
        public string PaymentMethodId { get; set; }
        public int PaymentMerchantId { get; set; }
        public CustomFields Custom { get; set; }

        public AutoshipType AutoshipType { get; set; }

        public List<LineItem> LineItems { get; set; }

        public string FrequencyString { get; set; }

        public double TotalCV { get; set; }
        public double TotalQV { get; set; }
        public string CurrencyCode { get; set; }
        public double SubTotal { get; set; }

        public class CustomFields
        {
            public string Field1 { get; set; }
            public string Field2 { get; set; }
            public string Field3 { get; set; }
            public string Field4 { get; set; }
            public string Field5 { get; set; }
        }

        public class Address
        {

            public string AddressLine1 { get; set; }
            public string AddressLine2 { get; set; }
            public string City { get; set; }
            public string State { get; set; }
            public string CountryCode { get; set; }
            public string PostalCode { get; set; }
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

            public class CustomFields
            {
                public string Field1 { get; set; }
                public string Field2 { get; set; }
                public string Field3 { get; set; }
                public string Field4 { get; set; }
                public string Field5 { get; set; }
            }

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
    }

    public enum AutoshipType
    {
        Standard = 0,
        Services = 1,
        YearlyRenewal = 2
    }

    public enum Frequency
    {
        Weekly,
        EveryTwoWeeks,
        Monthly,
        BiMonthly,
        TriMonthly,
        TwiceAYear,
        Yearly,
        Every4weeks,
        Every6Weeks,
        Every8Weeks,
        Every12Weeks,
        Every30Days,
        Every45Days,
        Every60Days
    }

    public class GetAutoshipsRequest
    {
        public int AssociateId { get; set; }
        public bool IncludeServiceAutoships { get; set; }
    }

    public class GetAutoshipsResponse
    {
        public Autoship[] Autoships { get; set; }
    }
}
