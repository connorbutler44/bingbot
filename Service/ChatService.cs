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
                    ChatMessage.FromSystem("You are a helpful assistant. Your name is Bingbot"),
                    ChatMessage.FromSystem($"The users name is {user.Username}"),
                    ChatMessage.FromSystem($"Responses should be concise")
                };
            }

            return userHistory;
        }
    }
}