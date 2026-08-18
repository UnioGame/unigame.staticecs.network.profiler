namespace UniGame.StaticEcs.Network.Profiler.Tests
{
    using System;
    using NUnit.Framework;

    /// <summary>Verifies the formatter shared by runtime and Editor diagnostics interfaces.</summary>
    [TestFixture]
    internal sealed class NetworkDebugTextFormatterTests
    {
        [SetUp]
        public void SetUp() => NetworkDebugRegistry.Reset();

        [TearDown]
        public void TearDown() => NetworkDebugRegistry.Reset();

        [Test]
        public void EveryPageFormatsRegisteredSourceWithoutEditorDependencies()
        {
            var config = NetworkSimulationPresets.Create(NetworkSimulationPreset.Local);
            using var simulator = new NetworkSimulator(new ConnectionId(17), in config);
            using var lease = NetworkDebugRegistry.Register("client", "Client",
                Array.Empty<NetworkSchemaEntry>(), out var source, simulator: simulator,
                worldName: "ClientWorld");
            var data = source.Capture();

            foreach (NetworkDebugPage page in Enum.GetValues(typeof(NetworkDebugPage)))
            {
                var text = NetworkDebugTextFormatter.Format(data, page);
                Assert.That(text, Is.Not.Null, page.ToString());
            }

            Assert.That(NetworkDebugTextFormatter.Format(data, NetworkDebugPage.Overview),
                Does.Contain("ClientWorld"));
            Assert.That(NetworkDebugTextFormatter.Format(data, NetworkDebugPage.Simulator),
                Does.Contain("Connected: True"));
        }

        /// <summary>Verifies the transport page exposes driver, channel, queue, failure, and reconnect data.</summary>
        [Test]
        public void TransportPageFormatsDetailedCounters()
        {
            var value = new NetworkTransportDebugData(true, "Unity Transport", "127.0.0.1:7777", "Connected",
                2, 200, 3, 300, 4, 400, 5, 500, 6, 7, 8, 9, 10, 11, 12,
                reconnectAttempts: 13, reconnectBackoffSeconds: 1.25);
            var source = new NetworkDebugSource("transport", "Transport", Array.Empty<NetworkSchemaEntry>(),
                transport: () => value);
            using var lease = NetworkDebugRegistry.Register(source);

            var text = NetworkDebugTextFormatter.Format(source.Capture(), NetworkDebugPage.Transport);

            Assert.That(text, Does.Contain("Driver: Unity Transport"));
            Assert.That(text, Does.Contain("Reliable: receive=2 packets/200 bytes; send=3 packets/300 bytes"));
            Assert.That(text, Does.Contain("Unreliable: receive=4 packets/400 bytes; send=5 packets/500 bytes"));
            Assert.That(text, Does.Contain("Queues: 6 packets; outstanding leases=7"));
            Assert.That(text, Does.Contain("receive overflow=8; malformed=10; send=9; dropped=11"));
            Assert.That(text, Does.Contain("disconnects=12; reconnect attempts=13; backoff=1.25 s"));
        }
    }
}
