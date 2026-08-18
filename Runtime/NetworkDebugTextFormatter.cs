namespace UniGame.StaticEcs.Network.Profiler
{
    using System;
    using System.Text;

    /// <summary>Formats payload-free network diagnostics for runtime and Editor interfaces.</summary>
    public static class NetworkDebugTextFormatter
    {
        /// <summary>Formats one diagnostics page from an immutable source snapshot.</summary>
        public static string Format(NetworkDebugData data, NetworkDebugPage page)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            var text = new StringBuilder(2048);
            switch (page)
            {
                case NetworkDebugPage.Overview:
                    FormatOverview(data, text);
                    break;
                case NetworkDebugPage.Sessions:
                    FormatSessions(data, text);
                    break;
                case NetworkDebugPage.Snapshots:
                    FormatSnapshots(data, text);
                    break;
                case NetworkDebugPage.Commands:
                    FormatTrace(data, text, commandsOnly: true);
                    break;
                case NetworkDebugPage.Traffic:
                    FormatTraffic(data, text);
                    break;
                case NetworkDebugPage.Schema:
                    FormatSchema(data, text);
                    break;
                case NetworkDebugPage.Simulator:
                    FormatSimulator(data, text);
                    break;
                case NetworkDebugPage.Transport:
                    FormatTransport(data, text);
                    break;
                default:
                    FormatTrace(data, text, commandsOnly: false);
                    break;
            }

            return text.ToString();
        }

        private static void FormatOverview(NetworkDebugData data, StringBuilder text)
        {
            text.AppendLine($"Source: {data.DisplayName} ({data.SourceId})");
            text.AppendLine($"World: {(string.IsNullOrEmpty(data.WorldName) ? "(not supplied)" : data.WorldName)}");
            text.AppendLine($"Role: {(data.HasRole ? data.Role.ToString() : "(not observed)")}");
            text.AppendLine($"Schema fingerprint: {data.SchemaFingerprint}");
            var clientTick = (long)data.ServerTick - data.ClientServerTickGap;
            text.AppendLine($"Authoritative tick: {data.ServerTick}; client tick: {clientTick}; gap: {data.ClientServerTickGap}");
            text.AppendLine($"Traffic totals: receive={data.ReceivedPackets} packets/{data.ReceivedBytes} bytes; send={data.SentPackets} packets/{data.SentBytes} bytes");
            text.AppendLine($"Errors: {data.Errors}");
            text.AppendLine($"Revision: {data.Revision}");
            text.AppendLine($"Schema rows: {data.Schema.Count}");
            text.AppendLine($"Session samples: {data.Sessions.Count}");
            text.AppendLine($"Snapshot samples: {data.Snapshots.Count}");
            text.AppendLine($"Trace rows: {data.Trace.Count}");
            if (data.Sessions.Count == 0)
                return;

            var session = data.Sessions[data.Sessions.Count - 1];
            text.AppendLine();
            text.AppendLine($"Latest session: {session.Role} / {session.State}");
            text.AppendLine($"Connection {session.ConnectionId}, peer {session.PeerId}, epoch {session.Epoch}");
            text.AppendLine($"Server tick {session.ServerTick}, acknowledged snapshot {session.AcknowledgedSnapshotTick}");
        }

        private static void FormatSessions(NetworkDebugData data, StringBuilder text)
        {
            for (var i = 0; i < data.Sessions.Count; i++)
            {
                var row = data.Sessions[i];
                text.AppendLine($"[{i}] {row.Role} {row.State} connection={row.ConnectionId} peer={row.PeerId} epoch={row.Epoch} scope={row.Scope}");
                text.AppendLine($"    tick={row.ServerTick} ackSnapshot={row.AcknowledgedSnapshotTick} processedCommand={row.ServerProcessedCommandSequence} sendCommand={row.NextSendCommandSequence} receiveCommand={row.NextReceiveCommandSequence} receivePacket={row.NextReceivePacketSequence} sendPacket={row.NextSendPacketSequence}");
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

        private static void FormatTraffic(NetworkDebugData data, StringBuilder text)
        {
            text.AppendLine($"Receive total: {data.ReceivedPackets} packets, {data.ReceivedBytes} bytes");
            for (var i = 0; i < data.Traffic.Count; i++)
            {
                var row = data.Traffic[i];
                if (row.Direction != NetworkTrafficDirection.Receive)
                    continue;
                text.AppendLine($"  {row.PacketKind}: {row.Packets} packets, {row.Bytes} bytes");
            }

            text.AppendLine();
            text.AppendLine($"Send total: {data.SentPackets} packets, {data.SentBytes} bytes");
            for (var i = 0; i < data.Traffic.Count; i++)
            {
                var row = data.Traffic[i];
                if (row.Direction != NetworkTrafficDirection.Send)
                    continue;
                text.AppendLine($"  {row.PacketKind}: {row.Packets} packets, {row.Bytes} bytes");
            }

            text.AppendLine();
            text.AppendLine($"Errors: {data.Errors}");
        }

        private static void FormatSchema(NetworkDebugData data, StringBuilder text)
        {
            for (var i = 0; i < data.Schema.Count; i++)
            {
                var row = data.Schema[i];
                text.AppendLine($"{row.Kind,-10} 0x{row.TypeId:X8} v{row.Version} maxBytes={row.MaxBytes} maxCount={row.MaxCount} {row.TypeName}");
            }
        }

        private static void FormatTrace(NetworkDebugData data, StringBuilder text, bool commandsOnly)
        {
            for (var i = 0; i < data.Trace.Count; i++)
            {
                var row = data.Trace[i];
                if (commandsOnly && row.Phase != NetworkPhase.CommandDispatch)
                    continue;
                text.AppendLine($"[{i}] {row.Timestamp} {row.Role} {row.Phase}/{row.Kind} {row.Result} packet={row.PacketKind} tick={row.ServerTick} target={row.TargetTick}");
                text.AppendLine($"    connection={row.ConnectionId} peer={row.PeerId} epoch={row.Epoch} bytes={row.Bytes} packets={row.Packets} entities={row.Entities} records={row.Records} commands={row.Commands} accepted={row.AcceptedCommands} rejected={row.RejectedCommands} durationNs={row.DurationNanoseconds}");
            }
        }

        private static void FormatSimulator(NetworkDebugData data, StringBuilder text)
        {
            if (!data.HasSimulator)
            {
                text.AppendLine("This endpoint does not expose a mock simulator capability.");
                return;
            }

            var stats = data.SimulationStats;
            text.AppendLine($"Connected: {stats.Connected}; paused: {stats.Paused}; generation: {stats.ConnectionGeneration}");
            text.AppendLine($"Time: {stats.TimeMilliseconds} ms; cycle: {stats.Cycle}; recording: {stats.Recording}; replaying: {stats.Replaying}; replay errors: {stats.ReplayErrors}");
            FormatDirection("Client -> Server", stats.ClientToServer, text);
            FormatDirection("Server -> Client", stats.ServerToClient, text);
            text.AppendLine();
            text.AppendLine("Recent decisions:");
            for (var i = 0; i < data.SimulationDecisions.Count; i++)
            {
                var row = data.SimulationDecisions[i];
                text.AppendLine($"[{i}] t={row.TimeMilliseconds} {row.Direction} #{row.Ordinal} {row.Kind} bytes={row.Bytes} due={row.ScheduledMilliseconds} reorder={row.Reordered} duplicate={row.Duplicated}");
            }
        }

        private static void FormatTransport(NetworkDebugData data, StringBuilder text)
        {
            if (!data.HasTransport || !data.Transport.Available)
            {
                text.AppendLine("Transport diagnostics unavailable.");
                return;
            }

            var transport = data.Transport;
            text.AppendLine($"Driver: {Display(transport.Driver)}");
            text.AppendLine($"Endpoint: {Display(transport.Endpoint)}");
            text.AppendLine($"State: {Display(transport.State)}");
            text.AppendLine($"Reliable: receive={transport.ReliableReceivedPackets} packets/{transport.ReliableReceivedBytes} bytes; send={transport.ReliableSentPackets} packets/{transport.ReliableSentBytes} bytes");
            text.AppendLine($"Unreliable: receive={transport.UnreliableReceivedPackets} packets/{transport.UnreliableReceivedBytes} bytes; send={transport.UnreliableSentPackets} packets/{transport.UnreliableSentBytes} bytes");
            text.AppendLine($"Queues: {transport.QueuedPackets} packets; outstanding leases={transport.OutstandingLeases}");
            text.AppendLine($"Failures: receive overflow={transport.ReceiveQueueOverflows}; malformed={transport.MalformedPackets}; send={transport.SendFailures}; dropped={transport.DroppedPackets}");
            text.AppendLine($"Lifecycle: disconnects={transport.Disconnects}; reconnect attempts={transport.ReconnectAttempts}; backoff={transport.ReconnectBackoffSeconds:0.###} s");
        }

        private static string Display(string value) => string.IsNullOrEmpty(value) ? "(not supplied)" : value;

        private static void FormatDirection(string label, NetworkSimulationDirectionStats stats,
            StringBuilder text)
        {
            text.AppendLine($"{label}: queued={stats.QueuedPackets}/{stats.QueuedBytes}B scheduled={stats.ScheduledPackets} delivered={stats.DeliveredPackets} lost={stats.LostPackets} overflow={stats.OverflowPackets} duplicates={stats.DuplicatePackets} reordered={stats.ReorderedPackets}");
        }
    }
}
