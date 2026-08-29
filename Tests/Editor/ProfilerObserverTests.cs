namespace UniGame.StaticEcs.Network.Profiler.Tests
{
    using System;
    using System.Collections;
    using NUnit.Framework;
    using Unity.Profiling;
    using UnityEngine.TestTools;

    public sealed class ProfilerObserverTests
    {
        private static readonly string[] MarkerNames =
        {
            "SECS.Net.Receive",
            "SECS.Net.Decode",
            "SECS.Net.CommandDispatch",
            "SECS.Net.SnapshotApply",
            "SECS.Net.SnapshotCapture",
            "SECS.Net.Send",
            "SECS.Net.ServerTick"
        };

        private static readonly string[] CounterNames =
        {
            "SECS.Net.Receive.Duration",
            "SECS.Net.Decode.Duration",
            "SECS.Net.CommandDispatch.Duration",
            "SECS.Net.SnapshotApply.Duration",
            "SECS.Net.SnapshotCapture.Duration",
            "SECS.Net.Send.Duration",
            "SECS.Net.ServerTick.Duration",
            "SECS.Net.BytesIn",
            "SECS.Net.BytesOut",
            "SECS.Net.PacketsIn",
            "SECS.Net.PacketsOut",
            "SECS.Net.RejectedCommands",
            "SECS.Net.Resyncs",
            "SECS.Net.ProtocolErrors",
            "SECS.Net.SchemaErrors",
            "SECS.Net.ActiveConnections",
            "SECS.Net.ActivePeers",
            "SECS.Net.CommandQueue",
            "SECS.Net.SnapshotBytes",
            "SECS.Net.SnapshotEntities",
            "SECS.Net.SnapshotRecords",
            "SECS.Net.HistoryTicks",
            "SECS.Net.HistoryBytes",
            "SECS.Net.ClientServerTickGap",
            "SECS.Net.Server.QueuedPackets",
            "SECS.Net.Server.OutstandingLeases",
            "SECS.Net.Client.QueuedPackets",
            "SECS.Net.Client.OutstandingLeases"
        };

        [Test]
        public void RegistersNetworkV2MarkersAndCounters()
        {
            var observer = new ProfilerObserver(17);

            Assert.That(observer.Source, Is.EqualTo(17));
            AssertRegistered(MarkerNames);
            AssertRegistered(CounterNames);
        }

        [UnityTest]
        public IEnumerator ProjectsEveryNetworkPhaseIntoOneMarkerAndDurationSample()
        {
            var observer = new ProfilerObserver(7);
            var markers = StartAll(MarkerNames);
            var durations = StartAll(new[]
            {
                "SECS.Net.Receive.Duration",
                "SECS.Net.Decode.Duration",
                "SECS.Net.CommandDispatch.Duration",
                "SECS.Net.SnapshotApply.Duration",
                "SECS.Net.SnapshotCapture.Duration",
                "SECS.Net.Send.Duration",
                "SECS.Net.ServerTick.Duration"
            });

            try
            {
                var phases = new[]
                {
                    NetworkPhase.Receive,
                    NetworkPhase.Decode,
                    NetworkPhase.CommandDispatch,
                    NetworkPhase.SnapshotApply,
                    NetworkPhase.SnapshotCapture,
                    NetworkPhase.Send,
                    NetworkPhase.ServerTick
                };

                for (var i = 0; i < phases.Length; i++)
                {
                    var value = Trace(phases[i], durationNanoseconds: i + 1);
                    observer.Observe(in value);
                }

                yield return null;

                for (var i = 0; i < phases.Length; i++)
                {
                    Assert.That(CompletedSamples(markers[i]), Is.EqualTo(1), MarkerNames[i]);
                    Assert.That(durations[i].LastValue, Is.EqualTo(i + 1), durations[i].ToString());
                }
            }
            finally
            {
                DisposeAll(markers);
                DisposeAll(durations);
            }
        }

        [UnityTest]
        public IEnumerator ProjectsTrafficOutcomesAndLatestGauges()
        {
            var observer = new ProfilerObserver();
            using var counters = new CounterRecorders();

            var received = Trace(
                NetworkPhase.Receive,
                bytes: 120,
                packets: 2,
                activeConnections: 5,
                activePeers: 4);
            observer.Observe(in received);

            var rejected = Trace(
                NetworkPhase.CommandDispatch,
                NetworkResultCategory.Policy,
                commands: 4,
                queueSize: 3,
                activeConnections: 5,
                activePeers: 4,
                rejectedCommands: 2);
            observer.Observe(in rejected);

            var snapshot = Trace(
                NetworkPhase.SnapshotCapture,
                bytes: 700,
                entities: 8,
                records: 13,
                queueSize: 2,
                historyTicks: 6,
                historyBytes: 900,
                activeConnections: 5,
                activePeers: 4,
                clientServerTickGap: 3);
            observer.Observe(in snapshot);

            var protocolError = Trace(
                NetworkPhase.Decode,
                NetworkResultCategory.Protocol,
                activeConnections: 5,
                activePeers: 4);
            observer.Observe(in protocolError);

            var schemaError = Trace(
                NetworkPhase.Decode,
                NetworkResultCategory.Schema,
                activeConnections: 5,
                activePeers: 4);
            observer.Observe(in schemaError);

            var decoded = Trace(
                NetworkPhase.Decode,
                clientServerTickGap: 2,
                role: NetworkRole.Client);
            observer.Observe(in decoded);

            var sent = Trace(
                NetworkPhase.Send,
                bytes: 80,
                packets: 1,
                packetKind: NetworkPacketKind.ResyncRequest,
                queueSize: 1,
                historyTicks: 7,
                historyBytes: 1000,
                activeConnections: 4,
                activePeers: 3,
                clientServerTickGap: 99);
            observer.Observe(in sent);

            yield return null;

            Assert.That(counters.BytesIn.LastValue, Is.EqualTo(120));
            Assert.That(counters.BytesOut.LastValue, Is.EqualTo(80));
            Assert.That(counters.PacketsIn.LastValue, Is.EqualTo(2));
            Assert.That(counters.PacketsOut.LastValue, Is.EqualTo(1));
            Assert.That(counters.RejectedCommands.LastValue, Is.EqualTo(2));
            Assert.That(counters.Resyncs.LastValue, Is.EqualTo(1));
            Assert.That(counters.ProtocolErrors.LastValue, Is.EqualTo(1));
            Assert.That(counters.SchemaErrors.LastValue, Is.EqualTo(1));
            Assert.That(counters.ActiveConnections.LastValue, Is.EqualTo(4));
            Assert.That(counters.ActivePeers.LastValue, Is.EqualTo(3));
            Assert.That(counters.CommandQueue.LastValue, Is.EqualTo(3));
            Assert.That(counters.SnapshotBytes.LastValue, Is.EqualTo(700));
            Assert.That(counters.SnapshotEntities.LastValue, Is.EqualTo(8));
            Assert.That(counters.SnapshotRecords.LastValue, Is.EqualTo(13));
            Assert.That(counters.HistoryTicks.LastValue, Is.EqualTo(6));
            Assert.That(counters.HistoryBytes.LastValue, Is.EqualTo(900));
            Assert.That(counters.ClientServerTickGap.LastValue, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator GaugePlaceholdersDoNotOverwriteAuthoritativeValues()
        {
            var observer = new ProfilerObserver();
            using var counters = new CounterRecorders();

            var dispatch = Trace(
                NetworkPhase.CommandDispatch,
                queueSize: 7,
                activeConnections: 2,
                activePeers: 2);
            observer.Observe(in dispatch);

            var capture = Trace(
                NetworkPhase.SnapshotCapture,
                bytes: 300,
                entities: 3,
                records: 5,
                historyTicks: 3,
                historyBytes: 300,
                activeConnections: 2,
                activePeers: 2);
            observer.Observe(in capture);

            var apply = Trace(
                NetworkPhase.SnapshotApply,
                bytes: 500,
                entities: 4,
                records: 8,
                historyTicks: 5,
                historyBytes: 500,
                clientServerTickGap: 4,
                role: NetworkRole.Client);
            observer.Observe(in apply);

            var sendPlaceholder = Trace(NetworkPhase.Send, role: NetworkRole.Client);
            observer.Observe(in sendPlaceholder);
            var receivePlaceholder = Trace(NetworkPhase.Receive, role: NetworkRole.Client);
            observer.Observe(in receivePlaceholder);

            yield return null;

            Assert.That(counters.ActiveConnections.LastValue, Is.EqualTo(2));
            Assert.That(counters.ActivePeers.LastValue, Is.EqualTo(2));
            Assert.That(counters.CommandQueue.LastValue, Is.EqualTo(7));
            Assert.That(counters.SnapshotBytes.LastValue, Is.EqualTo(500));
            Assert.That(counters.SnapshotEntities.LastValue, Is.EqualTo(4));
            Assert.That(counters.SnapshotRecords.LastValue, Is.EqualTo(8));
            Assert.That(counters.HistoryTicks.LastValue, Is.EqualTo(5));
            Assert.That(counters.HistoryBytes.LastValue, Is.EqualTo(500));
            Assert.That(counters.ClientServerTickGap.LastValue, Is.EqualTo(4));

            var emptyDispatch = Trace(NetworkPhase.CommandDispatch);
            observer.Observe(in emptyDispatch);
            var emptyApply = Trace(NetworkPhase.SnapshotApply, role: NetworkRole.Client);
            observer.Observe(in emptyApply);
            var noConnections = Trace(NetworkPhase.Decode);
            observer.Observe(in noConnections);

            yield return null;

            Assert.That(counters.ActiveConnections.LastValue, Is.EqualTo(0));
            Assert.That(counters.ActivePeers.LastValue, Is.EqualTo(0));
            Assert.That(counters.CommandQueue.LastValue, Is.EqualTo(0));
            Assert.That(counters.SnapshotBytes.LastValue, Is.EqualTo(0));
            Assert.That(counters.SnapshotEntities.LastValue, Is.EqualTo(0));
            Assert.That(counters.SnapshotRecords.LastValue, Is.EqualTo(0));
            Assert.That(counters.HistoryTicks.LastValue, Is.EqualTo(0));
            Assert.That(counters.HistoryBytes.LastValue, Is.EqualTo(0));
            Assert.That(counters.ClientServerTickGap.LastValue, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator SupportsExplicitBeginAndEndTraceBoundaries()
        {
            var observer = new ProfilerObserver(9);
            using var marker = Start("SECS.Net.Send");
            using var packets = Start("SECS.Net.PacketsOut");

            var begin = Trace(NetworkPhase.Send, NetworkResultCategory.None, NetworkTraceKind.Begin);
            var end = Trace(NetworkPhase.Send, kind: NetworkTraceKind.End, packets: 1);
            observer.Observe(in begin);
            observer.Observe(in end);

            yield return null;

            Assert.That(CompletedSamples(marker), Is.EqualTo(1));
            Assert.That(packets.LastValue, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ProjectsTransportQueueAndLeaseGauges()
        {
            using var serverQueue = Start("SECS.Net.Server.QueuedPackets");
            using var serverLeases = Start("SECS.Net.Server.OutstandingLeases");
            using var clientQueue = Start("SECS.Net.Client.QueuedPackets");
            using var clientLeases = Start("SECS.Net.Client.OutstandingLeases");

            ProfilerObserver.SampleTransport(NetworkRole.Server, 7, 3);
            ProfilerObserver.SampleTransport(NetworkRole.Client, 5, 2);
            yield return null;

            Assert.That(serverQueue.LastValue, Is.EqualTo(7));
            Assert.That(serverLeases.LastValue, Is.EqualTo(3));
            Assert.That(clientQueue.LastValue, Is.EqualTo(5));
            Assert.That(clientLeases.LastValue, Is.EqualTo(2));
        }

        [Test]
        public void WarmProjectionDoesNotAllocateManagedMemory()
        {
            var observer = new ProfilerObserver(3);
            var tick = Trace(NetworkPhase.ServerTick, durationNanoseconds: 100);
            observer.Observe(in tick);
            ProfilerObserver.SampleTransport(NetworkRole.Server, 1, 2);
            ProfilerObserver.SampleTransport(NetworkRole.Client, 3, 4);

            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 100; i++)
            {
                observer.Observe(in tick);
                ProfilerObserver.SampleTransport(NetworkRole.Server, i, i + 1);
                ProfilerObserver.SampleTransport(NetworkRole.Client, i + 2, i + 3);
            }

            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
        }

        private static NetworkTraceEvent Trace(
            NetworkPhase phase,
            NetworkResultCategory result = NetworkResultCategory.Success,
            NetworkTraceKind kind = NetworkTraceKind.Point,
            int bytes = 0,
            int packets = 0,
            int entities = 0,
            int records = 0,
            int commands = 0,
            int queueSize = 0,
            int historyTicks = 0,
            long historyBytes = 0,
            int activeConnections = 0,
            int activePeers = 0,
            int clientServerTickGap = 0,
            long durationNanoseconds = 0,
            NetworkPacketKind packetKind = NetworkPacketKind.None,
            int rejectedCommands = 0,
            NetworkRole role = NetworkRole.Server)
        {
            return new NetworkTraceEvent(
                phase,
                kind,
                result,
                role,
                1,
                2,
                3,
                4,
                5,
                bytes,
                packets,
                entities,
                records,
                commands,
                queueSize,
                historyTicks,
                activeConnections,
                activePeers,
                6,
                packetKind,
                historyBytes,
                clientServerTickGap,
                durationNanoseconds,
                rejectedCommands: rejectedCommands);
        }

        private static void AssertRegistered(string[] names)
        {
            var recorders = StartAll(names);
            try
            {
                for (var i = 0; i < names.Length; i++)
                    Assert.That(recorders[i].Valid, Is.True, names[i]);
            }
            finally
            {
                DisposeAll(recorders);
            }
        }

        private static ProfilerRecorder[] StartAll(string[] names)
        {
            var recorders = new ProfilerRecorder[names.Length];
            for (var i = 0; i < names.Length; i++)
                recorders[i] = Start(names[i]);
            return recorders;
        }

        private static void DisposeAll(ProfilerRecorder[] recorders)
        {
            for (var i = 0; i < recorders.Length; i++)
            {
                if (recorders[i].Valid)
                    recorders[i].Dispose();
            }
        }

        private static ProfilerRecorder Start(string name)
        {
            return ProfilerRecorder.StartNew(
                ProfilerCategory.Network,
                name,
                64,
                ProfilerRecorderOptions.StartImmediately |
                ProfilerRecorderOptions.WrapAroundWhenCapacityReached |
                ProfilerRecorderOptions.SumAllSamplesInFrame);
        }

        private static long CompletedSamples(ProfilerRecorder recorder)
        {
            long count = 0;
            for (var i = 0; i < recorder.Count; i++)
                count += recorder.GetSample(i).Count;
            return count;
        }

        private sealed class CounterRecorders : IDisposable
        {
            internal CounterRecorders()
            {
                BytesIn = Start("SECS.Net.BytesIn");
                BytesOut = Start("SECS.Net.BytesOut");
                PacketsIn = Start("SECS.Net.PacketsIn");
                PacketsOut = Start("SECS.Net.PacketsOut");
                RejectedCommands = Start("SECS.Net.RejectedCommands");
                Resyncs = Start("SECS.Net.Resyncs");
                ProtocolErrors = Start("SECS.Net.ProtocolErrors");
                SchemaErrors = Start("SECS.Net.SchemaErrors");
                ActiveConnections = Start("SECS.Net.ActiveConnections");
                ActivePeers = Start("SECS.Net.ActivePeers");
                CommandQueue = Start("SECS.Net.CommandQueue");
                SnapshotBytes = Start("SECS.Net.SnapshotBytes");
                SnapshotEntities = Start("SECS.Net.SnapshotEntities");
                SnapshotRecords = Start("SECS.Net.SnapshotRecords");
                HistoryTicks = Start("SECS.Net.HistoryTicks");
                HistoryBytes = Start("SECS.Net.HistoryBytes");
                ClientServerTickGap = Start("SECS.Net.ClientServerTickGap");
            }

            internal ProfilerRecorder BytesIn;
            internal ProfilerRecorder BytesOut;
            internal ProfilerRecorder PacketsIn;
            internal ProfilerRecorder PacketsOut;
            internal ProfilerRecorder RejectedCommands;
            internal ProfilerRecorder Resyncs;
            internal ProfilerRecorder ProtocolErrors;
            internal ProfilerRecorder SchemaErrors;
            internal ProfilerRecorder ActiveConnections;
            internal ProfilerRecorder ActivePeers;
            internal ProfilerRecorder CommandQueue;
            internal ProfilerRecorder SnapshotBytes;
            internal ProfilerRecorder SnapshotEntities;
            internal ProfilerRecorder SnapshotRecords;
            internal ProfilerRecorder HistoryTicks;
            internal ProfilerRecorder HistoryBytes;
            internal ProfilerRecorder ClientServerTickGap;

            public void Dispose()
            {
                BytesIn.Dispose();
                BytesOut.Dispose();
                PacketsIn.Dispose();
                PacketsOut.Dispose();
                RejectedCommands.Dispose();
                Resyncs.Dispose();
                ProtocolErrors.Dispose();
                SchemaErrors.Dispose();
                ActiveConnections.Dispose();
                ActivePeers.Dispose();
                CommandQueue.Dispose();
                SnapshotBytes.Dispose();
                SnapshotEntities.Dispose();
                SnapshotRecords.Dispose();
                HistoryTicks.Dispose();
                HistoryBytes.Dispose();
                ClientServerTickGap.Dispose();
            }
        }
    }
}
