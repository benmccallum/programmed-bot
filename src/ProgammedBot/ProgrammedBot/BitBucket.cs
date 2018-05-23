using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using Slack.Webhooks;
using System.Linq;
using System.Collections.Generic;

namespace ProgrammedBot
{
    public static class BitBucket
    {
        [FunctionName("BitBucket")]
        public async static Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequest req, TraceWriter log)
        {           
            var requestBody = new StreamReader(req.Body).ReadToEnd();

            var data = JsonConvert.DeserializeObject<PushNotification>(requestBody);

            var dataJson = JsonConvert.SerializeObject(data);
            log.Info("Received a request.");

            var author = data.actor.display_name;
            var changes = data.push.changes;
            var numOfCommits = changes.Sum(c => Math.Max(c.truncated ? 5 : c.commits.Count, c.commits.Count));
            var couldBeMoreCommits = changes.Any(c => c.truncated);
            var repository = data.repository.name;

            var slackWebhookUrl = Environment.GetEnvironmentVariable("SlackWebhookUrl", EnvironmentVariableTarget.Process);
            log.Info($"SlackWebhookUrl: {slackWebhookUrl}");

            var slackClient = new SlackClient(slackWebhookUrl);

            var slackMessage = new SlackMessage
            {
                Text = $":medal: {author} :medal: just pushed {(couldBeMoreCommits ? "at least " : "")}{numOfCommits} commit{(numOfCommits > 1 ? "s" : "")} to the {repository} repository. :clap::clap::clap:"
            };

            var success = await slackClient.PostAsync(slackMessage);
            if (success)
            {
                var successMsg = "Success received from slack webhook. Message sent was: " + slackMessage.Text;
                log.Info(successMsg);
                return new OkObjectResult(successMsg);
            }
            else
            {
                log.Info("Error received from slack webhook. Message sent was: " + slackMessage.Text);
                return new StatusCodeResult(500);
            }
        }

        public class PushNotification
        {
            public Push push { get; set; }
            public Repository repository { get; set; }
            public Actor actor { get; set; }
        }

        public class Push
        {
            public List<Change> changes { get; set; }
        }

        public class Change
        {
            public bool truncated { get; set; }
            public List<Commit> commits { get; set; }
        }

        public class Commit
        {
            public string hash { get; set; }
            public DateTime date { get; set; }
            public string message { get; set; }
            public string type { get; set; }
        }

        public class Repository
        {
            public string website { get; set; }
            public string name { get; set; }
        }

        public class Actor
        {
            public string username { get; set; }
            public string display_name { get; set; }
        }        
    }
}
