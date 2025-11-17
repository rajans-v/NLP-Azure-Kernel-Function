
Note: For Developing the NLP-Azure-Kernel-Function solution, i have take online code suggestion AI Tools.

- How to run/test.
	- To Run Locally: Clone the repository and open the solution in Visual Studio. 
	- Set the startup project to NLP-Azure-Kernel-Function.
	- build and run the project. 
	- Ensure you have the necessary environment variables set for Azure OpenAI and Redis.
	- Note down the Function Name endpoint URL

- Required environment variables (Azure OpenAI, Redis, feature flags like USE_REDIS).
	- Keep the below environment under local.settings.json
	     "AzureOpenAI:Endpoint": "https://<Type-your-url-instance>.azure.com",
         "AzureOpenAI:ApiKey": "<APIkey>",
         "AzureOpenAI:DeploymentName": "",
         "Redis:ConnectionString": "<redis-endpoint-url>,password=<password>,ssl=True,abortConnect=False"

- How to Test
	- By using api testing tools like Postman or curl, send a POST request to the function endpoint with a JSON body containing the user query.
	  JSON Body Example:
	  {
           "message": "is 6205 ball barring is top production",
           "sessionId": "postman-test-2"
      }
	  Example of Message content/Interactions:
      Q: “What is the width of 6205?”
      Expected Ans: “The width of the 6205 bearing is 15 mm.”

	  Q: Follow-up using state: “And what about its diameter?”
      Expected Ans: “The diameter of the 6205 bearing is … mm.”

	  Q: Feedback: “That last width is wrong—store my correction: 6205 width 15 mm.”
      Expected Ans: “Thanks—your feedback for 6205 / width has been saved.”

	  Q: Not Found: “Diameter for 9999?”
      Expected Ans: “Sorry, I can’t find that information for ‘9999’. Please try another designation or attribute.”

- Notes on caching, hallucination reduction, and conversational state.
    - Caching: Implemented using Redis to store and retrieve previous interactions,
	  reducing redundant API calls and improving response times.
	 - Hallucation Reduction taken care by using actual product data,
	   and attribute validation ensures responses match actual product specifications.
	- Redis-backed sessions maintain context across requests
	
- AI Validation Notes: a short summary (bulleted) of AI feedback you applied—covering architecture, security, clean code, and patterns.
	- Agent-based design with orchestrator routing to specialized agents (QA, Feedback)
	- Semantic Kernel integration for AI orchestration and function calling
	- Error handling that avoids exposing internal details
	- Single Responsibility - Each agent handles one concern
	- Dependency Injection for loose coupling and testability
	- Single Responsibility - Each agent handles one concern
	- Dependency Injection for loose coupling and testability