using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using NLP_Azure_Kernel_Function;
using NLP_Azure_Kernel_Function.Agents;
using NLP_Azure_Kernel_Function.Services;
using StackExchange.Redis;


var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        // Configuration
        var configuration = context.Configuration;

        // Add Application Insights
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // Azure OpenAI Chat Completion Service
        services.AddSingleton<IChatCompletionService>(sp =>
        {
            var endpoint = configuration["AzureOpenAI:Endpoint"] ??
                          throw new ArgumentNullException("AzureOpenAI:Endpoint");
            var apiKey = configuration["AzureOpenAI:ApiKey"] ??
                        throw new ArgumentNullException("AzureOpenAI:ApiKey");
            var deploymentName = configuration["AzureOpenAI:DeploymentName"] ??
                        throw new ArgumentNullException("AzureOpenAI:DeploymentName");
            var apiVersion = configuration["AzureOpenAI:ApiVersion"] ?? "2025-11-17";

            return new AzureOpenAIChatCompletionService(
                deploymentName,
                endpoint,
                apiKey,
                apiVersion);
        });

        // Redis
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var connectionString = configuration["Redis:ConnectionString"] ?? "localhost:6379";
            return ConnectionMultiplexer.Connect(connectionString);
        });

        // Services
        services.AddSingleton<IRedisService, RedisService>();
        services.AddSingleton<IDataService, DataService>();

        // Agents
        services.AddTransient<QuestionAnsweringAgent>();
        services.AddTransient<FeedbackAgent>();
        services.AddTransient<OrchestratorAgent>();

        // Register IAgent with OrchestratorAgent
        services.AddTransient<IAgent>(sp =>
        {
            var chatService = sp.GetRequiredService<IChatCompletionService>();
            var questionAgent = sp.GetRequiredService<QuestionAnsweringAgent>();
            var feedbackAgent = sp.GetRequiredService<FeedbackAgent>();

            return new OrchestratorAgent(chatService, questionAgent, feedbackAgent);
        });

        // Function
        services.AddScoped<ProductQnAHttpFunction>();
    })
    .Build();

host.Run();

