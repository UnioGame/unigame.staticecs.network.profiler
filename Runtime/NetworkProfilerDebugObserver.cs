namespace UniGame.StaticEcs.Network.Profiler
{
    using System;

    /// <summary>Feeds one endpoint phase stream to Unity Profiler and the registered Network Debug source.</summary>
    public sealed class NetworkProfilerDebugObserver : INetworkDiagnosticsObserver
    {
        /// <summary>Creates the purpose-built profiler and debug observer pair.</summary>
        public NetworkProfilerDebugObserver(ProfilerObserver profiler, NetworkDebugSource debugSource)
        {
            Profiler = profiler ?? throw new ArgumentNullException(nameof(profiler));
            DebugSource = debugSource ?? throw new ArgumentNullException(nameof(debugSource));
        }

        /// <summary>Gets the Unity Profiler projection.</summary>
        public ProfilerObserver Profiler { get; }

        /// <summary>Gets the bounded Network Debug source.</summary>
        public NetworkDebugSource DebugSource { get; }

        /// <inheritdoc />
        public void Observe(in NetworkTraceEvent value)
        {
            Profiler.Observe(in value);
            DebugSource.Observe(in value);
        }

        /// <inheritdoc />
        public void ObserveSession(in NetworkSessionDiagnostics value) => DebugSource.ObserveSession(in value);

        /// <inheritdoc />
        public void ObserveSnapshot(in NetworkSnapshotDiagnostics value) => DebugSource.ObserveSnapshot(in value);
    }
}
