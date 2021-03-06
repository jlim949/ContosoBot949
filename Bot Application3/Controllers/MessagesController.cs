﻿using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using Bot_Application3.Models;
using System.Collections.Generic;
using Microsoft.WindowsAzure.MobileServices;

namespace Bot_Application3
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        public Boolean convertFound = false;
        public Boolean helpFound = false;
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            bool isCurrencyRequest = true;
            var userMessage = activity.Text;
            string source = "";
            string endOutput = "Sorry, we don't quite get what you mean. Type something else?";
            if (activity.Type == ActivityTypes.Message)
            {
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));

                StateClient stateClient = activity.GetStateClient();
                BotData userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);

                // calculate something for us to return
                if (!userData.GetProperty<bool>("PreviousUser"))
                {
                    userData.SetProperty<bool>("PreviousUser", true);
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                    Activity greet = activity.CreateReply("Hello " + activity.From.Name + ", I don't think you've talked to us before. Welcome!");
                    await connector.Conversations.ReplyToActivityAsync(greet);
                    helpFound = true;
                }

                ConvertLUIS StLUIS = await GetEntityFromLUIS(activity.Text);
                if (StLUIS.intents.Count() > 0)
                {
                    switch (StLUIS.intents[0].intent)
                    {
                        case "convertcommand":
                            convertFound = true;
                            isCurrencyRequest = false;
                            break;
                        case "helpcommand":
                            helpFound = true;
                            isCurrencyRequest = false;
                            break;
                        default:
                            convertFound = false;
                            isCurrencyRequest = false;
                            break;
                    }
                }
                if (helpFound == true)
                {
                    endOutput = "Here are some of the commands you can type - you can acheive a lot with this bot!";
                    isCurrencyRequest = false;
                    Activity helpReply = activity.CreateReply($"");
                    helpReply.Recipient = activity.From;
                    helpReply.Type = "message";
                    helpReply.Attachments = new List<Attachment>();
                    List<CardImage> helpImage = new List<CardImage>();
                    helpImage.Add(new CardImage(url: "https://s3.postimg.org/yewfjbroj/command_Box.png"));
                    HeroCard conversionCard = new HeroCard()
                    {
                        Images = helpImage
                    };
                    Attachment plAttachment = conversionCard.ToAttachment();
                    helpReply.Attachments.Add(plAttachment);
                    await connector.Conversations.ReplyToActivityAsync(helpReply);
                }

                if (convertFound == true)
                {
                    endOutput = "This bot will give you an exchange rate from any currency into NZD. Please type what currency you would like to convert FROM. eg. From AUD";

                    isCurrencyRequest = false;
                }

                if (userMessage.ToLower().Contains("clear"))
                {
                    endOutput = "Your data has been cleared. Thanks " + activity.From.Name;
                    await stateClient.BotState.DeleteStateForUserAsync(activity.ChannelId, activity.From.Id);
                    helpFound = false;
                }

                if (userMessage.ToLower().Contains("update balance"))
                {
                    List<ContosoTable949> timelines = await AzureManager.AzureManagerInstance.GetTimelines();
                    string[] inputs = userMessage.Split(' ');
                    string newBalance = inputs[3];
                    double doubleBalance = Convert.ToDouble(newBalance);
                    endOutput = "Sorry, there is no account under that name.";
                    foreach (ContosoTable949 account in timelines)
                    {
                        if (inputs[2].ToLower() == account.Name.ToLower())
                        {
                            account.Balance = doubleBalance;
                            await AzureManager.AzureManagerInstance.UpdateTimeline(account);
                            endOutput = "Your balance was successfully updated. Do you wish to view your account?";

                            Activity ynReply = activity.CreateReply($"");
                            ynReply.Recipient = activity.From;
                            ynReply.Type = "message";
                            ynReply.Attachments = new List<Attachment>();
                            List<CardAction> ynbuttons = new List<CardAction>();
                            CardAction yesButton = new CardAction()
                            {
                                Value = "get " + inputs[2],
                                Type = "imBack",
                                Title = "Yes"
                            };
                            ynbuttons.Add(yesButton);
                            CardAction noButton = new CardAction()
                            {
                                Value = "Bye",
                                Type = "imBack",
                                Title = "No"
                            };
                            ynbuttons.Add(noButton);
                            HeroCard conversionCard = new HeroCard()
                            {
                                Title = "View account?",
                                Buttons = ynbuttons
                            };
                            Attachment plAttachment = conversionCard.ToAttachment();
                            ynReply.Attachments.Add(plAttachment);
                            await connector.Conversations.ReplyToActivityAsync(ynReply);


                        }
                    }

                    isCurrencyRequest = false;
                }

                if (userMessage.ToLower().Contains("deposit"))
                {
                    List<ContosoTable949> timelines = await AzureManager.AzureManagerInstance.GetTimelines();
                    string[] inputs = userMessage.Split(' ');
                    string newBalance = inputs[2];
                    double doubleBalance = Convert.ToDouble(newBalance);
                    endOutput = "Sorry, there is no account under that name.";
                    foreach (ContosoTable949 account in timelines)
                    {
                        if (inputs[1].ToLower() == account.Name.ToLower())
                        {
                            account.Balance = account.Balance + doubleBalance;
                            await AzureManager.AzureManagerInstance.UpdateTimeline(account);
                            endOutput = "Your balance was successfully updated. Do you wish to view your account?";

                            Activity ynReply = activity.CreateReply($"");
                            ynReply.Recipient = activity.From;
                            ynReply.Type = "message";
                            ynReply.Attachments = new List<Attachment>();
                            List<CardAction> ynbuttons = new List<CardAction>();
                            CardAction yesButton = new CardAction()
                            {
                                Value = "get " + inputs[1],
                                Type = "imBack",
                                Title = "Yes"
                            };
                            ynbuttons.Add(yesButton);
                            CardAction noButton = new CardAction()
                            {
                                Value = "Bye",
                                Type = "imBack",
                                Title = "No"
                            };
                            ynbuttons.Add(noButton);
                            HeroCard conversionCard = new HeroCard()
                            {
                                Title = "View account?",
                                Buttons = ynbuttons
                            };
                            Attachment plAttachment = conversionCard.ToAttachment();
                            ynReply.Attachments.Add(plAttachment);
                            await connector.Conversations.ReplyToActivityAsync(ynReply);


                        }
                    }

                    isCurrencyRequest = false;
                }

                if (userMessage.ToLower().Contains("withdraw"))
                {
                    List<ContosoTable949> timelines = await AzureManager.AzureManagerInstance.GetTimelines();
                    string[] inputs = userMessage.Split(' ');
                    string newBalance = inputs[2];
                    double doubleBalance = Convert.ToDouble(newBalance);
                    endOutput = "Sorry, there is no account under that name.";
                    foreach (ContosoTable949 account in timelines)
                    {
                        if (inputs[1].ToLower() == account.Name.ToLower())
                        {
                            account.Balance = account.Balance - doubleBalance;
                            await AzureManager.AzureManagerInstance.UpdateTimeline(account);
                            endOutput = "Your balance was successfully updated. Do you wish to view your account?";

                            Activity ynReply = activity.CreateReply($"");
                            ynReply.Recipient = activity.From;
                            ynReply.Type = "message";
                            ynReply.Attachments = new List<Attachment>();
                            List<CardAction> ynbuttons = new List<CardAction>();
                            CardAction yesButton = new CardAction()
                            {
                                Value = "get " + inputs[1],
                                Type = "imBack",
                                Title = "Yes"
                            };
                            ynbuttons.Add(yesButton);
                            CardAction noButton = new CardAction()
                            {
                                Value = "Bye",
                                Type = "imBack",
                                Title = "No"
                            };
                            ynbuttons.Add(noButton);
                            HeroCard conversionCard = new HeroCard()
                            {
                                Title = "View account?",
                                Buttons = ynbuttons
                            };
                            Attachment plAttachment = conversionCard.ToAttachment();
                            ynReply.Attachments.Add(plAttachment);
                            await connector.Conversations.ReplyToActivityAsync(ynReply);
                        }
                    }

                    isCurrencyRequest = false;
                }

                if (userMessage.ToLower().Contains("delete"))
                {
                    List<ContosoTable949> timelines = await AzureManager.AzureManagerInstance.GetTimelines();
                    string userName = userMessage.Substring(7);
                    endOutput = "Sorry, there is no account under that name.";
                    foreach (ContosoTable949 row in timelines)
                    {
                        if (userName.ToLower() == row.Name.ToLower())
                        {
                            await AzureManager.AzureManagerInstance.DeleteTimeline(row);
                            endOutput = "Your account was successfully deleted.";

                        }
                    }

                    isCurrencyRequest = false;
                }

                if (userMessage.ToLower().Contains("get"))
                {
                    List<ContosoTable949> timelines = await AzureManager.AzureManagerInstance.GetTimelines();
                    endOutput = "Sorry, there is no account under that name.";
                    string name = userMessage.ToLower().Substring(4);
                    foreach (ContosoTable949 account in timelines)
                    {
                        if (name == account.Name.ToLower())
                        {
                            endOutput = "Name: " + account.Name + ", Balance (in NZD): " + account.Balance;
                        }
                        
                    }

                    isCurrencyRequest = false;
                }
                if (userMessage.ToLower().Equals("account"))
                {
                    endOutput = "Type: *New/Get/Delete Your Name* to create/view/delete your account. You can also type *Update balance*, *Deposit* or *Withdraw* followed by your name and amount of money to update your balance. EXAMPLE. New James, Update balance Sam 500, etc";
                    isCurrencyRequest = false;
                }
                if (userMessage.ToLower().Contains("new"))
                {
                    string userName = userMessage.Substring(4);
                    ContosoTable949 timeline = new ContosoTable949()
                    {
                        Name = userName,
                        Balance = 0,
                        Date = DateTime.Now
                    };

                    await AzureManager.AzureManagerInstance.AddTimeline(timeline);

                    Activity ynReply = activity.CreateReply($"");
                    ynReply.Recipient = activity.From;
                    ynReply.Type = "message";
                    ynReply.Attachments = new List<Attachment>();
                    List<CardAction> ynbuttons = new List<CardAction>();
                    CardAction yesButton = new CardAction()
                    {
                        Value = "get " + userName,
                        Type = "imBack",
                        Title = "Yes"
                    };
                    ynbuttons.Add(yesButton);
                    CardAction noButton = new CardAction()
                    {
                        Value = "Help",
                        Type = "imBack",
                        Title = "No"
                    };
                    ynbuttons.Add(noButton);
                    HeroCard conversionCard = new HeroCard()
                    {
                        Title = "View account?",
                        Buttons = ynbuttons
                    };
                    Attachment plAttachment = conversionCard.ToAttachment();
                    ynReply.Attachments.Add(plAttachment);
                    await connector.Conversations.ReplyToActivityAsync(ynReply);

                    isCurrencyRequest = false;

                    endOutput = "Thanks " + userName + ", you now have a Contoso Bank account!";
                }

                if (userMessage.ToLower().Equals("bye"))
                {
                    endOutput = "Thanks for using this bot! Feel free to talk to us again anytime you wish.";
                    isCurrencyRequest = false;
                }
                if (userMessage.ToLower().Contains("from"))
                {
                    source = userMessage.Substring(5);
                    isCurrencyRequest = true;

                    currencyObject.RootObject rootObject;

                    HttpClient client = new HttpClient();
                    string x = await client.GetStringAsync(new Uri("http://api.fixer.io/latest?base=" + source.ToUpper()));

                    rootObject = JsonConvert.DeserializeObject<currencyObject.RootObject>(x);

                    double conversion = rootObject.rates.NZD;
                    //Activity reply = activity.CreateReply($"The current {source.ToUpper()} to NZD rate is {conversion}");
                    Activity reply = activity.CreateReply($"");
                    reply.Recipient = activity.From;
                    reply.Type = "message";
                    reply.Attachments = new List<Attachment>();
                    List<CardImage> convertImage = new List<CardImage>();
                    convertImage.Add(new CardImage(url: "https://s15.postimg.org/630c7wm5n/Untitled_2.png"));
                    ThumbnailCard conversionCard = new ThumbnailCard()
                    {
                        Title = conversion.ToString(),
                        Subtitle = "Exchange rate from " + source.ToUpper() + " to NZD",
                        Images = convertImage
                    };
                    Attachment plAttachment = conversionCard.ToAttachment();
                    reply.Attachments.Add(plAttachment);
                    await connector.Conversations.ReplyToActivityAsync(reply);

                }
                if (userMessage.ToLower().Contains("contoso"))
                {
                    Activity contosoReply = activity.CreateReply("Contoso Bank information");
                    contosoReply.Recipient = activity.From;
                    contosoReply.Type = "message";
                    contosoReply.Attachments = new List<Attachment>();
                    List<CardImage> cardImages = new List<CardImage>();
                    cardImages.Add(new CardImage(url: "https://s14.postimg.org/9znwwgaw1/Contoso_Logo.png"));
                    List<CardAction> cardButtons = new List<CardAction>();
                    CardAction contosoButton = new CardAction()
                    {
                        Value = "https://www.asb.co.nz",
                        Type = "openUrl",
                        Title = "Click here!"
                    };
                    cardButtons.Add(contosoButton);
                    ThumbnailCard contosoCard = new ThumbnailCard()
                    {
                        Title = "Contoso Bank",
                        Subtitle = "Want to find out more?",
                        Images = cardImages,
                        Buttons = cardButtons
                    };
                    Attachment plAttachment = contosoCard.ToAttachment();
                    contosoReply.Attachments.Add(plAttachment);
                    await connector.Conversations.SendToConversationAsync(contosoReply);

                    return Request.CreateResponse(HttpStatusCode.OK);

                }

                if (!isCurrencyRequest)
                {
                    Activity initialReply = activity.CreateReply(endOutput);
                    await connector.Conversations.ReplyToActivityAsync(initialReply);
                }
            }
            else
            {
                HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }
        private static async Task<ConvertLUIS> GetEntityFromLUIS(string Query)
        {
            Query = Uri.EscapeDataString(Query);
            ConvertLUIS Data = new ConvertLUIS();
            using (HttpClient client = new HttpClient())
            {
                string RequestURI = "https://api.projectoxford.ai/luis/v2.0/apps/1bf75df6-67b4-42a6-ad23-ae7c18daaac0?subscription-key=1300e7e24fac4e0c9edf5cfeb07ccdab&q=" + Query + " &verbose=true";
                HttpResponseMessage msg = await client.GetAsync(RequestURI);

                if (msg.IsSuccessStatusCode)
                {
                    var JsonDataResponse = await msg.Content.ReadAsStringAsync();
                    Data = JsonConvert.DeserializeObject<ConvertLUIS>(JsonDataResponse);
                }
            }
            return Data;
        }
    }
}