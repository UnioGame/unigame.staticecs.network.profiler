namespace UniGame.StaticEcs.Network.Profiler
{
    using Unity.Profiling;

    /// <summary>Projects privacy-safe network trace events into Unity Profiler markers and counters.</summary>
    public sealed class ProfilerObserver : INetworkObserver
    {
        private const string DurationMetadata = "DurationNs";
        private const string ServerTickMetadata = "ServerTick";
        private const string SourceMetadata = "Source";

        private static readonly ProfilerMarker<long, uint, uint> ReceiveMarker = Marker("SECS.Net.Receive");
        private static readonly ProfilerMarker<long, uint, uint> DecodeMarker = Marker("SECS.Net.Decode");
        private static readonly ProfilerMarker<long, uint, uint> CommandDispatchMarker = Marker("SECS.Net.CommandDispatch");
        private static readonly ProfilerMarker<long, uint, uint> SnapshotApplyMarker = Marker("SECS.Net.SnapshotApply");
        private static readonly ProfilerMarker<long, uint, uint> SnapshotCaptureMarker = Marker("SECS.Net.SnapshotCapture");
        private static readonly ProfilerMarker<long, uint, uint> SendMarker = Marker("SECS.Net.Send");

        private static readonly ProfilerCounter<long> ReceiveDurationCounter = Duration("Receive");
        private static readonly ProfilerCounter<long> DecodeDurationCounter = Duration("Decode");
        private static readonly ProfilerCounter<long> CommandDispatchDurationCounter = Duration("CommandDispatch");
        private static readonly ProfilerCounter<long> SnapshotApplyDurationCounter = Duration("SnapshotApply");
        private static readonly ProfilerCounter<long> SnapshotCaptureDurationCounter = Duration("SnapshotCapture");
        private static readonly ProfilerCounter<long> SendDurationCounter = Duration("Send");

        private static readonly ProfilerCounter<long> BytesInCounter = Counter<long>("SECS.Net.BytesIn", ProfilerMarkerDataUnit.Bytes);
        private static readonly ProfilerCounter<long> BytesOutCounter = Counter<long>("SECS.Net.BytesOut", ProfilerMarkerDataUnit.Bytes);
        private static readonly ProfilerCounter<int> PacketsInCounter = Counter<int>("SECS.Net.PacketsIn", ProfilerMarkerDataUnit.Count);
        private static readonly ProfilerCounter<int> PacketsOutCounter = Counter<int>("SECS.Net.PacketsOut", ProfilerMarkerDataUnit.Count);
        private static readonly ProfilerCounter<int> RejectedCommandsCounter = Counter<int>("SECS.Net.RejectedCommands", ProfilerMarkerDataUnit.Count);
        private static readonly ProfilerCounter<int> ResyncsCounter = Counter<int>("SECS.Net.Resyncs", ProfilerMarkerDataUnit.Count);
        private static readonly ProfilerCounter<int> ProtocolErrorsCounter = Counter<int>("SECS.Net.ProtocolErrors", ProfilerMarkerDataUnit.Count);
        private static readonly ProfilerCounter<int> SchemaErrorsCounter = Counter<int>("SECS.Net.SchemaErrors", ProfilerMarkerDataUnit.Count);

        private static ProfilerCounterValue<int> ActiveConnectionsCounter = Gauge<int>("SECS.Net.ActiveConnections", ProfilerMarkerDataUnit.Count);
        private static ProfilerCounterValue<int> ActivePeersCounter = Gauge<int>("SECS.Net.ActivePeers", ProfilerMarkerDataUnit.Count);
        private static ProfilerCounterValue<int> CommandQueueCounter = Gauge<int>("SECS.Net.CommandQueue", ProfilerMarkerDataUnit.Count);
        private static ProfilerCounterValue<int> SnapshotBytesCounter = Gauge<int>("SECS.Net.SnapshotBytes", ProfilerMarkerDataUnit.Bytes);
        private static ProfilerCounterValue<int> SnapshotEntitiesCounter = Gauge<int>("SECS.Net.SnapshotEntities", ProfilerMarkerDataUnit.Count);
        private static ProfilerCounterValue<int> SnapshotRecordsCounter = Gauge<int>("SECS.Net.SnapshotRecords", ProfilerMarkerDataUnit.Count);
        private static ProfilerCounterValue<int> HistoryTicksCounter = Gauge<int>("SECS.Net.HistoryTicks", ProfilerMarkerDataUnit.Count);
        private static ProfilerCounterValue<long> HistoryBytesCounter = Gauge<long>("SECS.Net.HistoryBytes", ProfilerMarkerDataUnit.Bytes);
        private static ProfilerCounterValue<int> ClientServerTickGapCounter = Gauge<int>("SECS.Net.ClientServerTickGap", ProfilerMarkerDataUnit.Count);

        /// <summary>Creates an observer for a caller-selected privacy-safe numeric source lane.</summary>
        public ProfilerObserver(uint source = 0)
        {
            Source = source;
        }

        /// <summary>Gets the caller-selected privacy-safe numeric source lane.</summary>
        public uint Source { get; }

        /// <summary>Projects one immutable network trace event into Unity Profiler telemetry.</summary>
        public void Observe(in NetworkTraceEvent value)
        {
            if (value.Kind == NetworkTraceKind.Begin)
            {
                Begin(in value);
                return;
            }

            if (value.Kind == NetworkTraceKind.End)
            {
                End(value.Phase);
            }
            else if (value.Kind == NetworkTraceKind.Point)
            {
                Begin(in value);
                End(value.Phase);
            }
            else
            {
                return;
            }

            Sample(in value);
        }

        private static ProfilerMarker<long, uint, uint> Marker(string name)
        {
            return new ProfilerMarker<long, uint, uint>(ProfilerCategory.Network, name,
                DurationMetadata, ServerTickMetadata, SourceMetadata);
        }

        private static ProfilerCounter<long> Duration(string phase)
        {
            return Counter<long>("SECS.Net." + phase + ".Duration", ProfilerMarkerDataUnit.TimeNanoseconds);
        }

        private static ProfilerCounter<T> Counter<T>(string name, ProfilerMarkerDataUnit unit) where T : unmanaged
        {
            return new ProfilerCounter<T>(ProfilerCategory.Network, name, unit);
        }

        private static ProfilerCounterValue<T> Gauge<T>(string name, ProfilerMarkerDataUnit unit) where T : unmanaged
        {
            return new ProfilerCounterValue<T>(ProfilerCategory.Network, name, unit);
        }

        private void Begin(in NetworkTraceEvent value)
        {
            switch (value.Phase)
            {
                case NetworkPhase.Receive: ReceiveMarker.Begin(value.DurationNanoseconds, value.ServerTick, Source); break;
                case NetworkPhase.Decode: DecodeMarker.Begin(value.DurationNanoseconds, value.ServerTick, Source); break;
                case NetworkPhase.CommandDispatch: CommandDispatchMarker.Begin(value.DurationNanoseconds, value.ServerTick, Source); break;
                case NetworkPhase.SnapshotApply: SnapshotApplyMarker.Begin(value.DurationNanoseconds, value.ServerTick, Source); break;
                case NetworkPhase.SnapshotCapture: SnapshotCaptureMarker.Begin(value.DurationNanoseconds, value.ServerTick, Source); break;
                case NetworkPhase.Send: SendMarker.Begin(value.DurationNanoseconds, value.ServerTick, Source); break;
            }
        }

        private static void End(NetworkPhase phase)
        {
            switch (phase)
            {
                case NetworkPhase.Receive: ReceiveMarker.End(); break;
                case NetworkPhase.Decode: DecodeMarker.End(); break;
                case NetworkPhase.CommandDispatch: CommandDispatchMarker.End(); break;
                case NetworkPhase.SnapshotApply: SnapshotApplyMarker.End(); break;
                case NetworkPhase.SnapshotCapture: SnapshotCaptureMarker.End(); break;
                case NetworkPhase.Send: SendMarker.End(); break;
            }
        }

        private static void Sample(in NetworkTraceEvent value)
        {
            SampleDuration(in value);
            SampleDeltas(in value);
            SampleGauges(in value);
        }

        private static void SampleDuration(in NetworkTraceEvent value)
        {
            if (value.DurationNanoseconds <= 0)
                return;

            switch (value.Phase)
            {
                case NetworkPhase.Receive: ReceiveDurationCounter.Sample(value.DurationNanoseconds); break;
                case NetworkPhase.Decode: DecodeDurationCounter.Sample(value.DurationNanoseconds); break;
                case NetworkPhase.CommandDispatch: CommandDispatchDurationCounter.Sample(value.DurationNanoseconds); break;
                case NetworkPhase.SnapshotApply: SnapshotApplyDurationCounter.Sample(value.DurationNanoseconds); break;
                case NetworkPhase.SnapshotCapture: SnapshotCaptureDurationCounter.Sample(value.DurationNanoseconds); break;
                case NetworkPhase.Send: SendDurationCounter.Sample(value.DurationNanoseconds); break;
            }
        }

        private static void SampleDeltas(in NetworkTraceEvent value)
        {
            if (value.Phase == NetworkPhase.Receive && value.Result == NetworkResultCategory.Success)
            {
                if (value.Bytes > 0) BytesInCounter.Sample(value.Bytes);
                if (value.Packets > 0) PacketsInCounter.Sample(value.Packets);
            }

            if (value.Phase == NetworkPhase.Send && value.Result == NetworkResultCategory.Success)
            {
                if (value.Bytes > 0) BytesOutCounter.Sample(value.Bytes);
                if (value.Packets > 0) PacketsOutCounter.Sample(value.Packets);
                if (value.PacketKind == NetworkPacketKind.ResyncRequest) ResyncsCounter.Sample(value.Packets > 0 ? value.Packets : 1);
            }

            if (value.Phase == NetworkPhase.CommandDispatch && value.RejectedCommands > 0)
                RejectedCommandsCounter.Sample(value.RejectedCommands);

            if (value.Result == NetworkResultCategory.Protocol ||
                value.Result == NetworkResultCategory.Malformed ||
                value.Result == NetworkResultCategory.Limits)
                ProtocolErrorsCounter.Sample(1);
            else if (value.Result == NetworkResultCategory.Schema)
                SchemaErrorsCounter.Sample(1);
        }

        private static void SampleGauges(in NetworkTraceEvent value)
        {
            ActiveConnectionsCounter.Value = value.ActiveConnections;
            ActivePeersCounter.Value = value.ActivePeers;
            CommandQueueCounter.Value = value.QueueSize;
            HistoryTicksCounter.Value = value.HistoryTicks;
            HistoryBytesCounter.Value = value.HistoryBytes;
            ClientServerTickGapCounter.Value = value.ClientServerTickGap;

            if ((value.Phase == NetworkPhase.SnapshotApply || value.Phase == NetworkPhase.SnapshotCapture) &&
                value.Result == NetworkResultCategory.Success)
            {
                SnapshotBytesCounter.Value = value.Bytes;
                SnapshotEntitiesCounter.Value = value.Entities;
                SnapshotRecordsCounter.Value = value.Records;
            }
        }
    }
}
