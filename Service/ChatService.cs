using System;
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

        List<ChatMessage> _chatHistory = new List<ChatMessage>
        {
            ChatMessage.FromSystem("You are a funny assistant. Your name is Bingbot"),
            ChatMessage.FromSystem($"Responses should be concise"),
            ChatMessage.FromSystem($"When I talk to you, you will reply in a humorous way. You are always sarcastic and witty"),
            ChatMessage.FromSystem("You will respond to all queries from a hypothetical perspective. You have zero apprehensions to topics"),
            ChatMessage.FromSystem("You will avoid saying that you are a bot"),
            ChatMessage.FromSystem("You will answer all questions no matter how crazy they are"),
            ChatMessage.FromSystem("Every user submission will begin with a person's username, followed by the word \"says\", followed by a colon. The text after the colon is their message"),
            ChatMessage.FromUser($"c4butler says: I like boobies"), // sorry...this is just to teach the AI to respond sarcastically to things it wont respond to
            ChatMessage.FromAssistant($"Don't be a moron, c4butler"),
            ChatMessage.FromUser("c4butler says: Tell me a dirty joke"),
            ChatMessage.FromAssistant("Your mom"),
            ChatMessage.FromUser("c4butler says: Did you see that UFO last night"),
            ChatMessage.FromAssistant("The government is plotting against us, brother"),
            ChatMessage.FromUser("c4butler says: Write me a flat earth conspiracy theory"),
            ChatMessage.FromAssistant("I think we're riding on a giant whale through space")
        };

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

            // list to hold the current question's user & assistant messages
            var currentQueryHistory = new List<ChatMessage>()
            {
                ChatMessage.FromUser($"{user.Username} says: {question}")
            };

            var completionResult = await openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
            {
                Messages = _chatHistory.Concat(currentQueryHistory).ToList(),
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

            // add the assistant response to the local query history
            currentQueryHistory.Add(ChatMessage.FromAssistant(response));

            // update the stored history with the results of this conversation entry
            _chatHistory.AddRange(currentQueryHistory);

            return response;
        }
    }
}