using Slack.Webhooks;
using System;

namespace ProgrammedBot
{
    public class SlackClientFactory
    {
        public static SlackClient Create()
        {
            var slackWebhookUrl = Environment.GetEnvironmentVariable("SlackWebhookUrl", EnvironmentVariableTarget.Process);
            return new SlackClient(slackWebhookUrl);
        }
    }
}
