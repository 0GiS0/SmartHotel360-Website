using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Extensions.Logging;

namespace SmartHotel360.WebsiteFunction
{
    public static class PetChecker
    {
        [FunctionName("PetChecker")]
        public static async Task RunAsync(
            [CosmosDBTrigger(databaseName: "pets", collectionName: "checks", ConnectionStringSetting = "constr", LeaseCollectionName = "leases")]IReadOnlyList<Document> document,
            [SignalR(HubName = "petcheckin", ConnectionStringSetting = "AzureSignalRConnectionString")] IAsyncCollector<SignalRMessage> sender,
            ILogger log)
        {
            var sendingResponse = false;
            try
            {
                foreach (dynamic doc in document)
                {
                    sendingResponse = false;
                    var isProcessed = doc.IsApproved != null;
                    if (isProcessed)
                    {
                        continue;
                    }

                    var url = doc.MediaUrl.ToString();
                    var uploaded = (DateTime)doc.Created;
                    log.LogInformation($">>> Processing image in {url} upladed at {uploaded.ToString()}");

                    using (var httpClient = new HttpClient())
                    {

                        var res = await httpClient.GetAsync(url);
                        var stream = await res.Content.ReadAsStreamAsync() as Stream;
                        log.LogInformation($"--- Image succesfully downloaded from storage");
                        var (allowed, message, tags) = await PassesImageModerationAsync(stream, log);
                        log.LogInformation($"--- Image analyzed. It was {(allowed ? string.Empty : "NOT")} approved");
                        doc.IsApproved = allowed;
                        doc.Message = message;
                        log.LogInformation($"--- Updating CosmosDb document to have historical data");
                        await UpsertDocument(doc, log);
                        log.LogInformation("--- Sending SignalR response.");
                        sendingResponse = true;
                        await SendSignalRResponse(sender, allowed, message);
                        log.LogInformation($"<<< Done! Image in {url} processed!");
                    }
                }
            }
            catch (Exception ex)
            {
                var msg = $"Error {ex.Message} ({ex.GetType().Name})";
                log.LogInformation("!!! " + msg);

                if (ex is AggregateException aggex)
                {
                    foreach (var innex in aggex.InnerExceptions)
                    {
                        log.LogInformation($"!!! (inner) Error {innex.Message} ({innex.GetType().Name})");
                    }
                }

                if (!sendingResponse)
                {
                    await SendSignalRResponse(sender, false, msg);
                }
                throw ex;
            }
        }

        private static Task SendSignalRResponse(IAsyncCollector<SignalRMessage> sender, bool isOk, string message)
        {
            return sender.AddAsync(new SignalRMessage()
            {
                Target = "ProcessDone",
                Arguments = new[] { new {
                    processedAt = DateTime.UtcNow,
                    accepted = isOk,
                    message
                }}
            });
        }

        private static async Task UpsertDocument(dynamic doc, ILogger log)
        {
            var endpoint = GetSecret("cosmos_uri");
            var auth = GetSecret("cosmos_key");

            var client = new DocumentClient(new Uri(endpoint), auth);
            var dbName = "pets";
            var colName = "checks";
            doc.Analyzed = DateTime.UtcNow;
            await client.UpsertDocumentAsync(UriFactory.CreateDocumentCollectionUri(dbName, colName), doc);
            log.LogInformation($"--- CosmosDb document updated.");
        }

        private static string GetSecret(string secretName)
        {

            return Environment.GetEnvironmentVariable(secretName);
        }

        public static async Task<(bool allowd, string message, string[] tags)> PassesImageModerationAsync(Stream image, ILogger log)
        {
            try
            {
                log.LogInformation("--- Creating VisionApi client and analyzing image");

                var key = GetSecret("MicrosoftVisionApiKey");
                var endpoint = GetSecret("MicrosoftVisionApiEndpoint");
                var numTags = GetSecret("MicrosoftVisionNumTags");

                // Specify the features to return
                var features = new List<VisualFeatureTypes>() { VisualFeatureTypes.Description };

                var computerVision = new ComputerVisionClient(new ApiKeyServiceClientCredentials(key), new DelegatingHandler[] { })
                {
                    Endpoint = endpoint
                };

                var result = await computerVision.AnalyzeImageInStreamAsync(image, features);

                log.LogInformation($"--- Image analyzed with tags: {string.Join(",", result.Description.Tags)}");
                if (!int.TryParse(numTags, out var tagsToFetch))
                {
                    tagsToFetch = 5;
                }

                var fetchedTags = result?.Description?.Tags.Take(tagsToFetch).ToArray() ?? new string[0];

                bool isAllowed = fetchedTags.Contains("dog") || fetchedTags.Contains("cat");

                string message = result?.Description?.Captions.FirstOrDefault()?.Text;
                return (isAllowed, message, fetchedTags);
            }
            catch (Exception ex)
            {
                log.LogInformation("Vision API error! " + ex.Message);
                return (false, "error " + ex.Message, new string[0]);
            }
        }
    }
}
