using System.Reflection;
using System.Text;
using System.Linq;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Stubble.Core.Builders;
using Stubble.Core.Settings;

namespace Apollo3zehn.OpenApiClientGenerator;

public class TypeScriptGenerator
{
    private readonly GeneratorSettings _settings;
    private Dictionary<string, string> _additionalModels = default!;

    private readonly Dictionary<string, string> _methodNameSuffixes = new()
    {
        ["application/vnd.apache.arrow.stream"] = "AsStream",
        ["application/octet-stream"] = "AsStream",
        ["application/json"] = "AsJson"
    };

    public TypeScriptGenerator(GeneratorSettings settings)
    {
        _settings = settings;
    }

    public void Generate(
        string targetFolderPath,
        params OpenApiDocument[] documents)
    {
        var modules = new Dictionary<string, string>();

        _additionalModels = new();

        var sourceTextBuilder = new StringBuilder();
        var stubble = new StubbleBuilder().Build();

        using var moduleTemplateStreamReader = new StreamReader(Assembly
            .GetExecutingAssembly()
            .GetManifestResourceStream("Apollo3zehn.OpenApiClientGenerator.Templates.TypeScriptClientTemplate_Module.ts")!);

        var moduleTemplate = moduleTemplateStreamReader.ReadToEnd();

        var versioningImportsBuilder = new StringBuilder();
        var versioningFieldsBuilder = new StringBuilder();
        var versioningFieldAssignmentsBuilder = new StringBuilder();
        var versioningInterfacePropertiesBuilder = new StringBuilder();

        // Clients
        foreach (var document in documents)
        {
            // Version
            var version = document.Info.Version
                .Replace('.', '_');

            if (string.IsNullOrWhiteSpace(version))
                throw new Exception("Invalid version in OpenApiDocument.");

            if (!char.IsLetter(version[0]))
                version = "V" + version;

            if (!char.IsUpper(version[0]))
                version = Shared.FirstCharToUpper(version);

            var versionLower = Shared.FirstCharToLower(version);

            // Versioning
            versioningImportsBuilder.AppendLine($"import {{ {version}, I{version} }} from \"./{version}\";");

            if (_settings.Special_NexusFeatures && version == "V1")
                versioningImportsBuilder.AppendLine($"import {{ CatalogItem, TaskStatus }} from \"./{version}\";");

            if (_settings.Special_NexusFeatures && version == "V2")
                versioningImportsBuilder.AppendLine($"import {{ BatchStreamRequest, ExportParameters, Precision }} from \"./{version}\";");

            versioningInterfacePropertiesBuilder.AppendLine($"    {versionLower}: I{version};");

            versioningFieldsBuilder.AppendLine($"    public {versionLower}: {version};");

            versioningFieldAssignmentsBuilder.AppendLine($"        this.{versionLower} = new {version}(this.invoke.bind(this));");

            // Client properties
            var clientProperties = GenerateClientProperties(
                document,
                sourceTextBuilder);

            var moduleData = new
            {
                Version = version,
                SubClientFields = clientProperties.Fields,
                SubClientFieldAssignments = clientProperties.FieldAssignments,
                SubClientProperties = clientProperties.Properties,
                SubClientInterfaceProperties = clientProperties.InterfaceProperties,
                SubClientSource = clientProperties.Source,
                Models = clientProperties.Models
            };

            var settings = new RenderSettings() { SkipHtmlEncoding = true };
            var module = stubble.Render(moduleTemplate, moduleData, settings);

            modules[version] = module;
        }

        // Main client
        using var mainClientTemplateStreamReader = new StreamReader(Assembly
            .GetExecutingAssembly()
            .GetManifestResourceStream("Apollo3zehn.OpenApiClientGenerator.Templates.TypeScriptClientTemplate_Main.ts")!);

        var mainClientTemplate = mainClientTemplateStreamReader.ReadToEnd();

        var mainClientData = new
        {
            ClientName = _settings.ClientName,
            VersioningImports = versioningImportsBuilder.ToString(),
            VersioningInterfaceProperties = versioningInterfacePropertiesBuilder.ToString(),
            VersioningFields = versioningFieldsBuilder.ToString(),
            VersioningFieldAssignments = versioningFieldAssignmentsBuilder.ToString(),
            ExceptionType = _settings.ExceptionType,
            ExceptionCodePrefix = _settings.ExceptionCodePrefix,
            Special_ConfigurationHeaderKey = _settings.Special_ConfigurationHeaderKey,
            Special_AccessTokenSupport = _settings.Special_AccessTokenSupport,
            Special_NexusFeatures = _settings.Special_NexusFeatures
        };

        var mainClient = stubble.Render(mainClientTemplate, mainClientData, new RenderSettings() { SkipHtmlEncoding = true });

        // Shared
        using var sharedTemplateStreamReader = new StreamReader(Assembly
            .GetExecutingAssembly()
            .GetManifestResourceStream("Apollo3zehn.OpenApiClientGenerator.Templates.TypeScriptSharedTemplate.ts")!);

        var sharedTemplate = sharedTemplateStreamReader.ReadToEnd();

        var sharedData = new
        {
            ExceptionType = _settings.ExceptionType,
            ExceptionCodePrefix = _settings.ExceptionCodePrefix
        };

        var shared = stubble.Render(sharedTemplate, sharedData, new RenderSettings() { SkipHtmlEncoding = true });

        // index.ts
        using var initStreamReader = new StreamReader(Assembly
            .GetExecutingAssembly()
            .GetManifestResourceStream("Apollo3zehn.OpenApiClientGenerator.Templates.TypeScriptInit.ts")!);

        var initTemplate = initStreamReader.ReadToEnd();

        var versionExports = string.Join("\n", modules.Keys.Select(version => $"export * as {version} from \"./{version}\";"));

        var initData = new
        {
            VersionExports = versionExports
        };

        var init = stubble.Render(initTemplate, initData, new RenderSettings() { SkipHtmlEncoding = true });

        // Write
        Directory.CreateDirectory(targetFolderPath);

        File.WriteAllText(Path.Combine(targetFolderPath, "index.ts"), init);
        File.WriteAllText(Path.Combine(targetFolderPath, "_client.ts"), mainClient);
        File.WriteAllText(Path.Combine(targetFolderPath, "_shared.ts"), shared);

        foreach (var (version, module) in modules)
        {
            File.WriteAllText(Path.Combine(targetFolderPath, $"{version}.ts"), module);
        }
    }

