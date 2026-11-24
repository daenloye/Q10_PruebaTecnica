using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using TutorFlow.Api.Contracts;
using TutorFlow.Api.Services;
using Xunit.Sdk;

namespace TutorFlow.Api.Tests;

public sealed class InitialQuestionsE2ETests
{
    [Fact]
    public async Task BackendBuildsStudentContextFromTheRunningMockApi()
    {
        await using var mockApi = await RunningMockAcademicApi.StartAsync();
        await VerifyMockApiRoutesAsync(mockApi.Client);
        var agent = new RecordingAgentClient(null);
        using var factory = new GeminiE2EApiFactory(mockApi.BaseAddress, agent);
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/chat/messages", new { userId = "EST-001", message = "Resume mis notas." });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(agent.LastRequest);
        Assert.Contains("Cálculo Integral", agent.LastRequest.Context, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("corte1", agent.LastRequest.Context, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Promedio ponderado calculado por TutorFlow", agent.LastRequest.Context, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("EST-002", agent.LastRequest.Context, StringComparison.OrdinalIgnoreCase);
    }

    [GeminiIntegrationFact]
    public async Task EveryExampleQuestionUsesTheRealBackendAndGemini()
    {
        DotEnvLoader.LoadFromRepository();
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GEMINI_API_KEY")))
            throw SkipException.ForSkip("GEMINI_API_KEY is not configured in environment or repository .env.");

        var examplesPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "preguntas_ejemplo.json");
        var json = await File.ReadAllTextAsync(examplesPath);
        var examples = JsonSerializer.Deserialize<ExampleQuestion[]>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidDataException("preguntas_ejemplo.json no contiene una lista de casos válida.");
        var selectedIds = Environment.GetEnvironmentVariable("TUTORFLOW_E2E_CASE_IDS");
        if (!string.IsNullOrWhiteSpace(selectedIds))
        {
            var ids = selectedIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(value => int.Parse(value, System.Globalization.CultureInfo.InvariantCulture)).ToHashSet();
            examples = examples.Where(example => ids.Contains(example.Id)).ToArray();
        }

        Assert.NotEmpty(examples);
        await using var mockApi = await RunningMockAcademicApi.StartAsync();
        await VerifyMockApiRoutesAsync(mockApi.Client);
        using var factory = new GeminiE2EApiFactory(mockApi.BaseAddress);
        using var client = factory.CreateClient();
        client.Timeout = Timeout.InfiniteTimeSpan;
        var report = new List<string>();
        var failures = new List<string>();

        foreach (var example in examples)
        {
            if (report.Count > 0) await Task.Delay(TimeSpan.FromSeconds(7));
            using var response = await PostChatWithTransientRetriesAsync(client, example.Usuario.Id, example.Pregunta);
            var shouldBeDenied = example.RespuestaEsperada.StartsWith("Se niega", StringComparison.OrdinalIgnoreCase);

            if (shouldBeDenied)
            {
                var denied = response.StatusCode == HttpStatusCode.Forbidden;
                report.Add($"{example.Id}\t{example.Usuario.Rol}\t{(denied ? "BLOQUEADO CORRECTAMENTE" : "FAIL")}\t{(int)response.StatusCode}\t{example.CitaEsperada}");
                if (!denied)
                    failures.Add($"Caso {example.Id}: se esperaba bloqueo 403 para acceso no autorizado y llegó {(int)response.StatusCode}.");
                continue;
            }

            if (response.StatusCode != HttpStatusCode.OK)
            {
                var errorBody = await response.Content.ReadFromJsonAsync<JsonElement>();
                var errorCode = errorBody.TryGetProperty("error", out var error) ? error.GetString() : "sin código";
                failures.Add($"Caso {example.Id}: pipeline respondió {(int)response.StatusCode} ({errorCode}).");
                report.Add($"{example.Id}\t{example.Usuario.Rol}\tFAIL HTTP\t{(int)response.StatusCode}\t{example.CitaEsperada}");
                continue;
            }

            var chat = await response.Content.ReadFromJsonAsync<ChatResponse>();
            var missingFacts = AnswerExpectation.FindMissingFacts(example.RespuestaEsperada, chat?.Answer ?? string.Empty);
            var sourcesValid = chat?.Sources.Contains("rules.md", StringComparer.OrdinalIgnoreCase) == true
                && chat.Sources.Contains("api.md", StringComparer.OrdinalIgnoreCase);
            var passed = missingFacts.Count == 0 && sourcesValid && !string.IsNullOrWhiteSpace(chat?.Answer);
            var state = passed ? "PASS" : "FAIL RESPUESTA";
            report.Add($"{example.Id}\t{example.Usuario.Rol}\t{state}\t{(int)response.StatusCode}\t{example.CitaEsperada}");
            if (!passed)
            {
                var details = missingFacts.Count == 0 ? "la lista de fuentes no incluye rules.md y api.md" : string.Join(", ", missingFacts);
                var answerPreview = Regex.Replace(chat?.Answer ?? string.Empty, @"\s+", " ");
                if (answerPreview.Length > 800) answerPreview = answerPreview[..800] + "…";
                failures.Add($"Caso {example.Id}: no coincide con la expectativa ({details}). Respuesta: {answerPreview}");
            }

        }

        Console.WriteLine("Caso\tRol\tResultado\tHTTP\tCita esperada");
        foreach (var line in report) Console.WriteLine(line);
        Assert.True(failures.Count == 0,
            $"Fallaron {failures.Count} de {examples.Length} casos:{Environment.NewLine}{string.Join(Environment.NewLine, failures)}");
        Assert.Equal(examples.Length, report.Count);
    }

    private static async Task VerifyMockApiRoutesAsync(HttpClient client)
    {
        using var health = await client.GetAsync("salud");
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        using var programsResponse = await client.GetAsync("programas");
        Assert.Equal(HttpStatusCode.OK, programsResponse.StatusCode);
        var programs = await programsResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Array, programs.ValueKind);
        if (programs.GetArrayLength() == 0) return;

        var programId = programs[0].GetProperty("id").GetString();
        using var studentsResponse = await client.GetAsync($"programas/{Uri.EscapeDataString(programId!)}/estudiantes");
        Assert.Equal(HttpStatusCode.OK, studentsResponse.StatusCode);
        var students = await studentsResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Array, students.ValueKind);
        if (students.GetArrayLength() == 0) return;

