using Microsoft.Extensions.Configuration;
using TutorFlow.Api.Services;
using Xunit.Sdk;

namespace TutorFlow.Api.Tests;

public sealed class GeminiIntegrationFactAttribute : FactAttribute
{
    public GeminiIntegrationFactAttribute()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("RUN_GEMINI_INTEGRATION_TESTS"), "true", StringComparison.OrdinalIgnoreCase))
            Skip = "Opt-in: define RUN_GEMINI_INTEGRATION_TESTS=true to call Gemini and consume quota.";
    }
}

public sealed class GeminiIntegrationTests
{
    [GeminiIntegrationFact]
    public async Task GeminiAnswersSimpleQuestion()
    {
        DotEnvLoader.LoadFromRepository();
        var key = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        if (string.IsNullOrWhiteSpace(key))
            throw SkipException.ForSkip("GEMINI_API_KEY is not configured in environment or repository .env.");

        var configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();
        using var generator = new GeminiSdkTextGenerator(configuration);
        var answer = await generator.GenerateAsync(
            configuration["GEMINI_MODEL"] ?? "gemini-3.5-flash-lite",
            "Answer briefly and accurately.",
            "What is 2 + 2? Reply with the number.",
            CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(answer));
        Assert.Contains("4", answer);
    }
}
