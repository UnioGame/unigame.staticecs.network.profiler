# Static ECS Network Profiler

Optional Unity Profiler integration for the privacy-safe Network v2 diagnostics stream.

## Capabilities

- Exposes `Receive`, `Decode`, `CommandDispatch`, `SnapshotApply`, `SnapshotCapture`, and `Send` markers in the Network category.
- Reports the measured nanosecond duration of every phase as marker metadata and as a dedicated duration counter.
- Reports process-wide traffic and outcome deltas for bytes, packets, rejected commands, resynchronizations, protocol errors, and schema errors.
- Reports the latest observed connection, peer, command queue, snapshot, history, and client/server tick-gap gauges.
- Keeps payloads, entity data, schema contents, and user identifiers out of profiler metadata.

## Usage

```csharp
var observer = new ProfilerObserver(source: 1);
var client = new NetworkClient<GameWorld>(transport, schema, scope, observer);
var server = new NetworkServer<GameWorld>(schema, scopeSelector, observer: observer);
```

The observer implements `INetworkObserver` and remains caller-owned. Network endpoints do not dispose it.

## Configuration

- Required: reference `unigame.staticecs.network.profiler` from the endpoint assembly.
- Required: pass the observer to each endpoint whose trace stream should appear in the Unity Profiler.
- Optional: assign a privacy-safe numeric `source` to distinguish lanes. Do not derive it from peers, epochs, schema fingerprints, or payload data.
- Delta counters emit positive event totals. Gauge counters retain the latest event value and flush at the end of the Unity frame.
- `Cycle` is reserved for mock or replay transport ordering and is not a profiler clock. Network markers use `ServerTick`; no frame or Static ECS tracking tick is presented as authoritative simulation time.
