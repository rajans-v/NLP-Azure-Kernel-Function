using Microsoft.SemanticKernel.ChatCompletion;
using NLP_Azure_Kernel_Function.Models;
using NLP_Azure_Kernel_Function.Services;

namespace NLP_Azure_Kernel_Function.Agents
{
    internal class FeedbackAgent : IAgent
    {
        private readonly IChatCompletionService _chatService;
        private readonly IDataService _dataService;

        public FeedbackAgent(IChatCompletionService chatService, IDataService dataService)
        {
            _chatService = chatService;
            _dataService = dataService;
        }

        public async Task<AgentResponse> ProcessAsync(string userInput, ConversationContext context)
        {
            // Extract rating and feedback
            var (rating, feedbackText) = await ExtractFeedbackAsync(userInput);

            // Store feedback
            if (!string.IsNullOrEmpty(context.PreviousResponseId))
            {
                var feedback = new Feedback
                {
                    SessionId = context.SessionId,
                    ResponseId = context.PreviousResponseId,
                    FeedbackText = feedbackText,
                    Rating = rating
                };

                await _dataService.StoreFeedbackAsync(feedback);
            }

            return new AgentResponse
            {
                Response = "Thank you for your feedback! It helps us improve our service.",
                QueryType = "feedback",
                SourceData = "user_feedback"
            };
        }

        private async Task<(int rating, string feedback)> ExtractFeedbackAsync(string userInput)
        {
            var chatHistory = new ChatHistory($"""
            Extract rating (1-5) and feedback text from the user message.
            If no clear rating, default to 3.
            
            User message: {userInput}
            
            Respond in format: "rating|feedback text"
            """);

            var response = await _chatService.GetChatMessageContentAsync(chatHistory);
            var parts = response.Content?.Split('|', 2) ?? new[] { "3", userInput };

            if (int.TryParse(parts[0], out int rating) && rating >= 1 && rating <= 5)
            {
                return (rating, parts.Length > 1 ? parts[1] : userInput);
            }

            return (3, userInput);
        }
    }
}
