using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Collections.Generic;
using Slack.Webhooks;

namespace ProgrammedBot.BitBucket
{
    public static class OnStaleBranchCheck
    {
        public static readonly HttpClient httpClient = new HttpClient();

        [FunctionName("BitBucket-OnStaleBranchCheck")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequest req, TraceWriter log)
        {
            log.Info("BitBucket-OnStaleBranchCheck fired.");

            var daysAfterWhichToConsiderStale = int.Parse(Environment.GetEnvironmentVariable("BitBucket-DaysAfterWhichToConsiderStale", EnvironmentVariableTarget.Process));
            var staleDateTimeCutoff = DateTime.Now.AddDays(-1 * daysAfterWhichToConsiderStale);
            var numberOfBranchesToSuggest = int.Parse(Environment.GetEnvironmentVariable("BitBucket-NumberOfBranchesToSuggest", EnvironmentVariableTarget.Process));

            var username = Environment.GetEnvironmentVariable("BitBucket-Username", EnvironmentVariableTarget.Process);
            var repoSlug = Environment.GetEnvironmentVariable("BitBucket-RepoSlug", EnvironmentVariableTarget.Process);
            var auth = Environment.GetEnvironmentVariable("BitBucket-Auth", EnvironmentVariableTarget.Process);

            var authByteArray = Encoding.ASCII.GetBytes(auth);
            var authBase64String = Convert.ToBase64String(authByteArray);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authBase64String);

            var repoReport = new RepoReport(repoSlug);

            var apiUrl = $"https://api.bitbucket.org/2.0/repositories/{username}/{repoSlug}/refs/branches";
            do
            {
                var response = await httpClient.GetStringAsync(apiUrl);
                var paginatedBranches = JsonConvert.DeserializeObject<PaginatedBranches>(response);

                repoReport.TotalBranchesCount = paginatedBranches.Size;
                repoReport.StaleBranches.AddRange(paginatedBranches.Values.Where(b => b.Target.Date < staleDateTimeCutoff));

                apiUrl = paginatedBranches.Next;
            }
            while (!string.IsNullOrWhiteSpace(apiUrl));

            if (!repoReport.StaleBranches.Any())
            {
                return new OkObjectResult("Success, no stale branches is any repos checked to report.");
            }

            repoReport.StaleBranches.Sort((a, b) => a.Target.Date.CompareTo(b.Target.Date));
            var oldestStaleBranchesMsg = repoReport.StaleBranches
                .Take(numberOfBranchesToSuggest)
                .Select(b => $" * <{b.Links.Html.Href}|{b.Name}>, last commit was on {b.Target.Date.ToShortDateString()} by {b.Target.Author.User.Display_Name}");

            var slackClient = SlackClientFactory.Create();

            var slackMessage = new SlackMessage()
            {
                Text = $"I've done some digging, and it looks like there's some cleaning to do...\n" +
                    $"The repo '{repoReport.RepoSlug}' has {repoReport.TotalBranchesCount} branches :dizzy_face:, " + 
                    $"{repoReport.StaleBranches.Count} of which haven't seen a commit in the last {daysAfterWhichToConsiderStale} days :zany_face:.\n" +
                    $"Here's the oldest {numberOfBranchesToSuggest} to look at:\n" +
                    string.Join("\n", oldestStaleBranchesMsg)
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
    }

    public class PaginatedBranches
    {
        public int Size { get; set; }
        public string Next { get; set; }
        public List<Branch> Values { get; set; }
    }

    public class Branch
    {
        public string Name { get; set; }
        public Links Links { get; set; }
        public Target Target { get; set; }
    }

    public class Links
    {
        public Link Html { get; set; }
    }

    public class Link
    {
        public string Href { get; set; }
    }

    public class Target
    {
        public Author Author { get; set; }
        public DateTime Date { get; set; }
    }

    public class Author
    {
        public string Raw { get; set; }
        public User User { get; set; }

    }

    public class User
    {
        public string Username { get; set; }
        public string Display_Name { get; set; }
    }

    public class RepoReport
    {
        public string RepoSlug { get; set; }
        public int TotalBranchesCount { get; set; }
        public List<Branch> StaleBranches { get; set; }

        public RepoReport(string repoSlug)
        {
            RepoSlug = repoSlug;
            StaleBranches = new List<Branch>();
        }
    }
}
