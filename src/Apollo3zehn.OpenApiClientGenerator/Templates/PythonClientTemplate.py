# 0 = ClientName
# 1 = NexusConfigurationHeaderKey
# 2 = AuthorizationHeaderKey
# 3 = SubClientFields
# 4 = SubClientFieldAssignment
# 5 = SubClientProperties
# 6 = ExceptionType
# 7 = Async / 
# 8 = async def / def
# 9 = await / 
# 10 = aclose / close
# 11 = aiter_bytes / iter_bytes
# 12 = asyncio.sleep / time.sleep
# 13 = aenter / enter
# 14 = aexit / exit
# 15 = aread / read
# 16 = async for / for

class _Disposable{{7}}Configuration:
    _client : {{0}}{{7}}Client

    def __init__(self, client: {{0}}{{7}}Client):
        self._client = client

    # "disposable" methods
    def __enter__(self):
        pass

    def __exit__(self, exc_type, exc_value, exc_traceback):
        self._client.clear_configuration()

class {{0}}{{7}}Client:
    """A client for the Nexus system."""
    
    _nexus_configuration_header_key: str = "{{1}}"
    _authorization_header_key: str = "{{2}}"

    _token_folder_path: str = os.path.join(str(Path.home()), ".nexus-api", "tokens")
    _mutex: Lock = Lock()

    _token_pair: Optional[TokenPair]
    _http_client: {{7}}Client
    _token_file_path: Optional[str]

{{3}}

    @classmethod
    def create(cls, base_url: str) -> {{0}}{{7}}Client:
        """
        Initializes a new instance of the {{0}}{{7}}Client
        
            Args:
                base_url: The base URL to use.
        """
        return {{0}}{{7}}Client({{7}}Client(base_url=base_url, timeout=60.0))

    def __init__(self, http_client: {{7}}Client):
        """
        Initializes a new instance of the {{0}}{{7}}Client
        
            Args:
                http_client: The HTTP client to use.
        """

        if http_client.base_url is None:
            raise Exception("The base url of the HTTP client must be set.")

        self._http_client = http_client
        self._token_pair = None

{{4}}

    @property
    def is_authenticated(self) -> bool:
        """Gets a value which indicates if the user is authenticated."""
        return self._token_pair is not None

