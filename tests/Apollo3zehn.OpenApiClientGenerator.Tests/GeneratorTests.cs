using Apollo3zehn.OpenApiClientGenerator;
using Microsoft.OpenApi.Readers;
using Xunit;

namespace DataSource;

public class GeneratorTests
{
    [Fact]
    public async Task Test()
    {
        // read open API document
        var client = new HttpClient();
        var response = await client.GetAsync("http://localhost:5000/openapi/v1/openapi.json");
        var response2 = await client.GetAsync("http://localhost:5000/openapi/v2/openapi.json");

        response.EnsureSuccessStatusCode();

        var openApiJsonString = await response.Content.ReadAsStringAsync();
        var openApiJsonString2 = await response2.Content.ReadAsStringAsync();

        // TODO: workaround
        openApiJsonString = openApiJsonString.Replace("3.1.0", "3.0.3");
        openApiJsonString = openApiJsonString.Replace("\"type\"", "type");

        openApiJsonString2 = openApiJsonString2.Replace("3.1.0", "3.0.3");
        openApiJsonString2 = openApiJsonString2.Replace("\"type\"", "type");

        var document_v1 = new OpenApiStringReader()
            .Read(openApiJsonString, out var diagnostic1);

        // document_v1.Info.Version = "v1";

        var document_v2 = new OpenApiStringReader()
            .Read(openApiJsonString2, out var diagnostic2);

        // document_v2.Info.Version = "v2";

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
            Namespace: "Nexus.Api",
            ClientName: "Nexus",
            Special_ConfigurationHeaderKey: default!,
            ExceptionType: "NexusException",
            ExceptionCodePrefix: "N",
            // GetOperationName: (path, type, _) => {
            //     if (!pathToMethodNameMap.TryGetValue($"{type}:{path}", out var methodName))
            //         methodName = pathToMethodNameMap[path];

            //     return $"{type}{methodName}";
            // },
            GetOperationName: (path, type, operation) => operation.OperationId.Split(['_'], 2)[1],
            Special_WebAssemblySupport: false,
            Special_AccessTokenSupport: false,
            Special_NexusFeatures: false);

        // generate C# client
        var csharpGenerator = new CSharpGenerator(settings);
        var csharpCode = csharpGenerator.Generate(document_v1, document_v2);

        var pythonGenerator = new PythonGenerator(settings);
        var pythonCode = pythonGenerator.Generate(document_v1, document_v2);

        // File.WriteAllText("/home/vincent/Downloads/out/nexus_api/csharp.cs", csharpCode);
        File.WriteAllText("/home/vincent/Downloads/out/nexus_api/Shared.py", pythonCode);
    }
}