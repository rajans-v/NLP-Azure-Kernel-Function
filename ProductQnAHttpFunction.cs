using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using NLP_Azure_Kernel_Function.Agents;
using NLP_Azure_Kernel_Function.Models;
using NLP_Azure_Kernel_Function.Services;
using System.Net;
using System.Text;
using System.Text.Json;
namespace NLP_Azure_Kernel_Function;


internal class ProductQnAHttpFunction
{
    private readonly IAgent _orchestratorAgent;
    private readonly IRedisService _redisService;
    private readonly ILogger _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ProductQnAHttpFunction(IAgent orchestratorAgent, IRedisService redisService, ILoggerFactory loggerFactory)
    {
        _orchestratorAgent = orchestratorAgent;
        _redisService = redisService;
        _logger = loggerFactory.CreateLogger<ProductQnAHttpFunction>();
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    [Function("ProductQnA")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "SKF-Mini-Product-Assistant")] HttpRequestData req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a bearing product query.");

        try
        {
            var request = await ReadRequestAsync(req);
            if (request == null || string.IsNullOrEmpty(request.Message))
            {
                return await CreateErrorResponseAsync(req, HttpStatusCode.BadRequest, "Invalid request: Message is required and cannot be empty");
            }

            var sessionId = request.SessionId ?? Guid.NewGuid().ToString();
            var context = await GetOrCreateContextAsync(sessionId);

            _logger.LogInformation($"Processing message for session {sessionId}: {request.Message}");

            // Process message through orchestrator - now returns AgentResponse
            var agentResponse = await _orchestratorAgent.ProcessAsync(request.Message, context);

            // Update conversation history
            context.MessageHistory.Add(new ChatMessage { 
                Role = "user", 
                Content = request.Message 
            });
            context.MessageHistory.Add(new ChatMessage { 
                Role = "assistant", 
                Content = agentResponse.Response,
                Metadata = $"Type:{agentResponse.QueryType}, Source:{agentResponse.SourceData}"
            });
            context.LastActivity = DateTime.UtcNow;

            if (context.MessageHistory.Count > 20)
            {
                context.MessageHistory = context.MessageHistory.Skip(context.MessageHistory.Count - 20).ToList();
            }

            await _redisService.SetAsync($"session:{sessionId}", context, TimeSpan.FromHours(24));

            // Return response with metadata from agent
            return await CreateJsonResponseAsync(req, HttpStatusCode.OK, new ChatResponse
            {
                SessionId = sessionId,
                Response = agentResponse.Response,
                Timestamp = DateTime.UtcNow,
                QueryType = agentResponse.QueryType,
                SourceData = agentResponse.SourceData
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing bearing product query");
            return await CreateErrorResponseAsync(req, HttpStatusCode.InternalServerError, 
                "An error occurred while processing your bearing product query. Please try again.");
        }
    }

    private async Task<ChatRequest?> ReadRequestAsync(HttpRequestData req)
    {
        try
        {
            using var reader = new StreamReader(req.Body);
            var requestBody = await reader.ReadToEndAsync();
            
            if (string.IsNullOrEmpty(requestBody))
            {
                _logger.LogWarning("Empty request body received");
                return null;
            }

            var request = JsonSerializer.Deserialize<ChatRequest>(requestBody, _jsonOptions);
            
            if (request != null)
            {
                request.Message = request.Message?.Trim() ?? string.Empty;
                request.SessionId = request.SessionId?.Trim();
            }

            return request;
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "JSON parsing error in request body");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading request body");
            return null;
        }
    }

    private async Task<ConversationContext> GetOrCreateContextAsync(string sessionId)
    {
        try
        {
            var context = await _redisService.GetAsync<ConversationContext>($"session:{sessionId}");
            if (context != null)
            {
                _logger.LogInformation($"Retrieved existing context for session {sessionId}");
                return context;
            }
            
            _logger.LogInformation($"Creating new context for session {sessionId}");
            return new ConversationContext { SessionId = sessionId };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving context for session {sessionId}");
            return new ConversationContext { SessionId = sessionId };
        }
    }

    private async Task<HttpResponseData> CreateErrorResponseAsync(HttpRequestData req, HttpStatusCode statusCode, string message)
    {
        var errorObj = new { 
            Error = message,
            Timestamp = DateTime.UtcNow
        };
        
        return await CreateJsonResponseAsync(req, statusCode, errorObj);
    }

    private async Task<HttpResponseData> CreateJsonResponseAsync(HttpRequestData req, HttpStatusCode statusCode, object data)
    {
        var response = req.CreateResponse(statusCode);
        
        // Set headers FIRST
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        
        // Then write the body
        var json = JsonSerializer.Serialize(data, _jsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        await response.Body.WriteAsync(bytes);
        
        return response;
    }
}

// Request/Response Models
public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
    public string? SessionId { get; set; }
    public string? QueryType { get; set; } // Optional: "bearing", "comparison", "technical", "dimensions"
    public string? SourcePreference { get; set; } // Optional: "cache", "database", "realtime"
}

public class ChatResponse
{
    public string SessionId { get; set; } = string.Empty;
    public string Response { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? QueryType { get; set; } = "general"; // Default value
    public string? SourceData { get; set; } = "product_database"; // Default value
}