using Apollo3zehn.OpenApiClientGenerator;
using Microsoft.OpenApi.Readers;
using Xunit;

namespace DataSource;

public class GeneratorTests
{
    [Fact]
    public void Test()
    {
        // Arrange
        var openApiJsonString = File.ReadAllText("openapi.json");

        var document = new OpenApiStringReader()
            .Read(openApiJsonString, out var _);

        var settings = new GeneratorSettings(
            Namespace: "Nexus.Api",
            ClientName: "Nexus",
            TokenFolderName: ".nexus-api",
            ConfigurationHeaderKey: "Nexus-Configuration",
            ExceptionType: "NexusException",
            ExceptionCodePrefix: "N",
            GetOperationName: (path, type, operation) => operation.OperationId.Split(new[] { '_' }, 2)[1],
            Special_RefreshTokenSupport: true,
            Special_NexusFeatures: true);

        var csharpGenerator = new CSharpGenerator(settings);
        var pythonGenerator = new PythonGenerator(settings);

        // Act
        var csharpCode = csharpGenerator.Generate(document);
        var pythonCode = pythonGenerator.Generate(document);
    }
}