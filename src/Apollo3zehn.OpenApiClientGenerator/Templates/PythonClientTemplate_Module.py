# Python <= 3.9
from __future__ import annotations

import json
from .{{{ClientName}}} import {{{ClientName}}}Client, {{{ClientName}}}AsyncClient
from . import JsonEncoder, _json_encoder_options, _to_string
from dataclasses import dataclass
from typing import Awaitable, Optional, Type, TypeVar, cast, Union, Iterable, AsyncIterable
from urllib.parse import quote
from datetime import datetime
from uuid import UUID
from httpx import AsyncClient, Client, Response

T = TypeVar("T")

{{SyncClient}}
{{SyncSubClients}}
{{AsyncClient}}
{{AsyncSubClients}}
{{Models}}
