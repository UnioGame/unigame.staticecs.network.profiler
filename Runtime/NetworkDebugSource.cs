namespace UniGame.StaticEcs.Network.Profiler
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>Aggregates bounded phase, session, snapshot, history, and schema diagnostics for one endpoint.</summary>
    public sealed class NetworkDebugSource : INetworkDiagnosticsObserver
    {
        private readonly object _gate = new object();
        private readonly NetworkDebugSchemaEntry[] _schema;
        private readonly RingBuffer<NetworkTraceEvent> _trace;
        private readonly RingBuffer<NetworkSessionDiagnostics> _sessions;
        private readonly RingBuffer<NetworkSnapshotDiagnostics> _snapshots;
        private readonly long[] _receivedBytesByKind;
        private readonly long[] _sentBytesByKind;
        private readonly long[] _receivedPacketsByKind;
        private readonly long[] _sentPacketsByKind;
        private long _revision;
        private long _receivedBytes;
        private long _sentBytes;
        private long _receivedPackets;
        private long _sentPackets;
        private long _errors;
        private bool _hasRole;
        private NetworkRole _role;
        private SchemaFingerprint _fingerprint;
        private uint _serverTick;
        private int _tickGap;
        private bool _traceEnabled;

        /// <summary>Creates an unregistered source and defensively copies its schema.</summary>
        public NetworkDebugSource(string sourceId, string displayName, IReadOnlyList<NetworkSchemaEntry> schema,
            int traceCapacity = 512, int historyCapacity = 128, string worldName = "")
        {
            if (string.IsNullOrWhiteSpace(sourceId)) throw new ArgumentException("A source id is required.", nameof(sourceId));
            if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("A display name is required.", nameof(displayName));
            if (schema == null) throw new ArgumentNullException(nameof(schema));
            if (traceCapacity < 1) throw new ArgumentOutOfRangeException(nameof(traceCapacity));
            if (historyCapacity < 1) throw new ArgumentOutOfRangeException(nameof(historyCapacity));

            SourceId = sourceId;
            DisplayName = displayName;
            WorldName = NormalizeWorldName(worldName);
            _schema = CopySchema(schema);
            _trace = new RingBuffer<NetworkTraceEvent>(traceCapacity);
            _sessions = new RingBuffer<NetworkSessionDiagnostics>(historyCapacity);
            _snapshots = new RingBuffer<NetworkSnapshotDiagnostics>(historyCapacity);
            var packetKindCount = Enum.GetValues(typeof(NetworkPacketKind)).Length;
            _receivedBytesByKind = new long[packetKindCount];
            _sentBytesByKind = new long[packetKindCount];
            _receivedPacketsByKind = new long[packetKindCount];
            _sentPacketsByKind = new long[packetKindCount];
        }

        /// <summary>Gets the stable registry identifier.</summary>
        public string SourceId { get; }

        /// <summary>Gets the human-readable source label.</summary>
        public string DisplayName { get; }

        /// <summary>Gets bounded caller-provided world display metadata.</summary>
        public string WorldName { get; }

        /// <summary>Gets or sets whether new phase events are retained in the bounded trace.</summary>
        public bool TraceEnabled
        {
            get { lock (_gate) return _traceEnabled; }
            set { lock (_gate) { if (_traceEnabled == value) return; _traceEnabled = value; _revision++; } }
        }

        /// <inheritdoc />
        public void Observe(in NetworkTraceEvent value)
        {
            lock (_gate)
            {
                Accumulate(in value);
                if (_traceEnabled) _trace.Add(value);
                _revision++;
            }
        }

        /// <inheritdoc />
        public void ObserveSession(in NetworkSessionDiagnostics value)
        {
            lock (_gate)
            {
                _sessions.Add(value);
                _hasRole = true;
                _role = value.Role;
                _serverTick = value.ServerTick;
                _revision++;
            }
        }

        /// <inheritdoc />
        public void ObserveSnapshot(in NetworkSnapshotDiagnostics value)
        {
            lock (_gate)
            {
                _snapshots.Add(value);
                _hasRole = true;
                _role = value.Role;
                _fingerprint = value.SchemaFingerprint;
                _serverTick = value.ServerTick;
                _revision++;
            }
        }

        /// <summary>Returns an immutable defensive snapshot of all retained diagnostics.</summary>
        public NetworkDebugData Capture()
        {
            lock (_gate)
            {
                return new NetworkDebugData(SourceId, DisplayName, WorldName, _revision,
                    (NetworkDebugSchemaEntry[])_schema.Clone(), _trace.Copy(), _sessions.Copy(), _snapshots.Copy(),
                    CopyTraffic(), _hasRole, _role, _fingerprint, _serverTick, _tickGap, _receivedBytes,
                    _sentBytes, _receivedPackets, _sentPackets, _errors);
            }
        }

        /// <summary>Clears only retained trace presentation; session and snapshot history are preserved.</summary>
        public void ClearTrace()
        {
            lock (_gate)
            {
                _trace.Clear();
                _revision++;
            }
        }

        /// <summary>Writes the current retained trace in the core strict payload-free NDJSON format.</summary>
        public void ExportTrace(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            NetworkTraceEvent[] values;
            lock (_gate) values = _trace.Copy();

            using var log = new NetworkNdjsonLog(stream, Math.Max(1, values.Length));
            for (var i = 0; i < values.Length; i++)
            {
                var value = values[i];
                log.Observe(in value);
            }
        }

        internal void Reset()
        {
            lock (_gate)
            {
                _trace.Clear();
                _sessions.Clear();
                _snapshots.Clear();
                Array.Clear(_receivedBytesByKind, 0, _receivedBytesByKind.Length);
                Array.Clear(_sentBytesByKind, 0, _sentBytesByKind.Length);
                Array.Clear(_receivedPacketsByKind, 0, _receivedPacketsByKind.Length);
                Array.Clear(_sentPacketsByKind, 0, _sentPacketsByKind.Length);
                _receivedBytes = _sentBytes = _receivedPackets = _sentPackets = _errors = 0;
                _hasRole = false;
                _role = default;
                _fingerprint = default;
                _serverTick = 0;
                _tickGap = 0;
                _revision++;
            }
        }

        private void Accumulate(in NetworkTraceEvent value)
        {
            _hasRole = true;
            _role = value.Role;
            _serverTick = value.ServerTick;
            _tickGap = value.ClientServerTickGap;
            if (value.SchemaFingerprint != SchemaFingerprint.Empty) _fingerprint = value.SchemaFingerprint;
            if (value.Kind == NetworkTraceKind.Begin) return;
            if (value.Result != NetworkResultCategory.None && value.Result != NetworkResultCategory.Success) _errors++;
            var kind = (int)value.PacketKind;
            if (kind < 0 || kind >= _receivedBytesByKind.Length) kind = (int)NetworkPacketKind.None;
            var bytes = Math.Max(0, value.Bytes);
            var packets = Math.Max(0, value.Packets);
            if (value.Phase == NetworkPhase.Receive)
            {
                _receivedBytes += bytes;
                _receivedPackets += packets;
                _receivedBytesByKind[kind] += bytes;
                _receivedPacketsByKind[kind] += packets;
            }
            else if (value.Phase == NetworkPhase.Send)
            {
                _sentBytes += bytes;
                _sentPackets += packets;
                _sentBytesByKind[kind] += bytes;
                _sentPacketsByKind[kind] += packets;
            }
        }

        private NetworkTrafficCounter[] CopyTraffic()
        {
            var count = _receivedBytesByKind.Length * 2;
            var result = new NetworkTrafficCounter[count];
            for (var i = 0; i < _receivedBytesByKind.Length; i++)
                result[i] = new NetworkTrafficCounter(NetworkTrafficDirection.Receive, (NetworkPacketKind)i,
                    _receivedBytesByKind[i], _receivedPacketsByKind[i]);
            for (var i = 0; i < _sentBytesByKind.Length; i++)
                result[_receivedBytesByKind.Length + i] = new NetworkTrafficCounter(NetworkTrafficDirection.Send,
                    (NetworkPacketKind)i, _sentBytesByKind[i], _sentPacketsByKind[i]);
            return result;
        }

        private static string NormalizeWorldName(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            value = value.Trim().Replace('\r', ' ').Replace('\n', ' ');
            return value.Length <= 128 ? value : value.Substring(0, 128);
        }

        private static NetworkDebugSchemaEntry[] CopySchema(IReadOnlyList<NetworkSchemaEntry> schema)
        {
            var result = new NetworkDebugSchemaEntry[schema.Count];
            for (var i = 0; i < schema.Count; i++)
            {
                var entry = schema[i] ?? throw new ArgumentException("Schema entries cannot be null.", nameof(schema));
                result[i] = new NetworkDebugSchemaEntry(entry.Kind, entry.TypeId.Value, entry.Version,
                    entry.MaxBytes, entry.MaxCount, entry.RuntimeType?.FullName);
            }

            Array.Sort(result, (left, right) =>
            {
                var kind = left.Kind.CompareTo(right.Kind);
                return kind != 0 ? kind : left.TypeId.CompareTo(right.TypeId);
            });
            return result;
        }

        private sealed class RingBuffer<T>
        {
            private readonly T[] _values;
            private int _start;
            private int _count;

            internal RingBuffer(int capacity) => _values = new T[capacity];

            internal void Add(T value)
            {
                if (_count < _values.Length)
                {
                    _values[(_start + _count) % _values.Length] = value;
                    _count++;
                    return;
                }

                _values[_start] = value;
                _start = (_start + 1) % _values.Length;
            }

            internal T[] Copy()
            {
                var result = new T[_count];
                for (var i = 0; i < _count; i++) result[i] = _values[(_start + i) % _values.Length];
                return result;
            }

            internal void Clear()
            {
                Array.Clear(_values, 0, _values.Length);
                _start = 0;
                _count = 0;
            }
        }
    }
}
