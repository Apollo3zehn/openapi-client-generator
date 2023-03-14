# pyright: reportPrivateUsage=false

# Python <= 3.9
from __future__ import annotations

{{{Encoder}}}

{{#Special_NexusFeatures}}
import asyncio
import base64
{{/Special_NexusFeatures}}
{{#Special_RefreshTokenSupport}}
import hashlib
{{/Special_RefreshTokenSupport}}
import json
{{#Special_RefreshTokenSupport}}
import os
{{/Special_RefreshTokenSupport}}
{{#Special_NexusFeatures}}
import time
from array import array
{{/Special_NexusFeatures}}
from dataclasses import dataclass
from datetime import datetime, timedelta
from enum import Enum
{{#Special_RefreshTokenSupport}}
from pathlib import Path
from tempfile import NamedTemporaryFile
from threading import Lock
{{/Special_RefreshTokenSupport}}
from typing import (Any, AsyncIterable, Awaitable, Callable, Iterable,
                    Optional, Type, Union, cast)
from urllib.parse import quote
from uuid import UUID
{{#Special_NexusFeatures}}
from zipfile import ZipFile
{{/Special_NexusFeatures}}

from httpx import AsyncClient, Client, Request, Response
{{#Special_RefreshTokenSupport}}
from httpx import codes
{{/Special_RefreshTokenSupport}}

def _to_string(value: Any) -> str:

    if type(value) is datetime:
        return value.isoformat()

    elif type(value) is str:
        return value

    else:
        return str(value)

_json_encoder_options: JsonEncoderOptions = JsonEncoderOptions(
    property_name_encoder=lambda value: to_camel_case(value) if value != "class_" else "class",
    property_name_decoder=lambda value: to_snake_case(value) if value != "class" else "_class"
)

_json_encoder_options.encoders[Enum] = lambda value: to_camel_case(value.name)
_json_encoder_options.decoders[Enum] = lambda typeCls, value: cast(Type[Enum], typeCls)[to_snake_case(value).upper()]

class {{{ExceptionType}}}(Exception):
    """A {{{ExceptionType}}}."""

    def __init__(self, status_code: str, message: str):
        self.status_code = status_code
        self.message = message

    status_code: str
    """The exception status code."""

    message: str
    """The exception message."""

{{{Models}}}
{{{AsyncSubClientsSource}}}
{{{SyncSubClientsSource}}}

{{#Special_NexusFeatures}}
@dataclass(frozen=True)
class DataResponse:
    """
    Result of a data request with a certain resource path.

    Args:
        catalog_item: The catalog item.
        name: The resource name.
        unit: The optional resource unit.
        description: The optional resource description.
        sample_period: The sample period.
        values: The data.
    """

    catalog_item: CatalogItem
    """The catalog item."""

    name: Optional[str]
    """The resource name."""

    unit: Optional[str]
    """The optional resource unit."""

    description: Optional[str]
    """The optional resource description."""

    sample_period: timedelta
    """The sample period."""

    values: array[float]
    """The data."""
{{/Special_NexusFeatures}}

{{{AsyncClient}}}
{{{SyncClient}}}