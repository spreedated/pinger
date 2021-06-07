using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using evn = neXn.Pinger.EventArgs;
using snn = System.Net.NetworkInformation;

namespace neXn.Pinger
{
    public partial class Ping
    {
        public List<PingReply> PingReplies { get; } = new List<PingReply>();
        public IPAddress[] Addresses { get; private set; }

        public event EventHandler<evn.PingReportEventArgs> AsyncPingReport;
        public event EventHandler<evn.PingFinishedEventArgs> AsyncPingFinished;

        internal byte[] oct1_range;
        internal byte[] oct2_range;
        internal byte[] oct3_range;
        internal byte[] oct4_range;

        #region Constructor
        public Ping(OctetRange oct1, OctetRange oct2, OctetRange oct3, OctetRange oct4)
        {
            CalcOctettRange(oct1, ref this.oct1_range);
            CalcOctettRange(oct2, ref this.oct2_range);
            CalcOctettRange(oct3, ref this.oct3_range);
            CalcOctettRange(oct4, ref this.oct4_range);
            GenerateAddresses();
        }
        public Ping(byte oct1, OctetRange oct2, OctetRange oct3, OctetRange oct4)
        {
            this.oct1_range = new byte[] { oct1 };
            CalcOctettRange(oct2, ref this.oct2_range);
            CalcOctettRange(oct3, ref this.oct3_range);
            CalcOctettRange(oct4, ref this.oct4_range);
            GenerateAddresses();
        }
        public Ping(byte oct1, byte oct2, OctetRange oct3, OctetRange oct4)
        {
            this.oct1_range = new byte[] { oct1 };
            this.oct2_range = new byte[] { oct2 };
            CalcOctettRange(oct3, ref this.oct3_range);
            CalcOctettRange(oct4, ref this.oct4_range);
            GenerateAddresses();
        }
        public Ping(byte oct1, byte oct2, byte oct3, OctetRange oct4)
        {
            this.oct1_range = new byte[] { oct1 };
            this.oct2_range = new byte[] { oct2 };
            this.oct3_range = new byte[] { oct3 };
            CalcOctettRange(oct4, ref this.oct4_range);
            GenerateAddresses();
        }
        public Ping(byte oct1, byte oct2, byte oct3, byte oct4)
        {
            this.oct1_range = new byte[] { oct1 };
            this.oct2_range = new byte[] { oct2 };
            this.oct3_range = new byte[] { oct3 };
            this.oct4_range = new byte[] { oct4 };
            GenerateAddresses();
        }
        public Ping(IPAddress address)
        {
            this.oct1_range = new byte[] { address.GetAddressBytes()[0] };
            this.oct2_range = new byte[] { address.GetAddressBytes()[1] };
            this.oct3_range = new byte[] { address.GetAddressBytes()[2] };
            this.oct4_range = new byte[] { address.GetAddressBytes()[3] };
            GenerateAddresses();
        }
        public Ping(string address)
        {
            IPAddress iPAddress = IPAddress.Parse(address);
            this.oct1_range = new byte[] { iPAddress.GetAddressBytes()[0] };
            this.oct2_range = new byte[] { iPAddress.GetAddressBytes()[1] };
            this.oct3_range = new byte[] { iPAddress.GetAddressBytes()[2] };
            this.oct4_range = new byte[] { iPAddress.GetAddressBytes()[3] };
            GenerateAddresses();
        }
        public Ping(IEnumerable<string> addresses)
        {
            string[] p = addresses.ToArray();
            List<IPAddress> pp = new List<IPAddress>();

            for (int i = 0; i < p.Length; i++)
            {
                pp.Add(IPAddress.Parse(p[i]));
            }

            this.Addresses = pp.ToArray();
        }
        #endregion

        #region Public
        public async void RunAsync()
        {
            List<Task> t = new List<Task>();

            foreach (IPAddress a in Addresses)
            {
                t.Add(Task.Factory.StartNew(() =>
                {
                    PingReply b = PingerObject(a);
                    this.AsyncPingReport?.Invoke(this, new evn.PingReportEventArgs() { PingReply = b });
                }));
            }

            await Task.WhenAll(t.ToArray());

            this.AsyncPingFinished?.Invoke(this, new evn.PingFinishedEventArgs() { AddressCount = this.Addresses.Length });
        }

        public List<PingReply> Run()
        {
            Task[] t = new Task[this.Addresses.Length];

            for (int i = 0; i < this.Addresses.Length; i++)
            {
                int inc = i;
                t[i] = Task.Factory.StartNew(() =>
                {
                    Thread.CurrentThread.Name = $"Pinging \"{this.Addresses[inc]}\"";
                    PingerObject(this.Addresses[inc]);
                    Debug.Print($"Task for \"{this.Addresses[inc]}\" completed");
                });
            }

            Debug.Print($"Tasklist with [{t.Count()}/{t.Length}] tasks created.");

            Task.WaitAll(t);

            Debug.Print($"Tasks ran to completion [faulted/completed]: [{t.Count(x => x.Status != TaskStatus.RanToCompletion)}/{t.Count(x => x.Status == TaskStatus.RanToCompletion)}]");

            return this.PingReplies;
        }

        public void RemoveAddress(IPAddress iPAddress)
        {
            List<IPAddress> ad = this.Addresses.ToList();
            ad.Remove(iPAddress);

            this.Addresses = ad.ToArray();
        }
        public void RemoveAddresses(IPAddress[] iPAddress)
        {
            List<IPAddress> ad = this.Addresses.ToList();

            for (int i = 0; i < iPAddress.Length; i++)
            {
                ad.Remove(iPAddress[i]);
            }

            this.Addresses = ad.ToArray();
        }
        #endregion

        #region Private
        private PingReply PingerObject(IPAddress address)
        {
            snn.Ping p = new snn.Ping();
            byte[] buffer = ASCIIEncoding.ASCII.GetBytes("ThisIsAPingMessageOf32.BitLength");

            snn.PingReply pR = null;

            try
            {
                snn.PingOptions options = new snn.PingOptions(64, true);
                pR = p.Send(address, 100, buffer, options);
            }
            catch (Exception) { }

            PingReply s = new PingReply()
            {
                Address = address,
                IsAvailable = pR != null && (pR.Status == snn.IPStatus.Success)
            };

            lock (this.PingReplies)
            {
                PingReplies.Add(s);
            }

            p.Dispose();

            return s;
        }
        private static void CalcOctettRange(OctetRange range, ref byte[] oct)
        {
            var s = Math.Abs(range.From - range.To);

            List<byte> ssd = new List<byte>();

            for (int i = 0; i < s + 1; i++)
            {
                ssd.Add((byte)(range.From + i));
            }

            oct = ssd.ToArray();
        }
        private void GenerateAddresses()
        {
            StringBuilder sb = new StringBuilder();

            foreach (int it in this.oct1_range)
            {
                foreach (int it2 in this.oct2_range)
                {
                    foreach (int it3 in this.oct3_range)
                    {
                        foreach (int it4 in this.oct4_range)
                        {
                            sb.Append(it).Append('.').Append(it2).Append('.').Append(it3).Append('.').Append(it4).Append('\n'); //Fluently seems to be faster than traditional
                        }
                    }
                }
            }
            this.Addresses = sb.ToString().Split('\n').ToList().Where(x => x.Length > 1 && x.Count(y => y == '.') == 3).Select(x => IPAddress.Parse(x)).ToArray();
        }
        #endregion
    }
}
