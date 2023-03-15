using System.Reflection;
using System.Text;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Stubble.Core.Builders;

namespace Apollo3zehn.OpenApiClientGenerator;

record SubClientProperties(
    string Fields,
    string FieldAssignments,
    string Properties,
    string Source,
    string InterfaceProperties);

public class PythonGenerator
{
    private readonly GeneratorSettings _settings;
    private Dictionary<string, string> _additionalModels = default!;

    private readonly Dictionary<string, string> _methodNameSuffixes = new()
    {
        ["application/octet-stream"] = "_as_stream",
        ["application/json"] = "_as_json"
    };

    public PythonGenerator(GeneratorSettings settings)
    {
        _settings = settings;
    }

    public string Generate(OpenApiDocument document)
    {
        _additionalModels = new();
        var sourceTextBuilder = new StringBuilder();
        var stubble = new StubbleBuilder().Build();

        using var clientTemplateStreamReader = new StreamReader(Assembly
            .GetExecutingAssembly()
            .GetManifestResourceStream("Apollo3zehn.OpenApiClientGenerator.Templates.PythonClientTemplate.py")!);

        var clientTemplate = clientTemplateStreamReader.ReadToEnd();

        // Build async client
        var asyncClients = GenerateClients(document, sourceTextBuilder, async: true);

        var data = new
        {
            ClientName = _settings.ClientName,
            TokenFoldername = _settings.TokenFolderName,
            ConfigurationHeaderKey = _settings.ConfigurationHeaderKey,
            SubClientFields = asyncClients.Fields,
            SubClientFieldAssignments = asyncClients.FieldAssignments,
            SubClientProperties = asyncClients.Properties,
            ExceptionType = _settings.ExceptionType,
            ExceptionCodePrefix = _settings.ExceptionCodePrefix,
            Async = "Async",
            Def = "async def",
            Await = "await ",
            Aclose = "aclose",
            Aiter_bytes = "aiter_bytes",
            AsyncioSleep = "asyncio.sleep",
            Enter = "aenter",
            Exit = "aexit",
            Read = "aread",
            For = "async for",
            Special_RefreshTokenSupport = _settings.Special_RefreshTokenSupport,
            Special_NexusFeatures = _settings.Special_NexusFeatures
        };

        var asyncClient = stubble.Render(clientTemplate, data);

        // Build sync client
        var syncClients = GenerateClients(document, sourceTextBuilder, async: false);

        var data2 = new
        {
            ClientName = _settings.ClientName,
            TokenFoldername = _settings.TokenFolderName,
            ConfigurationHeaderKey = _settings.ConfigurationHeaderKey,
            SubClientFields = syncClients.Fields,
            SubClientFieldAssignments = syncClients.FieldAssignments,
            SubClientProperties = syncClients.Properties,
            ExceptionType = _settings.ExceptionType,
            ExceptionCodePrefix = _settings.ExceptionCodePrefix,
            Async = "",
            Def = "def",
            Await = "",
            Aclose = "close",
            Aiter_bytes = "iter_bytes",
            AsyncioSleep = "time.sleep",
            Enter = "enter",
            Exit = "exit",
            Read = "read",
            For = "for",
            Special_RefreshTokenSupport = _settings.Special_RefreshTokenSupport,
            Special_NexusFeatures = _settings.Special_NexusFeatures
        };

        var syncClient = stubble.Render(clientTemplate, data2);

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

        using var encoderStreamReader = new StreamReader(Assembly
            .GetExecutingAssembly()
            .GetManifestResourceStream("Apollo3zehn.OpenApiClientGenerator.Templates.PythonEncoder.py")!);

        var encoder = encoderStreamReader.ReadToEnd();

        using var finalTemplateStreamReader = new StreamReader(Assembly
            .GetExecutingAssembly()
            .GetManifestResourceStream("Apollo3zehn.OpenApiClientGenerator.Templates.PythonTemplate.py")!);

        var finalTemplate = finalTemplateStreamReader.ReadToEnd();

        var data3 = new
        {
            Encoder = encoder,
            AsyncSubClientsSource = asyncClients.Source,
            SyncSubClientsSource = syncClients.Source,
            ExceptionType = _settings.ExceptionType,
            Models = models,
            AsyncClient = asyncClient,
            SyncClient = syncClient,
            Special_RefreshTokenSupport = _settings.Special_RefreshTokenSupport,
            Special_NexusFeatures = _settings.Special_NexusFeatures
        };

        return stubble.Render(finalTemplate, data3);
    }

