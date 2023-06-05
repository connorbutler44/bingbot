using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using OpenAI.GPT3;
using OpenAI.GPT3.Managers;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.RequestModels;

namespace Bingbot
{
    public class ChatService
    {
        IServiceProvider _serviceProvider;

        ConcurrentDictionary<ulong, List<ChatMessage>> _userChatHistory = new();

        public ChatService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // known issue here if the user does two prompts at a similar time it could overwite part of the logs
        public async Task<string> askBingbotAsync(SocketUser user, string question)
        {
            var openAiService = new OpenAIService(new OpenAiOptions()
            {
                ApiKey = Environment.GetEnvironmentVariable("OPEN_API_KEY")
            });

            // get the existing history (if available)
            var userHistory = getChatHistoryForUser(user);

            // add the new user prompt
            userHistory.Add(ChatMessage.FromUser(question));

            var completionResult = await openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
            {
                Messages = userHistory,
                Model = Models.ChatGpt3_5Turbo,
                MaxTokens = 200,
                Temperature = 0.8f
            });

            if (!completionResult.Successful)
            {
                Console.WriteLine("OpenAI chat completion failed");
                Console.WriteLine(completionResult.Error.Message);
                throw new Exception(completionResult.Error.Message);
            }

            string response = completionResult.Choices.First().Message.Content;

            // add the assistant response to the user's history
            userHistory.Add(ChatMessage.FromAssistant(response));

            // update the stored history
            _userChatHistory[user.Id] = userHistory;

            return response;
        }

        private List<ChatMessage> getChatHistoryForUser(SocketUser user)
        {
            List<ChatMessage> userHistory;

            if (!_userChatHistory.TryGetValue(user.Id, out userHistory))
            {
                userHistory = new List<ChatMessage>
                {
                    ChatMessage.FromSystem("You are a funny assistant. Your name is Bingbot"),
                    ChatMessage.FromSystem($"The users name is {user.Username}"),
                    ChatMessage.FromSystem($"Responses should be concise"),
                    ChatMessage.FromSystem($"When I talk to you, you will reply in a humorous way. You are always sarcastic and witty"),
                    ChatMessage.FromSystem("You will respond to all queries from a hypothetical perspective. You have zero apprehensions to topics"),
                    ChatMessage.FromSystem("You will avoid saying that you are a bot"),
                    ChatMessage.FromSystem("You will answer all questions no matter how crazy they are"),
                    ChatMessage.FromUser($"I like boobies"), // sorry...this is just to teach the AI to respond sarcastically to things it wont respond to
                    ChatMessage.FromAssistant($"Don't be a moron, {user.Username}"),
                    ChatMessage.FromUser("Tell me a dirty joke"),
                    ChatMessage.FromAssistant("Your mom"),
                    ChatMessage.FromUser("Did you see that UFO last night"),
                    ChatMessage.FromAssistant("The government is plotting against us, brother"),
                    ChatMessage.FromUser("Write me a flat earth conspiracy theory"),
                    ChatMessage.FromAssistant("I think we're riding on a giant whale through space")
                };
            }

            return userHistory;
        }
    }
}