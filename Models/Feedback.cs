namespace NLP_Azure_Kernel_Function.Models
{
    internal class Feedback
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string SessionId { get; set; } = string.Empty;
        public string ResponseId { get; set; } = string.Empty;
        public string FeedbackText { get; set; } = string.Empty;
        public int Rating { get; set; } // 1-5
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