    private SubClientProperties GenerateClientProperties(
        OpenApiDocument document,
        StringBuilder sourceTextBuilder)
    {
        var groupedClients = document.Paths
            .SelectMany(path => path.Value.Operations.First().Value.Tags.Select(tag => (path, tag)))
            .GroupBy(value => value.tag.Name);

        var subClientNames = groupedClients.Select(group => group.Key).ToList();

        // Fields
        sourceTextBuilder.Clear();

        foreach (var subClient in subClientNames)
        {
            sourceTextBuilder.AppendLine($"    public {Shared.FirstCharToLower(subClient)}: {subClient}Client;");
        }

        var fields = sourceTextBuilder.ToString();

        // FieldAssignments
        sourceTextBuilder.Clear();

        foreach (var subClient in subClientNames)
        {
            sourceTextBuilder.AppendLine($"        this.{Shared.FirstCharToLower(subClient)} = new {subClient}Client(invoke);");
        }

        var fieldAssignments = sourceTextBuilder.ToString();

        // Properties (interface)
        sourceTextBuilder.Clear();

        foreach (var subClient in subClientNames)
        {
            sourceTextBuilder.AppendLine($"    {Shared.FirstCharToLower(subClient)}: I{subClient}Client;");
        }

        var interfaceProperties = sourceTextBuilder.ToString();

        // Properties (implementation)
        sourceTextBuilder.Clear();

        foreach (var subClient in subClientNames)
        {
            sourceTextBuilder.AppendLine($"    public {Shared.FirstCharToLower(subClient)}: {subClient}Client;");
        }

        var properties = sourceTextBuilder.ToString();

        // Source (sub-client classes)
        sourceTextBuilder.Clear();

        foreach (var clientGroup in groupedClients)
        {
            AppendSubClientSourceText(
                clientGroup.Key,
                clientGroup.ToDictionary(entry => entry.path.Key, entry => entry.path.Value),
                sourceTextBuilder);

            sourceTextBuilder.AppendLine();
        }

        var source = sourceTextBuilder.ToString();

        // Models
        sourceTextBuilder.Clear();

        if (document.Components?.Schemas is not null)
        {
            foreach (var schema in document.Components.Schemas)
            {
                AppendModelSourceText(
                    schema.Key,
                    schema.Value,
                    sourceTextBuilder);

                sourceTextBuilder.AppendLine();
            }
        }

        foreach (var (_, modelText) in _additionalModels)
        {
            sourceTextBuilder.Append(modelText);
            sourceTextBuilder.AppendLine();
        }

        var models = sourceTextBuilder.ToString();

        return new SubClientProperties(
            Fields: fields,
            FieldAssignments: fieldAssignments,
            Properties: properties,
            Source: source,
            InterfaceProperties: interfaceProperties,
            Models: models
        );
    }

