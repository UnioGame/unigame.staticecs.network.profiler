namespace UniGame.StaticEcs.Network.Profiler
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>Owns the process-wide deterministic registry of live network diagnostics sources.</summary>
    public static class NetworkDebugRegistry
    {
        private static readonly object Gate = new object();
        private static readonly SortedDictionary<string, NetworkDebugSource> Registered =
            new SortedDictionary<string, NetworkDebugSource>(StringComparer.Ordinal);

        /// <summary>Creates and registers a source, copying schema rows before publication.</summary>
        public static IDisposable Register(string sourceId, string displayName, IReadOnlyList<NetworkSchemaEntry> schema,
            out NetworkDebugSource source, int traceCapacity = 512, int historyCapacity = 128, string worldName = "")
        {
            source = new NetworkDebugSource(sourceId, displayName, schema, traceCapacity, historyCapacity, worldName);
            return Register(source);
        }

        /// <summary>Registers a debug source and creates one observer that also emits Unity Profiler telemetry.</summary>
        public static IDisposable RegisterWithProfiler(string sourceId, string displayName,
            IReadOnlyList<NetworkSchemaEntry> schema, out NetworkProfilerDebugObserver observer,
            uint profilerSource = 0, int traceCapacity = 512, int historyCapacity = 128, string worldName = "")
        {
            var lease = Register(sourceId, displayName, schema, out var debugSource, traceCapacity,
                historyCapacity, worldName);
            observer = new NetworkProfilerDebugObserver(new ProfilerObserver(profilerSource), debugSource);
            return lease;
        }

        /// <summary>Registers a caller-owned source and returns an idempotent removal lease.</summary>
        public static IDisposable Register(NetworkDebugSource source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            lock (Gate)
            {
                if (Registered.ContainsKey(source.SourceId))
                    throw new InvalidOperationException($"Network debug source id `{source.SourceId}` is already registered.");
                Registered.Add(source.SourceId, source);
            }
            return new Lease(source);
        }

        /// <summary>Returns a defensive source array ordered by ordinal source id.</summary>
        public static IReadOnlyList<NetworkDebugSource> Sources()
        {
            lock (Gate)
            {
                var result = new NetworkDebugSource[Registered.Count];
                Registered.Values.CopyTo(result, 0);
                return Array.AsReadOnly(result);
            }
        }

        /// <summary>Removes every source and clears retained diagnostic state.</summary>
        public static void Reset()
        {
            lock (Gate)
            {
                foreach (var source in Registered.Values) source.Reset();
                Registered.Clear();
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSubsystem() => Reset();

        private sealed class Lease : IDisposable
        {
            private NetworkDebugSource _source;

            internal Lease(NetworkDebugSource source) => _source = source;

            public void Dispose()
            {
                var source = _source;
                if (source == null) return;
                _source = null;
                lock (Gate)
                {
                    if (Registered.TryGetValue(source.SourceId, out var current) && ReferenceEquals(current, source))
                        Registered.Remove(source.SourceId);
                }
            }
        }
    }
}
