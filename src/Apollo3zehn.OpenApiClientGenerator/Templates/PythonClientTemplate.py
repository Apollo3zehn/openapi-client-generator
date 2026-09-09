from __future__ import annotations

{{#Special_NexusFeatures}}
import base64
{{/Special_NexusFeatures}}
import json
{{#Special_NexusFeatures}}
import struct
import asyncio
import time
{{/Special_NexusFeatures}}
from dataclasses import dataclass
{{#Special_NexusFeatures}}
from datetime import datetime, timedelta
from tempfile import NamedTemporaryFile
{{/Special_NexusFeatures}}
from typing import (Any, AsyncIterable, Callable, Iterable, Optional, Type,
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
