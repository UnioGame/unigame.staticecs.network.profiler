namespace UniGame.StaticEcs.Network.Profiler
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    /// <summary>Contains a copied, payload-free schema row for diagnostics presentation.</summary>
    public readonly struct NetworkDebugSchemaEntry
    {
        /// <summary>Creates a copied schema row.</summary>
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

        /// <summary>Gets the wire shape.</summary>
        public NetworkSchemaKind Kind { get; }

        /// <summary>Gets the generated wire identifier.</summary>
        public uint TypeId { get; }

        /// <summary>Gets the serialization version.</summary>
        public byte Version { get; }

        /// <summary>Gets the maximum encoded byte count.</summary>
        public uint MaxBytes { get; }

        /// <summary>Gets the maximum collection count.</summary>
        public uint MaxCount { get; }

        /// <summary>Gets the copied runtime type name.</summary>
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
        /// <summary>Creates one cumulative traffic counter.</summary>
        public NetworkTrafficCounter(NetworkTrafficDirection direction, NetworkPacketKind packetKind,
            long bytes, long packets)
        {
            Direction = direction;
            PacketKind = packetKind;
            Bytes = bytes;
            Packets = packets;
        }

        /// <summary>Gets the traffic direction.</summary>
        public NetworkTrafficDirection Direction { get; }
        /// <summary>Gets the packet kind.</summary>
        public NetworkPacketKind PacketKind { get; }
        /// <summary>Gets cumulative bytes.</summary>
        public long Bytes { get; }
        /// <summary>Gets cumulative packets.</summary>
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
            long sentPackets, long errors)
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
        }

        /// <summary>Gets the stable registry identifier.</summary>
        public string SourceId { get; }

        /// <summary>Gets the human-readable source label.</summary>
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

        /// <summary>Gets whether an endpoint role has been observed.</summary>
        public bool HasRole { get; }

        /// <summary>Gets the latest observed endpoint role.</summary>
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
    }
}
