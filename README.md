# ProgrammedBot

This bot integrates BitBucket and Slack in the following ways:
* On push to a repository, posts a message to a Slack channel with author's name, repository name, number of commits pushed and the branch name.
* (WIP) On a schedule, can scan a repository for branches that are stale and post a message to a Slack channel with branch name, last commit date and diff to master so that it can potentially be removed.

## Getting started

1. Fork this repo.
2. Create a local.settings.json file with the following contents:
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "AzureWebJobsDashboard": "UseDevelopmentStorage=true",
    "SlackWebhookUrl": "https://hooks.slack.com/services/your-slack-webhook-url-to-post-messages-to"
  }
}
```
3. Create a Slack webhook that can be called to post messages into a channel and set the URL in the `SlackWebhookUrl` configuration.
4. Create an Azure Function with the portal, then configure git/kudu deployment to your forked repo.
5. Commit and push your changes to trigger a deployment.
6. Once the automated deployment completes, copy your Azure Function's `BitBucket-OnPush` URL.
7. Create an "on push" webhook in a BitBucket repository pasting your `BitBucket-OnPush` URL.
8. Commit and push changes to this BitBucket repository and see your Slack "bot" configured earlier report on the push automagically.
