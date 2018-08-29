using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using Slack.Webhooks;

namespace ProgrammedBot.CheckUserProfileTitles
{
    public static class CheckUserProfileTitles
    {
        const string NineAmEveryTuesday = "0 0 9 * * 2";

        public static readonly HttpClient httpClient = new HttpClient();

        [FunctionName("CheckUserProfileTitles-OnTimer")]
        public async static void Run([TimerTrigger(NineAmEveryTuesday, RunOnStartup = true)]TimerInfo timerInfo, TraceWriter log)
        {
            log.Info("CheckUserProfileTitles-OnTimer trigger.");

            var token = Environment.GetEnvironmentVariable("SlackToken", EnvironmentVariableTarget.Process);
            var usersListUrl = $"https://slack.com/api/users.list?token={token}";

            UsersListResponse response = null;
            try
            {
                using (var stream = await httpClient.GetStreamAsync(usersListUrl))
                {
                    var serializer = new JsonSerializer();
                    using (var sr = new StreamReader(stream))
                    using (var jtr = new JsonTextReader(sr))
                    {
                        response = serializer.Deserialize<UsersListResponse>(jtr);
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("Problem calling Slack's users.list API.", ex);
                return;
            }

            if (response == null || !response.ok)
            {
                log.Error("Problem calling Slack's users.list API - returned no data or ok=false .");
                return;
            }

            var usersWithoutTitle = response.members.Where(m => string.IsNullOrWhiteSpace(m?.profile?.title));
            var meWithoutTitle = usersWithoutTitle.Where(m => m?.profile?.display_name == "benmccallum");

            var slackClient = SlackClientFactory.Create();
            
            // TODO: Use 

            //foreach (var user in meWithoutTitle)
            //{
            //    var slackMessage = new SlackMessage()
            //    {
            //        Channel = $"@{user.profile.display_name}", // will send through slackbot but that's OK...
            //        Text = $"Yo! It looks like you don't have your job title set in your Slack profile. Take one minute and let others know what you do!\n\n" +
            //            "*On desktop* — Click your workspace name in the top left to open the menu, select *Profile & account* and then *Edit Profile*.\n" +
            //            "*On mobile* — Tap the overflow menu button(three dots in the top-right corner), choose *Settings*, and then select *Edit Profile*.\n\n" +
            //            "Thanks!"
            //    };

            //    var success = await slackClient.PostAsync(slackMessage);
            //    log.Info((success ? "Success" : "Error") + $" received from slack webhook for . Message sent was: {slackMessage.Text}");
            //}
        }

        public class UsersListResponse
        {
            public bool ok { get; set; }
            public Member[] members { get; set; }
        }

        public class Member
        {
            public Profile profile { get; set; }
        }

        public class Profile
        {
            public string display_name { get; set; }
            public string title { get; set; }
        }
    }
}
