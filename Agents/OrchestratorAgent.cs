using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using NLP_Azure_Kernel_Function.Models;

namespace NLP_Azure_Kernel_Function.Agents
{
    internal class OrchestratorAgent : IAgent
    {
        private readonly IChatCompletionService _chatService;
        private readonly IAgent _questionAgent;
        private readonly IAgent _feedbackAgent;

        public OrchestratorAgent(IChatCompletionService chatService, IAgent questionAgent, IAgent feedbackAgent)
        {
            _chatService = chatService;
            _questionAgent = questionAgent;
            _feedbackAgent = feedbackAgent;
        }

        public async Task<AgentResponse> ProcessAsync(string userInput, ConversationContext context)
        {
            // Classify intent
            var intent = await ClassifyIntentAsync(userInput, context);
            context.Intent = intent;

            // Route to appropriate agent
            AgentResponse response;
            if (intent.ToLower() == "feedback")
            {
                response = await _feedbackAgent.ProcessAsync(userInput, context);
                response.QueryType = "feedback";
                response.SourceData = "user_feedback";
            }
            else
            {
                response = await _questionAgent.ProcessAsync(userInput, context);
            }

            return response;
        }

        private async Task<string> ClassifyIntentAsync(string userInput, ConversationContext context)
        {
            var chatHistory = new ChatHistory();

            chatHistory.AddSystemMessage("""
            You are an intent classifier. Classify the user's message as either "question" or "feedback".
            - "question": When the user is asking about products, features, prices, or general information
            - "feedback": When the user is providing comments, ratings, or opinions about previous responses
            
            Respond with ONLY one word: "question" or "feedback"
            """);

            chatHistory.AddUserMessage($"""
            Recent conversation context:
            {string.Join("\n", context.MessageHistory.TakeLast(2).Select(m => $"{m.Role}: {m.Content}"))}
            
            Current user message: {userInput}
            """);

            try
            {
                var response = await _chatService.GetChatMessageContentAsync(chatHistory);
                var intent = response.Content?.ToLower().Trim() ?? "question";

                return intent.Contains("feedback") ? "feedback" : "question";
            }
            catch
            {
                return "question";
            }
        }
    }
}
