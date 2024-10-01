using Microsoft.OpenApi.Models;

namespace Apollo3zehn.OpenApiClientGenerator
{
    public record GeneratorSettings(
        string? Namespace,
        string ClientName,
        string ExceptionType,
        string ExceptionCodePrefix,
        Func<string, OperationType, OpenApiOperation, string> GetOperationName,
        string Special_ConfigurationHeaderKey,
        bool Special_WebAssemblySupport,
        bool Special_AccessTokenSupport,
        bool Special_NexusFeatures);
}