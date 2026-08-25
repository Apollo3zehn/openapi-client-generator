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
        return {{{ClientName}}}{{{Async}}}Client({{{Async}}}Client(base_url=base_url, timeout=60.0, http2=True))

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
            
            message = response.text
            status_code = f"{{{ExceptionCodePrefix}}}00.{response.status_code}"

            if not message:
                raise {{{ExceptionType}}}(status_code, f"The HTTP request failed with status code {response.status_code}.")

            else:
                raise {{{ExceptionType}}}(status_code, f"The HTTP request failed with status code {response.status_code}. The response message is: {message}")

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
        on_progress: Optional[Callable[[float], None]]) -> dict[str, DataResponse]:
        """This high-level methods simplifies loading multiple resources at once.

        Args:
            begin: Start date/time.
            end: End date/time.
            resource_paths: The resource paths.
            onProgress: A callback which accepts the current progress.
        """

        resource_path_list = list(resource_paths)
        catalog_item_map = {{{Await}}}self.v1.catalogs.search_catalog_items(resource_path_list)
        session = {{{Await}}}self.v2.data.register_batch_stream(BatchStreamRequest(begin, end, resource_path_list))
        result: dict[str, DataResponse] = {}
{{#Async}}
        responses = await asyncio.gather(*[
            self.v2.data.get_batch_stream_channel(session.session_id, channel.channel_id)
            for channel in session.channels
        ])

        response_entries = [
            (channel.resource_path, response)
            for (channel, response) in zip(session.channels, responses)
        ]
{{/Async}}
{{^Async}}
        responses = [
            (channel.resource_path, self.v2.data.get_batch_stream_channel(session.session_id, channel.channel_id))
            for channel in session.channels
        ]

        response_entries = responses
{{/Async}}

        total_length = 0
        for (_, response) in response_entries:
            try:
                total_length += int(response.headers["Content-Length"])
            except:
                pass

{{#Async}}
        consumed = 0

        def report_progress(bytes_read):
            nonlocal consumed
            consumed += bytes_read
            if total_length > 0 and on_progress is not None:
                on_progress(consumed / total_length)

        try:
            async def _read_channel(resource_path, response):
                try:
                    values = await self._read_as_double(response, report_progress)
                    return (resource_path, values, None)
                except Exception as ex:
                    return (resource_path, None, ex)

            results = await asyncio.gather(*[_read_channel(rp, resp) for (rp, resp) in response_entries])
{{/Async}}
{{^Async}}
        consumed = [0]
        _lock = Lock()

        def report_progress(bytes_read):
            with _lock:
                consumed[0] += bytes_read
                if total_length > 0 and on_progress is not None:
                    on_progress(consumed[0] / total_length)

        try:
            def _read_channel(entry):
                resource_path, response = entry
                try:
                    values = self._read_as_double(response, report_progress)
                    return (resource_path, values, None)
                except Exception as ex:
                    return (resource_path, None, ex)

            with ThreadPoolExecutor(max_workers=len(response_entries)) as executor:
                results = list(executor.map(_read_channel, response_entries))
{{/Async}}

        finally:
            for (_, response) in response_entries:
                {{{Await}}}response.{{{Aclose}}}()

        errors = [(rp, ex) for (rp, _, ex) in results if ex is not None]

        if errors:
            {{{Await}}}self._create_channel_exception(session.session_id, errors[0][0], errors[0][1])

        values = [cast(array[float], val) for (_, val, _) in results]

        for ((resource_path, _), double_data) in zip(response_entries, values):

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
                values=double_data
            )

        if on_progress is not None:
            on_progress(1)
                
        return result

    {{{Def}}} _read_as_double(self, response: Response, report_progress: Optional[Callable[[int], None]] = None):
        
        chunks = []
        
        {{{For}}} data in response.{{{Aiter_bytes}}}():
            chunks.append(data)
            if report_progress is not None:
                report_progress(len(data))
        
        byteBuffer = b"".join(chunks)

        if len(byteBuffer) % 8 != 0:
            raise Exception("The data length is invalid.")

        doubleBuffer = array("d", byteBuffer)

        return doubleBuffer 

    {{{Def}}} _create_channel_exception(self, session_id: UUID, resource_path: str, error: Exception) -> NoReturn:
        try:
            status = {{{Await}}}self.v2.data.get_batch_stream_session_status(session_id)
        except:
            status = None

        if status is not None and \
            status.state == BatchStreamSessionState.FAULTED and \
            status.fault_reason:

            root_cause_path = status.faulted_channel_resource_path or resource_path
            message = f"The batch stream session faulted. Root cause channel: {root_cause_path}. Reason: {status.fault_reason}"
            raise NexusException("N02", message) from error

        raise NexusException("N02", f"The batch stream session faulted. Channel: {resource_path}. Reason: {error}") from error

    {{{Def}}} export(
        self,
        begin: datetime, 
        end: datetime, 
        file_period: timedelta,
        file_format: Optional[str],
        resource_paths: Iterable[str],
        configuration: dict[str, object],
        target_folder: str,
        on_progress: Optional[Callable[[float, str], None]]) -> None:
        """This high-level methods simplifies exporting multiple resources at once.

        Args:
            begin: Start date/time.
            end: End date/time.
            filePeriod: The file period. Use timedelta(0) to get a single file.
            fileFormat: The target file format. If null, data will be read (and possibly cached) but not returned. This is useful for data pre-aggregation.
            resource_paths: The resource paths to export.
            configuration: The configuration.
            targetFolder: The target folder for the files to extract.
            onProgress: A callback which accepts the current progress and the progress message.
        """

        export_parameters = ExportParameters(
            begin,
            end,
            file_period,
            file_format,
            list(resource_paths),
            configuration
        )

        # Start job
        job = {{{Await}}}self.v1.jobs.export(export_parameters)

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
