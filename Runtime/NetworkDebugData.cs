namespace UniGame.StaticEcs.Network.Profiler
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    /// <summary>Contains a copied, payload-free schema row for diagnostics presentation.</summary>
    public readonly struct NetworkDebugSchemaEntry
    {
        public NetworkDebugSchemaEntry(NetworkSchemaKind kind, uint typeId, byte version, uint maxBytes,
            uint maxCount, string typeName)
        {
            Kind = kind;
            TypeId = typeId;
            Version = version;
            MaxBytes = maxBytes;
            MaxCount = maxCount;
            TypeName = typeName ?? string.Empty;
        }

        public NetworkSchemaKind Kind { get; }

        public uint TypeId { get; }

        public byte Version { get; }

        public uint MaxBytes { get; }

        public uint MaxCount { get; }

        public string TypeName { get; }
    }

    /// <summary>Identifies one traffic counter direction.</summary>
    public enum NetworkTrafficDirection : byte
    {
        /// <summary>Traffic received by the endpoint.</summary>
        Receive,
        /// <summary>Traffic sent by the endpoint.</summary>
        Send
    }

    /// <summary>Contains deterministic cumulative traffic counters for one direction and packet kind.</summary>
    public readonly struct NetworkTrafficCounter
    {
        public NetworkTrafficCounter(NetworkTrafficDirection direction, NetworkPacketKind packetKind,
            long bytes, long packets)
        {
            Direction = direction;
            PacketKind = packetKind;
            Bytes = bytes;
            Packets = packets;
        }

        public NetworkTrafficDirection Direction { get; }
        public NetworkPacketKind PacketKind { get; }
        public long Bytes { get; }
        public long Packets { get; }
    }

    /// <summary>Contains one immutable defensive snapshot of a registered network diagnostics source.</summary>
    public sealed class NetworkDebugData
    {
        internal NetworkDebugData(string sourceId, string displayName, string worldName, long revision,
            NetworkDebugSchemaEntry[] schema, NetworkTraceEvent[] trace,
            NetworkSessionDiagnostics[] sessions, NetworkSnapshotDiagnostics[] snapshots,
            NetworkTrafficCounter[] traffic, bool hasRole, NetworkRole role, SchemaFingerprint fingerprint,
            uint serverTick, int tickGap, long receivedBytes, long sentBytes, long receivedPackets,
            long sentPackets, long errors, bool hasSimulator, NetworkSimulationConfig simulationConfig,
            NetworkSimulationStats simulationStats, NetworkSimulationDecision[] simulationDecisions,
            bool hasTransport, NetworkTransportDebugData transport)
        {
            SourceId = sourceId;
            DisplayName = displayName;
            WorldName = worldName;
            Revision = revision;
            Schema = new ReadOnlyCollection<NetworkDebugSchemaEntry>(schema);
            Trace = new ReadOnlyCollection<NetworkTraceEvent>(trace);
            Sessions = new ReadOnlyCollection<NetworkSessionDiagnostics>(sessions);
            Snapshots = new ReadOnlyCollection<NetworkSnapshotDiagnostics>(snapshots);
            Traffic = new ReadOnlyCollection<NetworkTrafficCounter>(traffic);
            HasRole = hasRole;
            Role = role;
            SchemaFingerprint = fingerprint;
            ServerTick = serverTick;
            ClientServerTickGap = tickGap;
            ReceivedBytes = receivedBytes;
            SentBytes = sentBytes;
            ReceivedPackets = receivedPackets;
            SentPackets = sentPackets;
            Errors = errors;
            HasSimulator = hasSimulator;
            SimulationConfig = simulationConfig;
            SimulationStats = simulationStats;
            SimulationDecisions = new ReadOnlyCollection<NetworkSimulationDecision>(simulationDecisions);
            HasTransport = hasTransport;
            Transport = transport;
        }

        public string SourceId { get; }

        public string DisplayName { get; }

        /// <summary>Gets bounded caller-provided world display metadata.</summary>
        public string WorldName { get; }

        /// <summary>Gets a monotonic source-local change revision.</summary>
        public long Revision { get; }

        /// <summary>Gets copied schema rows ordered by kind and wire identifier.</summary>
        public IReadOnlyList<NetworkDebugSchemaEntry> Schema { get; }

        /// <summary>Gets retained trace events in chronological order.</summary>
        public IReadOnlyList<NetworkTraceEvent> Trace { get; }

        /// <summary>Gets retained session samples in chronological order.</summary>
        public IReadOnlyList<NetworkSessionDiagnostics> Sessions { get; }

        /// <summary>Gets retained snapshot samples in chronological order.</summary>
        public IReadOnlyList<NetworkSnapshotDiagnostics> Snapshots { get; }

        /// <summary>Gets cumulative traffic rows ordered by direction then packet kind.</summary>
        public IReadOnlyList<NetworkTrafficCounter> Traffic { get; }

        public bool HasRole { get; }

        public NetworkRole Role { get; }

        /// <summary>Gets the latest observed schema fingerprint.</summary>
        public SchemaFingerprint SchemaFingerprint { get; }

        /// <summary>Gets the latest authoritative server tick.</summary>
        public uint ServerTick { get; }

        /// <summary>Gets the latest authoritative-to-client tick gap.</summary>
        public int ClientServerTickGap { get; }

        /// <summary>Gets cumulative received bytes.</summary>
        public long ReceivedBytes { get; }

        /// <summary>Gets cumulative sent bytes.</summary>
        public long SentBytes { get; }

        /// <summary>Gets cumulative received packets.</summary>
        public long ReceivedPackets { get; }

        /// <summary>Gets cumulative sent packets.</summary>
        public long SentPackets { get; }

        /// <summary>Gets cumulative non-success result rows.</summary>
        public long Errors { get; }

        /// <summary>Gets whether this source exposes a controllable mock network simulator.</summary>
        public bool HasSimulator { get; }

        /// <summary>Gets the copied active simulator configuration.</summary>
        public NetworkSimulationConfig SimulationConfig { get; }

        /// <summary>Gets the copied simulator counters and connection state.</summary>
        public NetworkSimulationStats SimulationStats { get; }

        /// <summary>Gets copied bounded payload-free simulator decisions.</summary>
        public IReadOnlyList<NetworkSimulationDecision> SimulationDecisions { get; }

        /// <summary>Gets whether a transport diagnostics provider returned an available snapshot.</summary>
        public bool HasTransport { get; }

        /// <summary>Gets the copied transport diagnostics snapshot, or an unavailable default value.</summary>
        public NetworkTransportDebugData Transport { get; }
    }
}