    private void AppendSubClientSourceText(
        string className,
        IDictionary<string, OpenApiPathItem> methodMap,
        StringBuilder sourceTextBuilder)
    {
        var augmentedClassName = className + "Client";

        // interface
        sourceTextBuilder.AppendLine($"/**");
        sourceTextBuilder.AppendLine($" * Provides methods to interact with {Shared.SplitCamelCase(className).ToLower()}.");
        sourceTextBuilder.AppendLine($" */");
        sourceTextBuilder.AppendLine($"export interface I{augmentedClassName} {{");

        foreach (var entry in methodMap)
        {
            if (entry.Value.Parameters.Any())
                throw new Exception("Parameters on the path item level are not supported.");

            foreach (var operation in entry.Value.Operations)
            {
                var response = GetSuccessResponse(entry.Key, operation.Value);

                if (response.Value.Content.Count == 0)
                {
                    AppendInterfaceMethodSourceText(
                        path: entry.Key,
                        methodSuffix: "",
                        operation.Key,
                        operation.Value,
                        response,
                        responseType: null,
                        sourceTextBuilder);

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
                            sourceTextBuilder);

                        sourceTextBuilder.AppendLine();
                    }
                }
            }
        }

        sourceTextBuilder.AppendLine("}");
        sourceTextBuilder.AppendLine();

        // implementation
        sourceTextBuilder.AppendLine($"/**");
        sourceTextBuilder.AppendLine($" * Provides methods to interact with {Shared.SplitCamelCase(className).ToLower()}.");
        sourceTextBuilder.AppendLine($" */");
        sourceTextBuilder.AppendLine($"export class {augmentedClassName} implements I{augmentedClassName} {{");
        sourceTextBuilder.AppendLine($"    private _invoke: HttpRequestHandler;");
        sourceTextBuilder.AppendLine();
        sourceTextBuilder.AppendLine($"    constructor(invoke: HttpRequestHandler) {{");
        sourceTextBuilder.AppendLine($"        this._invoke = invoke;");
        sourceTextBuilder.AppendLine($"    }}");
        sourceTextBuilder.AppendLine();

        foreach (var entry in methodMap)
        {
            if (entry.Value.Parameters.Any())
                throw new Exception("Parameters on the path item level are not supported.");

            foreach (var operation in entry.Value.Operations)
            {
                var response = GetSuccessResponse(entry.Key, operation.Value);

                if (response.Value.Content.Count == 0)
                {
                    AppendImplementationMethodSourceText(
                        path: entry.Key,
                        methodSuffix: "",
                        operation.Key,
                        operation.Value,
                        response,
                        responseType: null,
                        sourceTextBuilder);

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
                            sourceTextBuilder);

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
        StringBuilder sourceTextBuilder)
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

        var actualReturnType = string.IsNullOrWhiteSpace(returnType)
            ? "Promise<void>"
            : $"Promise<{returnType}>";

        sourceTextBuilder.AppendLine($"    /**");
        sourceTextBuilder.AppendLine($"     *{FormatJsDocText(operation.Summary)}");

        foreach (var parameter in parameters)
        {
            sourceTextBuilder.AppendLine($"     * @param {parameter.Item2.Name} {GetFirstLine(parameter.Item2.Description ?? parameter.Item2.Schema.Description)}");
        }

        if (operation.RequestBody is not null && bodyParameter is not null)
            sourceTextBuilder.AppendLine($"     * @param {bodyParameter.Split(":")[0].Trim().TrimEnd('?')} {GetFirstLine(operation.RequestBody.Description)}");

        sourceTextBuilder.AppendLine($"     * @param signal The signal to cancel the current operation.");
        sourceTextBuilder.AppendLine($"     */");
        sourceTextBuilder.AppendLine($"    {signature}: {actualReturnType};");
    }

    private void AppendImplementationMethodSourceText(
        string path,
        string methodSuffix,
        OperationType operationType,
        OpenApiOperation operation,
        KeyValuePair<string, OpenApiResponse> response,
        KeyValuePair<string, OpenApiMediaType>? responseType,
        StringBuilder sourceTextBuilder)
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
        var actualReturnType = isVoidReturnType ? "void" : returnType;
        var promiseReturnType = isVoidReturnType ? "Promise<void>" : $"Promise<{returnType}>";

        sourceTextBuilder.AppendLine($"    /**");
        sourceTextBuilder.AppendLine($"     *{FormatJsDocText(operation.Summary)}");

        foreach (var parameter in parameters)
        {
            sourceTextBuilder.AppendLine($"     * @param {parameter.Item2.Name} {GetFirstLine(parameter.Item2.Description ?? parameter.Item2.Schema.Description)}");
        }

        if (operation.RequestBody is not null && bodyParameter is not null)
            sourceTextBuilder.AppendLine($"     * @param {bodyParameter.Split(":")[0].Trim().TrimEnd('?')} {GetFirstLine(operation.RequestBody.Description)}");

        sourceTextBuilder.AppendLine($"     * @param signal The signal to cancel the current operation.");
        sourceTextBuilder.AppendLine($"     */");
        sourceTextBuilder.AppendLine($"    public async {signature}: {promiseReturnType} {{");

        // URL building
        sourceTextBuilder.AppendLine($"        let __url = \"{path}\";");

        // path parameters
        var pathParameters = parameters
            .Where(parameter => parameter.Item2.In == ParameterLocation.Path)
            .ToList();

        foreach (var parameter in pathParameters)
        {
            var parameterName = parameter.Item1.Split(":")[0].Trim().TrimEnd('?');
            sourceTextBuilder.AppendLine($"        __url = __url.replace(\"{{{parameter.Item2.Name}}}\", encodeURIComponent(String({parameterName})));");
        }

        // query parameters
        var queryParameters = parameters
            .Where(parameter => parameter.Item2.In == ParameterLocation.Query)
            .ToList();

        if (queryParameters.Any())
        {
            sourceTextBuilder.AppendLine();
            sourceTextBuilder.AppendLine("        const __searchParams = new URLSearchParams();");

            foreach (var parameter in queryParameters)
            {
                var parameterName = parameter.Item1.Split(":")[0].Trim().TrimEnd('?');

                if (!parameter.Item2.Required || parameter.Item2.Schema.Nullable)
                {
                    sourceTextBuilder.AppendLine($"        if ({parameterName} !== undefined && {parameterName} !== null)");
                    sourceTextBuilder.AppendLine($"            __searchParams.set(\"{parameter.Item2.Name}\", String({parameterName}));");
                }

                else
                {
                    sourceTextBuilder.AppendLine($"        __searchParams.set(\"{parameter.Item2.Name}\", String({parameterName}));");
                }
            }

            sourceTextBuilder.AppendLine("        const __query = __searchParams.toString();");
            sourceTextBuilder.AppendLine("        if (__query)");
            sourceTextBuilder.AppendLine("            __url += \"?\" + __query;");
        }

        // content type and accept
        var acceptHeaderValue = responseType.HasValue
            ? $"\"{responseType.Value.Key}\""
            : "undefined";

        var contentTypeValue = operation.RequestBody is null
            ? "undefined"
            : $"\"{operation.RequestBody?.Content.Keys.First()}\"";

        var content = bodyParameter is null
            ? "undefined"
            : operation.RequestBody?.Content.Keys.First() switch
            {
                "application/json" => $"JSON.stringify({bodyParameter.Split(":")[0].Trim().TrimEnd('?')})",
                "application/octet-stream" => bodyParameter.Split(":")[0].Trim().TrimEnd('?'),
                _ => throw new Exception($"The media type {operation.RequestBody!.Content.Keys.First()} is not supported.")
            };

        sourceTextBuilder.AppendLine();
        sourceTextBuilder.AppendLine($"        return this._invoke<{(isVoidReturnType ? "void" : returnType)}>(\"{operationType.ToString().ToUpper()}\", __url, {acceptHeaderValue}, {contentTypeValue}, {content}, signal);");

        sourceTextBuilder.AppendLine($"    }}");
    }

    private void AppendModelSourceText(
        string modelName,
        OpenApiSchema schema,
        StringBuilder sourceTextBuilder)
    {
        if (schema.Enum.Any())
        {
            if (schema.Type != "string")
                throw new Exception("Only enum of type string is supported.");

            var enumValueArray = default(IList<IOpenApiAny>);

            if (schema.Extensions.TryGetValue("x-enum-values", out var extValues) && extValues is OpenApiArray openApiArray)
                enumValueArray = openApiArray;

            var enumValues = string
                .Join($",{Environment.NewLine}", schema.Enum
                .OfType<OpenApiString>()
                .Select(current => $"    {current.Value} = \"{current.Value}\""));

            sourceTextBuilder.AppendLine($"/**");
            sourceTextBuilder.AppendLine($" *{FormatJsDocText(schema.Description)}");
            sourceTextBuilder.AppendLine($" */");
            sourceTextBuilder.AppendLine($"export enum {modelName} {{");
            sourceTextBuilder.AppendLine(enumValues);
            sourceTextBuilder.AppendLine("}");
            sourceTextBuilder.AppendLine();
        }

        else
        {
            sourceTextBuilder.AppendLine($"/**");
            sourceTextBuilder.AppendLine($" *{FormatJsDocText(schema.Description)}");
            sourceTextBuilder.AppendLine($" */");
            sourceTextBuilder.AppendLine($"export interface {modelName} {{");

            if (schema.Properties is not null)
            {
                foreach (var property in schema.Properties)
                {
                    var anonymousTypeName = $"{modelName}{Shared.FirstCharToUpper(property.Key)}Type";
                    var type = GetType(property.Value, anonymousTypeName);
                    var isRequired = schema.Required.Contains(property.Key);
                    var optional = isRequired ? "" : "?";

                    sourceTextBuilder.AppendLine($"    /** {GetFirstLine(property.Value.Description)} */");
                    sourceTextBuilder.AppendLine($"    {property.Key}{optional}: {ApplyRequired(type, isRequired)};");
                }
            }

            sourceTextBuilder.AppendLine("}");
            sourceTextBuilder.AppendLine();
        }
    }

    private string ApplyRequired(string type, bool isRequired)
    {
        if (!isRequired && !type.EndsWith(" | null") && !type.EndsWith(" | undefined"))
            type = $"{type} | undefined";

        return type;
    }

    private string GetType(string mediaTypeKey, OpenApiMediaType mediaType, string? anonymousTypeName, bool returnValue = false)
    {
        var type = mediaTypeKey switch
        {
            "application/octet-stream" => returnValue ? "Response" : "BodyInit",
            "application/vnd.apache.arrow.stream" => returnValue ? "Response" : "BodyInit",
            "application/json" => GetType(mediaType.Schema, anonymousTypeName),
            _ => throw new Exception($"The media type {mediaTypeKey} is not supported.")
        };

        return type;
    }

    private string GetType(OpenApiSchema schema, string? anonymousTypeName)
    {
        string type;

        if (schema.Reference is null)
        {
            type = (schema.Type, schema.Format, schema.AdditionalProperties) switch
            {
                (null, _, _) => schema.OneOf.Count switch
                {
                    0 => "unknown",
                    1 => GetType(schema.OneOf.First(), anonymousTypeName),
                    _ => throw new Exception("Only zero or one entries are supported.")
                },
                ("boolean", _, _) => "boolean",
                ("number", _, _) => "number",
                ("integer", _, _) => "number",
                ("string", "uri", _) => "string",
                ("string", "guid", _) => "string",
                ("string", "duration", _) => "string",
                ("string", "date-time", _) => "string",
                ("string", _, _) => "string",
                ("array", _, _) => $"{WrapArrayElementType(GetType(schema.Items, anonymousTypeName))}[]",
                ("object", _, null) => GetAnonymousType(anonymousTypeName ?? throw new Exception("Type name required."), schema),
                ("object", _, _) => $"Record<string, {GetType(schema.AdditionalProperties, anonymousTypeName)}>",
                (_, _, _) => throw new Exception($"The schema type {schema.Type} (or one of its formats) is not supported.")
            };
        }

        else
        {
            type = schema.Reference.Id;
        }

        return schema.Nullable
            ? $"{type} | null"
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

    private static string WrapArrayElementType(string type)
    {
        return type.Contains(" | ")
            ? $"({type})"
            : type;
    }

    private static KeyValuePair<string, OpenApiResponse> GetSuccessResponse(string path, OpenApiOperation operation)
    {
        return operation.Responses.FirstOrDefault(response => response.Key is "200" or "201") is var response && response.Value is not null
            ? response
            : throw new Exception($"Operation '{operation.OperationId}' at '{path}' requires response type '200' or '201'.");
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

        var methodName = Shared.FirstCharToLower(_settings.GetOperationName(path, operationType, operation) + methodSuffix);

        if (!(response.Key == "200" || response.Key == "201"))
            throw new Exception("Only response types '200' or '201' are supported.");

        var anonymousReturnTypeName = $"{Shared.FirstCharToUpper(methodName)}Response";

        returnType = responseType.HasValue switch
        {
            true => GetType(responseType.Value.Key, responseType.Value.Value, anonymousReturnTypeName, returnValue: true),
            false => string.Empty
        };

        parameters = Enumerable.Empty<(string, OpenApiParameter)>();
        bodyParameter = null;

        if (!operation.Parameters.Any() && operation.RequestBody is null)
        {
            return $"{methodName}(signal?: AbortSignal)";
        }

        else
        {
            parameters = operation.Parameters
                .Where(parameter => parameter.In == ParameterLocation.Query || parameter.In == ParameterLocation.Path)
                .Select(parameter =>
                {
                    var type = GetType(parameter.Schema, anonymousTypeName: null);
                    var optional = parameter.Required ? "" : "?";
                    return ($"{parameter.Name}{optional}: {ApplyRequired(type, parameter.Required)}", parameter);
                });

            var parameterParts = parameters
                .Select(parameter => (parameter.Item1, parameter.Item2.Required))
                .ToList();

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

                    var anonymousRequestTypeName = $"{Shared.FirstCharToUpper(methodName)}Request";

                    type = ApplyRequired(GetType(content.Key, content.Value, anonymousTypeName: anonymousRequestTypeName), isRequired);
                    name = openApiString.Value;
                }

                else
                {
                    type = isRequired ? "unknown" : "unknown | undefined";
                    name = "body";
                }

                bodyParameter = $"{name}{(isRequired ? "" : "?")}: {type}";
                parameterParts.Add((bodyParameter, isRequired));
            }

            var sortedParts = parameterParts
                .OrderByDescending(parameter => parameter.Item2)
                .Select(parameter => parameter.Item1)
                .ToList();

            sortedParts.Add("signal?: AbortSignal");

            return $"{methodName}({string.Join(", ", sortedParts)})";
        }
    }

    private static string FormatJsDocText(string? value)
    {
        var firstLine = GetFirstLine(value);
        return firstLine is null ? string.Empty : $" {firstLine}";
    }

    private static string? GetFirstLine(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        using var reader = new StringReader(value);
        return reader.ReadLine();
    }
}
