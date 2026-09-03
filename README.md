# Static ECS Network Profiler

Privacy-safe Unity Profiler instrumentation and bounded runtime/Editor diagnostics for
Network protocol v7.

## Capabilities

- Emits receive, decode, command dispatch, snapshot apply/capture, send, and correlated resync markers and counters.
- Publishes bounded payload-free debug snapshots through `NetworkDebugRegistry`.
- Provides shared formatter contracts and a dockable Editor diagnostics window.
- Exposes optional simulator controls without allowing diagnostics to mutate ECS or session internals.
- Omits payload bytes, command values, ECS handles, entity data, and user identifiers.

## Usage

```csharp
using var registration = NetworkDebugRegistry.RegisterWithProfiler(
    "client-main", "Client Main", schema.Entries, out var observer,
    worldName: typeof(ClientWorld).Name);

var client = new NetworkClient<ClientWorld>(
    transport, schema, scope, observer);
```

Keep the registration with endpoint lifetime and dispose it during shutdown. Open
`Game > Static ECS > Network Debug` to inspect registered sources.

## Configuration

- Required: reference `unigame.staticecs.network.profiler` from the endpoint assembly.
- Required for telemetry: pass the returned observer to the endpoint.
- Optional bounded capacities default to 512 trace rows and 128 history rows.
- Optional simulator control remains caller-owned.
- Trace retention is opt-in; file export remains Editor-only.
- See [network architecture](../../../docs/guides/network-static-ecs.md) for package composition.
