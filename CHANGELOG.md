## v1.0.0-beta.25 - 2026-09-03

- Generate Nexus batch stream clients for the single framed response format

## v1.0.0-beta.24 - 2026-09-02

- Generate Nexus batch stream clients for the single framed response format

## v1.0.0-beta.23 - 2026-08-25

- Fix pyright errors in generated Python client: add missing `UUID` import, annotate `_create_channel_exception` return type as `NoReturn`, cast `values` to `array[float]`

## v1.0.0-beta.22 - 2026-08-25

- Add `Load`/`LoadAsync` to the `INexusClient` interface to enable DI consumers (e.g. Blazor UI) to call high-level data loading through the interface

## v1.0.0-beta.21 - 2026-08-25

- Add per-channel error capture in C# and Python high-level `Load`/`load` methods
- Query batch stream session status endpoint on failure to identify the root-cause channel and fault reason
- Wrap channel errors in `NexusException` with root-cause channel and fault reason
- Preserve original error as `InnerException` (C#) / via `raise ... from ...` (Python) in wrapped `NexusException`
- Detect user cancellation and propagate `OperationCanceledException` as-is (C#)
- Add `BatchStreamSessionState` to Python V2 imports
- Add `CreateChannelExceptionAsync` / `_create_channel_exception` helper methods

## v1.0.0-beta.20 - 2026-08-25

- Switch C# and Python high-level `Load`/`load` methods to the Nexus v2 batch streaming API
- Read all channels concurrently (`Task.WhenAll` / `asyncio.gather` / `ThreadPoolExecutor`) instead of sequentially
- Add byte-level progress reporting across all channels (replaces per-resource step progress)
- Enable HTTP/2 and streaming responses for the Python client
- Stream Python `_read_as_double` in chunks via `iter_bytes`/`aiter_bytes` instead of buffering the entire body
- Split versioned Nexus feature imports by API version (v1: `CatalogItem`, `ExportParameters`, `TaskStatus`; v2: `BatchStreamRequest`)

## v1.0.0-beta.19 - 2025-01-22

Revert changes regarding `from __future__ import annotations`

## v1.0.0-beta.18 - 2025-01-22

Fix encoding and decoding of `UUID` keys in dictionaries

## v1.0.0-beta.17 - 2024-10-30

Fixes for Nexus.

## v1.0.0-beta.16 - 2024-10-30

Fixes for Nexus.

## v1.0.0-beta.15 - 2024-10-30

Fixes for Nexus.

## v1.0.0-beta.14 - 2024-10-30

Fixes for Nexus.

## v1.0.0-beta.13 - 2024-10-29

Fixes for Nexus.

## v1.0.0-beta.12 - 2024-10-01

Fixes wrong python import.

## v1.0.0-beta.11 - 2024-10-01

Add versioned API support.

## v1.0.0-beta.10 - 2024-03-05

Adapt to Nexus token changes.

## v1.0.0-beta.9 - 2024-03-05

Adapt to Nexus token changes.

## v1.0.0-beta.8 - 2024-02-28

Fix pyright errors.

## v1.0.0-beta.7 - 2024-02-28

Fix pyright errors.

## v1.0.0-beta.6 - 2023-07-13

Fix pyright errors.

## v1.0.0-beta.5 - 2023-03-15

Add switch to enable/disable webassembly support.

## v1.0.0-beta.4 - 2023-03-15

Fix type name generation if there is more than one response.

## v1.0.0-beta.3 - 2023-03-15

Fix Python reserved keyword handling of keyword `class`.

## v1.0.0-beta.2 - 2023-03-15

Fix generated anonymous type names.

## v1.0.0-beta.1 - 2023-03-14

Initial release.
