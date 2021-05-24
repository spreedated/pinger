using static neXn.Pinger.Ping;

namespace neXn.Pinger.EventArgs
{
    public class PingReportEventArgs : System.EventArgs
    {
        public PingReply PingReply { get; set; }
    }
}
