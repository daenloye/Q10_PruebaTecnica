using Google.GenAI;
using Google.GenAI.Types;

namespace TutorFlow.Api.Services;

public sealed class GeminiSdkTextGenerator : IGeminiTextGenerator, IDisposable
{
    private readonly Client? _client;

    public GeminiSdkTextGenerator(IConfiguration configuration)
    {
        var apiKey = configuration["GEMINI_API_KEY"];
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            _client = new Client(
                apiKey: apiKey,
                httpOptions: new HttpOptions
                {
                    RetryOptions = new HttpRetryOptions { Attempts = 1 }
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
                RetryOptions = new HttpRetryOptions { Attempts = 1 }
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
            throw MapApiException(exception);
        }
        catch (HttpRequestException exception)
        {
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
        >= 400 and < 500 => new GeminiAgentException(502, "gemini_request_rejected",
            "Gemini rechazó la solicitud. Revisa la clave, el modelo configurado y la cuota del proyecto.", exception),
        _ => new GeminiAgentException(502, "gemini_service_error",
            "El servicio de Gemini no está disponible en este momento. Intenta de nuevo más tarde.", exception)
    };

    public void Dispose() => _client?.Dispose();
}
