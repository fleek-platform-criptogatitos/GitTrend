﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using GitHubApiStatus;
using GitTrends.Shared;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace GitTrends.Functions;

class GetTestToken
{
	readonly static IReadOnlyDictionary<string, string> _testTokenDictionary = new Dictionary<string, string>
		{
			{ "UITestToken_brminnick", Environment.GetEnvironmentVariable("UITestToken_brminnick") ?? string.Empty },
			{ "UITestToken_GitTrendsApp",  Environment.GetEnvironmentVariable("UITestToken_GitTrendsApp") ?? string.Empty },
			{ "UITestToken_TheCodeTraveler",  Environment.GetEnvironmentVariable("UITestToken_TheCodeTraveler") ?? string.Empty },
		};

	readonly GitHubApiV3Service _gitHubApiV3Service;
	readonly IGitHubApiStatusService _gitHubApiStatusService;
	readonly GitHubGraphQLApiService _gitHubGraphQLApiService;

	public GetTestToken(GitHubApiV3Service gitHubApiV3Service,
						IGitHubApiStatusService gitHubApiStatusService,
						GitHubGraphQLApiService gitHubGraphQLApiService)
	{
		_gitHubApiV3Service = gitHubApiV3Service;
		_gitHubApiStatusService = gitHubApiStatusService;
		_gitHubGraphQLApiService = gitHubGraphQLApiService;
	}

	[Function(nameof(GetTestToken))]
	public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req, FunctionContext functionContext)
	{
		var log = functionContext.GetLogger<GetTestToken>();

		foreach (var testTokenPair in _testTokenDictionary)
		{
			log.LogInformation($"Retrieving Rate Limits for {testTokenPair.Key}");

			var timeout = TimeSpan.FromSeconds(2);
			var cancellationTokenSource = new CancellationTokenSource(timeout);

			try
			{
				_gitHubApiStatusService.SetAuthenticationHeaderValue(new AuthenticationHeaderValue("bearer", testTokenPair.Value));
				var gitHubApiRateLimits = await _gitHubApiStatusService.GetApiRateLimits(cancellationTokenSource.Token).ConfigureAwait(false);

				log.LogInformation($"\tREST API Rate Limit: {gitHubApiRateLimits.RestApi.RemainingRequestCount}");
				log.LogInformation($"\tGraphQL API Rate Limit: {gitHubApiRateLimits.GraphQLApi.RemainingRequestCount}");

				if (gitHubApiRateLimits.RestApi.RemainingRequestCount > 1000
					&& gitHubApiRateLimits.GraphQLApi.RemainingRequestCount > 1000)
				{

					var gitHubToken = new GitHubToken(testTokenPair.Value, GitHubConstants.OAuthScope, "Bearer");

					var okResponse = req.CreateResponse(HttpStatusCode.OK);

					var gitHubTokenJson = JsonConvert.SerializeObject(gitHubToken);

					await okResponse.WriteStringAsync(gitHubTokenJson).ConfigureAwait(false);

					return okResponse;
				}

				log.LogInformation($"\tAPI Limits for {testTokenPair.Key} too low");
				log.LogInformation($"\tRetrieving next token");
			}
			catch (Exception e)
			{
				log.LogError(e, e.Message);

				var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
				await errorResponse.WriteStringAsync(e.ToString()).ConfigureAwait(false);

				return errorResponse;
			}
		};

		var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
		await notFoundResponse.WriteStringAsync("No Valid GitHub Token Found").ConfigureAwait(false);

		return notFoundResponse;
	}
}