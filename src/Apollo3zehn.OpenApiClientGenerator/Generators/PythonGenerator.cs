using System.Reflection;
using System.Text;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Stubble.Core.Builders;

namespace Apollo3zehn.OpenApiClientGenerator
{
    record SubClientProperties(
        string Fields,
        string FieldAssignments,
        string Properties,
        string Source,
        string InterfaceProperties);

    public class PythonGenerator
    {
        public string Generate(OpenApiDocument document, GeneratorSettings settings)
        {
            var sourceTextBuilder = new StringBuilder();
            var stubble = new StubbleBuilder().Build();

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

            var models = sourceTextBuilder.ToString();

            using var clientTemplateStreamReader = new StreamReader(Assembly
                .GetExecutingAssembly()
                .GetManifestResourceStream("Apollo3zehn.OpenApiClientGenerator.Templates.PythonClientTemplate.py")!);

            var clientTemplate = clientTemplateStreamReader.ReadToEnd();

            // Build async client
            var asyncClients = GenerateClients(document, sourceTextBuilder, settings, async: true);

            var data = new
            {
                ClientName = settings.ClientName,
                TokenFoldername = settings.TokenFolderName,
                ConfigurationHeaderKey = settings.ConfigurationHeaderKey,
                SubClientFields = asyncClients.Fields,
                SubClientFieldAssignments = asyncClients.FieldAssignments,
                SubClientProperties = asyncClients.Properties,
                ExceptionType = settings.ExceptionType,
                ExceptionCodePrefix = settings.ExceptionCodePrefix,
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
                Special_NexusFeatures = settings.Special_NexusFeatures
            };

            var asyncClient = stubble.Render(clientTemplate, data);

            // Build sync client
            var syncClients = GenerateClients(document, sourceTextBuilder, settings, async: false);

            var data2 = new
            {
                ClientName = settings.ClientName,
                TokenFoldername = settings.TokenFolderName,
                ConfigurationHeaderKey = settings.ConfigurationHeaderKey,
                SubClientFields = syncClients.Fields,
                SubClientFieldAssignments = syncClients.FieldAssignments,
                SubClientProperties = syncClients.Properties,
                ExceptionType = settings.ExceptionType,
                ExceptionCodePrefix = settings.ExceptionCodePrefix,
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
                Special_NexusFeatures = settings.Special_NexusFeatures
            };

            var syncClient = stubble.Render(clientTemplate, data2);

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
                ExceptionType = settings.ExceptionType,
                Models = models,
                AsyncClient = asyncClient,
                SyncClient = syncClient,
                Special_NexusFeatures = settings.Special_NexusFeatures
            };

            return stubble.Render(finalTemplate, data3);
        }

        private SubClientProperties GenerateClients(OpenApiDocument document, StringBuilder sourceTextBuilder, GeneratorSettings settings, bool async)
        {
            var prefix = async ? "Async" : "";

            // add clients
            var groupedClients = document.Paths
                .GroupBy(path => path.Value.Operations.First().Value.OperationId.Split(new[] { '_' }, 2).First());

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
                    clientGroup.ToDictionary(entry => entry.Key, entry => entry.Value),
                    sourceTextBuilder,
                    settings,
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
            GeneratorSettings settings,
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

    ___client: {settings.ClientName}{prefix}Client
    
    def __init__(self, client: {settings.ClientName}{prefix}Client):
        self.___client = client
");

            foreach (var entry in methodMap)
            {
                if (entry.Value.Parameters.Any())
                    throw new Exception("Parameters on the path item level are not supported.");

                foreach (var operation in entry.Value.Operations)
                {
                    AppendImplementationMethodSourceText(
                        path: entry.Key,
                        operation.Key,
                        operation.Value,
                        sourceTextBuilder,
                        async);

                    sourceTextBuilder.AppendLine();
                }
            }
        }

