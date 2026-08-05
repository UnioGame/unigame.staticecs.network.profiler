# Static ECS Network Profiler

Privacy-safe Unity Profiler instrumentation and an Editor-only diagnostics window for Network v2.

## Capabilities

- Exposes `Receive`, `Decode`, `CommandDispatch`, `SnapshotApply`, `SnapshotCapture`, and `Send` markers in the Network category.
- Reports measured nanosecond phase duration as marker metadata and dedicated duration counters.
- Reports process-wide traffic, outcome deltas, connection state, snapshot, history, and tick-gap gauges.
- Publishes bounded immutable debug snapshots through `NetworkDebugRegistry` and `NetworkDebugSource`.
- Provides a dockable UI Toolkit window for overview, session, snapshot, command, traffic, schema, and trace inspection.
- Keeps payload bytes, command values, ECS handles, entity data, and user identifiers out of diagnostics.

## Usage

Use `ProfilerObserver` when only Unity Profiler markers and counters are needed:

```csharp
var observer = new ProfilerObserver(source: 1);
var client = new NetworkClient<GameWorld>(transport, schema, scope, observer);
var server = new NetworkServer<GameWorld>(schema, scopeSelector, observer: observer);
```

The observer implements `INetworkObserver` and remains caller-owned. Network endpoints do not dispose it.

Register a detailed endpoint diagnostics source and pass it as the endpoint observer:

```csharp
var lease = NetworkDebugRegistry.RegisterWithProfiler(
    "client-main",
    "Client Main",
    schema.Entries,
    out var diagnostics,
    worldName: typeof(GameWorld).Name);

var client = new NetworkClient<GameWorld>(transport, schema, scope, diagnostics);
```

Keep the returned lease with the endpoint lifetime and dispose it during shutdown. Open
`Tools > Static ECS > Network Debug` to inspect registered sources. Live pause stops display
refresh only; endpoint diagnostics continue. Trace collection can be disabled or cleared,
and retained trace rows can be exported as strict payload-free NDJSON.

The observer stream, clocks, and endpoint flow are documented in the cross-package
[network architecture guide](../../../docs/guides/static-ecs-network.md).

## Configuration

- Required: reference `unigame.staticecs.network.profiler` from the endpoint assembly.
- Required for Unity Profiler telemetry: pass a `ProfilerObserver` to each endpoint whose trace stream should be sampled.
- Required for the Debug Window: register a unique stable source id and dispose its lease during endpoint shutdown.
- Optional: configure per-source trace and history capacities; defaults are 512 trace rows and 128 session/snapshot rows. Trace retention is opt-in and starts disabled.
- Optional: supply a bounded world display name at registration; it is metadata only and never retains a world or ECS handle.
- Optional: assign a privacy-safe numeric profiler `source` lane. Do not derive it from peers, epochs, schema fingerprints, or payload data.
- Delta counters emit positive totals. Gauge counters retain the latest value and flush at the end of the Unity frame.
- `Cycle` is mock or replay transport ordering, while network diagnostics use `ServerTick`; neither Unity frame nor Static ECS tracking tick is presented as authoritative simulation time.

## Limitations

- The Debug Window is Editor-only and read-only; it cannot mutate ECS entities, sessions, or transports.
- Trace and history are process-local bounded diagnostics, not a persistent capture service.
- Detailed records intentionally omit payload bytes, command values, ECS handles, and Unity object references.
