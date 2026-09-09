from __future__ import annotations

{{#Special_NexusFeatures}}
import base64
import io
{{/Special_NexusFeatures}}
import json
{{#Special_NexusFeatures}}
import asyncio
import time
import pyarrow as pa
import pyarrow.ipc as pa_ipc
{{/Special_NexusFeatures}}
from dataclasses import dataclass
{{#Special_NexusFeatures}}
from datetime import datetime, timedelta
from tempfile import NamedTemporaryFile
{{/Special_NexusFeatures}}
from typing import (Any, AsyncIterable, Callable, Iterable, Iterator, Optional, Type,
                    TypeVar, Union, cast)
{{#Special_NexusFeatures}}
from zipfile import ZipFile
{{/Special_NexusFeatures}}

from httpx import AsyncClient, Client, Request, Response

from ._encoder import JsonEncoder
from ._shared import {{{ExceptionType}}}, _json_encoder_options
{{{VersioningImports}}}

T = TypeVar("T")

{{{SyncMainClient}}}
{{{AsyncMainClient}}}

{{#Special_NexusFeatures}}
class _IterableByteStream:
    _chunks: Iterable[bytes]
    _pending: bytearray
    _iterator: Optional[Iterator[bytes]]
    closed: bool

    def __init__(self, chunks: Iterable[bytes]):
        self._chunks = chunks
        self._pending = bytearray()
        self._iterator = None
        self.closed = False

    def close(self) -> None:
        self.closed = True

    def readable(self) -> bool:
        return True

    def seekable(self) -> bool:
        return False

    def read(self, size: Optional[int] = None) -> bytes:
        if self.closed:
            raise ValueError("I/O operation on closed file")

        iterator = self._iterator

        if iterator is None:
            iterator = iter(self._chunks)
            self._iterator = iterator

        if size is None or size < 0:
            chunks = [bytes(self._pending)]
            self._pending.clear()
            chunks.extend(iterator)
            return b"".join(chunks)

        while len(self._pending) < size:
            try:
                self._pending.extend(next(iterator))
            except StopIteration:
                break

        result = bytes(self._pending[:size])
        del self._pending[:size]
        return result


@dataclass(frozen=True)
class ResourceInfo:
    """
    Metadata for a data resource.

    Args:
        catalog_item: The catalog item.
        name: The resource name.
        unit: The optional resource unit.
        description: The optional resource description.
        sample_period: The sample period.
    """

    catalog_item: CatalogItem
    """The catalog item."""

    name: str
    """The resource name."""

    unit: Optional[str]
    """The optional resource unit."""

    description: Optional[str]
    """The optional resource description."""

    sample_period: timedelta
    """The sample period."""


@dataclass(frozen=True)
class DataResponse:
    """
    Result of a data request with a certain resource path.

    Args:
        info: The resource metadata.
        values: The data.
    """

    info: ResourceInfo
    """The resource metadata."""

    values: memoryview
    """The data."""
{{/Special_NexusFeatures}}
