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
                return new BadRequestObjectResult("Invalid order data.");
            }

            var picklist = new PickLists
            {
                ObjectType = order[0].ObjectType,
                PickDate = order[0].PickDate.Date.ToString("yyyy-MM-dd"),
                //PickDate = order[0].PickDate,
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

            var pickliatJson = JsonConvert.SerializeObject(picklist);

            string requestUrl;

            if (order[0].DBName.ToLower().Contains("us"))
                requestUrl = "https://mhcdev-integration-apim.azure-api.net/serviceLayer/create-object-us/PickLists";
            else
                requestUrl = "https://mhcdev-integration-apim.azure-api.net/serviceLayer/create-object-ca/PickLists";

            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
            {
                Content = new StringContent(JsonConvert.SerializeObject(picklist), Encoding.UTF8, "application/json")
            };

            var subscriptionKey = Environment.GetEnvironmentVariable("SUBSCRIPTION_KEY");
            request.Headers.Add("Ocp-Apim-Subscription-Key", "4e3fddcb396f4940957c49f784d1560a");

            log.LogInformation("Picklist JSON: " + JsonConvert.SerializeObject(picklist));

            try
            {
                var response = await httpClient.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    log.LogError($"Error creating picklist: {responseBody}");
                    return new BadRequestObjectResult($"Error creating picklist: {responseBody}");
                }

                log.LogInformation("Picklist created successfully.");
                return new OkObjectResult(responseBody);
            }
            catch (Exception ex)
            {
                log.LogError($"Exception occurred: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

        }
    }
}
