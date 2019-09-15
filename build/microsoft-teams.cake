MicrosoftTeamsMessageCard GetMsTeamsCard(string summary, string title, Exception exception = null) {
   return new MicrosoftTeamsMessageCard {
      summary = summary,
      title = exception != null ? $"{title} With Error!" : title,
      sections = exception != null ? new [] {
         new MicrosoftTeamsMessageSection {
            activityImage = "https://emojipedia-us.s3.dualstack.us-west-1.amazonaws.com/thumbs/120/microsoft/153/collision-symbol_1f4a5.png",
            facts = new [] {
               new MicrosoftTeamsMessageFacts { name = "Message", value = exception.Message },
               new MicrosoftTeamsMessageFacts { name = "StackTrace", value = exception.StackTrace }
            }
         }
      } : new [] { 
         new MicrosoftTeamsMessageSection {
            activityImage = "https://emojipedia-us.s3.dualstack.us-west-1.amazonaws.com/thumbs/120/microsoft/153/rocket_1f680.png"
         }
      }
   };
}

void SendMessage(string webhookUrl, MicrosoftTeamsMessageCard messageCard) {
   MicrosoftTeamsPostMessage(messageCard, new MicrosoftTeamsSettings {
      IncomingWebhookUrl = webhookUrl
   });
}