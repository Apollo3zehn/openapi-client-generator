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
            Special_NexusFeatures: true);

        var csharpGenerator = new CSharpGenerator();
        var pythonGenerator = new PythonGenerator();

        // Act
        var csharpCode = csharpGenerator.Generate(document, settings);
        var pythonCode = pythonGenerator.Generate(document, settings);

        // Assert

        // TODO: REMOVE THIS LATER
        File.WriteAllText("/home/vincent/Downloads/clients/dotnet.cs", csharpCode);
        File.WriteAllText("/home/vincent/Downloads/clients/python.py", pythonCode);
    }
}