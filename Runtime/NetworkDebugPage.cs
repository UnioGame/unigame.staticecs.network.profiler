namespace UniGame.StaticEcs.Network.Profiler
{
    /// <summary>Identifies one diagnostics page rendered by network debug interfaces.</summary>
    public enum NetworkDebugPage : byte
    {
        /// <summary>Displays the latest endpoint summary.</summary>
        Overview,

        /// <summary>Displays retained session samples.</summary>
        Sessions,

        /// <summary>Displays retained snapshot samples.</summary>
        Snapshots,

        /// <summary>Displays command-dispatch trace rows.</summary>
        Commands,

        /// <summary>Displays cumulative traffic counters.</summary>
        Traffic,

        /// <summary>Displays the generated network schema.</summary>
        Schema,

        /// <summary>Displays the retained diagnostics trace.</summary>
        Trace,

        /// <summary>Displays mock simulator state and decisions.</summary>
        Simulator
    }
}
