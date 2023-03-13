namespace Apollo3zehn.OpenApiClientGenerator
{
    public record GeneratorSettings(
        string? Namespace,
        string ClientName,
        string TokenFolderName,
        string ConfigurationHeaderKey,
        string ExceptionType,
        string ExceptionCodePrefix,
        bool Special_NexusFeatures);
}