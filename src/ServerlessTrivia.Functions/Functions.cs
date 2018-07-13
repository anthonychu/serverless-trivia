using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using F23.StringSimilarity;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;

namespace ServerlessTrivia
{
    public static class Functions
    {
        [FunctionName(nameof(TriviaOrchestrator))]
        public static async Task TriviaOrchestrator(
            [OrchestrationTrigger] DurableOrchestrationContext context, ILogger logger)
        {
            Clue previousClue = null;
            try
            {
                var input = context.GetInput<int>();
                var outputs = new List<string>();

                var clue = await context.CallActivityAsync<Clue>(nameof(GetClue), null);
                logger.LogInformation(JsonConvert.SerializeObject(clue));

                DateTime nextRun = context.CurrentUtcDateTime.AddSeconds(15);
                logger.LogInformation($"*** Next run: {nextRun.ToString()}");
                await context.CreateTimer(nextRun, CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.ToString());
            }
            finally
            {
                context.ContinueAsNew(previousClue);
            }
        }

        [FunctionName(nameof(GetClue))]
        public static async Task<Clue> GetClue(
            [ActivityTrigger] DurableActivityContext context,
            [Table("clues")] IAsyncCollector<Clue> clues,
            ILogger logger)
        {
            logger.LogInformation($"*** Getting clue...");
            var client = System.Net.Http.HttpClientFactory.Create();
            var responseJson = await client.GetStringAsync("http://jservice.io/api/random");
            var response = JsonConvert.DeserializeObject<IEnumerable<JServiceResponse>>(responseJson);

            var clue = response.First().ToClue();
            await clues.AddAsync(clue);
            return clue;
        }

        [FunctionName(nameof(SubmitGuess))]
        public static async Task<object> SubmitGuess(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")]Guess guess,
            [Table("clues", "{clueId}", Clue.RowKeyValue)] Clue clue,
            [Table("guesses")] IAsyncCollector<Guess> guesses)
        {
            var l = new NormalizedLevenshtein();
            var similarity = l.Similarity(NormalizeString(clue.Answer), NormalizeString(guess.Value));

            await guesses.AddAsync(guess);

            return new
            {
                guess.SessionId,
                ClueId = clue.PartitionKey,
                Guess = guess.Value,
                CorrectAnswer = clue.Answer,
                IsCorrect = similarity > 0.8,
                Similarity = similarity
            };
        }

        private static string NormalizeString(string input)
        {
            return Regex.Replace(input, @"(\b(a|an|the|of)\b|[^a-zA-Z0-9]+)", "", RegexOptions.IgnoreCase).ToLowerInvariant();
        }

        [FunctionName("HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")]HttpRequestMessage req,
            [OrchestrationClient]DurableOrchestrationClient starter,
            TraceWriter log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync(nameof(TriviaOrchestrator), null);

            log.Info($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName("HttpStartSingle")]
        public static async Task<HttpResponseMessage> RunSingle(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestMessage req,
            [OrchestrationClient] DurableOrchestrationClient starter,
            TraceWriter log)
        {
            const string instanceId = "1";
            // Check if an instance with the specified ID already exists.
            var existingInstance = await starter.GetStatusAsync(instanceId);
            if (existingInstance == null)
            {
                await starter.StartNewAsync(nameof(TriviaOrchestrator), instanceId);
                log.Info($"Started orchestration with ID = '{instanceId}'.");
                return starter.CreateCheckStatusResponse(req, instanceId);
            }
            else
            {
                // An instance with the specified ID exists, don't create one.
                return req.CreateErrorResponse(
                    HttpStatusCode.Conflict,
                    $"An instance with ID '{instanceId}' already exists.");
            }
        }

        [FunctionName(nameof(SignalRInfo))]
        public static IActionResult SignalRInfo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")]HttpRequestMessage req, 
            [SignalRConnectionInfo(HubName = "trivia")]AzureSignalRConnectionInfo info, 
            ILogger logger)
        {
            return info != null
                ? (ActionResult)new OkObjectResult(info)
                : new NotFoundObjectResult("Failed to load SignalR Info.");
        }
    }
}