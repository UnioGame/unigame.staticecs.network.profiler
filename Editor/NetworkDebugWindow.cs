namespace UniGame.StaticEcs.Network.Profiler.Editor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
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
            "Overview", "Sessions", "Snapshots", "Commands", "Traffic", "Schema", "Trace", "Simulator"
        };

        private readonly List<NetworkDebugSource> _sources = new List<NetworkDebugSource>();
        private DropdownField _sourceDropdown;
        private Toggle _liveToggle;
        private Toggle _traceToggle;
        private Label _contentTitle;
        private Label _noSource;
        private ScrollView _contentScroll;
        private Label _contentLabel;
        private VisualElement _simulatorControls;
        private DropdownField _simPreset;
        private IntegerField _simSeed;
        private IntegerField _simLatency;
        private IntegerField _simJitter;
        private FloatField _simLoss;
        private FloatField _simDuplicate;
        private FloatField _simReorder;
        private LongField _simBandwidth;
        private IntegerField _simMaxPackets;
        private LongField _simMaxBytes;
        private IntegerField _simDecisionCapacity;
        private IReadOnlyList<NetworkSimulationDecision> _lastRecording = Array.Empty<NetworkSimulationDecision>();
        private TwoPaneSplitView _split;
        private VisualElement _sidebar;
        private IVisualElementScheduledItem _polling;
        private NetworkDebugSource _source;
        private string _tab = "Overview";
        private bool _pollingActive;
        private int _presentationRefreshCount;
        private bool _simulatorControlsLoaded;

        internal bool PollingActive => _pollingActive;
        internal int PresentationRefreshCount => _presentationRefreshCount;
        internal string RenderedText => _contentLabel?.text ?? string.Empty;
        internal NetworkDebugSource SelectedSource => _source;

        /// <summary>Opens or focuses the singleton Network Debug window.</summary>
        [MenuItem("Game/Static ECS/Network Debug")]
        public static void Open()
        {
            var window = GetWindow<NetworkDebugWindow>();
            window.titleContent = new GUIContent("Network Debug");
            window.minSize = new Vector2(760, 420);
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                window.Show();
                window.Focus();
            }
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
            _pollingActive = true;
        }

        private void OnDisable()
        {
            SavePreferences();
            _polling?.Pause();
            _polling = null;
            _pollingActive = false;
        }

        [InitializeOnLoadMethod]
        private static void RegisterDomainReloadCleanup()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= BeforeAssemblyReload;
            AssemblyReloadEvents.beforeAssemblyReload += BeforeAssemblyReload;
        }

        private static void BeforeAssemblyReload() => NetworkDebugRegistry.Reset();

        internal static void BeforeAssemblyReloadForTests() => BeforeAssemblyReload();

        private void BindControls()
        {
            _sourceDropdown = rootVisualElement.Q<DropdownField>("source-dropdown");
            _liveToggle = rootVisualElement.Q<Toggle>("live-toggle");
            _traceToggle = rootVisualElement.Q<Toggle>("trace-toggle");
            _contentTitle = rootVisualElement.Q<Label>("content-title");
            _noSource = rootVisualElement.Q<Label>("no-source");
            _contentScroll = rootVisualElement.Q<ScrollView>("content-scroll");
            _contentLabel = rootVisualElement.Q<Label>("content-label");
            _simulatorControls = rootVisualElement.Q<VisualElement>("simulator-controls");
            _simPreset = rootVisualElement.Q<DropdownField>("sim-preset");
            _simSeed = rootVisualElement.Q<IntegerField>("sim-seed");
            _simLatency = rootVisualElement.Q<IntegerField>("sim-latency");
            _simJitter = rootVisualElement.Q<IntegerField>("sim-jitter");
            _simLoss = rootVisualElement.Q<FloatField>("sim-loss");
            _simDuplicate = rootVisualElement.Q<FloatField>("sim-duplicate");
            _simReorder = rootVisualElement.Q<FloatField>("sim-reorder");
            _simBandwidth = rootVisualElement.Q<LongField>("sim-bandwidth");
            _simMaxPackets = rootVisualElement.Q<IntegerField>("sim-max-packets");
            _simMaxBytes = rootVisualElement.Q<LongField>("sim-max-bytes");
            _simDecisionCapacity = rootVisualElement.Q<IntegerField>("sim-decision-capacity");
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
            _simPreset.choices = new List<string>(Enum.GetNames(typeof(NetworkSimulationPreset)));
            _simPreset.index = 0;
            _simPreset.RegisterValueChangedCallback(_ => ApplyPreset());
            rootVisualElement.Q<Button>("sim-apply").clicked += ApplySimulatorConfig;
            rootVisualElement.Q<Button>("sim-connect").clicked += () => Control()?.Connect();
            rootVisualElement.Q<Button>("sim-disconnect").clicked += () => Control()?.Disconnect();
            rootVisualElement.Q<Button>("sim-pause").clicked += ToggleSimulatorPause;
            rootVisualElement.Q<Button>("sim-reset").clicked += () => Control()?.Reset();
            rootVisualElement.Q<Button>("sim-record").clicked += ToggleSimulatorRecording;
            rootVisualElement.Q<Button>("sim-replay").clicked += ReplayLastRecording;

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

        internal void PollForTests() => Poll();
        internal void RefreshSourcesForTests() => ManualRefresh();
        internal void SetLiveForTests(bool value) => _liveToggle.value = value;
        internal void SelectTabForTests(string value) => SelectTab(value);
        internal void SelectSourceForTests(int index)
        {
            _sourceDropdown.index = index;
            SelectSource();
        }
        internal void DisableForTests() => OnDisable();

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
            _simulatorControlsLoaded = false;
            if (_source != null) EditorPrefs.SetString(SourcePreference, _source.SourceId);
            _traceToggle.SetEnabled(_source != null);
            if (_source != null) _traceToggle.SetValueWithoutNotify(_source.TraceEnabled);
            RefreshDisplay();
        }

        private void SelectTab(string tab)
        {
            _tab = tab;
            if (tab == "Simulator") _simulatorControlsLoaded = false;
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
            _presentationRefreshCount++;
            _contentTitle.text = _tab;
            var hasSource = _source != null;
            var showSimulator = _tab == "Simulator";
            _simulatorControls.style.display = showSimulator ? DisplayStyle.Flex : DisplayStyle.None;
            _noSource.style.display = hasSource ? DisplayStyle.None : DisplayStyle.Flex;
            _contentScroll.style.display = hasSource ? DisplayStyle.Flex : DisplayStyle.None;
            if (!hasSource)
            {
                _contentLabel.text = string.Empty;
                return;
            }

            var data = _source.Capture();
            _simulatorControls.SetEnabled(data.HasSimulator);
            if (showSimulator && data.HasSimulator && !_simulatorControlsLoaded)
            {
                LoadSimulatorConfig(data.SimulationConfig);
                _simulatorControlsLoaded = true;
            }
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
            return Enum.TryParse(tab, out NetworkDebugPage page)
                ? NetworkDebugTextFormatter.Format(data, page)
                : NetworkDebugTextFormatter.Format(data, NetworkDebugPage.Overview);
        }

        private INetworkSimulatorControl Control() => _source?.SimulatorControl;

        private void ApplyPreset()
        {
            if (!Enum.TryParse(_simPreset.value, out NetworkSimulationPreset preset)) return;
            LoadSimulatorConfig(NetworkSimulationPresets.Create(preset));
        }

        private void LoadSimulatorConfig(NetworkSimulationConfig config)
        {
            _simSeed.SetValueWithoutNotify(unchecked((int)config.Seed));
            _simLatency.SetValueWithoutNotify(config.LatencyMilliseconds);
            _simJitter.SetValueWithoutNotify(config.JitterMilliseconds);
            _simLoss.SetValueWithoutNotify(config.LossProbability);
            _simDuplicate.SetValueWithoutNotify(config.DuplicateProbability);
            _simReorder.SetValueWithoutNotify(config.ReorderProbability);
            _simBandwidth.SetValueWithoutNotify(config.BandwidthBytesPerSecond);
            _simMaxPackets.SetValueWithoutNotify(config.MaxQueuedPackets);
            _simMaxBytes.SetValueWithoutNotify(config.MaxQueuedBytes);
            _simDecisionCapacity.SetValueWithoutNotify(config.DecisionCapacity);
        }

        private void ApplySimulatorConfig()
        {
            var control = Control();
            if (control == null) return;
            var config = control.CaptureConfig();
            config.Seed = unchecked((uint)_simSeed.value);
            config.LatencyMilliseconds = _simLatency.value;
            config.JitterMilliseconds = _simJitter.value;
            config.LossProbability = _simLoss.value;
            config.DuplicateProbability = _simDuplicate.value;
            config.ReorderProbability = _simReorder.value;
            config.BandwidthBytesPerSecond = _simBandwidth.value;
            config.MaxQueuedPackets = _simMaxPackets.value;
            config.MaxQueuedBytes = _simMaxBytes.value;
            config.DecisionCapacity = _simDecisionCapacity.value;
            control.ApplyConfig(in config);
            RefreshDisplay();
        }

        private void ToggleSimulatorPause()
        {
            var control = Control();
            if (control == null) return;
            control.SetPaused(!control.CaptureStats().Paused);
        }

        private void ToggleSimulatorRecording()
        {
            var control = Control();
            if (control == null) return;
            if (control.CaptureStats().Recording)
                _lastRecording = control.StopRecording();
            else
                control.StartRecording();
        }

        private void ReplayLastRecording()
        {
            var control = Control();
            if (control == null || _lastRecording.Count == 0) return;
            control.StartReplay(_lastRecording);
        }

    }
}
