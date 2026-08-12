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
    }
}
