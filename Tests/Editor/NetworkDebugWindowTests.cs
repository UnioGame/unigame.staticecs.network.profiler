namespace UniGame.StaticEcs.Network.Profiler.Tests
{
    using System;
    using NUnit.Framework;
    using UniGame.StaticEcs.Network.Profiler.Editor;
    using UnityEditor;
    using UnityEngine.UIElements;

    /// <summary>Verifies the authored Network Debug UI Toolkit layout.</summary>
    public sealed class NetworkDebugWindowTests
    {
        private const string PreferencePrefix = "com.unigame.staticecs.network.profiler.NetworkDebugWindow.";

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
            Assert.That(root.Q<Button>("refresh-button"), Is.Not.Null);
            Assert.That(root.Q<Button>("close-button"), Is.Not.Null);

            var tabs = new[] { "Overview", "Sessions", "Snapshots", "Commands", "Traffic", "Schema", "Trace" };
            for (var i = 0; i < tabs.Length; i++)
                Assert.That(root.Q<Button>("tab-" + tabs[i]), Is.Not.Null, tabs[i]);
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
                window.Close();
                EditorPrefs.DeleteKey(PreferencePrefix + "source");
                EditorPrefs.DeleteKey(PreferencePrefix + "tab");
                EditorPrefs.DeleteKey(PreferencePrefix + "sidebarWidth");
                NetworkDebugRegistry.Reset();
            }
        }
    }
}
