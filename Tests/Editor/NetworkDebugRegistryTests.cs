namespace UniGame.StaticEcs.Network.Profiler.Tests
{
    using System;
    using System.Collections;
    using System.IO;
    using System.Text;
    using FFS.Libraries.StaticEcs;
    using NUnit.Framework;

    /// <summary>Verifies bounded, lifetime-safe network diagnostics publication.</summary>
    public sealed class NetworkDebugRegistryTests
    {
        /// <summary>Clears the process-wide registry before each test.</summary>
        [SetUp]
        public void SetUp() => NetworkDebugRegistry.Reset();

        /// <summary>Clears the process-wide registry after each test.</summary>
        [TearDown]
        public void TearDown() => NetworkDebugRegistry.Reset();

        /// <summary>Verifies deterministic source order, unique ids, and idempotent leases.</summary>
        [Test]
        public void RegistrationIsUniqueOrderedAndIdempotent()
        {
            var schema = Array.Empty<NetworkSchemaEntry>();
            var secondLease = NetworkDebugRegistry.Register("server", "Server", schema, out var second);
            var firstLease = NetworkDebugRegistry.Register("client", "Client", schema, out var first);

            var sources = NetworkDebugRegistry.Sources();
            Assert.That(sources.Count, Is.EqualTo(2));
            Assert.That(sources[0], Is.SameAs(first));
            Assert.That(sources[1], Is.SameAs(second));
            Assert.Throws<InvalidOperationException>(() => NetworkDebugRegistry.Register(first));

            firstLease.Dispose();
            firstLease.Dispose();
            Assert.That(NetworkDebugRegistry.Sources(), Has.Count.EqualTo(1));
            secondLease.Dispose();
        }

        /// <summary>Verifies schema copying, overwrite-oldest bounds, and defensive snapshots.</summary>
        [Test]
        public void CaptureIsBoundedAndDefensive()
        {
            var factory = NetworkCompilerSupport.Create<TestWorld>();
            factory.Entity<TestEntity>(new NetworkTypeId(42));
            var schema = factory.Freeze();
            using var lease = NetworkDebugRegistry.Register("source", "Source", schema.Entries, out var source,
                traceCapacity: 2, historyCapacity: 2);

            for (var i = 1; i <= 3; i++)
            {
                var trace = Trace((uint)i);
                source.Observe(in trace);
                var session = Session((uint)i);
                source.ObserveSession(in session);
                var snapshot = Snapshot((uint)i);
                source.ObserveSnapshot(in snapshot);
            }

            var first = source.Capture();
            Assert.That(first.Schema, Has.Count.EqualTo(1));
            Assert.That(first.Schema[0].TypeId, Is.EqualTo(42));
            Assert.That(first.Schema[0].TypeName, Does.EndWith(nameof(TestEntity)));
            Assert.That(first.Trace, Has.Count.EqualTo(2));
            Assert.That(first.Trace[0].ServerTick, Is.EqualTo(2));
            Assert.That(first.Sessions, Has.Count.EqualTo(2));
            Assert.That(first.Snapshots, Has.Count.EqualTo(2));
            Assert.Throws<NotSupportedException>(() => ((IList)first.Trace)[0] = default(NetworkTraceEvent));

            source.ClearTrace();
            var second = source.Capture();
            Assert.That(second.Trace, Is.Empty);
            Assert.That(first.Trace, Has.Count.EqualTo(2), "An earlier snapshot must remain unchanged.");
            Assert.That(second.Sessions, Has.Count.EqualTo(2));
        }

        /// <summary>Verifies trace pause affects collection only and export remains strict payload-free NDJSON.</summary>
        [Test]
        public void TracePauseAndExportArePresentationOnlyAndPayloadFree()
        {
            using var lease = NetworkDebugRegistry.Register("source", "Source", Array.Empty<NetworkSchemaEntry>(),
                out var source, traceCapacity: 4);
            source.TraceEnabled = false;
            var ignored = Trace(1);
            source.Observe(in ignored);
            var session = Session(1);
            source.ObserveSession(in session);
            Assert.That(source.Capture().Trace, Is.Empty);
            Assert.That(source.Capture().Sessions, Has.Count.EqualTo(1));

            source.TraceEnabled = true;
            var retained = Trace(2);
            source.Observe(in retained);
            using var stream = new MemoryStream();
            source.ExportTrace(stream);
            stream.Position = 0;
            using var reader = new StreamReader(stream, Encoding.UTF8, false, 1024, true);
            var line = reader.ReadLine();
            Assert.That(line, Does.StartWith("{\"phase\":"));
            Assert.That(line, Does.Contain("\"server_tick\":2"));
            Assert.That(line, Does.Not.Contain("payload"));
            Assert.That(line, Does.Not.Contain("command_value"));
            Assert.That(reader.ReadLine(), Is.Null);
        }

        private static NetworkTraceEvent Trace(uint tick) => new NetworkTraceEvent(
            NetworkPhase.Receive, NetworkTraceKind.Point, NetworkResultCategory.Success, NetworkRole.Client,
            1, 2, 3, tick, 0, 10, 1, 0, 0, 0, 0, 0, 1, 1, tick);

        private static NetworkSessionDiagnostics Session(uint tick) => new NetworkSessionDiagnostics(
            NetworkRole.Client, NetworkSessionState.Established, 1, 2, 3, new ScopeId(4), tick,
            tick - 1, 5, 6, 7, 8, 9);

        private static NetworkSnapshotDiagnostics Snapshot(uint tick) => new NetworkSnapshotDiagnostics(
            NetworkRole.Client, 1, 2, 3, new ScopeId(4), tick, new SchemaFingerprint(5, 6), 7,
            8, 9, 10, 2, 20, tick - 1, tick, 4, 100);

        private readonly struct TestWorld : IWorldType { }
        private readonly struct TestEntity : IEntityType, INetworkType
        {
            /// <summary>Returns the isolated test entity kind.</summary>
            public byte Id() => 1;
        }
    }
}
