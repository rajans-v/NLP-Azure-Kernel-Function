namespace NLP_Azure_Kernel_Function.Models
{
    internal class AgentResponse
    {
        public string Response { get; set; } = string.Empty;
        public string QueryType { get; set; } = "general";
        public string SourceData { get; set; } = "product_database";
        public List<string> UsedFunctions { get; set; } = new();
        public bool FromCache { get; set; } = false;
    }
}
