# THIS HAS BEEN ARCHIVED
I really wanted this one to pan out but had trouble really getting anything useful out.

I basically wanted cyberstat for CS1 and that just isn't the case here so I'm archiving this for now.


# Telex

Telex is a Cities: Skylines 1 instrumentation mod. It samples city state once per in-game day and publishes JSON envelopes over HTTP for later analysis.

The schema is built around CS1 mechanics: districts, park/industry areas, transfer reasons, service budgets, roads, buildings, and citizens. Sampling stays coarse because CS1 runs on an older Unity/Mono stack and mods should avoid expensive work on every simulation frame.

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
nix develop
make
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

## Dummy Receiver

Run the local Go receiver while Telex is posting:

```sh
go run ./cmd/synco
```

It listens on `127.0.0.1:2145` and writes each POST to:

```text
synco/{category}/{timestamp}.json
```

The category comes from the `type` query parameter or telemetry envelope. The filename timestamp comes from the in-game `date` field.

## Current Samples

Telex writes one JSON envelope per telemetry domain:

- `economy` - cash amount, tax rates, day/night service budgets, named income/expense maps, loan expenses, and policy expenses. Population and cash deltas are derived from daily `citizens` and `economy` snapshots.
- `resources` - transfer reason flow counters and natural resource totals.
- `buildings` - building positions, utility buffers, district/road references, and CS1-native industry building fields grouped as classification, materials, production, logistics, utilities, employment, costs, and problems.
- `citizens` - citizen census records with home/work building refs, home district, workplace name/zone, age group, education flags, health, wellbeing, and employment/student/tourist flags. Per-instance and vehicle refs are intentionally omitted.
- `roads` - road segment graph records with node references, curve points, traffic fields, and building access edges.
- `industry_areas` - CS1 Industries/Campus/Parklife-style area records from `DistrictManager.m_parks`, including synthetic GIS placement, area type/level, policies, worker/storage deltas, production amount, and per-resource production/consumption/import/export/buffer data.
- `districts` - district IDs, names, and policy-like fields when present.
- `transport` - public transport line names, stop counts, vehicle counts, and passenger counts.

The reflection samplers are deliberately shallow. They record primitive or enum fields and avoid walking object graphs.
