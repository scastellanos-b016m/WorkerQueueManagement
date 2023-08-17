using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace WorkerQueueManagement.Models
{
    public class AppLog
    {
        public AppLog()
        {
            IPAddress[] address = Dns.GetHostEntry(this.HostName).AddressList;
        }

        public string Name { get; set; } = "WorkerQueueManagement";
        public string HostName { get; set; } = Dns.GetHostName();
        public string ApiKey { get; set; } = "N/A";
        public string Uri { get; set; } = "N/A";
        public int ResponseCode { get; set; }
        public long ResponseTime { get; set; }
        public string Ip { get; set; }
        public int Level { get; set; }
        public string Message { get; set; }
        public string DateTime { get; set; }
        public string Version { get; set; } = "v1";

    }
}