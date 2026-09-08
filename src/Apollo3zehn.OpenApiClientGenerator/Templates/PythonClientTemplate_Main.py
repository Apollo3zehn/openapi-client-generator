{{#Special_NexusFeatures}}
class _Disposable{{{Async}}}Configuration:
    ___client : {{{ClientName}}}{{{Async}}}Client

    def __init__(self, client: {{{ClientName}}}{{{Async}}}Client):
        self.___client = client

    # "disposable" methods
    def __enter__(self):
        pass

    def __exit__(self, exc_type, exc_value, exc_traceback):
        self.___client.clear_configuration()
{{/Special_NexusFeatures}}

class {{{ClientName}}}{{{Async}}}Client:
    """A client for the {{{ClientName}}} system."""
    
{{#Special_NexusFeatures}}
    ___configuration_header_key: str = "{{{Special_ConfigurationHeaderKey}}}"
    ___batch_stream_protocol_version: int = 1
    ___batch_stream_data_frame_type: int = 1
    ___batch_stream_error_frame_type: int = 2
    ___batch_stream_end_frame_type: int = 3
    ___batch_stream_max_error_message_length: int = 64 * 1024
{{/Special_NexusFeatures}}
{{#Special_AccessTokenSupport}}
    ___authorization_header_key: str = "Authorization"

    ___token: Optional[str]
{{/Special_AccessTokenSupport}}
    ___http_client: {{{Async}}}Client

{{{VersioningFields}}}

    @classmethod
    def create(cls, base_url: str) -> {{{ClientName}}}{{{Async}}}Client:
        """
        Initializes a new instance of the {{{ClientName}}}{{{Async}}}Client
        
            Args:
                base_url: The base URL to use.
        """
        return {{{ClientName}}}{{{Async}}}Client({{{Async}}}Client(base_url=base_url, timeout=60.0))

    def __init__(self, http_client: {{{Async}}}Client):
        """
        Initializes a new instance of the {{{ClientName}}}{{{Async}}}Client
        
            Args:
                http_client: The HTTP client to use.
        """

        if http_client.base_url is None:
            raise Exception("The base url of the HTTP client must be set.")

        self.___http_client = http_client
{{#Special_AccessTokenSupport}}
        self.___token = None
{{/Special_AccessTokenSupport}}

{{{VersioningFieldAssignments}}}

{{#Special_AccessTokenSupport}}
    @property
    def is_authenticated(self) -> bool:
        """Gets a value which indicates if the user is authenticated."""
        return self.___token is not None
{{/Special_AccessTokenSupport}}

{{{VersioningProperties}}}

{{#Special_AccessTokenSupport}}
    def sign_in(self, access_token: str):
        """Signs in the user.

        Args:
            access_token: The access token.
        """

        authorization_header_value = f"Bearer {access_token}"

        if self.___authorization_header_key in self.___http_client.headers:
            del self.___http_client.headers[self.___authorization_header_key]

        self.___http_client.headers[self.___authorization_header_key] = authorization_header_value
        self.___token = access_token
{{/Special_AccessTokenSupport}}

{{#Special_NexusFeatures}}
    def attach_configuration(self, configuration: Any) -> Any:
        """Attaches configuration data to subsequent API requests.
        
        Args:
            configuration: The configuration data.
        """

        encoded_json = base64.b64encode(json.dumps(configuration).encode("utf-8")).decode("utf-8")

        if self.___configuration_header_key in self.___http_client.headers:
            del self.___http_client.headers[self.___configuration_header_key]

        self.___http_client.headers[self.___configuration_header_key] = encoded_json

        return _Disposable{{{Async}}}Configuration(self)

    def clear_configuration(self) -> None:
        """Clears configuration data for all subsequent API requests."""

        if self.___configuration_header_key in self.___http_client.headers:
            del self.___http_client.headers[self.___configuration_header_key]
{{/Special_NexusFeatures}}

    {{{Def}}} _invoke(self, typeOfT: Optional[Type[T]], method: str, relative_url: str, accept_header_value: Optional[str], content_type_value: Optional[str], content: Union[None, str, bytes, Iterable[bytes], AsyncIterable[bytes]]) -> T:

        # prepare request
        request = self._build_request_message(method, relative_url, content, content_type_value, accept_header_value)

        # send request
        response = {{{Await}}}self.___http_client.send(request, stream=typeOfT is Response)

        # process response
        if not response.is_success:
            try:
                {{{Await}}}response.{{{Read}}}()
                message = response.text
                status_code = f"{{{ExceptionCodePrefix}}}00.{response.status_code}"

                if not message:
                    raise {{{ExceptionType}}}(status_code, f"The HTTP request failed with status code {response.status_code}.")

                else:
                    raise {{{ExceptionType}}}(status_code, f"The HTTP request failed with status code {response.status_code}. The response message is: {message}")
            finally:
                {{{Await}}}response.{{{Aclose}}}()

        try:

            if typeOfT is type(None):
                return cast(T, type(None))

            elif typeOfT is Response:
                return cast(T, response)

            else:

                jsonObject = json.loads(response.text)
                return_value = JsonEncoder.decode(cast(Type[T], typeOfT), jsonObject, _json_encoder_options)

                if return_value is None:
                    raise {{{ExceptionType}}}("{{{ExceptionCodePrefix}}}01", "Response data could not be deserialized.")

                return return_value

        finally:
            if typeOfT is not Response:
                {{{Await}}}response.{{{Aclose}}}()
    
    def _build_request_message(self, method: str, relative_url: str, content: Any, content_type_value: Optional[str], accept_header_value: Optional[str]) -> Request:
       
        request_message = self.___http_client.build_request(method, relative_url, content = content)

        if content_type_value is not None:
            request_message.headers["Content-Type"] = content_type_value

        if accept_header_value is not None:
            request_message.headers["Accept"] = accept_header_value

        return request_message

    # "disposable" methods
    {{{Def}}} __{{{Enter}}}__(self) -> {{{ClientName}}}{{{Async}}}Client:
        return self

    {{{Def}}} __{{{Exit}}}__(self, exc_type, exc_value, exc_traceback):
        if (self.___http_client is not None):
            {{{Await}}}self.___http_client.{{{Aclose}}}()

{{#Special_NexusFeatures}}
    {{{Def}}} load(
        self,
        begin: datetime, 
        end: datetime, 
        resource_paths: Iterable[str],
        precision: Precision,
        on_progress: Optional[Callable[[float], None]] = None) -> dict[str, DataResponse]:
        """This high-level methods simplifies loading multiple resources at once.

        Args:
            begin: Start date/time.
            end: End date/time.
            resource_paths: The resource paths.
            precision: The floating point precision requested from the server.
            onProgress: A callback which accepts the current progress.
        """

        resource_path_list = list(resource_paths)

        if not resource_path_list:
            return {}

        precision_size = precision.value

        catalog_item_map = {{{Await}}}self.v1.catalogs.search_catalog_items(resource_path_list)
        response = {{{Await}}}self.v2.data.get_stream(BatchStreamRequest(begin, end, resource_path_list, precision))
        expected_lengths = [
            ((end - begin) // catalog_item_map[path].representation.sample_period) * precision_size
            for path in resource_path_list]
        total_length = sum(expected_lengths)
        consumed = 0

        def report_progress(bytes_read: int) -> None:
            nonlocal consumed
            consumed += bytes_read
            if total_length > 0 and on_progress is not None:
                on_progress(min(1, consumed / total_length))

        try:
            values = {{{Await}}}self._read_batch(response, expected_lengths, precision, report_progress)
        finally:
            {{{Await}}}response.{{{Aclose}}}()

        result: dict[str, DataResponse] = {}

        for resource_path, value in zip(resource_path_list, values):

            catalog_item = catalog_item_map[resource_path]

            resource = catalog_item.resource

            unit = cast(str, resource.properties["unit"]) \
                if resource.properties is not None and "unit" in resource.properties and type(resource.properties["unit"]) == str \
                else None

            description = cast(str, resource.properties["description"]) \
                if resource.properties is not None and "description" in resource.properties and type(resource.properties["description"]) == str \
                else None

            sample_period = catalog_item.representation.sample_period

            result[resource_path] = DataResponse(
                catalog_item=catalog_item,
                name=resource.id,
                unit=unit,
                description=description,
                sample_period=sample_period,
                values=value
            )

        if on_progress is not None:
            on_progress(1)
                
        return result

    {{{Def}}} _read_batch(
        self,
        response: Response,
        expected_lengths: list[int],
        precision: Precision,
        report_progress: Optional[Callable[[int], None]] = None) -> list[memoryview]:
        array_type = "f" if precision == Precision.FLOAT32 else "d"
        precision_size = precision.value

        buffers = [bytearray(length) for length in expected_lengths]
        byte_views = [memoryview(buffer).cast("B") for buffer in buffers]
        offsets = [0] * len(expected_lengths)
        pending = bytearray()
        has_version = False
        has_end_frame = False

        {{{For}}} data in response.{{{Aiter_bytes}}}():
            pending.extend(data)

            while True:
                if not has_version:
                    if len(pending) < 1:
                        break

                    version = pending[0]
                    del pending[:1]

                    if version != self.___batch_stream_protocol_version:
                        raise Exception(f"The batch stream uses an unsupported protocol version: {version}.")

                    has_version = True

                if len(pending) < 1:
                    break

                frame_type = pending[0]

                if frame_type == self.___batch_stream_end_frame_type:
                    del pending[:1]

                    if pending:
                        raise Exception("The batch stream contains data after the end frame.")

                    has_end_frame = True
                    break

                if frame_type == self.___batch_stream_error_frame_type:
                    if len(pending) < 5:
                        break

                    message_length = struct.unpack_from("<i", pending, 1)[0]

                    if message_length < 0 or message_length > self.___batch_stream_max_error_message_length:
                        raise Exception("The batch stream contains an invalid error message length.")

                    if len(pending) < 5 + message_length:
                        break

                    message = bytes(pending[5:5 + message_length]).decode("utf-8")
                    raise {{{ExceptionType}}}("{{{ExceptionCodePrefix}}}02", message)

                if frame_type != self.___batch_stream_data_frame_type:
                    raise Exception(f"The batch stream contains an unknown frame type: {frame_type}.")

                if len(pending) < 6:
                    break

                current_index = pending[1]
                payload_length = struct.unpack_from("<i", pending, 2)[0]

                if current_index >= len(byte_views):
                    raise Exception("The batch stream contains an invalid resource index.")

                if payload_length < 0:
                    raise Exception("The batch stream contains an invalid payload length.")

                if payload_length % precision_size != 0:
                    raise Exception("The batch stream contains an unaligned payload length.")

                if offsets[current_index] > expected_lengths[current_index] - payload_length:
                    raise Exception("The batch stream contains more data than expected.")

                if len(pending) < 6 + payload_length:
                    break

                offset = offsets[current_index]
                byte_views[current_index][offset:offset + payload_length] = pending[6:6 + payload_length]
                del pending[:6 + payload_length]
                offsets[current_index] += payload_length

                if report_progress is not None:
                    report_progress(payload_length)

            if has_end_frame:
                break

        if not has_version:
            raise Exception("The batch stream ended before the protocol version was received.")

        if pending and not has_end_frame:
            raise Exception("The batch stream ended in the middle of a frame.")

        if not has_end_frame:
            raise Exception("The batch stream ended before the end frame was received.")

        if offsets != expected_lengths:
            raise Exception("The batch stream ended before all data was received.")

        return [cast(memoryview, memoryview(buffer).cast(array_type)) for buffer in buffers]

    {{{Def}}} export(
        self,
        begin: datetime, 
        end: datetime, 
        file_period: timedelta,
        file_format: Optional[str],
        resource_paths: Iterable[str],
        configuration: dict[str, object],
        target_folder: str,
        precision: Precision,
        on_progress: Optional[Callable[[float, str], None]] = None) -> None:
        """This high-level methods simplifies exporting multiple resources at once.

        Args:
            begin: Start date/time.
            end: End date/time.
            filePeriod: The file period. Use timedelta(0) to get a single file.
            fileFormat: The target file format. If null, data will be read (and possibly cached) but not returned. This is useful for data pre-aggregation.
            resource_paths: The resource paths to export.
            configuration: The configuration.
            targetFolder: The target folder for the files to extract.
            precision: The floating point precision requested from the server.
            onProgress: A callback which accepts the current progress and the progress message.
        """

        export_parameters = ExportParameters(
            begin,
            end,
            file_period,
            file_format,
            list(resource_paths),
            configuration,
            precision
        )

        # Start job
        job = {{{Await}}}self.v2.jobs.export(export_parameters)

        # Wait for job to finish
        artifact_id: Optional[str] = None

        while True:
            {{{Await}}}{{{AsyncioSleep}}}(1)
            
            job_status = {{{Await}}}self.v1.jobs.get_job_status(job.id)

            if (job_status.status == TaskStatus.CANCELED):
                raise Exception("The job has been cancelled.")

            elif (job_status.status == TaskStatus.FAULTED):
                raise Exception(f"The job has failed. Reason: {job_status.exception_message}")

            elif (job_status.status == TaskStatus.RAN_TO_COMPLETION):

                if (job_status.result is not None and \
                    type(job_status.result) == str):

                    artifact_id = cast(Optional[str], job_status.result)

                    break

            if job_status.progress < 1 and on_progress is not None:
                on_progress(job_status.progress, "export")

        if on_progress is not None:
            on_progress(1, "export")

        if artifact_id is None:
            raise Exception("The job result is invalid.")

        if file_format is None:
            return

        # Download zip file
        with NamedTemporaryFile() as target_stream:

            response = {{{Await}}}self.v1.artifacts.download(artifact_id)
            
            try:

                length: Optional[int] = None

                try:
                    length = int(response.headers["Content-Length"])
                except:
                    pass

                consumed = 0.0

                {{{For}}} data in response.{{{Aiter_bytes}}}():

                    target_stream.write(data)
                    consumed += len(data)

                    if length is not None and on_progress is not None:
                        if consumed < length:
                            on_progress(consumed / length, "download")

            finally:
                {{{Await}}}response.{{{Aclose}}}()

            if on_progress is not None:
                on_progress(1, "download")

            # Extract file
            with ZipFile(target_stream, "r") as zipFile:
                zipFile.extractall(target_folder)

        if on_progress is not None:
            on_progress(1, "extract")
{{/Special_NexusFeatures}}
