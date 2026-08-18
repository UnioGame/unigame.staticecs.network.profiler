namespace UniGame.StaticEcs.Network.Profiler
{
    /// <summary>Contains one immutable, payload-free snapshot of endpoint transport diagnostics.</summary>
    public readonly struct NetworkTransportDebugData
    {
        /// <summary>Creates one transport diagnostics snapshot.</summary>
        public NetworkTransportDebugData(bool available, string driver, string endpoint, string state,
            long reliableReceivedPackets, long reliableReceivedBytes,
            long reliableSentPackets, long reliableSentBytes,
            long unreliableReceivedPackets, long unreliableReceivedBytes,
            long unreliableSentPackets, long unreliableSentBytes,
            int queuedPackets, int outstandingLeases, long receiveQueueOverflows,
            long sendFailures, long malformedPackets, long droppedPackets, long disconnects,
            long reconnectAttempts, double reconnectBackoffSeconds)
        {
            Available = available;
            Driver = driver ?? string.Empty;
            Endpoint = endpoint ?? string.Empty;
            State = state ?? string.Empty;
            ReliableReceivedPackets = reliableReceivedPackets;
            ReliableReceivedBytes = reliableReceivedBytes;
            ReliableSentPackets = reliableSentPackets;
            ReliableSentBytes = reliableSentBytes;
            UnreliableReceivedPackets = unreliableReceivedPackets;
            UnreliableReceivedBytes = unreliableReceivedBytes;
            UnreliableSentPackets = unreliableSentPackets;
            UnreliableSentBytes = unreliableSentBytes;
            QueuedPackets = queuedPackets;
            OutstandingLeases = outstandingLeases;
            ReceiveQueueOverflows = receiveQueueOverflows;
            SendFailures = sendFailures;
            MalformedPackets = malformedPackets;
            DroppedPackets = droppedPackets;
            Disconnects = disconnects;
            ReconnectAttempts = reconnectAttempts;
            ReconnectBackoffSeconds = reconnectBackoffSeconds;
        }

        /// <summary>Gets whether transport diagnostics are currently available.</summary>
        public bool Available { get; }

        /// <summary>Gets the transport driver label.</summary>
        public string Driver { get; }

        /// <summary>Gets the configured or connected endpoint label.</summary>
        public string Endpoint { get; }

        /// <summary>Gets the current transport lifecycle state.</summary>
        public string State { get; }

        /// <summary>Gets cumulative reliable packets received.</summary>
        public long ReliableReceivedPackets { get; }

        /// <summary>Gets cumulative reliable bytes received.</summary>
        public long ReliableReceivedBytes { get; }

        /// <summary>Gets cumulative reliable packets sent.</summary>
        public long ReliableSentPackets { get; }

        /// <summary>Gets cumulative reliable bytes sent.</summary>
        public long ReliableSentBytes { get; }

        /// <summary>Gets cumulative unreliable packets received.</summary>
        public long UnreliableReceivedPackets { get; }

        /// <summary>Gets cumulative unreliable bytes received.</summary>
        public long UnreliableReceivedBytes { get; }

        /// <summary>Gets cumulative unreliable packets sent.</summary>
        public long UnreliableSentPackets { get; }

        /// <summary>Gets cumulative unreliable bytes sent.</summary>
        public long UnreliableSentBytes { get; }

        /// <summary>Gets the current number of queued receive packets.</summary>
        public int QueuedPackets { get; }

        /// <summary>Gets the number of receive leases owned outside the transport pool.</summary>
        public int OutstandingLeases { get; }

        /// <summary>Gets cumulative receive queue overflow events.</summary>
        public long ReceiveQueueOverflows { get; }

        /// <summary>Gets cumulative send failures.</summary>
        public long SendFailures { get; }

        /// <summary>Gets cumulative malformed packets rejected by the transport.</summary>
        public long MalformedPackets { get; }

        /// <summary>Gets cumulative packets and lifecycle notifications dropped by bounds or rejection.</summary>
        public long DroppedPackets { get; }

        /// <summary>Gets cumulative observed transport disconnects.</summary>
        public long Disconnects { get; }

        /// <summary>Gets cumulative reconnect attempts.</summary>
        public long ReconnectAttempts { get; }

        /// <summary>Gets the current reconnect backoff in seconds.</summary>
        public double ReconnectBackoffSeconds { get; }
    }
}
