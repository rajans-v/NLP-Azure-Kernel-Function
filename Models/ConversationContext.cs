namespace NLP_Azure_Kernel_Function.Models
{
    internal class ConversationContext
    {
        public string SessionId { get; set; } = string.Empty;
        public string Intent { get; set; } = "question"; // "question" or "feedback"
        public List<ChatMessage> MessageHistory { get; set; } = new();
        public string? PreviousResponseId { get; set; }
        public DateTime LastActivity { get; set; } = DateTime.UtcNow;
        public string? LastBearingDesignation { get; set; } // Track last discussed bearing
        public List<string> RecentSearches { get; set; } = new(); // Track recent search terms
    }

    internal class ChatMessage
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? Metadata { get; set; } // Optional: store additional context
    }
}
