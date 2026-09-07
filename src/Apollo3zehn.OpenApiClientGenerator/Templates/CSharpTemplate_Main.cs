#nullable enable

{{#Special_NexusFeatures}}
using System.Buffers;
using System.Buffers.Binary;
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
using System.Runtime.CompilerServices;
{{/Special_NexusFeatures}}
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
{{#Special_NexusFeatures}}
using Precision = Nexus.Api.V2.Precision;
{{/Special_NexusFeatures}}

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

    /// <summary>
    /// This high-level methods simplifies loading multiple resources at once.
    /// </summary>
    /// <param name="begin">Start date/time.</param>
    /// <param name="end">End date/time.</param>
    /// <param name="resourcePaths">The resource paths.</param>
    /// <param name="bufferProvider">An optional callback which provides a writable buffer for each resource path, chunk element count, and remaining resource element count.</param>
    /// <param name="onProgress">A callback which accepts the current progress.</param>
    /// <typeparam name="T">The element type. Use <see cref="double"/> for 64-bit or <see cref="float"/> for 32-bit precision.</typeparam>
    IReadOnlyDictionary<string, DataResponse<T>> Load<T>(
        DateTime begin,
        DateTime end,
        IEnumerable<string> resourcePaths,
        Func<string, int, long, Memory<T>>? bufferProvider = default,
        Action<double>? onProgress = default)
        where T : struct;

    /// <summary>
    /// This high-level methods simplifies loading multiple resources at once.
    /// </summary>
    /// <param name="begin">Start date/time.</param>
    /// <param name="end">End date/time.</param>
    /// <param name="resourcePaths">The resource paths.</param>
    /// <param name="bufferProvider">An optional callback which provides a writable buffer for each resource path, chunk element count, and remaining resource element count.</param>
    /// <param name="onProgress">A callback which accepts the current progress.</param>
    /// <param name="cancellationToken">A token to cancel the current operation.</param>
    /// <typeparam name="T">The element type. Use <see cref="double"/> for 64-bit or <see cref="float"/> for 32-bit precision.</typeparam>
    Task<IReadOnlyDictionary<string, DataResponse<T>>> LoadAsync<T>(
        DateTime begin,
        DateTime end,
        IEnumerable<string> resourcePaths,
        Func<string, int, long, Memory<T>>? bufferProvider = default,
        Action<double>? onProgress = default,
        CancellationToken cancellationToken = default)
        where T : struct;

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
    /// <param name="precision">The floating point precision used for exported sample values.</param>
    /// <param name="onProgress">A callback which accepts the current progress and the progress message.</param>
    void Export(
        DateTime begin,
        DateTime end,
        TimeSpan filePeriod,
        string? fileFormat,
        IEnumerable<string> resourcePaths,
        IReadOnlyDictionary<string, object>? configuration,
        string targetFolder,
        Precision precision,
        Action<double, string>? onProgress = default);

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
    /// <param name="precision">The floating point precision used for exported sample values.</param>
    /// <param name="onProgress">A callback which accepts the current progress and the progress message.</param>
    /// <param name="cancellationToken">A token to cancel the current operation.</param>
    Task ExportAsync(
        DateTime begin,
        DateTime end,
        TimeSpan filePeriod,
        string? fileFormat,
        IEnumerable<string> resourcePaths,
        IReadOnlyDictionary<string, object>? configuration,
        string targetFolder,
        Precision precision,
        Action<double, string>? onProgress = default,
        CancellationToken cancellationToken = default);
{{/Special_NexusFeatures}}
}

/// <inheritdoc />
public class {{{ClientName}}}Client : I{{{ClientName}}}Client, IDisposable
{
{{#Special_NexusFeatures}}
    private const string ConfigurationHeaderKey = "{{{Special_ConfigurationHeaderKey}}}";
    private const string BatchStreamPerformanceLoggingSwitch = "Nexus.BatchStreamPerformanceLogging";
    private const string BatchStreamPerformanceLoggingEnvironmentVariable = "NEXUS_BATCH_STREAM_DEBUG";
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

    private static bool IsBatchStreamPerformanceLoggingEnabled()
    {
        if (AppContext.TryGetSwitch(BatchStreamPerformanceLoggingSwitch, out var isEnabled))
            return isEnabled;

        var value = Environment.GetEnvironmentVariable(BatchStreamPerformanceLoggingEnvironmentVariable);

        return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static void LogBatchStreamPerformance(string message) =>
        Console.WriteLine($"[nexus chart perf] {DateTimeOffset.Now:HH:mm:ss.fff} {message}");

    private static double GetElapsedMilliseconds(long startTimestamp) =>
        (Stopwatch.GetTimestamp() - startTimestamp) * 1000.0 / Stopwatch.Frequency;
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
            using (response)
            {
                var message = new StreamReader(response.Content.ReadAsStream()).ReadToEnd();
                var statusCode = $"{{{ExceptionCodePrefix}}}00.{(int)response.StatusCode}";

                if (string.IsNullOrWhiteSpace(message))
                    throw new {{{ExceptionType}}}(statusCode, $"The HTTP request failed with status code {response.StatusCode}.");

                else
                    throw new {{{ExceptionType}}}(statusCode, $"The HTTP request failed with status code {response.StatusCode}. The response message is: {message}");
            }
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
            using (response)
            {
                var message = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var statusCode = $"{{{ExceptionCodePrefix}}}00.{(int)response.StatusCode}";

                if (string.IsNullOrWhiteSpace(message))
                    throw new {{{ExceptionType}}}(statusCode, $"The HTTP request failed with status code {response.StatusCode}.");

                else
                    throw new {{{ExceptionType}}}(statusCode, $"The HTTP request failed with status code {response.StatusCode}. The response message is: {message}");
            }
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
    /// <inheritdoc />
    public IReadOnlyDictionary<string, DataResponse<T>> Load<T>(
        DateTime begin, 
        DateTime end, 
        IEnumerable<string> resourcePaths,
        Func<string, int, long, Memory<T>>? bufferProvider = default,
        Action<double>? onProgress = default)
        where T : struct
    {
        var precision = GetPrecisionFromType<T>();
        var resourcePathList = resourcePaths.ToList();

        if (resourcePathList.Count == 0)
            return new Dictionary<string, DataResponse<T>>();

        var catalogItemMap = V1.Catalogs.SearchCatalogItems(resourcePathList);
        using var response = V2.Data.GetStream(new V2.BatchStreamRequest(begin, end, resourcePathList, precision));
        var expectedLengths = GetExpectedLengths(begin, end, resourcePathList, catalogItemMap, precision);
        var totalLength = expectedLengths.Sum(length => (long)length);
        var consumedLength = 0L;
        var data = ReadBatchAsync<T>(response, resourcePathList, expectedLengths, bufferProvider, useAsync: false, ReportProgress).GetAwaiter().GetResult();

        onProgress?.Invoke(1);
        return resourcePathList
            .Select((resourcePath, index) => (resourcePath, Values: data[index]))
            .ToDictionary(
                item => item.resourcePath,
                item => CreateDataResponse<T>(item.resourcePath, catalogItemMap[item.resourcePath], item.Values));

        void ReportProgress(long bytesRead)
        {
            if (totalLength > 0)
                onProgress?.Invoke(Math.Min(1, Interlocked.Add(ref consumedLength, bytesRead) / (double)totalLength));
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, DataResponse<T>>> LoadAsync<T>(
        DateTime begin, 
        DateTime end, 
        IEnumerable<string> resourcePaths,
        Func<string, int, long, Memory<T>>? bufferProvider = default,
        Action<double>? onProgress = default,
        CancellationToken cancellationToken = default)
        where T : struct
    {
        var precision = GetPrecisionFromType<T>();
        var resourcePathList = resourcePaths.ToList();

        if (resourcePathList.Count == 0)
            return new Dictionary<string, DataResponse<T>>();

        var catalogItemMap = await V1.Catalogs.SearchCatalogItemsAsync(resourcePathList, cancellationToken).ConfigureAwait(false);
        using var response = await V2.Data.GetStreamAsync(new V2.BatchStreamRequest(begin, end, resourcePathList, precision), cancellationToken).ConfigureAwait(false);
        var expectedLengths = GetExpectedLengths(begin, end, resourcePathList, catalogItemMap, precision);
        var totalLength = expectedLengths.Sum(length => (long)length);
        var consumedLength = 0L;
        var data = await ReadBatchAsync<T>(response, resourcePathList, expectedLengths, bufferProvider, useAsync: true, ReportProgress, cancellationToken).ConfigureAwait(false);

        onProgress?.Invoke(1);
        return resourcePathList
            .Select((resourcePath, index) => (resourcePath, Values: data[index]))
            .ToDictionary(
                item => item.resourcePath,
                item => CreateDataResponse<T>(item.resourcePath, catalogItemMap[item.resourcePath], item.Values));

        void ReportProgress(long bytesRead)
        {
            if (totalLength > 0)
                onProgress?.Invoke(Math.Min(1, Interlocked.Add(ref consumedLength, bytesRead) / (double)totalLength));
        }
    }

    private static long[] GetExpectedLengths(
        DateTime begin,
        DateTime end,
        IEnumerable<string> resourcePaths,
        IReadOnlyDictionary<string, V1.CatalogItem> catalogItemMap,
        Precision precision)
    {
        return resourcePaths.Select(resourcePath => checked(
            (end - begin).Ticks /
            catalogItemMap[resourcePath].Representation.SamplePeriod.Ticks *
            (long)precision)).ToArray();
    }

    private static Precision GetPrecisionFromType<T>() where T : struct
    {
        if (typeof(T) == typeof(double))
            return Precision.Float64;

        if (typeof(T) == typeof(float))
            return Precision.Float32;

        throw new NotSupportedException($"The type {typeof(T)} is not supported. Only double and float are allowed.");
    }

    private static DataResponse<T> CreateDataResponse<T>(string resourcePath, V1.CatalogItem catalogItem, ReadOnlyMemory<T> values)
        where T : struct
    {
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

        return new DataResponse<T>(
            CatalogItem: catalogItem,
            Name: resource.Id,
            Unit: unit,
            Description: description,
            SamplePeriod: catalogItem.Representation.SamplePeriod,
            Values: values);
    }

    private static async Task<Memory<T>[]> ReadBatchAsync<T>(
        HttpResponseMessage responseMessage,
        IReadOnlyList<string> resourcePaths,
        long[] expectedLengths,
        Func<string, int, long, Memory<T>>? bufferProvider,
        bool useAsync,
        Action<long>? reportProgress = default,
        CancellationToken cancellationToken = default)
        where T : struct
    {
        var elementSize = Unsafe.SizeOf<T>();
        var maxChunkLength = Math.Max(1, 16 * 1024 * 1024 / elementSize);
        var values = new Memory<T>[expectedLengths.Length];
        var chunks = new Memory<T>[expectedLengths.Length];
        var chunkOffsets = new int[expectedLengths.Length];
        var chunkLengths = new int[expectedLengths.Length];
        var offsets = new long[expectedLengths.Length];
        var completedChunkCounts = new int[expectedLengths.Length];
        var chunkFrameCounts = new int[expectedLengths.Length];
        var chunkReadOperations = new int[expectedLengths.Length];
        var chunkReadMilliseconds = new double[expectedLengths.Length];
        var chunkTimestamps = new long[expectedLengths.Length];
        var batchLogPerf = IsBatchStreamPerformanceLoggingEnabled();
        var batchTimestamp = batchLogPerf ? Stopwatch.GetTimestamp() : 0;
        var frameCount = 0L;
        var totalPayloadBytes = 0L;

        if (batchLogPerf)
            LogBatchStreamPerformance($"client readStart resources={resourcePaths.Count} expectedBytes={expectedLengths.Sum()} elementSize={elementSize} chunkElements={maxChunkLength} async={useAsync}");

        for (var index = 0; index < expectedLengths.Length; index++)
        {
            if (expectedLengths[index] % elementSize != 0)
                throw new Exception("The expected resource length is not aligned to the requested precision.");

            var requiredLength = expectedLengths[index] / elementSize;

            if (bufferProvider is null)
            {
                var allocateTimestamp = batchLogPerf ? Stopwatch.GetTimestamp() : 0;

                if (requiredLength > int.MaxValue)
                    throw new InvalidOperationException($"The resource '{resourcePaths[index]}' is too large for a single contiguous buffer. Provide a chunk-aware buffer provider.");

                values[index] = new T[checked((int)requiredLength)];
                chunks[index] = values[index];
                chunkLengths[index] = checked((int)expectedLengths[index]);

                if (batchLogPerf)
                {
                    chunkTimestamps[index] = Stopwatch.GetTimestamp();
                    LogBatchStreamPerformance($"client allocate resource='{resourcePaths[index]}' index={index} length={requiredLength} bytes={expectedLengths[index]} allocateMs={GetElapsedMilliseconds(allocateTimestamp):F1}");
                }
            }
            else
            {
                values[index] = Memory<T>.Empty;

                if (requiredLength > 0)
                    RentNextChunk(index, requiredLength);
            }
        }

        var header = new byte[8];
        var streamTimestamp = batchLogPerf ? Stopwatch.GetTimestamp() : 0;
        Stream stream = useAsync
            ? await responseMessage.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false)
            : responseMessage.Content.ReadAsStream(cancellationToken);

        if (batchLogPerf)
            LogBatchStreamPerformance($"client streamReady streamMs={GetElapsedMilliseconds(streamTimestamp):F1}");

        while (true)
        {
            var headerTimestamp = batchLogPerf ? Stopwatch.GetTimestamp() : 0;
            if (await ReadAsync(header.AsMemory(0, 1)).ConfigureAwait(false) == 0)
                break;

            await ReadExactlyAsync(header.AsMemory(1)).ConfigureAwait(false);
            var headerMs = batchLogPerf ? GetElapsedMilliseconds(headerTimestamp) : 0;

            var resourceIndex = BinaryPrimitives.ReadInt32LittleEndian(header);
            var payloadLength = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(4));

            if (resourceIndex < 0 || resourceIndex >= values.Length)
                throw new Exception("The batch stream contains an invalid resource index.");

            if (payloadLength < 0)
                throw new Exception("The batch stream contains an invalid payload length.");

            if (payloadLength % elementSize != 0)
                throw new Exception("The batch stream contains an unaligned payload length.");

            if (offsets[resourceIndex] > expectedLengths[resourceIndex] - payloadLength)
                throw new Exception("The batch stream contains more data than expected.");

            var remainingPayloadLength = payloadLength;
            var frameTimestamp = batchLogPerf ? Stopwatch.GetTimestamp() : 0;
            var frameOffset = offsets[resourceIndex];
            frameCount++;
            totalPayloadBytes += payloadLength;

            if (batchLogPerf)
                chunkFrameCounts[resourceIndex]++;

            while (remainingPayloadLength > 0)
            {
                if (chunkOffsets[resourceIndex] == chunkLengths[resourceIndex])
                {
                    var remainingLength = (expectedLengths[resourceIndex] - offsets[resourceIndex]) / elementSize;
                    RentNextChunk(resourceIndex, remainingLength);
                }

                var count = Math.Min(remainingPayloadLength, chunkLengths[resourceIndex] - chunkOffsets[resourceIndex]);

                var readTimestamp = batchLogPerf ? Stopwatch.GetTimestamp() : 0;
                using var manager = new CastMemoryManager<T, byte>(chunks[resourceIndex]);
                var target = manager.Memory.Slice(chunkOffsets[resourceIndex], count);
                await ReadExactlyAsync(target).ConfigureAwait(false);
                var readMs = batchLogPerf ? GetElapsedMilliseconds(readTimestamp) : 0;

                chunkOffsets[resourceIndex] += count;
                offsets[resourceIndex] += count;
                remainingPayloadLength -= count;
                reportProgress?.Invoke(count);

                if (batchLogPerf)
                {
                    var chunkLength = chunkLengths[resourceIndex];
                    var isChunkComplete = chunkLength > 0 && chunkOffsets[resourceIndex] == chunkLength;
                    chunkReadOperations[resourceIndex]++;
                    chunkReadMilliseconds[resourceIndex] += readMs;

                    if (readMs >= 10)
                    {
                        LogBatchStreamPerformance($"client readPayload resource='{resourcePaths[resourceIndex]}' index={resourceIndex} offsetBytes={offsets[resourceIndex] - count} bytes={count} readMs={readMs:F1} chunkOffsetBytes={chunkOffsets[resourceIndex]} chunkBytes={chunkLength}");
                    }

                    if (isChunkComplete)
                    {
                        completedChunkCounts[resourceIndex]++;
                        LogBatchStreamPerformance($"client chunkComplete resource='{resourcePaths[resourceIndex]}' index={resourceIndex} chunk={completedChunkCounts[resourceIndex]} receivedBytes={offsets[resourceIndex]} expectedBytes={expectedLengths[resourceIndex]} frames={chunkFrameCounts[resourceIndex]} reads={chunkReadOperations[resourceIndex]} readMs={chunkReadMilliseconds[resourceIndex]:F1} chunkMs={GetElapsedMilliseconds(chunkTimestamps[resourceIndex]):F1} elapsedMs={GetElapsedMilliseconds(batchTimestamp):F1}");
                        chunkFrameCounts[resourceIndex] = 0;
                        chunkReadOperations[resourceIndex] = 0;
                        chunkReadMilliseconds[resourceIndex] = 0;
                    }
                }
            }

            if (batchLogPerf && (headerMs >= 10 || GetElapsedMilliseconds(frameTimestamp) >= 10))
                LogBatchStreamPerformance($"client frame resource='{resourcePaths[resourceIndex]}' index={resourceIndex} offsetBytes={frameOffset} payloadBytes={payloadLength} headerMs={headerMs:F1} totalMs={GetElapsedMilliseconds(frameTimestamp):F1} frames={frameCount}");
        }

        if (!offsets.SequenceEqual(expectedLengths))
            throw new Exception("The batch stream ended before all data was received.");

        if (batchLogPerf)
            LogBatchStreamPerformance($"client readComplete frames={frameCount} payloadBytes={totalPayloadBytes} totalMs={GetElapsedMilliseconds(batchTimestamp):F1}");

        return values;

        void RentNextChunk(int index, long remainingLength)
        {
            if (bufferProvider is null)
                throw new Exception("The batch stream contains more chunk data than expected.");

            var chunkLength = checked((int)Math.Min(maxChunkLength, remainingLength));
            var providerTimestamp = batchLogPerf ? Stopwatch.GetTimestamp() : 0;
            var memory = bufferProvider(resourcePaths[index], chunkLength, remainingLength);

            if (memory.Length < chunkLength)
                throw new ArgumentException($"The buffer provided for resource path '{resourcePaths[index]}' is too small. Required length: {chunkLength}. Provided length: {memory.Length}.", nameof(bufferProvider));

            chunks[index] = memory[..chunkLength];
            chunkOffsets[index] = 0;
            chunkLengths[index] = checked(chunkLength * elementSize);

            if (batchLogPerf)
            {
                chunkTimestamps[index] = Stopwatch.GetTimestamp();
                LogBatchStreamPerformance($"client rentChunk resource='{resourcePaths[index]}' index={index} chunk={completedChunkCounts[index] + 1} offsetBytes={offsets[index]} length={chunkLength} remainingLength={remainingLength} providerMs={GetElapsedMilliseconds(providerTimestamp):F1}");
            }
        }

        ValueTask<int> ReadAsync(Memory<byte> buffer)
        {
            return useAsync
                ? stream.ReadAsync(buffer, cancellationToken)
                : ValueTask.FromResult(stream.Read(buffer.Span));
        }

        async Task ReadExactlyAsync(Memory<byte> buffer)
        {
            while (!buffer.IsEmpty)
            {
                var bytesRead = await ReadAsync(buffer).ConfigureAwait(false);

                if (bytesRead == 0)
                    throw new Exception("The batch stream ended in the middle of a frame.");

                buffer = buffer[bytesRead..];
            }
        }
    }

    /// <inheritdoc />
    public void Export(
        DateTime begin, 
        DateTime end,
        TimeSpan filePeriod,
        string? fileFormat,
        IEnumerable<string> resourcePaths,
        IReadOnlyDictionary<string, object>? configuration,
        string targetFolder,
        Precision precision,
        Action<double, string>? onProgress = default)
    {
        var actualConfiguration = configuration is null
            ? default
            : JsonSerializer.Deserialize<IReadOnlyDictionary<string, JsonElement>?>(JsonSerializer.Serialize(configuration));

        var exportParameters = new V2.ExportParameters(
            begin,
            end,
            filePeriod,
            fileFormat,
            resourcePaths.ToList(),
            actualConfiguration,
            precision);

        // Start Job
        var job = V2.Jobs.Export(exportParameters);

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

    /// <inheritdoc />
    public async Task ExportAsync(
        DateTime begin, 
        DateTime end,
        TimeSpan filePeriod,
        string? fileFormat,
        IEnumerable<string> resourcePaths,
        IReadOnlyDictionary<string, object>? configuration,
        string targetFolder,
        Precision precision,
        Action<double, string>? onProgress = default,
        CancellationToken cancellationToken = default)
    {
        var actualConfiguration = configuration is null
            ? default
            : JsonSerializer.Deserialize<IReadOnlyDictionary<string, JsonElement>?>(JsonSerializer.Serialize(configuration));

        var exportParameters = new V2.ExportParameters(
            begin,
            end,
            filePeriod,
            fileFormat,
            resourcePaths.ToList(),
            actualConfiguration,
            precision);

        // Start Job
        var job = await V2.Jobs.ExportAsync(exportParameters).ConfigureAwait(false);

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
/// <typeparam name="T">The element type of the data.</typeparam>
public record DataResponse<T>(
    V1.CatalogItem CatalogItem, 
    string? Name,
    string? Unit,
    string? Description,
    TimeSpan SamplePeriod,
    ReadOnlyMemory<T> Values) where T : struct;

internal sealed class CastMemoryManager<TFrom, TTo> : MemoryManager<TTo>
    where TFrom : struct
    where TTo : struct
{
    private readonly Memory<TFrom> _values;
    private MemoryHandle _handle;

    public CastMemoryManager(Memory<TFrom> values) => _values = values;

    public override Span<TTo> GetSpan() => MemoryMarshal.Cast<TFrom, TTo>(_values.Span);

    protected override void Dispose(bool disposing)
    {
        //
    }

    public override unsafe MemoryHandle Pin(int elementIndex = 0)
    {
        if ((uint)elementIndex > (uint)(_values.Length * Unsafe.SizeOf<TFrom>()))
            throw new ArgumentOutOfRangeException(nameof(elementIndex));

        _handle = _values.Pin();
        var pointer = (byte*)_handle.Pointer + elementIndex;

        return new MemoryHandle(pointer, pinnable: this);
    }

    public override void Unpin()
    {
        _handle.Dispose();
    }
}
{{/Special_NexusFeatures}}
}

{{#SubClients}}
{{.}}
{{/SubClients}}
