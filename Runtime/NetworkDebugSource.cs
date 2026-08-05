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
        private long _revision;
        private bool _traceEnabled = true;

        /// <summary>Creates an unregistered source and defensively copies its schema.</summary>
        public NetworkDebugSource(string sourceId, string displayName, IReadOnlyList<NetworkSchemaEntry> schema,
            int traceCapacity = 512, int historyCapacity = 128)
        {
            if (string.IsNullOrWhiteSpace(sourceId)) throw new ArgumentException("A source id is required.", nameof(sourceId));
            if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("A display name is required.", nameof(displayName));
            if (schema == null) throw new ArgumentNullException(nameof(schema));
            if (traceCapacity < 1) throw new ArgumentOutOfRangeException(nameof(traceCapacity));
            if (historyCapacity < 1) throw new ArgumentOutOfRangeException(nameof(historyCapacity));

            SourceId = sourceId;
            DisplayName = displayName;
            _schema = CopySchema(schema);
            _trace = new RingBuffer<NetworkTraceEvent>(traceCapacity);
            _sessions = new RingBuffer<NetworkSessionDiagnostics>(historyCapacity);
            _snapshots = new RingBuffer<NetworkSnapshotDiagnostics>(historyCapacity);
        }

        /// <summary>Gets the stable registry identifier.</summary>
        public string SourceId { get; }

        /// <summary>Gets the human-readable source label.</summary>
        public string DisplayName { get; }

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
                if (!_traceEnabled) return;
                _trace.Add(value);
                _revision++;
            }
        }

        /// <inheritdoc />
        public void ObserveSession(in NetworkSessionDiagnostics value)
        {
            lock (_gate)
            {
                _sessions.Add(value);
                _revision++;
            }
        }

        /// <inheritdoc />
        public void ObserveSnapshot(in NetworkSnapshotDiagnostics value)
        {
            lock (_gate)
            {
                _snapshots.Add(value);
                _revision++;
            }
        }

        /// <summary>Returns an immutable defensive snapshot of all retained diagnostics.</summary>
        public NetworkDebugData Capture()
        {
            lock (_gate)
            {
                return new NetworkDebugData(SourceId, DisplayName, _revision,
                    (NetworkDebugSchemaEntry[])_schema.Clone(), _trace.Copy(), _sessions.Copy(), _snapshots.Copy());
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
                _revision++;
            }
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
