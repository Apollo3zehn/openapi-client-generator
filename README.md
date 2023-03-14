# Apollo3zehn.OpenApiClientGenerator

[![GitHub Actions](https://github.com/Apollo3zehn/apollo3zehn-openapi-client-generator/actions/workflows/build-and-publish.yml/badge.svg)](https://github.com/Apollo3zehn/apollo3zehn-openapi-client-generator/actions) [![NuGet](https://img.shields.io/nuget/v/FluentModbus.svg?label=Nuget)](https://www.nuget.org/packages/Apollo3zehn.OpenApiClientGenerator)

- This project provides an OpenAPI client generator, i.e. it takes an openapi.json file as input and generates the corresponding output.
- The generator code has been tested on three different projects but is yet fully generic, so it might not lead to perfect results in your case. Please file an issue if you run into problems.
- The generated C# code makes heavy use of [C# 10 Record types ](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record). 
- Both types of clients (C# and Python) will be generated with sync and async support.
- All types are strongly-typed, i.e. JSON data will be serialized and deserialized as required.

Sample usage (taken from https://github.com/Apollo3zehn/hsds-api):

`dotnet add package Apollo3zehn.OpenApiClientGenerator --prerelease`

```cs
using Apollo3zehn.OpenApiClientGenerator;
using Microsoft.OpenApi.Readers;

namespace Hsds.ClientGenerator;

public static class Program
{
    public static async Task Main(string[] args)
    {
        // read open API document
        var client = new HttpClient();
        var response = await client.GetAsync("https://raw.githubusercontent.com/HDFGroup/hdf-rest-api/master/openapi.yaml");

        response.EnsureSuccessStatusCode();

        var openApiJsonString = await response.Content.ReadAsStringAsync();

        // TODO: workaround (OpenAPI version 3.1.0 is not yet supported)
        openApiJsonString = openApiJsonString.Replace("3.1.0", "3.0.3");

        var document = new OpenApiStringReader()
            .Read(openApiJsonString, out var diagnostic);

        // generate clients
        var basePath = Assembly.GetExecutingAssembly().Location;

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
            TokenFolderName: default!, /* Apollo3zehn-specific option */
            ConfigurationHeaderKey: default!, /* Apollo3zehn-specific option */
            ExceptionType: "HsdsException",
            ExceptionCodePrefix: "H",
            GetOperationName: (path, type, _) => {
                if (!pathToMethodNameMap.TryGetValue($"{type}:{path}", out var methodName))
                    methodName = pathToMethodNameMap[path];

                return $"{type}{methodName}";
            },
            Special_RefreshTokenSupport: false, /* Apollo3zehn-specific option */
            Special_NexusFeatures: false); /* Apollo3zehn-specific option */

        // generate C# client
        var csharpGenerator = new CSharpGenerator(settings);
        var csharpCode = csharpGenerator.Generate(document);
        File.WriteAllText("my_csharp_code.cs", csharpcode)

        // generate Python client
        var pythonGenerator = new PythonGenerator(settings);
        var pythonCode = pythonGenerator.Generate(document);
        File.WriteAllText("my_python_code.py", pythonCode)
    }
}
```