using Apollo3zehn.OpenApiClientGenerator;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Xunit;

namespace DataSource;

public class GeneratorTests
{
    [Fact]
    public void IgnoresDocumentedErrorResponsesWhenGeneratingMethods()
    {
        var targetFolderPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var settings = new GeneratorSettings(
            Namespace: "Test.Api",
            ClientName: "Test",
            ExceptionType: "TestException",
            ExceptionCodePrefix: "T",
            GetOperationName: (_, _, _) => "GetValue",
            Special_ConfigurationHeaderKey: default!,
            Special_WebAssemblySupport: false,
            Special_AccessTokenSupport: false,
            Special_NexusFeatures: false);
        var document = CreateDocument("v1");
        document.Paths.Add("/value", new OpenApiPathItem
        {
            Operations =
            {
                [OperationType.Get] = new OpenApiOperation
                {
                    Tags = [new OpenApiTag { Name = "Values" }],
                    Responses = new OpenApiResponses
                    {
                        ["404"] = new OpenApiResponse { Description = "Not found" },
                        ["200"] = new OpenApiResponse { Description = "Success" }
                    }
                }
            }
        });

        try
        {
            new CSharpGenerator(settings).Generate(targetFolderPath, document);
            new PythonGenerator(settings).Generate(targetFolderPath, document);

            Assert.Contains("void GetValue()", File.ReadAllText(Path.Combine(targetFolderPath, "TestClient.g.cs")));
            Assert.Contains("def get_value(self)", File.ReadAllText(Path.Combine(targetFolderPath, "V1.py")));
        }
        finally
        {
            if (Directory.Exists(targetFolderPath))
                Directory.Delete(targetFolderPath, recursive: true);
        }
    }

    [Fact]
    public async Task Test()
    {
        // read open API document
        var client = new HttpClient();
        var response = await client.GetAsync("https://raw.githubusercontent.com/HDFGroup/hdf-rest-api/master/openapi.yaml");

        response.EnsureSuccessStatusCode();

        var openApiJsonString = await response.Content.ReadAsStringAsync();

        // TODO: workaround
        openApiJsonString = openApiJsonString.Replace("3.1.0", "3.0.3");
        openApiJsonString = openApiJsonString.Replace("\"type\"", "type");

        var document_v1 = new OpenApiStringReader()
            .Read(openApiJsonString, out var diagnostic1);

        document_v1.Info.Version = "v1";

        var document_v2 = new OpenApiStringReader()
            .Read(openApiJsonString, out var diagnostic2);

        document_v2.Info.Version = "v2";

        // generate clients

        // TODO: remove when https://github.com/HDFGroup/hdf-rest-api/issues/10 is resolved
        var pathToMethodNameMap = new Dictionary<string, string>()
        {
            ["/"] = "Domain",
            ["Post:/groups"] = "Group",
            ["Get:/groups"] = "Groups",
            ["/groups/{id}"] = "Group",
            ["/groups/{id}/links"] = "Links",
            ["/groups/{id}/links/{linkname}"] = "Link",
            ["Post:/datasets"] = "Dataset",
            ["Get:/datasets"] = "Datasets",
            ["/datasets/{id}"] = "Dataset",
            ["/datasets/{id}/shape"] = "Shape",
            ["/datasets/{id}/type"] = "DataType",
            ["/datasets/{id}/value"] = "Values",
            ["/datatypes"] = "DataType",
            ["/datatypes/{id}"] = "Datatype",
            ["/{collection}/{obj_uuid}/attributes"] = "Attributes",
            ["/{collection}/{obj_uuid}/attributes/{attr}"] = "Attribute",
            ["/acls"] = "AccessLists",
            ["/acls/{user}"] = "UserAccess",
            ["/groups/{id}/acls"] = "GroupAccessLists",
            ["/groups/{id}/acls/{user}"] = "GroupUserAccess",
            ["/datasets/{id}/acls"] = "DatasetAccessLists",
            ["/datatypes/{id}/acls"] = "DataTypeAccessLists"
        };

        var settings = new GeneratorSettings(
            Namespace: "Hsds.Api",
            ClientName: "Hsds",
            ExceptionType: "HsdsException",
            ExceptionCodePrefix: "H",
            GetOperationName: (path, type, _) => {
                if (!pathToMethodNameMap.TryGetValue($"{type}:{path}", out var methodName))
                    methodName = pathToMethodNameMap[path];

                return $"{type}{methodName}";
            },
            Special_ConfigurationHeaderKey: default!,
            Special_WebAssemblySupport: false,
            Special_AccessTokenSupport: false,
            Special_NexusFeatures: false
        );

        // generate C# client
        var csharpGenerator = new CSharpGenerator(settings);
        csharpGenerator.Generate(".", document_v1, document_v2);

        // generate python client
        var pythonGenerator = new PythonGenerator(settings);
        pythonGenerator.Generate(".", document_v1, document_v2);
    }

    private static OpenApiDocument CreateDocument(string version)
    {
        return new OpenApiDocument
        {
            Info = new OpenApiInfo { Version = version },
            Paths = new OpenApiPaths(),
            Components = new OpenApiComponents()
        };
    }
}
