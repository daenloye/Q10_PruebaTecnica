using System.Text.RegularExpressions;
using Google.GenAI;
using Google.GenAI.Types;

namespace TutorFlow.Api.Services;

public sealed class GeminiSdkTextGenerator : IGeminiTextGenerator, IDisposable
{
    private readonly Client? _client;
    private readonly ILogger<GeminiSdkTextGenerator>? _logger;

    public GeminiSdkTextGenerator(IConfiguration configuration, ILogger<GeminiSdkTextGenerator>? logger = null)
    {
        _logger = logger;
        var apiKey = configuration["GEMINI_API_KEY"];
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            _client = new Client(
                apiKey: apiKey,
                httpOptions: new HttpOptions
                {
                    RetryOptions = CreateRetryOptions()
                });
        }
    }

    public async Task<string?> GenerateAsync(string model, string systemInstruction, string prompt, CancellationToken cancellationToken)
    {
        if (_client is null)
            throw new GeminiAgentException(503, "gemini_not_configured",
                "Configura GEMINI_API_KEY en el archivo .env para habilitar el asistente.");

        var config = new GenerateContentConfig
        {
            SystemInstruction = new Content
            {
                Parts = [new Part { Text = systemInstruction }]
            },
            Temperature = 0.2,
            HttpOptions = new HttpOptions
            {
                RetryOptions = CreateRetryOptions()
            }
        };

        try
        {
            var response = await _client.Models.GenerateContentAsync(
                model: model,
                contents: prompt,
                config: config,
                cancellationToken: cancellationToken);
            return response.Text;
        }
        catch (ApiException exception)
        {
            _logger?.LogWarning(
                "Gemini API returned HTTP {StatusCode} for model {Model}. Provider detail: {ProviderDetail}",
                exception.StatusCode, model, SafeProviderDetail(exception.Message));
            throw MapApiException(exception);
        }
        catch (HttpRequestException exception)
        {
            _logger?.LogWarning("Gemini request failed with network error {ErrorType}.", exception.GetType().Name);
            throw new GeminiAgentException(502, "gemini_network_error",
                "No fue posible conectar con Gemini. Revisa tu conexión e inténtalo de nuevo.", exception);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new GeminiAgentException(504, "gemini_timeout",
                "Gemini tardó demasiado en responder. Inténtalo de nuevo.", exception);
        }
    }

    internal static GeminiAgentException MapApiException(ApiException exception) => exception.StatusCode switch
    {
        429 => new GeminiAgentException(429, "gemini_quota_exceeded",
            "Se alcanzó la cuota disponible de Gemini. Espera un momento e inténtalo de nuevo.", exception),
        401 or 403 => new GeminiAgentException(503, "gemini_credentials_rejected",
            "Gemini rechazó las credenciales o el acceso del proyecto. Verifica la clave y su configuración.", exception),
        404 => new GeminiAgentException(503, "gemini_model_unavailable",
            "Gemini no reconoce el modelo configurado. Verifica GEMINI_MODEL y los modelos disponibles para la API key.", exception),
        503 => new GeminiAgentException(503, "gemini_overloaded",
            "Gemini está temporalmente saturado. La solicitud se reintentó; inténtalo de nuevo en unos minutos.", exception),
        >= 400 and < 500 => new GeminiAgentException(502, "gemini_request_rejected",
            "Gemini rechazó la solicitud. Revisa la clave, el modelo configurado y la cuota del proyecto.", exception),
        _ => new GeminiAgentException(502, "gemini_service_error",
            "El servicio de Gemini no está disponible en este momento. Intenta de nuevo más tarde.", exception)
    };

    private static HttpRetryOptions CreateRetryOptions() => new()
    {
        Attempts = 3,
        InitialDelay = 1,
        MaxDelay = 4,
        ExpBase = 2,
        Jitter = 0.5
    };

    private static string SafeProviderDetail(string message)
    {
        var safe = Regex.Replace(message, @"AIza[0-9A-Za-z_-]{20,}", "[redacted-api-key]");
        safe = Regex.Replace(safe, @"(?i)(x-goog-api-key|api[_-]?key)(\s*[:=]\s*)[^,\s;""}]+", "$1$2[redacted]");
        return safe.Length <= 600 ? safe : safe[..600];
    }

    public void Dispose() => _client?.Dispose();
}
