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
                    await UpdateStagingTableStatus(order[0].StagingtableId, "F", responseBody, log);
                    return new BadRequestObjectResult($"Error creating picklist: {responseBody}");
                }

                log.LogInformation("Picklist created successfully. Updating Wayfair_Order_Staging table...");

                await UpdateStagingTableStatus(order[0].StagingtableId, "S", null, log);

                return new OkObjectResult(responseBody);
            }
            catch (Exception ex)
            {
                log.LogError($"Exception occurred: {ex.Message}");
                await UpdateStagingTableStatus(order[0].StagingtableId, "F", ex.Message, log);
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        public static async Task UpdateStagingTableStatus(string stagingtableId, string status, string comment, ILogger log)
        {
            var statusJson = new Dictionary<string, object>
            {
                { "ProcStatus", status}
            };

            CrudData crudData = new CrudData
            {
                Comment = comment,
                CreateDateTime = DateTime.Now,
                DBName = "Moes_data_repository_Test",
                FieldsAndValuesJson = statusJson,
                OperationStatus = "N",
                OperationType = "update",
                PrimaryFieldName = "DocEntry",
                PrimaryFieldValue = stagingtableId,
                SchemaName = "dbo",
                SequentialPrimaryKey = "0",
                TableName = "Wayfair_Order_Staging"
            };

            var requestUrl = Environment.GetEnvironmentVariable("https://prod-24.canadacentral.logic.azure.com:443/workflows/00d6a3a5987b4aa899957ed4d96841a4/triggers/manual/paths/invoke?api-version=2016-10-01&sp=%2Ftriggers%2Fmanual%2Frun&sv=1.0&sig=gxWUtiYBvNe66--IpS-hruioC8rYIfBbWsjgFE4y_Wo");
            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
            {
                Content = new StringContent(JsonConvert.SerializeObject(crudData), Encoding.UTF8, "application/json")
            };

            try
            {
                var response = await httpClient.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    log.LogError($"Failed to update Wayfair_Order_Staging: {responseBody}");
                }
                else
                {
                    log.LogInformation("Successfully updated Wayfair_Order_Staging.");
                }
            }
            catch (Exception ex)
            {
                log.LogError($"Error updating Wayfair_Order_Staging: {ex.Message}");
            }
        }

    }
}
