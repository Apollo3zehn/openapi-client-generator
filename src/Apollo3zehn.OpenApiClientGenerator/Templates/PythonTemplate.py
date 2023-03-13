# pyright: reportPrivateUsage=false

# 0 = Namespace
# 1 = ClientName
# 2 = AsyncSubClientSource
# 3 = SyncSubClientSource
# 4 = ExceptionType
# 5 = Model
# 6 = AsyncClient 
# 7 = SyncClient

# Python <= 3.9
from __future__ import annotations

import asyncio
import base64
import hashlib
import json
import os
import time
import typing
from array import array
from dataclasses import dataclass
from datetime import datetime, timedelta
from enum import Enum
from pathlib import Path
from tempfile import NamedTemporaryFile
from threading import Lock
from typing import (Any, AsyncIterable, Awaitable, Callable, Iterable,
                    Optional, Type, TypeVar, Union, cast)
from urllib.parse import quote
from uuid import UUID
from zipfile import ZipFile

from httpx import AsyncClient, Client, Request, Response, codes

from ._encoder import (JsonEncoder, JsonEncoderOptions, to_camel_case,
                       to_snake_case)

T = TypeVar("T")

def _to_string(value: Any) -> str:

    if type(value) is datetime:
        return value.isoformat()

    elif type(value) is str:
        return value

    else:
        return str(value)

_json_encoder_options: JsonEncoderOptions = JsonEncoderOptions(
    property_name_encoder=to_camel_case,
    property_name_decoder=to_snake_case
)

_json_encoder_options.encoders[Enum] = lambda value: to_camel_case(value.name)
_json_encoder_options.decoders[Enum] = lambda typeCls, value: cast(Type[Enum], typeCls)[to_snake_case(value).upper()]

class {{4}}(Exception):
    """A {{4}}."""

    def __init__(self, status_code: str, message: str):
        self.status_code = status_code
        self.message = message

    status_code: str
    """The exception status code."""

    message: str
    """The exception message."""

{{5}}
{{2}}
{{3}}
# high-level dataclasses

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

{{6}}
{{7}}