namespace UniGame.StaticEcs.Network.Profiler.Editor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;

    /// <summary>Displays registered network endpoint diagnostics in a dockable UI Toolkit window.</summary>
    public sealed class NetworkDebugWindow : EditorWindow
    {
        private const string PackagePath = "Packages/com.unigame.staticecs.network.profiler/Editor/";
        private const long PollIntervalMilliseconds = 200;
        private const string PreferencesPrefix = "com.unigame.staticecs.network.profiler.NetworkDebugWindow.";
        private const string SourcePreference = PreferencesPrefix + "source";
        private const string TabPreference = PreferencesPrefix + "tab";
        private const string SidebarWidthPreference = PreferencesPrefix + "sidebarWidth";

        private static readonly string[] Tabs =
        {
            "Overview", "Sessions", "Snapshots", "Commands", "Traffic", "Schema", "Trace"
        };

        private readonly List<NetworkDebugSource> _sources = new List<NetworkDebugSource>();
        private DropdownField _sourceDropdown;
        private Toggle _liveToggle;
        private Toggle _traceToggle;
        private Label _contentTitle;
        private Label _noSource;
        private ScrollView _contentScroll;
        private Label _contentLabel;
        private TwoPaneSplitView _split;
        private VisualElement _sidebar;
        private IVisualElementScheduledItem _polling;
        private NetworkDebugSource _source;
        private string _tab = "Overview";

        /// <summary>Opens or focuses the singleton Network Debug window.</summary>
        [MenuItem("Tools/Static ECS/Network Debug")]
        public static void Open()
        {
            var window = GetWindow<NetworkDebugWindow>();
            window.titleContent = new GUIContent("Network Debug");
            window.minSize = new Vector2(760, 420);
            window.Show();
            window.Focus();
        }

        /// <summary>Builds the window from package-owned UXML and USS assets.</summary>
        public void CreateGUI()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(PackagePath + "NetworkDebugWindow.uxml");
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(PackagePath + "NetworkDebugWindow.uss");
            if (visualTree == null || styleSheet == null)
            {
                rootVisualElement.Add(new HelpBox("Network Debug UXML or USS asset is missing.", HelpBoxMessageType.Error));
                return;
            }

            visualTree.CloneTree(rootVisualElement);
            rootVisualElement.styleSheets.Add(styleSheet);
            BindControls();
            RestorePreferences();
            RefreshSources();
            RefreshDisplay();
            _polling = rootVisualElement.schedule.Execute(Poll).Every(PollIntervalMilliseconds);
        }

        private void OnDisable()
        {
            SavePreferences();
            _polling?.Pause();
            _polling = null;
        }

        [InitializeOnLoadMethod]
        private static void RegisterDomainReloadCleanup()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= NetworkDebugRegistry.Reset;
            AssemblyReloadEvents.beforeAssemblyReload += NetworkDebugRegistry.Reset;
        }

        private void BindControls()
        {
            _sourceDropdown = rootVisualElement.Q<DropdownField>("source-dropdown");
            _liveToggle = rootVisualElement.Q<Toggle>("live-toggle");
            _traceToggle = rootVisualElement.Q<Toggle>("trace-toggle");
            _contentTitle = rootVisualElement.Q<Label>("content-title");
            _noSource = rootVisualElement.Q<Label>("no-source");
            _contentScroll = rootVisualElement.Q<ScrollView>("content-scroll");
            _contentLabel = rootVisualElement.Q<Label>("content-label");
            _split = rootVisualElement.Q<TwoPaneSplitView>("main-split");
            _sidebar = rootVisualElement.Q<VisualElement>("sidebar");

            _sourceDropdown.RegisterValueChangedCallback(_ => SelectSource());
            _traceToggle.RegisterValueChangedCallback(change =>
            {
                if (_source != null) _source.TraceEnabled = change.newValue;
            });
            rootVisualElement.Q<Button>("refresh-button").clicked += ManualRefresh;
            rootVisualElement.Q<Button>("clear-trace-button").clicked += ClearTrace;
            rootVisualElement.Q<Button>("export-button").clicked += ExportTrace;
            rootVisualElement.Q<Button>("close-button").clicked += Close;

            for (var i = 0; i < Tabs.Length; i++)
            {
                var tab = Tabs[i];
                rootVisualElement.Q<Button>("tab-" + tab).clicked += () => SelectTab(tab);
            }
        }

        private void Poll()
        {
            if (_liveToggle != null && _liveToggle.value)
            {
                RefreshSources();
                RefreshDisplay();
            }
        }

        private void ManualRefresh()
        {
            RefreshSources();
            RefreshDisplay();
        }

        private void RefreshSources()
        {
            var selectedId = _source?.SourceId ?? EditorPrefs.GetString(SourcePreference, string.Empty);
            var registered = NetworkDebugRegistry.Sources();
            _sources.Clear();
            var choices = new List<string>(registered.Count);
            var selectedIndex = -1;
            for (var i = 0; i < registered.Count; i++)
            {
                var source = registered[i];
                _sources.Add(source);
                choices.Add(source.SourceId + " — " + source.DisplayName);
                if (source.SourceId == selectedId) selectedIndex = i;
            }

            if (selectedIndex < 0 && _sources.Count > 0) selectedIndex = 0;
            _sourceDropdown.choices = choices;
            _sourceDropdown.index = selectedIndex;
            _source = selectedIndex >= 0 ? _sources[selectedIndex] : null;
            if (_source != null) EditorPrefs.SetString(SourcePreference, _source.SourceId);
            _traceToggle.SetEnabled(_source != null);
            if (_source != null) _traceToggle.SetValueWithoutNotify(_source.TraceEnabled);
        }

        private void SelectSource()
        {
            var index = _sourceDropdown.index;
            _source = index >= 0 && index < _sources.Count ? _sources[index] : null;
            if (_source != null) EditorPrefs.SetString(SourcePreference, _source.SourceId);
            _traceToggle.SetEnabled(_source != null);
            if (_source != null) _traceToggle.SetValueWithoutNotify(_source.TraceEnabled);
            RefreshDisplay();
        }

        private void SelectTab(string tab)
        {
            _tab = tab;
            EditorPrefs.SetString(TabPreference, tab);
            for (var i = 0; i < Tabs.Length; i++)
            {
                var button = rootVisualElement.Q<Button>("tab-" + Tabs[i]);
                button.EnableInClassList("selected", Tabs[i] == tab);
            }
            RefreshDisplay();
        }

        private void RestorePreferences()
        {
            var width = EditorPrefs.GetFloat(SidebarWidthPreference, 190f);
            if (width >= 120f && width <= 480f) _split.fixedPaneInitialDimension = width;

            var tab = EditorPrefs.GetString(TabPreference, "Overview");
            for (var i = 0; i < Tabs.Length; i++)
            {
                if (Tabs[i] != tab) continue;
                _tab = tab;
                break;
            }

            for (var i = 0; i < Tabs.Length; i++)
            {
                var button = rootVisualElement.Q<Button>("tab-" + Tabs[i]);
                button.EnableInClassList("selected", Tabs[i] == _tab);
            }
        }

        private void SavePreferences()
        {
            if (_source != null) EditorPrefs.SetString(SourcePreference, _source.SourceId);
            EditorPrefs.SetString(TabPreference, _tab);
            if (_sidebar == null) return;
            var width = _sidebar.resolvedStyle.width;
            if (!float.IsNaN(width) && width >= 120f && width <= 480f)
                EditorPrefs.SetFloat(SidebarWidthPreference, width);
        }

        private void RefreshDisplay()
        {
            _contentTitle.text = _tab;
            var hasSource = _source != null;
            _noSource.style.display = hasSource ? DisplayStyle.None : DisplayStyle.Flex;
            _contentScroll.style.display = hasSource ? DisplayStyle.Flex : DisplayStyle.None;
            if (!hasSource)
            {
                _contentLabel.text = string.Empty;
                return;
            }

            var data = _source.Capture();
            _contentLabel.text = Format(data, _tab);
        }

        private void ClearTrace()
        {
            _source?.ClearTrace();
            RefreshDisplay();
        }

        private void ExportTrace()
        {
            if (_source == null) return;
            var path = EditorUtility.SaveFilePanel("Export Network Trace", string.Empty,
                _source.SourceId + "-network-trace.ndjson", "ndjson");
            if (string.IsNullOrEmpty(path)) return;
            using var stream = File.Create(path);
            _source.ExportTrace(stream);
        }

        private static string Format(NetworkDebugData data, string tab)
        {
            var text = new StringBuilder(2048);
            if (tab == "Overview") FormatOverview(data, text);
            else if (tab == "Sessions") FormatSessions(data, text);
            else if (tab == "Snapshots") FormatSnapshots(data, text);
            else if (tab == "Commands") FormatTrace(data, text, NetworkPhase.CommandDispatch, false);
            else if (tab == "Traffic") FormatTrace(data, text, default, true);
            else if (tab == "Schema") FormatSchema(data, text);
            else FormatTrace(data, text, default, false);
            return text.ToString();
        }

        private static void FormatOverview(NetworkDebugData data, StringBuilder text)
        {
            text.AppendLine($"Source: {data.DisplayName} ({data.SourceId})");
            text.AppendLine($"Revision: {data.Revision}");
            text.AppendLine($"Schema rows: {data.Schema.Count}");
            text.AppendLine($"Session samples: {data.Sessions.Count}");
            text.AppendLine($"Snapshot samples: {data.Snapshots.Count}");
            text.AppendLine($"Trace rows: {data.Trace.Count}");
            if (data.Sessions.Count > 0)
            {
                var session = data.Sessions[data.Sessions.Count - 1];
                text.AppendLine();
                text.AppendLine($"Latest session: {session.Role} / {session.State}");
                text.AppendLine($"Connection {session.ConnectionId}, peer {session.PeerId}, epoch {session.Epoch}");
                text.AppendLine($"Server tick {session.ServerTick}, acknowledged snapshot {session.AcknowledgedSnapshotTick}");
            }
        }

        private static void FormatSessions(NetworkDebugData data, StringBuilder text)
        {
            for (var i = 0; i < data.Sessions.Count; i++)
            {
                var row = data.Sessions[i];
                text.AppendLine($"[{i}] {row.Role} {row.State} connection={row.ConnectionId} peer={row.PeerId} epoch={row.Epoch} scope={row.Scope}");
                text.AppendLine($"    tick={row.ServerTick} ackSnapshot={row.AcknowledgedSnapshotTick} ackCommand={row.AcknowledgedCommandSequence} sendCommand={row.NextSendCommandSequence} receiveCommand={row.NextReceiveCommandSequence} receivePacket={row.NextReceivePacketSequence} sendPacket={row.NextSendPacketSequence}");
            }
        }

        private static void FormatSnapshots(NetworkDebugData data, StringBuilder text)
        {
            for (var i = 0; i < data.Snapshots.Count; i++)
            {
                var row = data.Snapshots[i];
                text.AppendLine($"[{i}] {row.Role} tick={row.ServerTick} connection={row.ConnectionId} peer={row.PeerId} epoch={row.Epoch} scope={row.Scope}");
                text.AppendLine($"    bytes={row.Bytes} entities={row.Entities} records={row.Records} hash={row.PayloadHash:X16} schema={row.SchemaFingerprint}");
                text.AppendLine($"    history={row.HistoryTicks}/{row.HistoryCapacity} ticks, {row.HistoryBytes}/{row.HistoryMaxBytes} bytes, bounds={row.OldestHistoryTick}..{row.NewestHistoryTick}");
            }
        }

        private static void FormatSchema(NetworkDebugData data, StringBuilder text)
        {
            for (var i = 0; i < data.Schema.Count; i++)
            {
                var row = data.Schema[i];
                text.AppendLine($"{row.Kind,-10} 0x{row.TypeId:X8} v{row.Version} maxBytes={row.MaxBytes} maxCount={row.MaxCount} {row.TypeName}");
            }
        }

        private static void FormatTrace(NetworkDebugData data, StringBuilder text, NetworkPhase phase, bool trafficOnly)
        {
            for (var i = 0; i < data.Trace.Count; i++)
            {
                var row = data.Trace[i];
                if (trafficOnly && row.Phase != NetworkPhase.Receive && row.Phase != NetworkPhase.Send) continue;
                if (!trafficOnly && phase == NetworkPhase.CommandDispatch && row.Phase != phase) continue;
                text.AppendLine($"[{i}] {row.Timestamp} {row.Role} {row.Phase}/{row.Kind} {row.Result} packet={row.PacketKind} tick={row.ServerTick} target={row.TargetTick}");
                text.AppendLine($"    connection={row.ConnectionId} peer={row.PeerId} epoch={row.Epoch} bytes={row.Bytes} packets={row.Packets} entities={row.Entities} records={row.Records} commands={row.Commands} accepted={row.AcceptedCommands} rejected={row.RejectedCommands} durationNs={row.DurationNanoseconds}");
            }
        }
    }
}