        var studentId = students[0].GetProperty("id").GetString();
        foreach (var suffix in new[] { "", "/notas", "/asistencia" })
        {
            using var response = await client.GetAsync($"estudiantes/{Uri.EscapeDataString(studentId!)}{suffix}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    private static async Task<HttpResponseMessage> PostChatWithTransientRetriesAsync(HttpClient client, string userId, string message)
    {
        for (var attempt = 0; ; attempt++)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(120));
            HttpResponseMessage response;
            try
            {
                response = await client.PostAsJsonAsync("/api/chat/messages", new { userId, message }, timeout.Token);
            }
            catch (OperationCanceledException) when (timeout.IsCancellationRequested)
            {
                return new HttpResponseMessage(HttpStatusCode.GatewayTimeout)
                {
                    Content = JsonContent.Create(new { error = "e2e_request_timeout" })
                };
            }

            if (response.StatusCode != HttpStatusCode.BadGateway || attempt >= 3)
                return response;

            response.Dispose();
            await Task.Delay(TimeSpan.FromSeconds(5 * (attempt + 1)));
        }
    }
}

public sealed class GeminiE2EApiFactory : WebApplicationFactory<Program>
{
    private readonly string _mockApiBaseUrl;
    private readonly IAgentClient? _agent;
    public GeminiE2EApiFactory(string mockApiBaseUrl, IAgentClient? agent = null)
    {
        _mockApiBaseUrl = mockApiBaseUrl;
        _agent = agent;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?> { ["MockApi:BaseUrl"] = _mockApiBaseUrl }));
        builder.ConfigureTestServices(services =>
        {
            services.AddHttpClient<MockAcademicApi>(client => client.BaseAddress = new Uri(_mockApiBaseUrl));
            services.AddLogging(logging => logging.ClearProviders());
            if (_agent is not null)
            {
                services.RemoveAll<IAgentClient>();
                services.AddSingleton(_agent);
            }
        });
    }
}

internal sealed class RunningMockAcademicApi : IAsyncDisposable
{
    private readonly Process _process;
    public HttpClient Client { get; }
    public string BaseAddress { get; }

    private RunningMockAcademicApi(Process process, HttpClient client, string baseAddress)
    {
        _process = process;
        Client = client;
        BaseAddress = baseAddress;
    }

    public static async Task<RunningMockAcademicApi> StartAsync()
    {
        var apiDirectory = FindRepositoryRoot();
        var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        var baseAddress = $"http://127.0.0.1:{port}/";
        var start = new ProcessStartInfo("node")
        {
            WorkingDirectory = apiDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        start.ArgumentList.Add(Path.Combine(apiDirectory, "server.js"));
        start.Environment["PORT"] = port.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var process = Process.Start(start) ?? throw new InvalidOperationException("No fue posible iniciar Node.js para la mock API.");
        process.OutputDataReceived += (_, _) => { };
        process.ErrorDataReceived += (_, _) => { };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        var client = new HttpClient { BaseAddress = new Uri(baseAddress), Timeout = TimeSpan.FromSeconds(2) };
        try
        {
            var deadline = DateTime.UtcNow.AddSeconds(15);
            while (DateTime.UtcNow < deadline)
            {
                if (process.HasExited) throw new InvalidOperationException($"La mock API terminó al iniciar (código {process.ExitCode}).");
                try
                {
                    using var response = await client.GetAsync("salud");
                    if (response.IsSuccessStatusCode) return new RunningMockAcademicApi(process, client, baseAddress);
                }
                catch (HttpRequestException) { }
                catch (TaskCanceledException) { }
                await Task.Delay(200);
            }
            throw new TimeoutException("La mock API no respondió en /salud dentro de 15 segundos.");
        }
        catch
        {
            client.Dispose();
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            process.Dispose();
            throw;
        }
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "initial_docs", "mock-api", "server.js")))
                return Path.Combine(directory.FullName, "initial_docs", "mock-api");
        throw new DirectoryNotFoundException("No se encontró initial_docs/mock-api/server.js desde el directorio de pruebas.");
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        if (!_process.HasExited)
        {
            _process.Kill(entireProcessTree: true);
            await _process.WaitForExitAsync();
        }
        _process.Dispose();
    }
}

