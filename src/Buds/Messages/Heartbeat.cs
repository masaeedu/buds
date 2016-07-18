using System;

namespace Buds.Messages
{
    public class Heartbeat : Message
    {
        public Heartbeat(Guid senderNodeId, long sequence, int pubPort, DateTime dispatchTime, string hostName) : base(senderNodeId)
        {
            PubPort = pubPort;
            DispatchTime = dispatchTime;
            HostName = hostName;
            Sequence = sequence;
        }

        public long Sequence { get; }
        public DateTime DispatchTime { get; }
        public int PubPort { get; }
        public string HostName { get; }
    }
}
