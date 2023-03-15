using System.Reflection;
using System.Text;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Stubble.Core.Builders;

namespace Apollo3zehn.OpenApiClientGenerator;

public class CSharpGenerator
{
    private readonly GeneratorSettings _settings;
    private Dictionary<string, string> _additionalModels = default!;

    private readonly Dictionary<string, string> _methodNameSuffixes = new()
    {
        ["application/octet-stream"] = "AsStream",
        ["application/json"] = "AsJson"
    };

    public CSharpGenerator(GeneratorSettings settings)
    {
        _settings = settings;
    }

    public string Generate(OpenApiDocument document)
    {
        _additionalModels = new();
        var sourceTextBuilder = new StringBuilder();
        var stubble = new StubbleBuilder().Build();

        // add clients
        var groupedClients = document.Paths
            .SelectMany(path => path.Value.Operations.First().Value.Tags.Select(tag => (path, tag)))
            .GroupBy(value => value.tag.Name);

        var subClients = groupedClients.Select(group => group.Key);

        // SubClientFields
        sourceTextBuilder.Clear();

        foreach (var subClient in subClients)
        {
            sourceTextBuilder.AppendLine($"    private {subClient}Client _{Shared.FirstCharToLower(subClient)};");
        }

        var subClientFields = sourceTextBuilder.ToString();

        // SubClientFieldAssignments
        sourceTextBuilder.Clear();

        foreach (var subClient in subClients)
        {
            sourceTextBuilder.AppendLine($"        _{Shared.FirstCharToLower(subClient)} = new {subClient}Client(this);");
        }

        var subClientFieldAssignments = sourceTextBuilder.ToString();

        // SubClientProperties
        sourceTextBuilder.Clear();

        foreach (var subClient in subClients)
        {
            sourceTextBuilder.AppendLine("    /// <inheritdoc />");
            sourceTextBuilder.AppendLine($"    public I{subClient}Client {subClient} => _{Shared.FirstCharToLower(subClient)};");
            sourceTextBuilder.AppendLine();
        }

        var subClientProperties = sourceTextBuilder.ToString();

        // SubClientInterfaceProperties
        sourceTextBuilder.Clear();

        foreach (var subClient in subClients)
        {
            sourceTextBuilder.AppendLine(
$@"    /// <summary>
    /// Gets the <see cref=""I{subClient}Client""/>.
    /// </summary>");
            sourceTextBuilder.AppendLine($"    I{subClient}Client {subClient} {{ get; }}");
            sourceTextBuilder.AppendLine();
        }

        var subClientInterfaceProperties = sourceTextBuilder.ToString();

        // SubClientSource
        sourceTextBuilder.Clear();

        foreach (var clientGroup in groupedClients)
        {
            AppendSubClientSourceText(
                clientGroup.Key,
                clientGroup.ToDictionary(entry => entry.path.Key, entry => entry.path.Value),
                sourceTextBuilder);

            sourceTextBuilder.AppendLine();
        }

        var subClientSource = sourceTextBuilder.ToString();

        // Models
        sourceTextBuilder.Clear();

        foreach (var schema in document.Components.Schemas)
        {
            AppendModelSourceText(
                schema.Key,
                schema.Value,
                sourceTextBuilder);

            sourceTextBuilder.AppendLine();
        }

        foreach (var (_, modelText) in _additionalModels)
        {
            sourceTextBuilder.Append(modelText);
            sourceTextBuilder.AppendLine();
        }

        var models = sourceTextBuilder.ToString();

        // Build final source text
        var basePath = Assembly.GetExecutingAssembly().Location;

        using var templateStreamReader = new StreamReader(Assembly
            .GetExecutingAssembly()
            .GetManifestResourceStream("Apollo3zehn.OpenApiClientGenerator.Templates.CSharpTemplate.cs")!);

        var template = templateStreamReader.ReadToEnd();

        var data = new
        {
            Namespace = _settings.Namespace,
            ClientName = _settings.ClientName,
            TokenFoldername = _settings.TokenFolderName,
            ConfigurationHeaderKey = _settings.ConfigurationHeaderKey,
            SubClientFields = subClientFields,
            SubClientFieldAssignments = subClientFieldAssignments,
            SubClientProperties = subClientProperties,
            SubClientInterfaceProperties = subClientInterfaceProperties,
            SubClientSource = subClientSource,
            ExceptionType = _settings.ExceptionType,
            ExceptionCodePrefix = _settings.ExceptionCodePrefix,
            Models = models,
            Special_RefreshTokenSupport = _settings.Special_RefreshTokenSupport,
            Special_NexusFeatures = _settings.Special_NexusFeatures
        };

        return stubble.Render(template, data);
    }

    private void AppendSubClientSourceText(
        string className,
        IDictionary<string, OpenApiPathItem> methodMap,
        StringBuilder sourceTextBuilder)
    {
        var augmentedClassName = className + "Client";

        // interface
        sourceTextBuilder.AppendLine(
$@"/// <summary>
/// Provides methods to interact with {Shared.SplitCamelCase(className).ToLower()}.
/// </summary>
public interface I{augmentedClassName}
{{");

        foreach (var entry in methodMap)
        {
            if (entry.Value.Parameters.Any())
                throw new Exception("Parameters on the path item level are not supported.");

            // if (operation.Responses.Count != 1)
            //     throw new Exception("Only a single response is supported.");

            foreach (var operation in entry.Value.Operations)
            {
                var response = operation.Value.Responses.First();

                if (response.Value.Content.Count == 0)
                {
                    AppendInterfaceMethodSourceText(
                        path: entry.Key,
                        methodSuffix: "",
                        operation.Key,
                        operation.Value,
                        response,
                        responseType: default,
                        sourceTextBuilder,
                        async: false);

                    sourceTextBuilder.AppendLine();

                    AppendInterfaceMethodSourceText(
                        path: entry.Key,
                        methodSuffix: "",
                        operation.Key,
                        operation.Value,
                        response,
                        responseType: default,
                        sourceTextBuilder,
                        async: true);

                    sourceTextBuilder.AppendLine();   
                }

                else
                {
                    foreach (var responseType in response.Value.Content)
                    {
                        var methodSuffix = response.Value.Content.Count == 1
                            ? ""
                            : _methodNameSuffixes[responseType.Key];

                        AppendInterfaceMethodSourceText(
                            path: entry.Key,
                            methodSuffix,
                            operation.Key,
                            operation.Value,
                            response,
                            responseType,
                            sourceTextBuilder,
                            async: false);

                        sourceTextBuilder.AppendLine();

                        AppendInterfaceMethodSourceText(
                            path: entry.Key,
                            methodSuffix,
                            operation.Key,
                            operation.Value,
                            response,
                            responseType,
                            sourceTextBuilder,
                            async: true);

                        sourceTextBuilder.AppendLine();   
                    }
                }
            }
        }

        sourceTextBuilder.AppendLine("}");
        sourceTextBuilder.AppendLine();

        // implementation
        sourceTextBuilder
            .AppendLine("/// <inheritdoc />");

        sourceTextBuilder.AppendLine(
$@"public class {augmentedClassName} : I{augmentedClassName}
{{
    private {_settings.ClientName}Client ___client;
    
    internal {augmentedClassName}({_settings.ClientName}Client client)
    {{
        ___client = client;
    }}
");

        foreach (var entry in methodMap)
        {
            if (entry.Value.Parameters.Any())
                throw new Exception("Parameters on the path item level are not supported.");

            // if (operation.Responses.Count != 1)
            //     throw new Exception("Only a single response is supported.");

            foreach (var operation in entry.Value.Operations)
            {
                var response = operation.Value.Responses.First();

                if (response.Value.Content.Count == 0)
                {
                    AppendImplementationMethodSourceText(
                        path: entry.Key,
                        methodSuffix: "",
                        operation.Key,
                        operation.Value,
                        response,
                        responseType: default,
                        sourceTextBuilder,
                        async: false);

                    sourceTextBuilder.AppendLine();

                    AppendImplementationMethodSourceText(
                        path: entry.Key,
                        methodSuffix: "",
                        operation.Key,
                        operation.Value,
                        response,
                        responseType: default,
                        sourceTextBuilder,
                        async: true);

                    sourceTextBuilder.AppendLine();   
                }

                else
                {
                    foreach (var responseType in response.Value.Content)
                    {
                        var methodSuffix = response.Value.Content.Count == 1
                            ? ""
                            : _methodNameSuffixes[responseType.Key];

                        AppendImplementationMethodSourceText(
                            path: entry.Key,
                            methodSuffix,
                            operation.Key,
                            operation.Value,
                            response,
                            responseType,
                            sourceTextBuilder,
                            async: false);

                        sourceTextBuilder.AppendLine();

                        AppendImplementationMethodSourceText(
                            path: entry.Key,
                            methodSuffix,
                            operation.Key,
                            operation.Value,
                            response,
                            responseType,
                            sourceTextBuilder,
                            async: true);

                        sourceTextBuilder.AppendLine();
                    }
                }
            }
        }

        sourceTextBuilder.AppendLine("}");
    }

    private void AppendInterfaceMethodSourceText(
        string path,
        string methodSuffix,
        OperationType operationType,
        OpenApiOperation operation,
        KeyValuePair<string, OpenApiResponse> response,
        KeyValuePair<string, OpenApiMediaType>? responseType,
        StringBuilder sourceTextBuilder,
        bool async)
    {
        var signature = GetMethodSignature(
            path,
            methodSuffix,
            operationType,
            operation,
            response,
            responseType,
            async: async,
            out var returnType,
            out var parameters,
            out var body);

        var preparedReturnType = string.IsNullOrWhiteSpace(returnType)
            ? async ? "Task" : "void"
            : async ? $"Task<{returnType}>" : returnType;

        sourceTextBuilder.AppendLine(
$@"    /// <summary>
    /// {GetFirstLine(operation.Summary)}
    /// </summary>");

        foreach (var parameter in parameters)
        {
            sourceTextBuilder.AppendLine($"    /// <param name=\"{parameter.Item2.Name}\">{GetFirstLine(parameter.Item2.Description ?? parameter.Item2.Schema.Description)}</param>");
        }

        if (operation.RequestBody is not null && body is not null)
            sourceTextBuilder.AppendLine($"    /// <param name=\"{body.Split(" ")[^1]}\">{GetFirstLine(operation.RequestBody.Description)}</param>");

        if (async)
            sourceTextBuilder.AppendLine($"    /// <param name=\"cancellationToken\">The token to cancel the current operation.</param>");

        sourceTextBuilder.AppendLine($"    {preparedReturnType} {signature};");
    }

    private void AppendImplementationMethodSourceText(
        string path,
        string methodSuffix,
        OperationType operationType,
        OpenApiOperation operation,
        KeyValuePair<string, OpenApiResponse> response,
        KeyValuePair<string, OpenApiMediaType>? responseType,
        StringBuilder sourceTextBuilder,
        bool async)
    {
        var signature = GetMethodSignature(
            path,
            methodSuffix,
            operationType,
            operation,
            response,
            responseType,
            async: async,
            out var returnType,
            out var parameters,
            out var bodyParameter);

        sourceTextBuilder
            .AppendLine("    /// <inheritdoc />");

        var isVoidReturnType = string.IsNullOrWhiteSpace(returnType);

        var actualReturnType = isVoidReturnType
            ? async ? "Task" : "void"
            : async ? $"Task<{returnType}>" : returnType;

        sourceTextBuilder
            .AppendLine($"    public {actualReturnType} {signature}")
            .AppendLine($"    {{");

        sourceTextBuilder
            .AppendLine("        var __urlBuilder = new StringBuilder();")
            .AppendLine($"        __urlBuilder.Append(\"{path}\");");

        // path parameters
        var pathParameters = parameters
            .Where(parameter => parameter.Item2.In == ParameterLocation.Path)
            .ToList();

        foreach (var parameter in pathParameters)
        {
            var parameterName = parameter.Item1.Split(" ")[1];
            var parameterToStringCode = GetParameterToStringCode(parameterName, parameter.Item2.Schema);
            sourceTextBuilder.AppendLine($"        __urlBuilder.Replace(\"{{{parameterName}}}\", Uri.EscapeDataString({parameterToStringCode}));");
        }

        // query parameters
        var queryParameters = parameters
            .Where(parameter => parameter.Item2.In == ParameterLocation.Query)
            .ToList();

        if (queryParameters.Any())
        {
            sourceTextBuilder.AppendLine();
            sourceTextBuilder.AppendLine("        var __queryValues = new Dictionary<string, string>();");
            sourceTextBuilder.AppendLine();

            foreach (var parameter in queryParameters)
            {
                var parameterName = parameter.Item1.Split(" ")[1];
                var parameterToStringCode = GetParameterToStringCode(parameterName, parameter.Item2.Schema);
                var parameterValue = $"Uri.EscapeDataString({parameterToStringCode})";

                if (!parameter.Item2.Required || parameter.Item2.Schema.Nullable)
                {
                    sourceTextBuilder.AppendLine($"        if ({parameterName} is not null)");
                    sourceTextBuilder.AppendLine($"            __queryValues[\"{parameterName}\"] = {parameterValue};");
                }

                else
                {
                    sourceTextBuilder.AppendLine($"        __queryValues[\"{parameterName}\"] = {parameterValue};");
                }

                sourceTextBuilder.AppendLine();
            }

            sourceTextBuilder.AppendLine("        var __query = \"?\" + string.Join('&', __queryValues.Select(entry => $\"{entry.Key}={entry.Value}\"));");
            sourceTextBuilder.AppendLine("        __urlBuilder.Append(__query);");
        }

        // url
        sourceTextBuilder.AppendLine();
        sourceTextBuilder.Append("        var __url = __urlBuilder.ToString();");
        sourceTextBuilder.AppendLine();

        if (isVoidReturnType)
            returnType = "object";

        var acceptHeaderValue = responseType.HasValue
            ? $"\"{responseType.Value.Key}\""
            : "default";

        var contentTypeValue = operation.RequestBody is null
            ? "default"
            : $"\"{operation.RequestBody?.Content.Keys.First()}\"";

        var content = bodyParameter is null
            ? "default"
            : operation.RequestBody?.Content.Keys.First() switch
            {
                "application/json" => $"JsonContent.Create({bodyParameter.Split(" ")[^1]}, options: Utilities.JsonOptions)",
                "application/octet-stream" => $"new StreamContent({bodyParameter.Split(" ")[^1]})",
                _ => throw new Exception($"The media type {operation.RequestBody!.Content.Keys.First()} is not supported.")
            };

        if (async)
            sourceTextBuilder.AppendLine($"        return ___client.InvokeAsync<{returnType}>(\"{operationType.ToString().ToUpper()}\", __url, {acceptHeaderValue}, {contentTypeValue}, {content}, cancellationToken);");

        else
            sourceTextBuilder.AppendLine($"        {(isVoidReturnType ? "" : "return ")}___client.Invoke<{returnType}>(\"{operationType.ToString().ToUpper()}\", __url, {acceptHeaderValue}, {contentTypeValue}, {content});");

        sourceTextBuilder.AppendLine($"    }}");
    }

    private void AppendModelSourceText(
        string modelName,
        OpenApiSchema schema,
        StringBuilder sourceTextBuilder)
    {
        // Maybe schema.Extensions[0].x-enumNames would be a better selection.

        if (schema.Enum.Any())
        {
            if (schema.Type != "string")
                throw new Exception("Only enum of type string is supported.");

            var enumValues = string
                .Join($",{Environment.NewLine}{Environment.NewLine}", schema.Enum
                .OfType<OpenApiString>()
                .Select(current =>
$@"    /// <summary>
    /// {GetFirstLine(current.Value)}
    /// </summary>
    {current.Value}"));

            sourceTextBuilder.AppendLine(
@$"/// <summary>
/// {GetFirstLine(schema.Description)}
/// </summary>");

            sourceTextBuilder.AppendLine(
@$"public enum {modelName}
{{
{enumValues}
}}");

            sourceTextBuilder.AppendLine();
        }

        else
        {
            var parameters = schema.Properties is null
               ? string.Empty
               : GetProperties(schema.Properties, anonymousTypePrefix: modelName);

            sourceTextBuilder.AppendLine(
@$"/// <summary>
/// {GetFirstLine(schema.Description)}
/// </summary>");

            if (schema.Properties is not null)
            {
                foreach (var property in schema.Properties)
                {
                    sourceTextBuilder.AppendLine($"/// <param name=\"{Shared.FirstCharToUpper(property.Key)}\">{GetFirstLine(property.Value.Description)}</param>");
                }
            }

            sourceTextBuilder
                .AppendLine($"public record {modelName}({parameters});");
        }
    }

    private string GetParameterToStringCode(string parameterName, OpenApiSchema schema)
    {
        var type = GetType(schema, anonymousTypeName: default, isRequired: true);

        return type switch
        {
            "DateTime" => $"{parameterName}.ToString(\"o\", CultureInfo.InvariantCulture)",
            "string" => parameterName,
            _ => $"Convert.ToString({parameterName}, CultureInfo.InvariantCulture)!"
        };
    }

    private string GetProperties(IDictionary<string, OpenApiSchema> propertyMap, string anonymousTypePrefix)
    {
        var methodParameters = propertyMap.Select(entry =>
        {
            var parameterName = Shared.FirstCharToUpper(entry.Key);
            var anonymousTypeName = $"{anonymousTypePrefix}{parameterName}Type";
            var type = GetType(entry.Value, anonymousTypeName, isRequired: true);
            return $"{type} {parameterName}";
        });

        return string.Join(", ", methodParameters);
    }

    private string GetType(string mediaTypeKey, OpenApiMediaType mediaType, string? anonymousTypeName, bool isRequired, bool returnValue = false)
    {
        return mediaTypeKey switch
        {
            "application/octet-stream" => returnValue ? "HttpResponseMessage" : "Stream",
            "application/json" => GetType(mediaType.Schema, anonymousTypeName, isRequired),
            _ => throw new Exception($"The media type {mediaTypeKey} is not supported.")
        };
    }

    private string GetType(OpenApiSchema schema, string? anonymousTypeName, bool isRequired)
    {
        string type;

        if (schema.Reference is null)
        {
            type = (schema.Type, schema.Format, schema.AdditionalProperties) switch
            {
                (null, _, _) => schema.OneOf.Count switch
                {
                    0 => "JsonElement",
                    1 => GetType(schema.OneOf.First(), anonymousTypeName, isRequired),
                    _ => throw new Exception("Only zero or one entries are supported.")
                },
                ("boolean", _, _) => "bool",
                ("number", "double", _) => "double",
                ("number", _, _) => "double",
                ("integer", "int32", _) => "int",
                ("integer", _, _) => "int",
                ("string", "uri", _) => "Uri",
                ("string", "guid", _) => "Guid",
                ("string", "duration", _) => "TimeSpan",
                ("string", "date-time", _) => "DateTime",
                ("string", _, _) => "string",
                ("array", _, _) => $"IReadOnlyList<{GetType(schema.Items, anonymousTypeName, isRequired)}>",
                ("object", _, null) => GetAnonymousType(anonymousTypeName ?? throw new Exception("Type name required."), schema),
                ("object", _, _) => $"IReadOnlyDictionary<string, {GetType(schema.AdditionalProperties, anonymousTypeName, isRequired)}>",
                (_, _, _) => throw new Exception($"The schema type {schema.Type} (or one of its formats) is not supported.")
            };
        }

        else
        {
            type = schema.Reference.Id;
        }

        return (schema.Nullable || !isRequired)
            ? $"{type}?"
            : type;
    }

    private string GetAnonymousType(string anonymousTypeName, OpenApiSchema schema)
    {
        var modelName = anonymousTypeName;
        var stringBuilder = new StringBuilder();

        AppendModelSourceText(modelName: modelName, schema, stringBuilder);

        var modelText = stringBuilder.ToString();
        _additionalModels[modelName] = modelText;

        return modelName;
    }

    private string GetMethodSignature(
        string path,
        string methodSuffix,
        OperationType operationType,
        OpenApiOperation operation,
        KeyValuePair<string, OpenApiResponse> response,
        KeyValuePair<string, OpenApiMediaType>? responseType,
        bool async,
        out string returnType,
        out IEnumerable<(string, OpenApiParameter)> parameters,
        out string? bodyParameter)
    {
        if (!(operationType == OperationType.Get ||
            operationType == OperationType.Put ||
            operationType == OperationType.Post ||
            operationType == OperationType.Delete))
            throw new Exception("Only get, put, post or delete operations are supported.");

        var methodName = _settings.GetOperationName(path, operationType, operation) + methodSuffix;
        var asyncMethodName = methodName + "Async";

        if (!(response.Key == "200" || response.Key == "201"))
            throw new Exception("Only response types '200' or '201' are supported.");

        var anonymousReturnTypeName = $"{methodName}Response";

        returnType = responseType.HasValue switch
        {
            true => $"{GetType(
                responseType.Value.Key, 
                responseType.Value.Value, 
                anonymousReturnTypeName,
                isRequired: true, 
                returnValue: true)}",

            false => string.Empty
        };

        parameters = Enumerable.Empty<(string, OpenApiParameter)>();
        bodyParameter = default;

        if (!operation.Parameters.Any() && operation.RequestBody is null)
        {
            return async
                ? $"{asyncMethodName}(CancellationToken cancellationToken = default)"
                : $"{methodName}()";
        }

        else
        {
            // if (operation.Parameters.Any(parameter
            //     => parameter.In != ParameterLocation.Path && parameter.In != ParameterLocation.Query))
            //     throw new Exception("Only path or query parameters are supported.");
            parameters = operation.Parameters
                .Where(parameter => parameter.In == ParameterLocation.Query || parameter.In == ParameterLocation.Path)
                .Select(parameter => ($"{GetType(parameter.Schema, anonymousTypeName: default, parameter.Required)} {parameter.Name}{(parameter.Required ? "" : " = default")}", parameter));

            if (operation.RequestBody is not null)
            {
                if (operation.RequestBody.Content.Count != 1)
                    throw new Exception("Only a single request body content is supported.");

                var content = operation.RequestBody.Content.First();

                if (!(content.Key == "application/json" || content.Key == "application/octet-stream"))
                    throw new Exception("Only body content media types application/json or application/octet-stream are supported.");

                string type;
                string name;

                var isRequired = operation.RequestBody.Required;

                if (operation.RequestBody.Extensions.TryGetValue("x-name", out var value))
                {
                    if (value is not OpenApiString openApiString)
                        throw new Exception("The actual x-name value type is not supported.");

                    var anonymousRequestTypeName = $"{methodName}Request";

                    type = GetType(content.Key, content.Value, anonymousTypeName: anonymousRequestTypeName, isRequired: operation.RequestBody.Required);
                    name = openApiString.Value;
                }
                else
                {
                    type = isRequired ? "JsonElement" : "JsonElement?";
                    name = "body";
                }

                bodyParameter = $"{type} {name}";
            }

            var parametersString = bodyParameter == default

                ? string.Join(", ", parameters
                    .OrderByDescending(parameter => parameter.Item2.Required)
                    .Select(parameter => parameter.Item1))

                : string.Join(", ", parameters
                    .Concat(new[] { (bodyParameter, default(OpenApiParameter)!) })
                    .OrderByDescending(parameter => parameter.Item2 is null || parameter.Item2.Required)
                    .Select(parameter => parameter.Item1));

            return async
                ? $"{asyncMethodName}({parametersString}, CancellationToken cancellationToken = default)"
                : $"{methodName}({parametersString})";
        }
    }

    private static string? GetFirstLine(string? value)
    {
        if (value is null)
            return null;

        using var reader = new StringReader(value);
        return reader.ReadLine();
    }
}