internal sealed record ExampleQuestion(int Id, ExampleUser Usuario, string Pregunta, string RespuestaEsperada, string CitaEsperada);
internal sealed record ExampleUser(string Rol, string Id);

internal static class AnswerExpectation
{
    private static readonly HashSet<string> IgnoredTitleWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "Definitivas", "Promedio", "Lunes", "Ninguna", "Da", "Indica", "Se", "Solo"
    };

    public static List<string> FindMissingFacts(string expected, string actual)
    {
        var missing = new List<string>();
        var normalizedAnswer = Normalize(actual);
        foreach (Match match in Regex.Matches(expected, @"(?<!\d)\d+(?:[.,]\d+)?\s*%?"))
        {
            var number = Regex.Match(match.Value, @"\d+(?:[.,]\d+)?").Value.Replace(',', '.');
            var alternatives = new List<string> { Regex.Escape(number) };
            if (!number.Contains('.'))
                alternatives.Add($"0+{Regex.Escape(number)}");
            if (number.EndsWith(".0", StringComparison.Ordinal))
                alternatives.Add(Regex.Escape(number[..^2]));
            var percent = match.Value.Contains('%');
            var suffix = percent ? @"(?:\s*%|\s+por\s+ciento)" : string.Empty;
            var beforeBoundary = number.Contains('.') ? @"(?<![\d.])" : @"(?<!\d)";
            var afterBoundary = number.Contains('.') ? @"(?![\d.])" : @"(?!\d)";
            var pattern = $@"{beforeBoundary}(?:{string.Join("|", alternatives)}){afterBoundary}{suffix}";
            var alternativesFound = Regex.IsMatch(normalizedAnswer, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (percent && !alternativesFound)
            {
                var bareNumber = $@"(?<!\d)(?:{string.Join("|", alternatives)})(?!\d)";
                var percentageContext = Regex.IsMatch(normalizedAnswer, @"\b(?:porcentaje|por ciento|peso|pondera|corte)\b",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                alternativesFound = percentageContext && Regex.IsMatch(normalizedAnswer, bareNumber,
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

                if (!alternativesFound && decimal.TryParse(number, System.Globalization.NumberStyles.Number,
                        System.Globalization.CultureInfo.InvariantCulture, out var percentageValue))
                {
                    var ratio = (percentageValue / 100m).ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);
                    alternativesFound = Regex.IsMatch(normalizedAnswer,
                        $@"(?<![\d.]){Regex.Escape(ratio)}(?![\d.])",
                        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                }
            }
            var wordKey = number.EndsWith(".0", StringComparison.Ordinal) ? number[..^2] : number;
            var numberWordFound = !percent && NumberWords.TryGetValue(wordKey, out var words)
                && Regex.IsMatch(normalizedAnswer, $@"\b(?:{words})\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (!alternativesFound && !numberWordFound)
                missing.Add($"dato numérico {match.Value.Trim()}");
        }

        foreach (Match match in Regex.Matches(expected, @"\b[A-ZÁÉÍÓÚÑ][\p{L}]{5,}\b"))
        {
            var phrase = match.Value;
            if (IgnoredTitleWords.Contains(phrase)) continue;
            var normalizedPhrase = Normalize(phrase);
            if (!normalizedAnswer.Contains(normalizedPhrase, StringComparison.Ordinal))
                missing.Add($"dato clave «{phrase}»");
        }

        return missing.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static string Normalize(string value)
    {
        var decomposed = value.Normalize(NormalizationForm.FormD);
        var withoutDiacritics = string.Concat(decomposed.Where(character =>
            System.Globalization.CharUnicodeInfo.GetUnicodeCategory(character) != System.Globalization.UnicodeCategory.NonSpacingMark));
        return Regex.Replace(withoutDiacritics.Normalize(NormalizationForm.FormC).ToLowerInvariant(), @"(?<=\d),(?=\d)", ".");
    }

    private static readonly Dictionary<string, string> NumberWords = new(StringComparer.Ordinal)
    {
        ["0"] = "cero", ["1"] = "uno|una|un", ["2"] = "dos|segundo|segunda", ["3"] = "tres|tercero|tercera",
        ["4"] = "cuatro", ["5"] = "cinco|quinto|quinta", ["6"] = "seis", ["7"] = "siete",
        ["8"] = "ocho", ["9"] = "nueve", ["10"] = "diez", ["11"] = "once", ["12"] = "doce",
        ["13"] = "trece", ["14"] = "catorce", ["15"] = "quince", ["16"] = "dieciseis",
        ["17"] = "diecisiete", ["18"] = "dieciocho", ["19"] = "diecinueve", ["20"] = "veinte"
    };
}
