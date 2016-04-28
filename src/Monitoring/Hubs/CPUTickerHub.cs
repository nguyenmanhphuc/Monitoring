using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Monitoring.Hubs
{
    public class CpuTickerHub : Hub
    {
        private readonly CpuTicker _CpuTicker;

        public CpuTickerHub(CpuTicker ticker)
        {
            this._CpuTicker = ticker;
        }

        public CpuData getCpuData()
        {
            return _CpuTicker._data;
        }
    }
}
