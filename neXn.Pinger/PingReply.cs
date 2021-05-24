using System.Net;

namespace neXn.Pinger
{
    public partial class Ping
    {
        public struct PingReply
        {
            public IPAddress Address { get; set; }
            public bool IsAvailable { get; set; }
        }
    }
}
