namespace UniGame.StaticEcs.Network.Profiler
{
    /// <summary>Contains one immutable, payload-free snapshot of endpoint transport diagnostics.</summary>
    public readonly struct NetworkTransportDebugData
    {
        /// <summary>Value used for optional native diagnostics that the active transport cannot expose.</summary>
        public const int Unavailable = -1;

        /// <summary>Creates one transport diagnostics snapshot.</summary>
        public NetworkTransportDebugData(bool available, string driver, string endpoint, string state,
            long reliableReceivedPackets, long reliableReceivedBytes,
            long reliableSentPackets, long reliableSentBytes,
            long unreliableReceivedPackets, long unreliableReceivedBytes,
            long unreliableSentPackets, long unreliableSentBytes,
            int queuedPackets, int outstandingLeases, long receiveQueueOverflows,
            long sendFailures, long malformedPackets, long droppedPackets, long disconnects,
            long reconnectAttempts, double reconnectBackoffSeconds,
            int pendingReliablePackets = Unavailable,
            long pendingReliableBytes = Unavailable,
            int pendingReliablePacketsHighWater = Unavailable,
            long pendingReliableBytesHighWater = Unavailable,
            long reliableSendQueueOverflows = Unavailable,
            int nativeReliableFragments = Unavailable,
            long nativeReliableBytes = Unavailable,
            int nativeReliableFragmentsHighWater = Unavailable,
            long nativeReliableBytesHighWater = Unavailable,
            int nativeReliableQueuePackets = Unavailable,
            int nativePacketPoolCount = Unavailable,
            int nativePacketPoolCapacity = Unavailable,
            int nativePacketPoolLowWater = Unavailable,
            long deliveryCallbacks = Unavailable)
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
            PendingReliablePackets = pendingReliablePackets;
            PendingReliableBytes = pendingReliableBytes;
            PendingReliablePacketsHighWater = pendingReliablePacketsHighWater;
            PendingReliableBytesHighWater = pendingReliableBytesHighWater;
            ReliableSendQueueOverflows = reliableSendQueueOverflows;
            NativeReliableFragments = nativeReliableFragments;
            NativeReliableBytes = nativeReliableBytes;
            NativeReliableFragmentsHighWater = nativeReliableFragmentsHighWater;
            NativeReliableBytesHighWater = nativeReliableBytesHighWater;
            NativeReliableQueuePackets = nativeReliableQueuePackets;
            NativePacketPoolCount = nativePacketPoolCount;
            NativePacketPoolCapacity = nativePacketPoolCapacity;
            NativePacketPoolLowWater = nativePacketPoolLowWater;
            DeliveryCallbacks = deliveryCallbacks;
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

        /// <summary>Gets the current pending reliable packets awaiting native transmission, or <see cref="Unavailable"/>.</summary>
        public int PendingReliablePackets { get; }

        /// <summary>Gets the current pending reliable bytes awaiting native transmission, or <see cref="Unavailable"/>.</summary>
        public long PendingReliableBytes { get; }

        /// <summary>Gets the high-water pending reliable packet count observed by the transport, or <see cref="Unavailable"/>.</summary>
        public int PendingReliablePacketsHighWater { get; }

        /// <summary>Gets the high-water pending reliable byte count observed by the transport, or <see cref="Unavailable"/>.</summary>
        public long PendingReliableBytesHighWater { get; }

        /// <summary>Gets cumulative reliable send queue overflow events, or <see cref="Unavailable"/>.</summary>
        public long ReliableSendQueueOverflows { get; }

        /// <summary>Gets the current native reliable fragment count, or <see cref="Unavailable"/>.</summary>
        public int NativeReliableFragments { get; }

        /// <summary>Gets the current native reliable fragment bytes, or <see cref="Unavailable"/>.</summary>
        public long NativeReliableBytes { get; }

        /// <summary>Gets the high-water native reliable fragment count observed by the transport, or <see cref="Unavailable"/>.</summary>
        public int NativeReliableFragmentsHighWater { get; }

        /// <summary>Gets the high-water native reliable fragment bytes observed by the transport, or <see cref="Unavailable"/>.</summary>
        public long NativeReliableBytesHighWater { get; }

        /// <summary>Gets the current native reliable queue packet count, or <see cref="Unavailable"/>.</summary>
        public int NativeReliableQueuePackets { get; }

        /// <summary>Gets the current native packet pool count, or <see cref="Unavailable"/>.</summary>
        public int NativePacketPoolCount { get; }

        /// <summary>Gets the native packet pool capacity, or <see cref="Unavailable"/>.</summary>
        public int NativePacketPoolCapacity { get; }

        /// <summary>Gets the native packet pool low-water mark, or <see cref="Unavailable"/> when never sampled.</summary>
        public int NativePacketPoolLowWater { get; }

        /// <summary>Gets cumulative native delivery callback events, or <see cref="Unavailable"/>.</summary>
        public long DeliveryCallbacks { get; }
    }
}
