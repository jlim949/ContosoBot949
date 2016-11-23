using System;
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
                string StockRateString;

                StateClient stateClient = activity.GetStateClient();
                BotData userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);

                string greeting = "Hello";

                // calculate something for us to return
                if (userData.GetProperty<bool>("PreviousUser"))
                {
                    greeting = "Hello";
                }
                else
                {
                    userData.SetProperty<bool>("PreviousUser", true);
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                    Activity greet = activity.CreateReply("Hello, I see that you haven't talked to us before. Welcome!");
                    await connector.Conversations.ReplyToActivityAsync(greet);
                }

                ConvertLUIS StLUIS = await GetEntityFromLUIS(activity.Text);
                if (StLUIS.intents.Count() > 0)
                {
                    switch (StLUIS.intents[0].intent)
                    {
                        case "convertcommand":
                            StockRateString = convertHelp(StLUIS.entities[0].entity);
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
                    helpImage.Add(new CardImage(url: "https://s11.postimg.org/jbsib4b4j/commands.png"));
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
                    endOutput = "This bot will give you an exchange rate from any currency into NZD. Please type what currency you would like to convert FROM. eg.From AUD";

                    isCurrencyRequest = false;
                }

                if (userMessage.ToLower().Contains("update balance"))
                {
                    List<ContosoTable949> timelines = await AzureManager.AzureManagerInstance.GetTimelines();
                    endOutput = "";
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
                            endOutput = "Your balance was successfully updated.";

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
                            endOutput = "[" + account.Date + "] Name: " + account.Name + ", Balance (in NZD): " + account.Balance;
                        }
                        
                    }

                    isCurrencyRequest = false;
                }
                if (userMessage.ToLower().Equals("account"))
                {
                    endOutput = "Type: \n\n New <your name>: to create a new account. \n\n Get <your name>: to view your account.";
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

                    isCurrencyRequest = false;

                    endOutput = "Thanks " + userName + ", you now have a Contoso Bank account!";
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
                    convertImage.Add(new CardImage(url: "https://s16.postimg.org/whosryd4l/8819c8dde4225d105f5ee2204231eeeb.png"));
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
                if (userMessage.ToLower().Equals("contoso"))
                {
                    Activity replyToConversation = activity.CreateReply("Contoso Bank information");
                    replyToConversation.Recipient = activity.From;
                    replyToConversation.Type = "message";
                    replyToConversation.Attachments = new List<Attachment>();
                    List<CardImage> cardImages = new List<CardImage>();
                    cardImages.Add(new CardImage(url: "https://s16.postimg.org/cw2td6g4l/Contoso_Logo.png"));
                    List<CardAction> cardButtons = new List<CardAction>();
                    CardAction plButton = new CardAction()
                    {
                        Value = "http://msa.ms",
                        Type = "openUrl",
                        Title = "Click here!"
                    };
                    cardButtons.Add(plButton);
                    ThumbnailCard plCard = new ThumbnailCard()
                    {
                        Title = "Contoso Bank",
                        Subtitle = "Want to find out more?",
                        Images = cardImages,
                        Buttons = cardButtons
                    };
                    Attachment plAttachment = plCard.ToAttachment();
                    replyToConversation.Attachments.Add(plAttachment);
                    await connector.Conversations.SendToConversationAsync(replyToConversation);

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
        private string convertHelp(string StockSymbol)
        {
            return StockSymbol;
        }
    }
}