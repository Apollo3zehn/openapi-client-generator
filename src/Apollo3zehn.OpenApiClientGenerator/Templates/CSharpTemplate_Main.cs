﻿#nullable enable

{{#Special_NexusFeatures}}
using System.Buffers;
using System.Diagnostics;
{{/Special_NexusFeatures}}
using System.Globalization;
{{#Special_NexusFeatures}}
using System.IO.Compression;
{{/Special_NexusFeatures}}
using System.Net.Http.Headers;
using System.Net.Http.Json;
{{#Special_NexusFeatures}}
using System.Runtime.InteropServices;
{{/Special_NexusFeatures}}
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace {{{Namespace}}}
{
/// <summary>
/// A client for the {{{ClientName}}} system.
/// </summary>
public interface I{{{ClientName}}}Client
{
{{{VersioningInterfaceProperties}}}

{{#Special_AccessTokenSupport}}
    /// <summary>
    /// Signs in the user.
    /// </summary>
    /// <param name="accessToken">The access token.</param>
    /// <returns>A task.</returns>
    void SignIn(string accessToken);
{{/Special_AccessTokenSupport}}

{{#Special_NexusFeatures}}
    /// <summary>
    /// Attaches configuration data to subsequent API requests.
    /// </summary>
    /// <param name="configuration">The configuration data.</param>
    IDisposable AttachConfiguration(object configuration);

    /// <summary>
    /// Clears configuration data for all subsequent API requests.
    /// </summary>
    void ClearConfiguration();
{{/Special_NexusFeatures}}
}

/// <inheritdoc />
public class {{{ClientName}}}Client : I{{{ClientName}}}Client, IDisposable
{
{{#Special_NexusFeatures}}
    private const string ConfigurationHeaderKey = "{{{Special_ConfigurationHeaderKey}}}";
{{/Special_NexusFeatures}}
{{#Special_AccessTokenSupport}}
    private const string AuthorizationHeaderKey = "Authorization";

    private string? __token;
{{/Special_AccessTokenSupport}}
    private HttpClient __httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="{{{ClientName}}}Client"/>.
    /// </summary>
    /// <param name="baseUrl">The base URL to connect to.</param>
    public {{{ClientName}}}Client(Uri baseUrl) : this(new HttpClient() { BaseAddress = baseUrl, Timeout = TimeSpan.FromSeconds(60) })
    {
        //
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="{{{ClientName}}}Client"/>.
    /// </summary>
    /// <param name="httpClient">The HTTP client to use.</param>
    public {{{ClientName}}}Client(HttpClient httpClient)
    {
        if (httpClient.BaseAddress is null)
            throw new Exception("The base address of the HTTP client must be set.");

        __httpClient = httpClient;

{{{VersioningPropertyAssignments}}}
    }

{{#Special_AccessTokenSupport}}
    /// <summary>
    /// Gets a value which indicates if the user is authenticated.
    /// </summary>
    public bool IsAuthenticated => __token is not null;
{{/Special_AccessTokenSupport}}

{{{VersioningProperties}}}

{{#Special_AccessTokenSupport}}
    /// <inheritdoc />
    public void SignIn(string accessToken)
    {
        var authorizationHeaderValue = $"Bearer {accessToken}";
        __httpClient.DefaultRequestHeaders.Remove(AuthorizationHeaderKey);
        __httpClient.DefaultRequestHeaders.Add(AuthorizationHeaderKey, authorizationHeaderValue);

        __token = accessToken;
    }
{{/Special_AccessTokenSupport}}

{{#Special_NexusFeatures}}
    /// <inheritdoc />
    public IDisposable AttachConfiguration(object configuration)
    {
        var encodedJson = Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(configuration));

        __httpClient.DefaultRequestHeaders.Remove(ConfigurationHeaderKey);
        __httpClient.DefaultRequestHeaders.Add(ConfigurationHeaderKey, encodedJson);

        return new DisposableConfiguration(this);
    }

    /// <inheritdoc />
    public void ClearConfiguration()
    {
        __httpClient.DefaultRequestHeaders.Remove(ConfigurationHeaderKey);
    }
{{/Special_NexusFeatures}}

    internal T Invoke<T>(string method, string relativeUrl, string? acceptHeaderValue, string? contentTypeValue, HttpContent? content)
    {
        // prepare request
        using var request = BuildRequestMessage(method, relativeUrl, content, contentTypeValue, acceptHeaderValue);

        // send request
        var response = __httpClient.Send(request, HttpCompletionOption.ResponseHeadersRead);

        // process response
        if (!response.IsSuccessStatusCode)
        {
            var message = new StreamReader(response.Content.ReadAsStream()).ReadToEnd();
            var statusCode = $"{{{ExceptionCodePrefix}}}00.{(int)response.StatusCode}";

            if (string.IsNullOrWhiteSpace(message))
                throw new {{{ExceptionType}}}(statusCode, $"The HTTP request failed with status code {response.StatusCode}.");

            else
                throw new {{{ExceptionType}}}(statusCode, $"The HTTP request failed with status code {response.StatusCode}. The response message is: {message}");
        }

        try
        {
            if (typeof(T) == typeof(object))
            {
                return default!;
            }

            else if (typeof(T) == typeof(HttpResponseMessage))
            {
                return (T)(object)(response);
            }

            else
            {
                var stream = response.Content.ReadAsStream();

                try
                {
                    return JsonSerializer.Deserialize<T>(stream, Utilities.JsonOptions)!;
                }
                catch (Exception ex)
                {
                    throw new {{{ExceptionType}}}("{{{ExceptionCodePrefix}}}01", "Response data could not be deserialized.", ex);
                }
            }
        }
        finally
        {
            if (typeof(T) != typeof(HttpResponseMessage))
                response.Dispose();
        }
    }

    internal async Task<T> InvokeAsync<T>(string method, string relativeUrl, string? acceptHeaderValue, string? contentTypeValue, HttpContent? content, CancellationToken cancellationToken)
    {
        // prepare request
        using var request = BuildRequestMessage(method, relativeUrl, content, contentTypeValue, acceptHeaderValue);

        // send request
        var response = await __httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

        // process response
        if (!response.IsSuccessStatusCode)
        {
            var message = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var statusCode = $"{{{ExceptionCodePrefix}}}00.{(int)response.StatusCode}";

            if (string.IsNullOrWhiteSpace(message))
                throw new {{{ExceptionType}}}(statusCode, $"The HTTP request failed with status code {response.StatusCode}.");

            else
                throw new {{{ExceptionType}}}(statusCode, $"The HTTP request failed with status code {response.StatusCode}. The response message is: {message}");
        }

        try
        {
            if (typeof(T) == typeof(object))
            {
                return default!;
            }

            else if (typeof(T) == typeof(HttpResponseMessage))
            {
                return (T)(object)(response);
            }

            else
            {
                var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    return (await JsonSerializer.DeserializeAsync<T>(stream, Utilities.JsonOptions).ConfigureAwait(false))!;
                }
                catch (Exception ex)
                {
                    throw new {{{ExceptionType}}}("{{{ExceptionCodePrefix}}}01", "Response data could not be deserialized.", ex);
                }
            }
        }
        finally
        {
            if (typeof(T) != typeof(HttpResponseMessage))
                response.Dispose();
        }
    }

{{#Special_WebAssemblySupport}}
    private static readonly HttpRequestOptionsKey<bool> WebAssemblyEnableStreamingResponseKey = new HttpRequestOptionsKey<bool>("WebAssemblyEnableStreamingResponse");
{{/Special_WebAssemblySupport}}

    private HttpRequestMessage BuildRequestMessage(string method, string relativeUrl, HttpContent? content, string? contentTypeHeaderValue, string? acceptHeaderValue)
    {
        var requestMessage = new HttpRequestMessage()
        {
            Method = new HttpMethod(method),
            RequestUri = new Uri(relativeUrl, UriKind.Relative),
            Content = content
        };

        if (contentTypeHeaderValue is not null && requestMessage.Content is not null)
            requestMessage.Content.Headers.ContentType = MediaTypeWithQualityHeaderValue.Parse(contentTypeHeaderValue);

        if (acceptHeaderValue is not null)
            requestMessage.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse(acceptHeaderValue));

{{#Special_WebAssemblySupport}}
        // For web assembly
        // https://docs.microsoft.com/de-de/dotnet/api/microsoft.aspnetcore.components.webassembly.http.webassemblyhttprequestmessageextensions.setbrowserresponsestreamingenabled?view=aspnetcore-6.0
        // https://github.com/dotnet/aspnetcore/blob/0ee742c53f2669fd7233df6da89db5e8ab944585/src/Components/WebAssembly/WebAssembly/src/Http/WebAssemblyHttpRequestMessageExtensions.cs
        requestMessage.Options.Set(WebAssemblyEnableStreamingResponseKey, true);
{{/Special_WebAssemblySupport}}

        return requestMessage;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        __httpClient?.Dispose();
    }

{{#Special_NexusFeatures}}
    /// <summary>
    /// This high-level methods simplifies loading multiple resources at once.
    /// </summary>
    /// <param name="begin">Start date/time.</param>
    /// <param name="end">End date/time.</param>
    /// <param name="resourcePaths">The resource paths.</param>
    /// <param name="onProgress">A callback which accepts the current progress.</param>
    public IReadOnlyDictionary<string, DataResponse> Load(
        DateTime begin, 
        DateTime end, 
        IEnumerable<string> resourcePaths,
        Action<double>? onProgress = default)
    {
        var catalogItemMap = V1.Catalogs.SearchCatalogItems(resourcePaths.ToList());
        var result = new Dictionary<string, DataResponse>();
        var progress = 0.0;

        foreach (var (resourcePath, catalogItem) in catalogItemMap)
        {
            using var responseMessage = V1.Data.GetStream(resourcePath, begin, end);

            var doubleData = ReadAsDoubleAsync(responseMessage, useAsync: false)
                .GetAwaiter()
                .GetResult();

            var resource = catalogItem.Resource;

            string? unit = default;

            if (resource.Properties is not null &&
                resource.Properties.TryGetValue("unit", out var unitElement) &&
                unitElement.ValueKind == JsonValueKind.String)
                unit = unitElement.GetString();

            string? description = default;

            if (resource.Properties is not null &&
                resource.Properties.TryGetValue("description", out var descriptionElement) &&
                descriptionElement.ValueKind == JsonValueKind.String)
                description = descriptionElement.GetString();

            var samplePeriod = catalogItem.Representation.SamplePeriod;

            result[resourcePath] = new DataResponse(
                CatalogItem: catalogItem,
                Name: resource.Id,
                Unit: unit,
                Description: description,
                SamplePeriod: samplePeriod,
                Values: doubleData
            );

            progress += 1.0 / catalogItemMap.Count;
            onProgress?.Invoke(progress);
        }

        return result;
    }

    /// <summary>
    /// This high-level methods simplifies loading multiple resources at once.
    /// </summary>
    /// <param name="begin">Start date/time.</param>
    /// <param name="end">End date/time.</param>
    /// <param name="resourcePaths">The resource paths.</param>
    /// <param name="onProgress">A callback which accepts the current progress.</param>
    /// <param name="cancellationToken">A token to cancel the current operation.</param>
    public async Task<IReadOnlyDictionary<string, DataResponse>> LoadAsync(
        DateTime begin, 
        DateTime end, 
        IEnumerable<string> resourcePaths,
        Action<double>? onProgress = default,
        CancellationToken cancellationToken = default)
    {
        var catalogItemMap = await V1.Catalogs.SearchCatalogItemsAsync(resourcePaths.ToList()).ConfigureAwait(false);
        var result = new Dictionary<string, DataResponse>();
        var progress = 0.0;

        foreach (var (resourcePath, catalogItem) in catalogItemMap)
        {
            using var responseMessage = await V1.Data.GetStreamAsync(resourcePath, begin, end, cancellationToken).ConfigureAwait(false);
            var doubleData = await ReadAsDoubleAsync(responseMessage, useAsync: true, cancellationToken).ConfigureAwait(false);
            var resource = catalogItem.Resource;

            string? unit = default;

            if (resource.Properties is not null &&
                resource.Properties.TryGetValue("unit", out var unitElement) &&
                unitElement.ValueKind == JsonValueKind.String)
                unit = unitElement.GetString();

            string? description = default;

            if (resource.Properties is not null &&
                resource.Properties.TryGetValue("description", out var descriptionElement) &&
                descriptionElement.ValueKind == JsonValueKind.String)
                description = descriptionElement.GetString();

            var samplePeriod = catalogItem.Representation.SamplePeriod;

            result[resourcePath] = new DataResponse(
                CatalogItem: catalogItem,
                Name: resource.Id,
                Unit: unit,
                Description: description,
                SamplePeriod: samplePeriod,
                Values: doubleData
            );

            progress += 1.0 / catalogItemMap.Count;
            onProgress?.Invoke(progress);
        }

        return result;
    }

    private async Task<double[]> ReadAsDoubleAsync(HttpResponseMessage responseMessage, bool useAsync, CancellationToken cancellationToken = default)
    {
        int? length = default;

        if (responseMessage.Content.Headers.TryGetValues("Content-Length", out var values) && 
            values.Any() && 
            int.TryParse(values.First(), out var contentLength))
        {
            length = contentLength;
        }

        if (!length.HasValue)
            throw new Exception("The data length is unknown.");

        if (length.Value % 8 != 0)
            throw new Exception("The data length is invalid.");

        var elementCount = length.Value / 8;
        var doubleBuffer = new double[elementCount];
        var byteBuffer = new CastMemoryManager<double, byte>(doubleBuffer).Memory;

        Stream stream = useAsync
            ? await responseMessage.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false)
            : responseMessage.Content.ReadAsStream(cancellationToken);

        var remainingBuffer = byteBuffer;

        while (!remainingBuffer.IsEmpty)
        {
            var bytesRead = await stream.ReadAsync(remainingBuffer, cancellationToken).ConfigureAwait(false);

            if (bytesRead == 0)
                throw new Exception("The stream ended early.");

            remainingBuffer = remainingBuffer.Slice(bytesRead);
        }

        return doubleBuffer;
    }

    private async Task<double[]> ReadAsDoubleAsync(HttpResponseMessage responseMessage, CancellationToken cancellationToken = default)
    {
        int? length = default;

        if (responseMessage.Content.Headers.TryGetValues("Content-Length", out var values) && 
            values.Any() && 
            int.TryParse(values.First(), out var contentLength))
        {
            length = contentLength;
        }

        if (!length.HasValue)
            throw new Exception("The data length is unknown.");

        if (length.Value % 8 != 0)
            throw new Exception("The data length is invalid.");

        var elementCount = length.Value / 8;
        var doubleBuffer = new double[elementCount];
        var byteBuffer = new CastMemoryManager<double, byte>(doubleBuffer).Memory;
        var stream = await responseMessage.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var remainingBuffer = byteBuffer;

        while (!remainingBuffer.IsEmpty)
        {
            var bytesRead = await stream.ReadAsync(remainingBuffer, cancellationToken).ConfigureAwait(false);

            if (bytesRead == 0)
                throw new Exception("The stream ended early.");

            remainingBuffer = remainingBuffer.Slice(bytesRead);
        }

        return doubleBuffer;
    }

    /// <summary>
    /// This high-level methods simplifies exporting multiple resources at once.
    /// </summary>
    /// <param name="begin">The begin date/time.</param>
    /// <param name="end">The end date/time.</param>
    /// <param name="filePeriod">The file period. Use TimeSpan.Zero to get a single file.</param>
    /// <param name="fileFormat">The target file format. If null, data will be read (and possibly cached) but not returned. This is useful for data pre-aggregation.</param>
    /// <param name="resourcePaths">The resource paths to export.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="targetFolder">The target folder for the files to extract.</param>
    /// <param name="onProgress">A callback which accepts the current progress and the progress message.</param>
    public void Export(
        DateTime begin, 
        DateTime end,
        TimeSpan filePeriod,
        string? fileFormat,
        IEnumerable<string> resourcePaths,
        IReadOnlyDictionary<string, object>? configuration,
        string targetFolder,
        Action<double, string>? onProgress = default)
    {
        var actualConfiguration = configuration is null
            ? default
            : JsonSerializer.Deserialize<IReadOnlyDictionary<string, JsonElement>?>(JsonSerializer.Serialize(configuration));

        var exportParameters = new V1.ExportParameters(
            begin,
            end,
            filePeriod,
            fileFormat,
            resourcePaths.ToList(),
            actualConfiguration);

        // Start Job
        var job = V1.Jobs.Export(exportParameters);

        // Wait for job to finish
        string? artifactId = default;

        while (true)
        {
            Thread.Sleep(TimeSpan.FromSeconds(1));

            var jobStatus = V1.Jobs.GetJobStatus(job.Id);

            if (jobStatus.Status == Nexus.Api.V1.TaskStatus.Canceled)
                throw new OperationCanceledException("The job has been cancelled.");

            else if (jobStatus.Status == Nexus.Api.V1.TaskStatus.Faulted)
                throw new OperationCanceledException($"The job has failed. Reason: {jobStatus.ExceptionMessage}");

            else if (jobStatus.Status == Nexus.Api.V1.TaskStatus.RanToCompletion)
            {
                if (jobStatus.Result.HasValue &&
                    jobStatus.Result.Value.ValueKind == JsonValueKind.String)
                {
                    artifactId = jobStatus.Result.Value.GetString();
                    break;
                }
            }

            if (jobStatus.Progress < 1)
                onProgress?.Invoke(jobStatus.Progress, "export");
        }

        onProgress?.Invoke(1, "export");

        if (artifactId is null)
            throw new Exception("The job result is invalid.");

        if (fileFormat is null)
            return;

        // Download zip file
        var responseMessage = V1.Artifacts.Download(artifactId);
        var sourceStream = responseMessage.Content.ReadAsStream();

        long? length = default;

        if (responseMessage.Content.Headers.TryGetValues("Content-Length", out var values) && 
            values.Any() && 
            int.TryParse(values.First(), out var contentLength))
        {
            length = contentLength;
        }

        var tmpFilePath = Path.GetTempFileName();

        try
        {
            using (var targetStream = File.OpenWrite(tmpFilePath))
            {
                var buffer = new byte[32768];
                var consumed = 0;
                var sw = Stopwatch.StartNew();
                var maxTicks = TimeSpan.FromSeconds(1).Ticks;

                int receivedBytes;

                while ((receivedBytes = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    targetStream.Write(buffer, 0, receivedBytes);
                    consumed += receivedBytes;

                    if (sw.ElapsedTicks > maxTicks)
                    {
                        sw.Reset();

                        if (length.HasValue)
                        {
                            if (consumed < length)
                                onProgress?.Invoke(consumed / (double)length, "download");
                        }
                    }
                }
            }

            onProgress?.Invoke(1, "download");

            // Extract file (do not use stream overload: https://github.com/dotnet/runtime/issues/59027)
            ZipFile.ExtractToDirectory(tmpFilePath, targetFolder, overwriteFiles: true);
            onProgress?.Invoke(1, "extract");
        }
        finally
        {
            try
            {
                File.Delete(tmpFilePath);
            }
            catch
            {
                //
            }
        }
    }

    /// <summary>
    /// This high-level methods simplifies exporting multiple resources at once.
    /// </summary>
    /// <param name="begin">The begin date/time.</param>
    /// <param name="end">The end date/time.</param>
    /// <param name="filePeriod">The file period. Use TimeSpan.Zero to get a single file.</param>
    /// <param name="fileFormat">The target file format. If null, data will be read (and possibly cached) but not returned. This is useful for data pre-aggregation.</param>
    /// <param name="resourcePaths">The resource paths to export.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="targetFolder">The target folder for the files to extract.</param>
    /// <param name="onProgress">A callback which accepts the current progress and the progress message.</param>
    /// <param name="cancellationToken">A token to cancel the current operation.</param>
    public async Task ExportAsync(
        DateTime begin, 
        DateTime end,
        TimeSpan filePeriod,
        string? fileFormat,
        IEnumerable<string> resourcePaths,
        IReadOnlyDictionary<string, object>? configuration,
        string targetFolder,
        Action<double, string>? onProgress = default,
        CancellationToken cancellationToken = default)
    {
        var actualConfiguration = configuration is null
            ? default
            : JsonSerializer.Deserialize<IReadOnlyDictionary<string, JsonElement>?>(JsonSerializer.Serialize(configuration));

        var exportParameters = new V1.ExportParameters(
            begin,
            end,
            filePeriod,
            fileFormat,
            resourcePaths.ToList(),
            actualConfiguration);

        // Start Job
        var job = await V1.Jobs.ExportAsync(exportParameters).ConfigureAwait(false);

        // Wait for job to finish
        string? artifactId = default;

        while (true)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);

            var jobStatus = await V1.Jobs.GetJobStatusAsync(job.Id, cancellationToken).ConfigureAwait(false);

            if (jobStatus.Status == Nexus.Api.V1.TaskStatus.Canceled)
                throw new OperationCanceledException("The job has been cancelled.");

            else if (jobStatus.Status == Nexus.Api.V1.TaskStatus.Faulted)
                throw new OperationCanceledException($"The job has failed. Reason: {jobStatus.ExceptionMessage}");

            else if (jobStatus.Status == Nexus.Api.V1.TaskStatus.RanToCompletion)
            {
                if (jobStatus.Result.HasValue &&
                    jobStatus.Result.Value.ValueKind == JsonValueKind.String)
                {
                    artifactId = jobStatus.Result.Value.GetString();
                    break;
                }
            }

            if (jobStatus.Progress < 1)
                onProgress?.Invoke(jobStatus.Progress, "export");
        }

        onProgress?.Invoke(1, "export");

        if (artifactId is null)
            throw new Exception("The job result is invalid.");

        if (fileFormat is null)
            return;

        // Download zip file
        var responseMessage = await V1.Artifacts.DownloadAsync(artifactId, cancellationToken).ConfigureAwait(false);
        var sourceStream = await responseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false);

        long? length = default;

        if (responseMessage.Content.Headers.TryGetValues("Content-Length", out var values) && 
            values.Any() && 
            int.TryParse(values.First(), out var contentLength))
        {
            length = contentLength;
        }

        var tmpFilePath = Path.GetTempFileName();

        try
        {
            using (var targetStream = File.OpenWrite(tmpFilePath))
            {
                var buffer = new byte[32768];
                var consumed = 0;
                var sw = Stopwatch.StartNew();
                var maxTicks = TimeSpan.FromSeconds(1).Ticks;

                int receivedBytes;

                while ((receivedBytes = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    targetStream.Write(buffer, 0, receivedBytes);
                    consumed += receivedBytes;

                    if (sw.ElapsedTicks > maxTicks)
                    {
                        sw.Reset();

                        if (length.HasValue)
                        {
                            if (consumed < length)
                                onProgress?.Invoke(consumed / (double)length, "download");
                        }
                    }
                }
            }

            onProgress?.Invoke(1, "download");

            // Extract file (do not use stream overload: https://github.com/dotnet/runtime/issues/59027)
            ZipFile.ExtractToDirectory(tmpFilePath, targetFolder, overwriteFiles: true);
            onProgress?.Invoke(1, "extract");
        }
        finally
        {
            try
            {
                File.Delete(tmpFilePath);
            }
            catch
            {
                //
            }
        }
    }
{{/Special_NexusFeatures}}
}

{{#Special_NexusFeatures}}
internal class CastMemoryManager<TFrom, TTo> : MemoryManager<TTo>
     where TFrom : struct
     where TTo : struct
{
    private readonly Memory<TFrom> _from;

    public CastMemoryManager(Memory<TFrom> from) => _from = from;

    public override Span<TTo> GetSpan() => MemoryMarshal.Cast<TFrom, TTo>(_from.Span);

    protected override void Dispose(bool disposing)
    {
        //
    }

    public override MemoryHandle Pin(int elementIndex = 0) => throw new NotSupportedException();

    public override void Unpin() => throw new NotSupportedException();
}
{{/Special_NexusFeatures}}

/// <summary>
/// A {{{ExceptionType}}}.
/// </summary>
public class {{{ExceptionType}}} : Exception
{
    internal {{{ExceptionType}}}(string statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }

    internal {{{ExceptionType}}}(string statusCode, string message, Exception innerException) : base(message, innerException)
    {
        StatusCode = statusCode;
    }

    /// <summary>
    /// The exception status code.
    /// </summary>
    public string StatusCode { get; }
}

{{#Special_NexusFeatures}}
internal class DisposableConfiguration : IDisposable
{
    private {{{ClientName}}}Client ___client;

    public DisposableConfiguration({{{ClientName}}}Client client)
    {
        ___client = client;
    }

    public void Dispose()
    {
        ___client.ClearConfiguration();
    }
}
{{/Special_NexusFeatures}}

internal static class Utilities
{
    internal static JsonSerializerOptions JsonOptions { get; }

    static Utilities()
    {
        JsonOptions = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        JsonOptions.Converters.Add(new JsonStringEnumConverter());
    }
}

{{#Special_NexusFeatures}}
/// <summary>
/// Result of a data request with a certain resource path.
/// </summary>
/// <param name="CatalogItem">The catalog item.</param>
/// <param name="Name">The resource name.</param>
/// <param name="Unit">The optional resource unit.</param>
/// <param name="Description">The optional resource description.</param>
/// <param name="SamplePeriod">The sample period.</param>
/// <param name="Values">The data.</param>
public record DataResponse(
    V1.CatalogItem CatalogItem, 
    string? Name,
    string? Unit,
    string? Description,
    TimeSpan SamplePeriod,
    double[] Values);
{{/Special_NexusFeatures}}
}

{{#SubClients}}
{{.}}
{{/SubClients}}