        private void AppendImplementationMethodSourceText(
            string path,
            OperationType operationType,
            OpenApiOperation operation,
            StringBuilder sourceTextBuilder,
            bool async)
        {
            var signature = GetMethodSignature(
                operationType,
                operation,
                out var returnType,
                out var parameters,
                out var bodyParameter);

            var isVoidReturnType = string.IsNullOrWhiteSpace(returnType);
            var actualReturnType = isVoidReturnType ? "None" : $"{returnType}";
            var actualActualReturnType = async ? $"Awaitable[{actualReturnType}]" : actualReturnType;

            sourceTextBuilder.AppendLine(
@$"    def {signature} -> {actualActualReturnType}:
        """"""
        {operation.Summary}

        Args:");

            foreach (var parameter in parameters)
            {
                var parameterName = parameter.Item1.Split(":")[0];
                sourceTextBuilder.AppendLine($"            {parameterName}: {parameter.Item2.Description}");
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

                    if (parameter.Item2.Schema.Nullable)
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

            var response = operation.Responses.First().Value.Content.FirstOrDefault();

            var acceptHeaderValue = response.Equals(default(KeyValuePair<string, OpenApiMediaType>))
                ? "None"
                : $"\"{response.Key}\"";

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
    """"""{current.Value}"""""""));

                sourceTextBuilder.AppendLine(
@$"class {modelName}(Enum):
    """"""{schema.Description}""""""

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
    {schema.Description}

    Args:");

                if (schema.Properties is not null)
                {
                    foreach (var property in schema.Properties)
                    {
                        sourceTextBuilder.AppendLine($"        {Shared.ToSnakeCase(property.Key)}: {property.Value.Description}");
                    }
                }

                sourceTextBuilder.AppendLine(
@"    """"""
");

                if (schema.Properties is not null)
                {
                    foreach (var property in schema.Properties)
                    {
                        var type = GetType(property.Value);
                        var propertyName = Shared.ToSnakeCase(property.Key);

                        sourceTextBuilder.AppendLine(
$@"    {propertyName}: {type}
    """"""{property.Value.Description}""""""
");
                    }
                }
            }
        }

        private string GetType(string mediaTypeKey, OpenApiMediaType mediaType, bool returnValue = false)
        {
            return mediaTypeKey switch
            {
                "application/octet-stream" => returnValue ? "Response" : "Union[bytes, Iterable[bytes], AsyncIterable[bytes]]",
                "application/json" => GetType(mediaType.Schema),
                _ => throw new Exception($"The media type {mediaTypeKey} is not supported.")
            };
        }

        private string GetType(OpenApiSchema schema)
        {
            string type;

            if (schema.Reference is null)
            {
                type = (schema.Type, schema.Format, schema.AdditionalPropertiesAllowed) switch
                {
                    (null, _, _) => schema.OneOf.Count switch
                    {
                        0 => "object",
                        1 => GetType(schema.OneOf.First()),
                        _ => throw new Exception("Only zero or one entries are supported.")
                    },
                    ("boolean", _, _) => "bool",
                    ("number", "double", _) => "float",
                    ("integer", "int32", _) => "int",
                    ("string", "uri", _) => "str",
                    ("string", "guid", _) => "UUID",
                    ("string", "duration", _) => "timedelta",
                    ("string", "date-time", _) => "datetime",
                    ("string", _, _) => "str",
                    ("array", _, _) => $"list[{GetType(schema.Items)}]",
                    ("object", _, true) => $"dict[str, {GetType(schema.AdditionalProperties)}]",
                    (_, _, _) => throw new Exception($"The schema type {schema.Type} (or one of its formats) is not supported.")
                };
            }

            else
            {
                type = schema.Reference.Id;
            }

            return schema.Nullable
                ? $"Optional[{type}]"
                : type;
        }

        private string GetMethodSignature(
            OperationType operationType,
            OpenApiOperation operation,
            out string returnType,
            out IEnumerable<(string, OpenApiParameter)> parameters,
            out string? bodyParameter)
        {
            if (!(operationType == OperationType.Get ||
                operationType == OperationType.Put ||
                operationType == OperationType.Post ||
                operationType == OperationType.Delete))
                throw new Exception("Only get, put, post or delete operations are supported.");

            var methodName = operation.OperationId.Split(new[] { '_' }, 2)[1];
            var asyncMethodName = methodName; // + "Async";

            if (operation.Responses.Count != 1)
                throw new Exception("Only a single response is supported.");

            var responseEntry = operation.Responses.First();
            var responseType = responseEntry.Key;
            var response = responseEntry.Value;

            if (responseType != "200")
                throw new Exception("Only response type '200' is supported.");

            returnType = response.Content.Count switch
            {
                0 => string.Empty,
                1 => $"{GetType(response.Content.Keys.First(), response.Content.Values.First(), returnValue: true)}",
                _ => throw new Exception("Only zero or one response contents are supported.")
            };

            parameters = Enumerable.Empty<(string, OpenApiParameter)>();
            bodyParameter = default;

            if (!operation.Parameters.Any() && operation.RequestBody is null)
            {
                return $"{Shared.ToSnakeCase(asyncMethodName)}(self)";
            }

            else
            {
                if (operation.Parameters.Any(parameter
                    => parameter.In != ParameterLocation.Path && parameter.In != ParameterLocation.Query))
                    throw new Exception("Only path or query parameters are supported.");

                parameters = operation.Parameters
                    .OrderByDescending(parameter => parameter.Required)
                    .Select(parameter => ($"{Shared.ToSnakeCase(parameter.Name)}: {GetType(parameter.Schema)}{(parameter.Required ? "" : " = None")}", parameter));

                if (operation.RequestBody is not null)
                {
                    if (operation.RequestBody.Content.Count != 1)
                        throw new Exception("Only a single request body content is supported.");

                    var content = operation.RequestBody.Content.First();

                    if (!(content.Key == "application/json" || content.Key == "application/octet-stream"))
                        throw new Exception("Only body content media types application/json or application/octet-stream are supported.");

                    if (!operation.RequestBody.Extensions.TryGetValue("x-name", out var value))
                        throw new Exception("x-name extension is missing.");


                    if (value is not OpenApiString name)
                        throw new Exception("The actual x-name value type is not supported.");

                    var type = GetType(content.Key, content.Value);
                    bodyParameter = $"{Shared.ToSnakeCase(name.Value)}: {type}";
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
    }
}