{{5}}

    {{8}} sign_in(self, refresh_token: str):
        """Signs in the user.

        Args:
            token_pair: The refresh token.
        """

        actual_refresh_token: str

        sha256 = hashlib.sha256()
        sha256.update(refresh_token.encode())
        refresh_token_hash = sha256.hexdigest()
        self._token_file_path = os.path.join(self._token_folder_path, refresh_token_hash + ".json")
        
        if Path(self._token_file_path).is_file():
            with open(self._token_file_path) as file:
                actual_refresh_token = file.read()

        else:
            Path(self._token_folder_path).mkdir(parents=True, exist_ok=True)

            with open(self._token_file_path, "w") as file:
                file.write(refresh_token)
                actual_refresh_token = refresh_token
                
        {{9}}self._refresh_token(actual_refresh_token)

    def attach_configuration(self, configuration: Any) -> Any:
        """Attaches configuration data to subsequent Nexus API requests.
        
        Args:
            configuration: The configuration data.
        """

        encoded_json = base64.b64encode(json.dumps(configuration).encode("utf-8")).decode("utf-8")

        if self._nexus_configuration_header_key in self._http_client.headers:
            del self._http_client.headers[self._nexus_configuration_header_key]

        self._http_client.headers[self._nexus_configuration_header_key] = encoded_json

        return _Disposable{{7}}Configuration(self)

    def clear_configuration(self) -> None:
        """Clears configuration data for all subsequent Nexus API requests."""

        if self._nexus_configuration_header_key in self._http_client.headers:
            del self._http_client.headers[self._nexus_configuration_header_key]

    {{8}} _invoke(self, typeOfT: Type[T], method: str, relative_url: str, accept_header_value: Optional[str], content_type_value: Optional[str], content: Union[None, str, bytes, Iterable[bytes], AsyncIterable[bytes]]) -> T:

        # prepare request
        request = self._build_request_message(method, relative_url, content, content_type_value, accept_header_value)

        # send request
        response = {{9}}self._http_client.send(request)

        # process response
        if not response.is_success:
            
            # try to refresh the access token
            if response.status_code == codes.UNAUTHORIZED and self._token_pair is not None:

                www_authenticate_header = response.headers.get("WWW-Authenticate")
                sign_out = True

                if www_authenticate_header is not None:

                    if "The token expired at" in www_authenticate_header:

                        try:
                            {{9}}self._refresh_token(self._token_pair.refresh_token)

                            new_request = self._build_request_message(method, relative_url, content, content_type_value, accept_header_value)
                            new_response = {{9}}self._http_client.send(new_request)

                            {{9}}response.{{10}}()
                            response = new_response
                            sign_out = False

                        except:
                            pass

                if sign_out:
                    self._sign_out()

            if not response.is_success:

                message = response.text
                status_code = f"N00.{response.status_code}"

                if not message:
                    raise {{6}}(status_code, f"The HTTP request failed with status code {response.status_code}.")

                else:
                    raise {{6}}(status_code, f"The HTTP request failed with status code {response.status_code}. The response message is: {message}")

        try:

            if typeOfT is type(None):
                return typing.cast(T, type(None))

            elif typeOfT is Response:
                return typing.cast(T, response)

            else:

                jsonObject = json.loads(response.text)
                return_value = JsonEncoder.decode(typeOfT, jsonObject, _json_encoder_options)

                if return_value is None:
                    raise {{6}}(f"N01", "Response data could not be deserialized.")

                return return_value

        finally:
            if typeOfT is not Response:
                {{9}}response.{{10}}()
    
    def _build_request_message(self, method: str, relative_url: str, content: Any, content_type_value: Optional[str], accept_header_value: Optional[str]) -> Request:
       
        request_message = self._http_client.build_request(method, relative_url, content = content)

        if content_type_value is not None:
            request_message.headers["Content-Type"] = content_type_value

        if accept_header_value is not None:
            request_message.headers["Accept"] = accept_header_value

        return request_message

    {{8}} _refresh_token(self, refresh_token: str):
        self._mutex.acquire()

        try:
            # make sure the refresh token has not already been redeemed
            if (self._token_pair is not None and refresh_token != self._token_pair.refresh_token):
                return

            # see https://github.com/AzureAD/azure-activedirectory-identitymodel-extensions-for-dotnet/blob/dev/src/Microsoft.IdentityModel.Tokens/Validators.cs#L390

            refresh_request = RefreshTokenRequest(refresh_token)
            token_pair = {{9}}self.users.refresh_token(refresh_request)

            if self._token_file_path is not None:
                Path(self._token_folder_path).mkdir(parents=True, exist_ok=True)
                
                with open(self._token_file_path, "w") as file:
                    file.write(token_pair.refresh_token)

            authorizationHeaderValue = f"Bearer {token_pair.access_token}"

            if self._authorization_header_key in self._http_client.headers:
                del self._http_client.headers[self._authorization_header_key]

            self._http_client.headers[self._authorization_header_key] = authorizationHeaderValue
            self._token_pair = token_pair

        finally:
            self._mutex.release()

    def _sign_out(self) -> None:

        if self._authorization_header_key in self._http_client.headers:
            del self._http_client.headers[self._authorization_header_key]

        self._token_pair = None

    # "disposable" methods
    {{8}} __{{13}}__(self) -> {{0}}{{7}}Client:
        return self

    {{8}} __{{14}}__(self, exc_type, exc_value, exc_traceback):
        if (self._http_client is not None):
            {{9}}self._http_client.{{10}}()

    # high-level methods

    {{8}} load(
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

        catalog_item_map = {{9}}self.catalogs.search_catalog_items(list(resource_paths))
        result: dict[str, DataResponse] = {}
        progress: float = 0

        for (resource_path, catalog_item) in catalog_item_map.items():

            response = {{9}}self.data.get_stream(resource_path, begin, end)

            try:
                double_data = {{9}}self._read_as_double(response)

            finally:
                {{9}}response.{{10}}()

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

            progress = progress + 1.0 / len(catalog_item_map)

            if on_progress is not None:
                on_progress(progress)
                
        return result

    {{8}} _read_as_double(self, response: Response):
        
        byteBuffer = {{9}}response.{{15}}()

        if len(byteBuffer) % 8 != 0:
            raise Exception("The data length is invalid.")

        doubleBuffer = array("d", byteBuffer)

        return doubleBuffer 

    {{8}} export(
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
        job = {{9}}self.jobs.export(export_parameters)

        # Wait for job to finish
        artifact_id: Optional[str] = None

        while True:
            {{9}}{{12}}(1)
            
            job_status = {{9}}self.jobs.get_job_status(job.id)

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

            response = {{9}}self.artifacts.download(artifact_id)
            
            try:

                length: Optional[int] = None

                try:
                    length = int(response.headers["Content-Length"])
                except:
                    pass

                consumed = 0.0

                {{16}} data in response.{{11}}():

                    target_stream.write(data)
                    consumed += len(data)

                    if length is not None and on_progress is not None:
                        if consumed < length:
                            on_progress(consumed / length, "download")

            finally:
                {{9}}response.{{10}}()

            if on_progress is not None:
                on_progress(1, "download")

            # Extract file
            with ZipFile(target_stream, "r") as zipFile:
                zipFile.extractall(target_folder)

        if on_progress is not None:
            on_progress(1, "extract")