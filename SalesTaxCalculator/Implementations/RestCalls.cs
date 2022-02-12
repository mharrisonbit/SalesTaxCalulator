using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly;
using SalesTaxCalculator.Interfaces;

namespace SalesTaxCalculator.Implementations
{
    public class RestCalls : IGetDataFromApi
    {
        public string ApiKey = "5da2f821eee4035db4771edab942a4cc";
        public string ApiUrl = "https://api.taxjar.com/v2/";

        public RestCalls()
        {
        }

        public async Task<string> GetTaxForOrderFromApi(JObject orderInfo)
        {
            var details = "";

            var authToken = ApiKey;
            var method = new HttpMethod("POST");
            var body = JsonConvert.SerializeObject(orderInfo);
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(method, $"{ApiUrl}taxes")
            {
                Content = content
            };

            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Token token=\"{authToken}\"");
            var response = await Policy
                .Handle<HttpRequestException>()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (result, timeSpan, retryCount, context) =>
                {
                })
                .ExecuteAsync(() => httpClient.SendAsync(request));

            if (response.IsSuccessStatusCode)
            {
                details = await response.Content.ReadAsStringAsync();
            }
            response.Dispose();
            request.Dispose();
            httpClient.Dispose();

            return details;
        }

        public async Task<string> GetTaxRatesFromApi(JObject callData)
        {
            var details = "";
            var urlString = $"{ApiUrl}rates/{callData["zipcode"]}?country={callData["country"]}&city={callData["city"]}";
            
            var method = new HttpMethod("GET");
            var request = new HttpRequestMessage(method, urlString) { };

            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Token token=\"{ApiKey}\"");

            var response = await Policy
                .Handle<HttpRequestException>()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (result, timeSpan, retryCount, context) => { })
                .ExecuteAsync(() => httpClient.SendAsync(request));

            if (response.IsSuccessStatusCode)
            {
                details = await response.Content.ReadAsStringAsync();
            }
            response.Dispose();
            httpClient.Dispose();

            return details;
        }
    }
}
