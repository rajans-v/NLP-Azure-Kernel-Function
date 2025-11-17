using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using NLP_Azure_Kernel_Function.Models;
using NLP_Azure_Kernel_Function.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace NLP_Azure_Kernel_Function.Agents
{
    internal class QuestionAnsweringAgent : IAgent
    {
        private readonly IChatCompletionService _chatService;
        private readonly IDataService _dataService;
        private readonly IRedisService _redisService;
        private readonly Kernel _kernel;

        public QuestionAnsweringAgent(IChatCompletionService chatService, IDataService dataService, IRedisService redisService)
        {
            _chatService = chatService;
            _dataService = dataService;
            _redisService = redisService;

            var kernelBuilder = Kernel.CreateBuilder();
            kernelBuilder.Services.AddSingleton<IChatCompletionService>(chatService);
            _kernel = kernelBuilder.Build();

            _kernel.ImportPluginFromObject(new DataPlugin(_dataService, _redisService), "DataPlugin");
        }

        public async Task<AgentResponse> ProcessAsync(string userInput, ConversationContext context)
        {
            var cacheKey = $"response:{context.SessionId}:{userInput.GetHashCode()}";
            var cachedResponse = await _redisService.GetAsync<string>(cacheKey);

            if (!string.IsNullOrEmpty(cachedResponse))
            {
                context.PreviousResponseId = cacheKey;
                return new AgentResponse
                {
                    Response = cachedResponse,
                    QueryType = "cached",
                    SourceData = "redis_cache",
                    FromCache = true
                };
            }

            // Extract product query
            var productQuery = await ExtractProductQueryAsync(userInput, context);

            // Generate response
            var response = await GenerateResponseAsync(userInput, productQuery, context);

            // Cache the response
            await _redisService.SetAsync(cacheKey, response, TimeSpan.FromHours(1));
            context.PreviousResponseId = cacheKey;

            // Determine query type and source data
            var queryType = DetermineQueryType(userInput, response);
            var sourceData = "azure_openai_enhanced";

            return new AgentResponse
            {
                Response = response,
                QueryType = queryType,
                SourceData = sourceData,
                FromCache = false
            };
        }

        private async Task<ProductQuery> ExtractProductQueryAsync(string userInput, ConversationContext context)
        {
            var extractionPrompt = $$"""
            Analyze the user's question about bearings and extract product information.
            
            User Question: {{userInput}}
            
            Extract bearing designation, dimensions, and performance attributes.
            Respond in JSON format:
            {
                "productName": "extracted bearing designation or null",
                "productCategory": "extracted category or null",
                "requestedAttributes": ["attribute1", "attribute2"],
                "queryType": "specific|comparison|list"
            }
            """;

            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage("You are a bearing technical specialist. Extract structured bearing queries from natural language.");
            chatHistory.AddUserMessage(extractionPrompt);

            try
            {
                var response = await _chatService.GetChatMessageContentAsync(chatHistory);
                var extractedJson = response.Content ?? "{}";

                return ParseProductQueryFromJson(extractedJson);
            }
            catch (Exception ex)
            {
                return ExtractProductQueryUsingKeywords(userInput);
            }
        }

        private ProductQuery ParseProductQueryFromJson(string json)
        {
            try
            {
                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                var query = new ProductQuery
                {
                    ProductName = root.GetProperty("productName").GetString(),
                    ProductCategory = root.GetProperty("productCategory").GetString(),
                    QueryType = root.GetProperty("queryType").GetString() ?? "general"
                };

                if (root.TryGetProperty("requestedAttributes", out var attributes) && attributes.ValueKind == JsonValueKind.Array)
                {
                    foreach (var attribute in attributes.EnumerateArray())
                    {
                        if (attribute.ValueKind == JsonValueKind.String)
                        {
                            query.RequestedAttributes.Add(attribute.GetString() ?? "");
                        }
                    }
                }

                return query;
            }
            catch
            {
                return new ProductQuery();
            }
        }

        private ProductQuery ExtractProductQueryUsingKeywords(string userInput)
        {
            var query = new ProductQuery();
            var input = userInput.ToLower();

            // Extract bearing designations
            var bearingMatch = System.Text.RegularExpressions.Regex.Match(input, @"\b\d{4,5}\b");
            if (bearingMatch.Success)
            {
                query.ProductName = bearingMatch.Value;
            }

            // Extract categories
            var categoryKeywords = new[] { "deep groove", "ball bearing", "bearing", "angular", "spherical" };
            foreach (var category in categoryKeywords)
            {
                if (input.Contains(category))
                {
                    query.ProductCategory = category;
                    break;
                }
            }

            // Extract attributes
            var attributeMap = new Dictionary<string, string[]>
            {
                ["bore"] = new[] { "bore", "diameter", "inner diameter", "d " },
                ["outside"] = new[] { "outside", "outer diameter", "d " },
                ["width"] = new[] { "width", "b " },
                ["load"] = new[] { "load", "rating", "capacity", "c ", "c0" },
                ["speed"] = new[] { "speed", "rpm", "rmin" }
            };

            foreach (var (attribute, keywords) in attributeMap)
            {
                if (keywords.Any(keyword => input.Contains(keyword)))
                {
                    query.RequestedAttributes.Add(attribute);
                }
            }

            // Detect comparison
            if (input.Contains("compare") || input.Contains("vs") || input.Contains("difference"))
            {
                query.QueryType = "comparison";
            }

            return query;
        }

        private async Task<string> GenerateResponseAsync(string userInput, ProductQuery query, ConversationContext context)
        {
            var chatHistory = new ChatHistory();

            chatHistory.AddSystemMessage($"""
            You are a helpful bearing product assistant. Use the available functions to get accurate bearing data.
            
            Extracted Query Details:
            - Bearing: {query.ProductName ?? "Not specified"}
            - Category: {query.ProductCategory ?? "Not specified"}
            - Requested Attributes: {string.Join(", ", query.RequestedAttributes)}
            - Query Type: {query.QueryType}
            
            Provide specific technical details about bearings. Be precise about dimensions and performance ratings.
            """);

            // Add conversation history
            foreach (var message in context.MessageHistory.TakeLast(3))
            {
                chatHistory.AddMessage(new AuthorRole(message.Role), message.Content);
            }

            chatHistory.AddUserMessage(userInput);

            try
            {
                var result = await _chatService.GetChatMessageContentAsync(
                    chatHistory,
                    executionSettings: new OpenAIPromptExecutionSettings
                    {
                        ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
                    },
                    kernel: _kernel);

                return result.Content ?? "I couldn't find specific information about that bearing.";
            }
            catch (Exception ex)
            {
                return $"I encountered an issue while searching for bearing information: {ex.Message}";
            }
        }

        private string DetermineQueryType(string userInput, string response)
        {
            var input = userInput.ToLower();
            var resp = response.ToLower();

            if (input.Contains("compare") || input.Contains("vs") || resp.Contains("comparison"))
                return "comparison";
            else if (input.Contains("dimension") || resp.Contains("dimension") || resp.Contains("mm"))
                return "dimensions";
            else if (input.Contains("load") || resp.Contains("load") || resp.Contains("rating") || resp.Contains("kn"))
                return "performance";
            else if (input.Contains("weight") || resp.Contains("weight") || resp.Contains("kg"))
                return "logistics";
            else if (input.Contains("what") && input.Contains("is"))
                return "definition";
            else if (input.Contains("show") || input.Contains("list") || input.Contains("all"))
                return "catalog";
            else
                return "general";
        }
    }

    // Data Plugin for function calling
    internal class DataPlugin
    {
        private readonly IDataService _dataService;
        private readonly IRedisService _redisService;

        public DataPlugin(IDataService dataService, IRedisService redisService)
        {
            _dataService = dataService;
            _redisService = redisService;
        }

        [KernelFunction]
        [Description("Search for bearing products by designation, category, or specifications")]
        public async Task<string> SearchBearingsAsync(
            [Description("The search query to find bearing products")] string query)
        {
            try
            {
                var cacheKey = $"bearing_search:{query.ToLower().Trim()}";
                var cachedResult = await _redisService.GetAsync<string>(cacheKey);

                if (!string.IsNullOrEmpty(cachedResult))
                    return cachedResult;

                var products = await _dataService.SearchProductsAsync(query);
                var result = FormatBearingProducts(products);

                await _redisService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(30));
                return result;
            }
            catch (Exception ex)
            {
                return $"Error searching bearing products: {ex.Message}";
            }
        }

        [KernelFunction]
        [Description("Get specific bearing product by designation (e.g., 6205, 6305)")]
        public async Task<string> GetBearingByDesignationAsync(
            [Description("The bearing designation number")] string designation)
        {
            try
            {
                var cacheKey = $"bearing:{designation.ToLower().Trim()}";
                var cachedResult = await _redisService.GetAsync<string>(cacheKey);

                if (!string.IsNullOrEmpty(cachedResult))
                    return cachedResult;

                var products = await _dataService.SearchProductsAsync(designation);
                var product = products.FirstOrDefault();

                if (product == null)
                    return $"Bearing designation '{designation}' not found.";

                var result = FormatBearingDetails(product);
                await _redisService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(30));
                return result;
            }
            catch (Exception ex)
            {
                return $"Error getting bearing product: {ex.Message}";
            }
        }

        [KernelFunction]
        [Description("Get bearing dimensions and specifications")]
        public async Task<string> GetBearingDimensionsAsync(
            [Description("The bearing designation number")] string designation)
        {
            try
            {
                var products = await _dataService.SearchProductsAsync(designation);
                var product = products.FirstOrDefault();

                if (product == null)
                    return $"Bearing designation '{designation}' not found.";

                return FormatBearingDimensions(product);
            }
            catch (Exception ex)
            {
                return $"Error getting bearing dimensions: {ex.Message}";
            }
        }

        [KernelFunction]
        [Description("Get bearing performance data (load ratings, speeds)")]
        public async Task<string> GetBearingPerformanceAsync(
            [Description("The bearing designation number")] string designation)
        {
            try
            {
                var products = await _dataService.SearchProductsAsync(designation);
                var product = products.FirstOrDefault();

                if (product == null)
                    return $"Bearing designation '{designation}' not found.";

                return FormatBearingPerformance(product);
            }
            catch (Exception ex)
            {
                return $"Error getting bearing performance: {ex.Message}";
            }
        }

        [KernelFunction]
        [Description("Compare two bearing products")]
        public async Task<string> CompareBearingsAsync(
            [Description("First bearing designation")] string bearing1,
            [Description("Second bearing designation")] string bearing2)
        {
            try
            {
                var products1 = await _dataService.SearchProductsAsync(bearing1);
                var products2 = await _dataService.SearchProductsAsync(bearing2);

                var prod1 = products1.FirstOrDefault();
                var prod2 = products2.FirstOrDefault();

                if (prod1 == null || prod2 == null)
                    return "One or both bearings not found for comparison.";

                return FormatBearingComparison(prod1, prod2);
            }
            catch (Exception ex)
            {
                return $"Error comparing bearings: {ex.Message}";
            }
        }

        private string FormatBearingProducts(List<Product> products)
        {
            if (!products.Any())
                return "No bearing products found matching your criteria.";

            return string.Join("\n\n", products.Take(5).Select(FormatBearingSummary));
        }

        private string FormatBearingSummary(Product product)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"**{product.Designation} - {product.Title}**");
            sb.AppendLine($"Category: {product.Category}");
            sb.AppendLine($"Description: {product.ShortDescription}");

            // Add key dimensions
            var bore = product.Dimensions.FirstOrDefault(d => d.Symbol == "d");
            var outside = product.Dimensions.FirstOrDefault(d => d.Symbol == "D");
            var width = product.Dimensions.FirstOrDefault(d => d.Symbol == "B");

            if (bore != null) sb.AppendLine($"Bore: {bore.Value} {bore.Unit}");
            if (outside != null) sb.AppendLine($"Outside: {outside.Value} {outside.Unit}");
            if (width != null) sb.AppendLine($"Width: {width.Value} {width.Unit}");

            // Add key performance
            var dynamicLoad = product.Performance.FirstOrDefault(p => p.Symbol == "C");
            if (dynamicLoad != null) sb.AppendLine($"Dynamic Load: {dynamicLoad.Value} {dynamicLoad.Unit}");

            return sb.ToString();
        }

        private string FormatBearingDetails(Product product)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"**{product.Designation} - {product.Title}**");
            sb.AppendLine($"Category: {product.Category}");
            sb.AppendLine($"Taxonomy: {product.Taxonomy}");
            sb.AppendLine();
            sb.AppendLine($"**Description:** {product.Description}");
            sb.AppendLine();
            sb.AppendLine($"**Benefits:** {product.Benefits}");
            sb.AppendLine();

            sb.AppendLine(FormatBearingDimensions(product));
            sb.AppendLine();
            sb.AppendLine(FormatBearingPerformance(product));

            // Add key properties
            if (product.Properties.Any())
            {
                sb.AppendLine("**Key Properties:**");
                foreach (var prop in product.Properties.Take(5))
                {
                    sb.AppendLine($"- {prop.Name}: {prop.Value}");
                }
            }

            return sb.ToString();
        }

        private string FormatBearingDimensions(Product product)
        {
            var sb = new StringBuilder();
            sb.AppendLine("**Dimensions:**");
            foreach (var dim in product.Dimensions)
            {
                sb.AppendLine($"- {dim.Name} ({dim.Symbol}): {dim.Value} {dim.Unit}");
            }
            return sb.ToString();
        }

        private string FormatBearingPerformance(Product product)
        {
            var sb = new StringBuilder();
            sb.AppendLine("**Performance Data:**");
            foreach (var perf in product.Performance)
            {
                var valueStr = perf.Value?.ToString() ?? "";
                if (!string.IsNullOrEmpty(perf.Unit))
                {
                    valueStr += $" {perf.Unit}";
                }
                sb.AppendLine($"- {perf.Name} ({perf.Symbol}): {valueStr}");
            }
            return sb.ToString();
        }

        private string FormatBearingComparison(Product product1, Product product2)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"**Comparison: {product1.Designation} vs {product2.Designation}**");
            sb.AppendLine();

            // Compare dimensions
            sb.AppendLine("**Dimensions:**");
            foreach (var dim in product1.Dimensions)
            {
                var dim2 = product2.Dimensions.FirstOrDefault(d => d.Symbol == dim.Symbol);
                var value2 = dim2 != null ? $"{dim2.Value} {dim2.Unit}" : "N/A";
                sb.AppendLine($"- {dim.Name} ({dim.Symbol}): {product1.Designation}={dim.Value} {dim.Unit}, {product2.Designation}={value2}");
            }
            sb.AppendLine();

            // Compare performance
            sb.AppendLine("**Performance:**");
            foreach (var perf in product1.Performance)
            {
                var perf2 = product2.Performance.FirstOrDefault(p => p.Symbol == perf.Symbol);
                if (perf2 != null)
                {
                    var value1 = $"{perf.Value} {perf.Unit}";
                    var value2 = $"{perf2.Value} {perf2.Unit}";
                    sb.AppendLine($"- {perf.Name} ({perf.Symbol}): {product1.Designation}={value1}, {product2.Designation}={value2}");
                }
            }

            return sb.ToString();
        }
    }
    
    internal class ProductQuery
    {
        public string? ProductName { get; set; }
        public string? ProductCategory { get; set; }
        public string? ProductId { get; set; }
        public List<string> RequestedAttributes { get; set; } = new();
        public string? ComparisonAttribute { get; set; }
        public string QueryType { get; set; } = "general";
    }
}
