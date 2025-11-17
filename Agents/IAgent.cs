using NLP_Azure_Kernel_Function.Models;

namespace NLP_Azure_Kernel_Function.Agents
{
    internal interface IAgent
    {
        Task<AgentResponse> ProcessAsync(string userInput, ConversationContext context);
    }
}
