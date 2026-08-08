# Telex

Telex is a Cities: Skylines 1 instrumentation mod scaffold. It samples city state once per in-game hour and publishes JSON envelopes over HTTP for later analysis.

The initial shape is based on the Cyberstat idea of keeping the instrumentation layer separate from export/storage. For CS1 this matters because the game runs on an older Unity/Mono stack and mods should avoid expensive work on every simulation frame.

## Layout

- `src/Telex/TelexMod.cs` - CS1 mod metadata.
- `src/Telex/TelexLoadingExtension.cs` - lifecycle setup/teardown.
- `src/Telex/TelexThreadingExtension.cs` - low-frequency sampling hook.
- `src/Telex/Instrumentation/*` - game data collection.
- `src/Telex/Serialization/*` - JSON-lines output.

## Build

The build expects the Cities: Skylines install at:

```sh
/home/khill/.local/share/Steam/steamapps/common/Cities_Skylines
```

Build with Mono:

```sh
make
```

If you use Nix:

```sh
nix-shell --run make
```

The DLL is written to `build/Telex.dll`.

## Install Locally

```sh
make install
```

That copies the DLL into:

```sh
~/.local/share/Colossal Order/Cities_Skylines/Addons/Mods/Telex/Telex.dll
```

Enable `Telex` in Content Manager, start a local receiver, then load a city.

```sh
TELEX_HTTP_URL=http://127.0.0.1:2145/ingest
```

Telex posts each envelope to:

```text
{TELEX_HTTP_URL}?program=telex&type={record_type}
```

Runtime settings:

- `TELEX_HTTP_URL` - endpoint URL. Defaults to `http://127.0.0.1:2145/ingest`.
- `TELEX_HTTP_TIMEOUT_MS` - request timeout. Defaults to `5000`.
- `TELEX_HTTP_MAX_QUEUE` - maximum queued records before oldest records are dropped. Defaults to `256`.
- `TELEX_HTTP_INSECURE_TLS` - set to `true` to accept self-signed HTTPS certificates.

The older file sink still exists as `JsonLinesTelemetrySink`, but `HttpTelemetrySink` is wired as the default.

## Current Samples

Telex writes one JSON envelope per domain, similar to Cyberstat's `type`-based publishing model:

- `economy` - cash and delta fields from `EconomyManager`, plus population.
- `resources` - material-like `TransferManager.TransferReason` rows with incoming/outgoing offer amounts and counts, natural resource totals, and reflected industry-area manager state.
- `buildings` - one record per created building with prefab, AI type, position, district, core utility buffers, import/export buffers, production rate, cargo traffic, and reflected industry/campus/warehouse AI fields.
- `districts` - district IDs, names, and policy-like fields when present.
- `transport` - line names, stop counts, vehicle counts, and passenger counts.
- `dlc_managers` - optional DLC-style manager snapshots discovered by reflection, including likely park/industry/campus manager objects when present.

The reflection samplers are deliberately shallow. They record primitive or enum fields and avoid walking object graphs.
