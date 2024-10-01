# Python <= 3.9
from __future__ import annotations

import json
from typing import (Any, AsyncIterable, Iterable, Optional, Type, TypeVar,
                    Union, cast)

from httpx import AsyncClient, Client, Request, Response
{{{VersioningImports}}}
from nexus_api.PythonEncoder import JsonEncoder, _json_encoder_options
from nexus_api.Shared import {{{ExceptionType}}}

T = TypeVar("T")

{{{SyncMainClient}}}
{{{AsyncMainClient}}}