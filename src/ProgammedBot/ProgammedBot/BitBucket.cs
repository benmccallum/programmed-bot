using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System.Net.Http;
using System;
using System.Threading.Tasks;

namespace ProgammedBot
{
    public static class BitBucket
    {
        public static HttpClient httpClient = new HttpClient();

        [FunctionName("BitBucket")]
        public async static Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequest req, TraceWriter log)
        {           
            var requestBody = new StreamReader(req.Body).ReadToEnd();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            var dataJson = JsonConvert.SerializeObject(data);
            log.Info("C# HTTP trigger function processed a request. " + dataJson);


            var response = await httpClient.PostAsJsonAsync<dynamic>(
                Environment.GetEnvironmentVariable("SlackWebhookUrl"),
                new
                {
                    payload = JsonConvert.SerializeObject(
                        new
                        {
                            text = dataJson
                        }
                    )
                }
            );

            return new OkResult();
        }
    }
}
