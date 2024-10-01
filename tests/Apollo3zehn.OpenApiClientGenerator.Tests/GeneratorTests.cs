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
}