    private SubClientProperties GenerateClients(OpenApiDocument document, StringBuilder sourceTextBuilder, bool async)
    {
        var prefix = async ? "Async" : "";

        // add clients
        var groupedClients = document.Paths
            .SelectMany(path => path.Value.Operations.First().Value.Tags.Select(tag => (path, tag)))
            .GroupBy(value => value.tag.Name);

        var subClients = groupedClients.Select(group => group.Key);

        // Fields
        sourceTextBuilder.Clear();

        foreach (var subClient in subClients)
        {
            sourceTextBuilder.AppendLine($"    _{Shared.FirstCharToLower(subClient)}: {subClient}{prefix}Client");
        }

        var fields = sourceTextBuilder.ToString();

        // FieldAssignments
        sourceTextBuilder.Clear();

        foreach (var subClient in subClients)
        {
            sourceTextBuilder.AppendLine($"        self._{Shared.FirstCharToLower(subClient)} = {subClient}{prefix}Client(self)");
        }

        var fieldAssignments = sourceTextBuilder.ToString();

        // Properties
        sourceTextBuilder.Clear();

        foreach (var subClient in subClients)
        {
            sourceTextBuilder.AppendLine(
$@"    @property
    def {Shared.ToSnakeCase(subClient)}(self) -> {subClient}{prefix}Client:
        """"""Gets the {subClient}{prefix}Client.""""""
        return self._{Shared.FirstCharToLower(subClient)}
");
        }

        var properties = sourceTextBuilder.ToString();

        // InterfaceProperties
        var interfaceProperties = string.Empty;

        // Source
        sourceTextBuilder.Clear();

        foreach (var clientGroup in groupedClients)
        {
            AppendSubClientSourceText(
                clientGroup.Key,
                clientGroup.ToDictionary(entry => entry.path.Key, entry => entry.path.Value),
                sourceTextBuilder,
                async);

            sourceTextBuilder.AppendLine();
        }

        var source = sourceTextBuilder.ToString();

        return new SubClientProperties
        (
            Fields: fields,
            FieldAssignments: fieldAssignments,
            Properties: properties,
            Source: source,
            InterfaceProperties: interfaceProperties
        );
    }

    private void AppendSubClientSourceText(
        string className,
        IDictionary<string, OpenApiPathItem> methodMap,
        StringBuilder sourceTextBuilder,
        bool async)
    {
        var prefix = async ? "Async" : "";
        var augmentedClassName = className + prefix + "Client";

        // interface
        /* nothing to do here */

        // implementation
        sourceTextBuilder.AppendLine(
$@"class {augmentedClassName}:
    """"""Provides methods to interact with {Shared.SplitCamelCase(className).ToLower()}.""""""

    ___client: {_settings.ClientName}{prefix}Client
    
    def __init__(self, client: {_settings.ClientName}{prefix}Client):
        self.___client = client
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
                        async);

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
                            async);

                        sourceTextBuilder.AppendLine();
                    }
                }
            }
        }
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
            out var returnType,
            out var parameters,
            out var bodyParameter);

        var isVoidReturnType = string.IsNullOrWhiteSpace(returnType);
        var actualReturnType = isVoidReturnType ? "None" : $"{returnType}";
        var actualActualReturnType = async ? $"Awaitable[{actualReturnType}]" : actualReturnType;

        sourceTextBuilder.AppendLine(
@$"    def {signature} -> {actualActualReturnType}:
        """"""
        {GetFirstLine(operation.Summary)}

        Args:");

        foreach (var parameter in parameters)
        {
            var parameterName = parameter.Item1.Split(":")[0];
            sourceTextBuilder.AppendLine($"            {parameterName}: {GetFirstLine(parameter.Item2.Description)}");
        }

        sourceTextBuilder.AppendLine(@"        """"""
");

        sourceTextBuilder
            .AppendLine($"        __url = \"{path}\"");

        // path parameters
        var pathParameters = parameters
            .Where(parameter => parameter.Item2.In == ParameterLocation.Path)
            .ToList();

        foreach (var parameter in pathParameters)
        {
            var originalParameterName = parameter.Item2.Name;
            var parameterName = parameter.Item1.Split(":")[0];
            sourceTextBuilder.AppendLine($"        __url = __url.replace(\"{{{originalParameterName}}}\", quote(str({parameterName}), safe=\"\"))");
        }

        // query parameters
        var queryParameters = parameters
            .Where(parameter => parameter.Item2.In == ParameterLocation.Query)
            .ToList();

        if (queryParameters.Any())
        {
            sourceTextBuilder.AppendLine();
            sourceTextBuilder.AppendLine("        __query_values: dict[str, str] = {}");
            sourceTextBuilder.AppendLine();

            foreach (var parameter in queryParameters)
            {
                var originalParameterName = parameter.Item2.Name;
                var parameterName = parameter.Item1.Split(":")[0];
                var parameterValue = $"quote(_to_string({parameterName}), safe=\"\")";

                if (!parameter.Item2.Required || parameter.Item2.Schema.Nullable)
                {
                    sourceTextBuilder.AppendLine($"        if {parameterName} is not None:");
                    sourceTextBuilder.AppendLine($"            __query_values[\"{originalParameterName}\"] = {parameterValue}");
                }

                else
                {
                    sourceTextBuilder.AppendLine($"        __query_values[\"{originalParameterName}\"] = {parameterValue}");
                }

                sourceTextBuilder.AppendLine();
            }

            sourceTextBuilder.AppendLine("        __query: str = \"?\" + \"&\".join(f\"{key}={value}\" for (key, value) in __query_values.items())");
            sourceTextBuilder.AppendLine("        __url += __query");
        }

        if (isVoidReturnType)
            returnType = "type(None)";

        var acceptHeaderValue = responseType.HasValue
            ? $"\"{responseType.Value.Key}\""
            : "None";

        var contentTypeValue = operation.RequestBody is null
            ? "None"
            : $"\"{operation.RequestBody?.Content.Keys.First()}\"";

        var content = bodyParameter is null
            ? "None"
            : operation.RequestBody?.Content.Keys.First() switch
            {
                "application/json" => $"json.dumps(JsonEncoder.encode({bodyParameter.Split(":")[0]}, _json_encoder_options))",
                "application/octet-stream" => bodyParameter.Split(":")[0],
                _ => throw new Exception($"The media type {operation.RequestBody!.Content.Keys.First()} is not supported.")
            };

        sourceTextBuilder.AppendLine();
        sourceTextBuilder.AppendLine($"        return self.___client._invoke({returnType}, \"{operationType.ToString().ToUpper()}\", __url, {acceptHeaderValue}, {contentTypeValue}, {content})");
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
                .Join($"{Environment.NewLine}{Environment.NewLine}", schema.Enum
                .OfType<OpenApiString>()
                .Select(current =>
$@"    {Shared.ToSnakeCase(current.Value).ToUpper()} = ""{Shared.ToSnakeCase(current.Value).ToUpper()}""
    """"""{GetFirstLine(current.Value)}"""""""));

            sourceTextBuilder.AppendLine(
@$"class {modelName}(Enum):
    """"""{GetFirstLine(schema.Description)}""""""

{enumValues}");

            sourceTextBuilder.AppendLine();
        }

        else
        {
            sourceTextBuilder
                .AppendLine(
$@"@dataclass(frozen=True)
class {modelName}:");

            sourceTextBuilder.AppendLine(
@$"    """"""
    {GetFirstLine(schema.Description)}

    Args:");

            if (schema.Properties is not null)
            {
                foreach (var property in schema.Properties)
                {
                    sourceTextBuilder.AppendLine($"        {Shared.ToSnakeCase(property.Key)}: {GetFirstLine(property.Value.Description)}");
                }
            }

            sourceTextBuilder.AppendLine(
@"    """"""
");

            if (schema.Properties is not null)
            {
                foreach (var property in schema.Properties)
                {
                    var propertyName = Shared.ToSnakeCase(property.Key);

                    if (propertyName == "class")
                        propertyName = "class_";

                    var anonymousTypePrefix = modelName;
                    var anonymousTypeName = $"{anonymousTypePrefix}{Shared.FirstCharToUpper(property.Key)}Type";
                    var type = GetType(property.Value, anonymousTypeName, isRequired: true);

                    sourceTextBuilder.AppendLine(
$@"    {propertyName}: {type}
    """"""{property.Value.Description}""""""
");
                }
            }
        }
    }

    private string GetType(string mediaTypeKey, OpenApiMediaType mediaType, string? anonymousTypeName, bool isRequired, bool returnValue = false)
    {
        return mediaTypeKey switch
        {
            "application/octet-stream" => returnValue ? "Response" : "Union[bytes, Iterable[bytes], AsyncIterable[bytes]]",
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
                    0 => "object",
                    1 => GetType(schema.OneOf.First(), anonymousTypeName, isRequired),
                    _ => throw new Exception("Only zero or one entries are supported.")
                },
                ("boolean", _, _) => "bool",
                ("number", "double", _) => "float",
                ("number", _, _) => "float",
                ("integer", "int32", _) => "int",
                ("integer", _, _) => "int",
                ("string", "uri", _) => "str",
                ("string", "guid", _) => "UUID",
                ("string", "duration", _) => "timedelta",
                ("string", "date-time", _) => "datetime",
                ("string", _, _) => "str",
                ("array", _, _) => $"list[{GetType(schema.Items, anonymousTypeName, isRequired)}]",
                ("object", _, null) => GetAnonymousType(anonymousTypeName ?? throw new Exception("Type name required."), schema),
                ("object", _, _) => $"dict[str, {GetType(schema.AdditionalProperties, anonymousTypeName, isRequired)}]",
                (_, _, _) => throw new Exception($"The schema type {schema.Type} (or one of its formats) is not supported.")
            };
        }

        else
        {
            type = schema.Reference.Id;
        }

        return (schema.Nullable || !isRequired)
            ? $"Optional[{type}]"
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
        var asyncMethodName = methodName; // + "Async";

        if (!(response.Key == "200" || response.Key == "201"))
            throw new Exception("Only response types '200' or '201' are supported.");

        var anonymousReturnTypeName = $"{methodName}Response";

        returnType = responseType.HasValue switch
        {
            true => $"{GetType(responseType.Value.Key, responseType.Value.Value, anonymousReturnTypeName, isRequired: true, returnValue: true)}",
            false => string.Empty
        };

        parameters = Enumerable.Empty<(string, OpenApiParameter)>();
        bodyParameter = default;

        if (!operation.Parameters.Any() && operation.RequestBody is null)
        {
            return $"{Shared.ToSnakeCase(asyncMethodName)}(self)";
        }

        else
        {
            // if (operation.Parameters.Any(parameter
            //     => parameter.In != ParameterLocation.Path && parameter.In != ParameterLocation.Query))
            //     throw new Exception("Only path or query parameters are supported.");

            parameters = operation.Parameters
                .Where(parameter => parameter.In == ParameterLocation.Query || parameter.In == ParameterLocation.Path)
                .Select(parameter => ($"{Shared.ToSnakeCase(parameter.Name)}: {GetType(parameter.Schema, anonymousTypeName: default, parameter.Required)}{(parameter.Required ? "" : " = None")}", parameter));

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

                    type = GetType(content.Key, content.Value, anonymousTypeName: anonymousRequestTypeName, isRequired: isRequired);
                    name = openApiString.Value;
                }
                else
                {
                    type = isRequired ? "object" : "Optional[object]";
                    name = "body";
                }

                bodyParameter = $"{Shared.ToSnakeCase(name)}: {type}";
            }

            var parametersString = bodyParameter == default

                ? string.Join(", ", parameters
                    .OrderByDescending(parameter => parameter.Item2.Required)
                    .Select(parameter => parameter.Item1))

                : string.Join(", ", parameters
                    .Concat(new[] { (bodyParameter, default(OpenApiParameter)!) })
                    .OrderByDescending(parameter => parameter.Item2 is null || parameter.Item2.Required)
                    .Select(parameter => parameter.Item1));

            return $"{Shared.ToSnakeCase(asyncMethodName)}(self, {parametersString})";
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