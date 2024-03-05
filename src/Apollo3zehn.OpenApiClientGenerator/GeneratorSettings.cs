using Microsoft.OpenApi.Models;

namespace Apollo3zehn.OpenApiClientGenerator
{
    public record GeneratorSettings(
        string? Namespace,
        string ClientName,
        string TokenFolderName,
        string ConfigurationHeaderKey,
        string ExceptionType,
        string ExceptionCodePrefix,
        Func<string, OperationType, OpenApiOperation, string> GetOperationName,
        bool Special_WebAssemblySupport,
        bool Special_AccessTokenSupport,
        bool Special_NexusFeatures);
}