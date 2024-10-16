using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using wayfair_order_picklist_dev.Models;

namespace wayfair_order_picklist_dev
{
    public static class CreatePickList
    {
        private static readonly HttpClient httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        private static readonly string logAnalyticsWorkspaceId = Environment.GetEnvironmentVariable("LOG_ANALYTICS_WORKSPACE_ID");
        private static readonly string logAnalyticsSharedKey = Environment.GetEnvironmentVariable("LOG_ANALYTICS_SHARED_KEY");
        private static readonly string logName = "PicklistLog";

        [FunctionName("CreatePicklist")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "CreatePicklist")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Received request to create a picklist.");

            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var order = JsonConvert.DeserializeObject<List<OrderLine>>(requestBody);

            if (order == null)
            {
                log.LogError("Invalid order data.");
                await SendLogToLogAnalytics("Invalid order data", "error", log);
                return new BadRequestObjectResult("Invalid order data.");
            }

            var picklist = new PickLists
            {
                ObjectType = order[0].ObjectType,
                PickDate = order[0].PickDate.Date.ToString("yyyy-MM-dd"),
                PickListsLines = new List<PickListsLine>()
            };

            foreach (var line in order)
            {
                var picklistline = new PickListsLine
                {
                    BaseObjectType = Convert.ToInt32(line.BaseObjectType),
                    OrderEntry = Convert.ToInt32(line.DocEntry),
                    OrderRowID = Convert.ToInt32(line.LineNum),
                    ReleasedQuantity = Convert.ToInt32(line.ReleasedQuantity)
                };
                picklist.PickListsLines.Add(picklistline);
            }

            string requestUrl;

            if (order[0].DBName.ToLower().Contains("us"))
                requestUrl = "https://mhcdev-integration-apim.azure-api.net/serviceLayer/create-object-us/PickLists";
            else
                requestUrl = "https://mhcdev-integration-apim.azure-api.net/serviceLayer/create-object-ca/PickLists";

            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
            {
                Content = new StringContent(JsonConvert.SerializeObject(picklist), Encoding.UTF8, "application/json")
            };

            var subscriptionKey = Environment.GetEnvironmentVariable("Ocp-Apim-Subscription-Key");
            request.Headers.Add("Ocp-Apim-Subscription-Key", subscriptionKey);

            log.LogInformation("Picklist JSON: " + JsonConvert.SerializeObject(picklist));

            try
            {
                var response = await httpClient.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    log.LogError($"Error creating picklist: {responseBody}");
                    await SendLogToLogAnalytics(responseBody, "error", log);
                    return new BadRequestObjectResult($"Error creating picklist: {responseBody}");
                }

                log.LogInformation("Picklist created successfully.");
                await SendLogToLogAnalytics("Picklist created successfully", "success", log);
                return new OkObjectResult(responseBody);
            }
            catch (Exception ex)
            {
                log.LogError($"Exception occurred: {ex.Message}");
                await SendLogToLogAnalytics(ex.Message, "error", log);
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        private static async Task SendLogToLogAnalytics(string message, string logType, ILogger log)
        {
            var logData = new List<object>
            {
                new
                {
                    identifier = Guid.NewGuid().ToString(),
                    category = "integration",
                    implementation = "wayfair_order_picklist_dev",
                    resourceGroup = "MHCDEV-integration-wayfair-common-assets",
                    resourceType = "function-app",
                    logType = logType,
                    message = message,
                    entity = "PickList"
                }
            };

            var jsonPayload = JsonConvert.SerializeObject(logData);
            var datestring = DateTime.UtcNow.ToString("r");
            var jsonBytes = Encoding.UTF8.GetBytes(jsonPayload);
            string stringToHash = "POST\n" + jsonBytes.Length + "\napplication/json\n" + "x-ms-date:" + datestring + "\n/api/logs";
            string hashedString = BuildSignature(stringToHash, logAnalyticsSharedKey);
            string signature = "SharedKey " + logAnalyticsWorkspaceId + ":" + hashedString;

            await PostData(signature, datestring, jsonPayload, log);
        }

        public static string BuildSignature(string message, string secret)
        {
            var encoding = new ASCIIEncoding();
            byte[] keyByte = Convert.FromBase64String(secret);
            byte[] messageBytes = encoding.GetBytes(message);
            using (var hmacsha256 = new HMACSHA256(keyByte))
            {
                byte[] hash = hmacsha256.ComputeHash(messageBytes);
                return Convert.ToBase64String(hash);
            }
        }

        public static async Task PostData(string signature, string date, string json, ILogger log)
        {
            try
            {
                string url = "https://" + logAnalyticsWorkspaceId + ".ods.opinsights.azure.com/api/logs?api-version=2016-04-01";

                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
                httpClient.DefaultRequestHeaders.Add("Log-Type", logName);
                httpClient.DefaultRequestHeaders.Add("Authorization", signature);
                httpClient.DefaultRequestHeaders.Add("x-ms-date", date);

                HttpContent httpContent = new StringContent(json, Encoding.UTF8);
                httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                HttpResponseMessage response = await httpClient.PostAsync(new Uri(url), httpContent);
                string result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    log.LogInformation("Log sent to Log Analytics successfully.");
                }
                else
                {
                    log.LogError($"Failed to send log to Log Analytics. Status code: {response.StatusCode}. Response: {result}");
                }
            }
            catch (Exception ex)
            {
                log.LogError("API Post Exception: " + ex.Message);
            }
        }
    }
}
