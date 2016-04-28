using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Newtonsoft.Json;
using System.Threading;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Hubs;
using System.Net.Sockets;
using System.Net;
using SnmpSharpNet;

namespace Monitoring.Hubs
{
    public class CpuData
    {
        [JsonProperty("cores")]
        public List<CpuCore> Cores { get; set; }
        //public string LastUpdatedBy { get; set; }
    }

    public class CpuCore
    {
        [JsonProperty(PropertyName = "index")]
        public int Index { get; set; }
        [JsonProperty(PropertyName = "load")]
        public int Load { get; set; }

    }

    public class CpuTicker
    {
        public CpuData _data { get; set; }
        private readonly TimeSpan _updateInterval = TimeSpan.FromMilliseconds(3000);
        private readonly Timer _timer;
        private IHubConnectionContext<dynamic> Clients { get; set; }
        public CpuTicker(IConnectionManager connectionManager)
        {
            _data = new CpuData() { Cores = new List<CpuCore>() };
            Clients = connectionManager.GetHubContext<CpuTickerHub>().Clients;
            var thread = new Thread(Listen);
            thread.Start();
            _timer = new Timer(UpdateData, null, _updateInterval, _updateInterval);
        }

        private void UpdateData(object state)
        {
            Clients.All.updateData(_data);
        }


        public void Listen()
        {

            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint ipep = new IPEndPoint(IPAddress.Any, 11000);
            EndPoint ep = (EndPoint)ipep;
            socket.Bind(ep);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 0);
            bool run = true;
            int inlen = -1;
            while (run)
            {
                byte[] indata = new byte[1024 * 1024];
                // 16KB receive buffer int inlen = 0;
                IPEndPoint peer = new IPEndPoint(IPAddress.Any, 0);
                EndPoint inep = (EndPoint)peer;
                try
                {
                    inlen = socket.ReceiveFrom(indata, ref inep);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception {0}", ex.Message);
                    inlen = -1;
                }
                if (inlen > 0)
                {
                    // Check protocol version int 
                    int ver = SnmpPacket.GetProtocolVersion(indata, inlen);
                    if (ver == (int)SnmpVersion.Ver1)
                    {
                        // Parse SNMP Version 1 TRAP packet 
                        SnmpV1TrapPacket pkt = new SnmpV1TrapPacket();
                        pkt.decode(indata, inlen);

                        _data.Cores = new List<CpuCore>();
                        for (var i = 0; i < pkt.Pdu.VbList.Count; ++i)
                        {
                            _data.Cores.Add(new CpuCore { Index = i, Load = int.Parse(pkt.Pdu.VbList[i].Value.ToString()) });
                        }
                        //foreach (Vb v in pkt.Pdu.VbList)
                        //{
                        //    Console.WriteLine("**** {0} {1}: {2}", v.Oid.ToString(), SnmpConstants.GetTypeName(v.Value.Type), v.Value.ToString());
                        //}
                        //Console.WriteLine("** End of SNMP Version 1 TRAP data.");
                    }
                    else
                    {
                        // Parse SNMP Version 2 TRAP packet 
                        SnmpV2Packet pkt = new SnmpV2Packet();
                        pkt.decode(indata, inlen);
                        Console.WriteLine("** SNMP Version 2 TRAP received from {0}:", inep.ToString());
                        if ((SnmpSharpNet.PduType)pkt.Pdu.Type != PduType.V2Trap)
                        {
                            Console.WriteLine("*** NOT an SNMPv2 trap ****");
                        }
                        else
                        {
                            _data.Cores = new List<CpuCore>();
                            for (var i = 0; i < pkt.Pdu.VbList.Count; ++i)
                            {
                                _data.Cores.Add(new CpuCore { Index = i, Load = int.Parse(pkt.Pdu.VbList[i].ToString()) });
                            }
                        }
                    }
                }
                else
                {
                    if (inlen == 0)
                        Console.WriteLine("Zero length packet received.");
                }
            }
        }
    }


}
