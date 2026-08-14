namespace UniGame.StaticEcs.Network.Profiler.Tests
{
    using System;
    using NUnit.Framework;
    using UniGame.StaticEcs.Network.Profiler.Editor;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.TestTools;
    using UnityEngine.UIElements;

    /// <summary>Verifies the authored Network Debug UI Toolkit layout.</summary>
    public sealed class NetworkDebugWindowTests
    {
        private const string PreferencePrefix = "com.unigame.staticecs.network.profiler.NetworkDebugWindow.";
        private static readonly NetworkBufferPool Buffers =
            new NetworkBufferPool(NetworkBufferPool.DefaultClientRetainedBytes);

        /// <summary>Resets process and preference state before each behavioral test.</summary>
        [SetUp]
        public void SetUp()
        {
            CloseWindows();
            ClearPreferences();
            NetworkDebugRegistry.Reset();
        }

        /// <summary>Releases process and preference state after each behavioral test.</summary>
        [TearDown]
        public void TearDown()
        {
            CloseWindows();
            ClearPreferences();
            NetworkDebugRegistry.Reset();
        }

        /// <summary>Verifies the fixed sidebar and every required toolbar and tab control.</summary>
        [Test]
        public void UxmlContainsRequiredLayoutAndControls()
        {
            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Packages/com.unigame.staticecs.network.profiler/Editor/NetworkDebugWindow.uxml");
            Assert.That(tree, Is.Not.Null);
            var root = tree.CloneTree();
            var split = root.Q<TwoPaneSplitView>("main-split");
            Assert.That(split, Is.Not.Null);
            Assert.That(split.fixedPaneInitialDimension, Is.EqualTo(190));
            Assert.That(root.Q<DropdownField>("source-dropdown"), Is.Not.Null);
            Assert.That(root.Q<Toggle>("live-toggle"), Is.Not.Null);
            Assert.That(root.Q<Toggle>("trace-toggle"), Is.Not.Null);
            Assert.That(root.Q<Toggle>("trace-toggle").value, Is.False);
            Assert.That(root.Q<Button>("refresh-button"), Is.Not.Null);
            Assert.That(root.Q<Button>("close-button"), Is.Not.Null);

            var tabs = new[]
            {
                "Overview", "Sessions", "Snapshots", "Commands", "Traffic", "Schema", "Trace",
                "Simulator"
            };
            for (var i = 0; i < tabs.Length; i++)
                Assert.That(root.Q<Button>("tab-" + tabs[i]), Is.Not.Null, tabs[i]);
            Assert.That(root.Q<VisualElement>("simulator-controls"), Is.Not.Null);
            Assert.That(root.Q<DropdownField>("sim-preset"), Is.Not.Null);
            Assert.That(root.Q<IntegerField>("sim-max-packets"), Is.Not.Null);
            Assert.That(root.Q<LongField>("sim-max-bytes"), Is.Not.Null);
            Assert.That(root.Q<IntegerField>("sim-decision-capacity"), Is.Not.Null);
            Assert.That(root.Q<Button>("sim-connect"), Is.Not.Null);
            Assert.That(root.Q<Button>("sim-disconnect"), Is.Not.Null);
            Assert.That(root.Q<Button>("sim-pause"), Is.Not.Null);
            Assert.That(root.Q<Button>("sim-reset"), Is.Not.Null);
            Assert.That(root.Q<Button>("sim-record"), Is.Not.Null);
            Assert.That(root.Q<Button>("sim-replay"), Is.Not.Null);
        }

        /// <summary>Verifies repeated Open calls focus one dockable singleton.</summary>
        [Test]
        public void OpenFocusesSingletonWindow()
        {
            var ignoreGraphicsLogs = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;
            var previousIgnore = LogAssert.ignoreFailingMessages;
            if (ignoreGraphicsLogs)
                LogAssert.ignoreFailingMessages = true;
            try
            {
                NetworkDebugWindow.Open();
                var first = EditorWindow.GetWindow<NetworkDebugWindow>();
                NetworkDebugWindow.Open();
                var second = EditorWindow.GetWindow<NetworkDebugWindow>();
                Assert.That(second, Is.SameAs(first));
                if (!ignoreGraphicsLogs)
                    Assert.That(EditorWindow.focusedWindow, Is.SameAs(first));
                Assert.That(Resources.FindObjectsOfTypeAll<NetworkDebugWindow>(), Has.Length.EqualTo(1));
            }
            finally
            {
                LogAssert.ignoreFailingMessages = previousIgnore;
            }
        }

        /// <summary>Verifies the public Editor command uses the game-owned Static ECS menu root.</summary>
        [Test]
        public void OpenUsesGameStaticEcsMenu()
        {
            var method = typeof(NetworkDebugWindow).GetMethod(nameof(NetworkDebugWindow.Open));
            var attribute = (MenuItem)Attribute.GetCustomAttribute(method, typeof(MenuItem));
            Assert.That(attribute, Is.Not.Null);
            Assert.That(attribute.menuItem, Is.EqualTo("Game/Static ECS/Network Debug"));
        }

        /// <summary>Verifies the window disable lifecycle stops the scheduled poll item.</summary>
        [Test]
        public void DisableLifecycleStopsPolling()
        {
            var window = CreateWindow();
            Assert.That(window.PollingActive, Is.True);
            window.DisableForTests();
            Assert.That(window.PollingActive, Is.False);
        }

        /// <summary>Verifies the registered domain-reload handler clears global source state.</summary>
        [Test]
        public void DomainReloadHandlerResetsRegistry()
        {
            using var lease = NetworkDebugRegistry.Register("client", "Client", Array.Empty<NetworkSchemaEntry>(), out _);
            Assert.That(NetworkDebugRegistry.Sources(), Has.Count.EqualTo(1));
            NetworkDebugWindow.BeforeAssemblyReloadForTests();
            Assert.That(NetworkDebugRegistry.Sources(), Is.Empty);
        }

        /// <summary>Verifies live pause freezes presentation while source diagnostics continue accumulating.</summary>
        [Test]
        public void LivePauseIsPresentationOnly()
        {
            using var lease = NetworkDebugRegistry.Register("client", "Client", Array.Empty<NetworkSchemaEntry>(), out var source);
            var window = CreateWindow();
            window.SetLiveForTests(false);
            var refreshes = window.PresentationRefreshCount;
            var session = Session(12);
            source.ObserveSession(in session);
            window.PollForTests();
            Assert.That(window.PresentationRefreshCount, Is.EqualTo(refreshes));
            Assert.That(source.Capture().Sessions, Has.Count.EqualTo(1));
            window.SetLiveForTests(true);
            window.PollForTests();
            Assert.That(window.PresentationRefreshCount, Is.GreaterThan(refreshes));
        }

        /// <summary>Verifies empty state and deterministic client plus embedded-server source refresh.</summary>
        [Test]
        public void RefreshesEmptyClientAndEmbeddedServerSources()
        {
            var window = CreateWindow();
            Assert.That(window.rootVisualElement.Q<Label>("no-source").style.display.value, Is.EqualTo(DisplayStyle.Flex));
            using var serverLease = NetworkDebugRegistry.Register("server-embedded", "Embedded Server",
                Array.Empty<NetworkSchemaEntry>(), out _);
            using var clientLease = NetworkDebugRegistry.Register("client-main", "Client",
                Array.Empty<NetworkSchemaEntry>(), out _);
            window.RefreshSourcesForTests();
            var dropdown = window.rootVisualElement.Q<DropdownField>("source-dropdown");
            Assert.That(dropdown.choices, Is.EqualTo(new[]
            {
                "client-main — Client", "server-embedded — Embedded Server"
            }));
            Assert.That(window.SelectedSource.SourceId, Is.EqualTo("client-main"));
            Assert.That(window.rootVisualElement.Q<Label>("no-source").style.display.value, Is.EqualTo(DisplayStyle.None));
        }

        /// <summary>Verifies Overview and Traffic render actual immutable aggregate data.</summary>
        [Test]
        public void RendersOverviewAndDeterministicTrafficCounters()
        {
            using var lease = NetworkDebugRegistry.Register("client", "Client", Array.Empty<NetworkSchemaEntry>(),
                out var source, worldName: "Main");
            var received = Trace(NetworkPhase.Receive, NetworkPacketKind.None, 100, 2,
                NetworkResultCategory.Success, 20, 3);
            source.Observe(in received);
            var decoded = Trace(NetworkPhase.Decode, NetworkPacketKind.Hello, 40, 1,
                NetworkResultCategory.Success, 20, 3);
            source.Observe(in decoded);
            var sent = Trace(NetworkPhase.Send, NetworkPacketKind.FullSnapshot, 250, 1,
                NetworkResultCategory.Success, 20, 3);
            source.Observe(in sent);
            var error = Trace(NetworkPhase.Decode, NetworkPacketKind.None, 0, 0,
                NetworkResultCategory.Protocol, 20, 3);
            source.Observe(in error);
            var window = CreateWindow();
            Assert.That(window.RenderedText, Does.Contain("World: Main"));
            Assert.That(window.RenderedText, Does.Contain("Role: Client"));
            Assert.That(window.RenderedText, Does.Contain("Schema fingerprint: 00000000000000060000000000000005"));
            Assert.That(window.RenderedText, Does.Contain("Authoritative tick: 20; client tick: 17; gap: 3"));
            Assert.That(window.RenderedText, Does.Contain("receive=2 packets/100 bytes; send=1 packets/250 bytes"));
            Assert.That(window.RenderedText, Does.Contain("Errors: 1"));

            window.SelectTabForTests("Traffic");
            Assert.That(window.RenderedText, Does.Contain("Receive total: 2 packets, 100 bytes"));
            Assert.That(window.RenderedText, Does.Contain("Hello: 2 packets, 100 bytes"));
            Assert.That(window.RenderedText, Does.Contain("Send total: 1 packets, 250 bytes"));
            Assert.That(window.RenderedText, Does.Contain("FullSnapshot: 1 packets, 250 bytes"));
        }

        /// <summary>Verifies the Simulator tab reflects an optional shared capability without touching ECS.</summary>
        [Test]
        public void SimulatorTabRendersCapabilityAndDisablesUnsupportedSources()
        {
            var config = NetworkSimulationPresets.Create(NetworkSimulationPreset.Local);
            using var simulator = new NetworkSimulator(new ConnectionId(1), in config);
            using var clientLease = NetworkDebugRegistry.Register("client", "Client",
                Array.Empty<NetworkSchemaEntry>(), out _, simulator: simulator);
            using var serverLease = NetworkDebugRegistry.Register("server", "Embedded Server",
                Array.Empty<NetworkSchemaEntry>(), out _, simulator: simulator);
            using var dedicatedLease = NetworkDebugRegistry.Register("z-dedicated", "Server",
                Array.Empty<NetworkSchemaEntry>(), out _);
            Assert.That(simulator.Client.TrySend(Buffers.Copy(new byte[] { 1, 2 })), Is.True);

            var window = CreateWindow();
            window.SelectTabForTests("Simulator");
            var controls = window.rootVisualElement.Q<VisualElement>("simulator-controls");
            Assert.That(controls.enabledInHierarchy, Is.True);
            Assert.That(window.RenderedText, Does.Contain("Connected: True"));
            Assert.That(window.RenderedText, Does.Contain("Client -> Server: queued=1/2B"));
            Assert.That(window.rootVisualElement.Q<IntegerField>("sim-latency").value,
                Is.EqualTo(config.LatencyMilliseconds));
            Assert.That(window.rootVisualElement.Q<IntegerField>("sim-max-packets").value,
                Is.EqualTo(config.MaxQueuedPackets));
            Assert.That(window.rootVisualElement.Q<LongField>("sim-max-bytes").value,
                Is.EqualTo(config.MaxQueuedBytes));

            var dropdown = window.rootVisualElement.Q<DropdownField>("source-dropdown");
            window.SelectSourceForTests(2);
            Assert.That(dropdown.value, Is.EqualTo(dropdown.choices[2]));
            Assert.That(controls.enabledInHierarchy, Is.False);
            Assert.That(window.RenderedText,
                Does.Contain("does not expose a mock simulator capability"));
        }

        /// <summary>Verifies persisted source and tab preferences are restored when the window is rebuilt.</summary>
        [Test]
        public void RestoresSourceAndTabPreferences()
        {
            NetworkDebugRegistry.Reset();
            EditorPrefs.SetString(PreferencePrefix + "source", "server");
            EditorPrefs.SetString(PreferencePrefix + "tab", "Snapshots");
            using var lease = NetworkDebugRegistry.Register("client", "Client", Array.Empty<NetworkSchemaEntry>(), out _);
            using var serverLease = NetworkDebugRegistry.Register("server", "Server", Array.Empty<NetworkSchemaEntry>(), out _);
            var window = UnityEngine.ScriptableObject.CreateInstance<NetworkDebugWindow>();
            try
            {
                window.CreateGUI();
                Assert.That(window.rootVisualElement.Q<DropdownField>("source-dropdown").value,
                    Does.StartWith("server —"));
                Assert.That(window.rootVisualElement.Q<Button>("tab-Snapshots").ClassListContains("selected"), Is.True);
            }
            finally
            {
                DestroyWindow(window);
            }
        }

        private static NetworkDebugWindow CreateWindow()
        {
            var window = ScriptableObject.CreateInstance<NetworkDebugWindow>();
            window.CreateGUI();
            return window;
        }

        private static NetworkSessionDiagnostics Session(uint tick) => new NetworkSessionDiagnostics(
            NetworkRole.Client, NetworkSessionState.Established, 1, 2, 3, new ScopeId(4), tick,
            tick - 1, 5, 6, 7, 8, 9);

        private static NetworkTraceEvent Trace(NetworkPhase phase, NetworkPacketKind packetKind, int bytes,
            int packets, NetworkResultCategory result, uint tick, int tickGap) => new NetworkTraceEvent(
            phase, NetworkTraceKind.Point, result, NetworkRole.Client, 1, 2, 3, tick, 0, bytes, packets,
            0, 0, 0, 0, 0, 1, 1, tick, packetKind, clientServerTickGap: tickGap,
            fingerprint: new SchemaFingerprint(5, 6));

        private static void CloseWindows()
        {
            var windows = Resources.FindObjectsOfTypeAll<NetworkDebugWindow>();
            for (var i = 0; i < windows.Length; i++) DestroyWindow(windows[i]);
        }

        private static void DestroyWindow(NetworkDebugWindow window)
        {
            if (window == null) return;
            UnityEngine.Object.DestroyImmediate(window);
        }

        private static void ClearPreferences()
        {
            EditorPrefs.DeleteKey(PreferencePrefix + "source");
            EditorPrefs.DeleteKey(PreferencePrefix + "tab");
            EditorPrefs.DeleteKey(PreferencePrefix + "sidebarWidth");
        }
    }
}
