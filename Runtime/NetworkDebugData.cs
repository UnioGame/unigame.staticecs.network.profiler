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

    /// <summary>Contains one immutable defensive snapshot of a registered network diagnostics source.</summary>
    public sealed class NetworkDebugData
    {
        internal NetworkDebugData(string sourceId, string displayName, long revision,
            NetworkDebugSchemaEntry[] schema, NetworkTraceEvent[] trace,
            NetworkSessionDiagnostics[] sessions, NetworkSnapshotDiagnostics[] snapshots)
        {
            SourceId = sourceId;
            DisplayName = displayName;
            Revision = revision;
            Schema = new ReadOnlyCollection<NetworkDebugSchemaEntry>(schema);
            Trace = new ReadOnlyCollection<NetworkTraceEvent>(trace);
            Sessions = new ReadOnlyCollection<NetworkSessionDiagnostics>(sessions);
            Snapshots = new ReadOnlyCollection<NetworkSnapshotDiagnostics>(snapshots);
        }

        /// <summary>Gets the stable registry identifier.</summary>
        public string SourceId { get; }

        /// <summary>Gets the human-readable source label.</summary>
        public string DisplayName { get; }

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
    }
}
