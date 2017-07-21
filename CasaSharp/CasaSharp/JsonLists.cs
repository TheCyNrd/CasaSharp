using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CasaSharp
{
    public class List
    {
        public string macAddress { get; set; }
        public string addressCode { get; set; }
        public int type { get; set; }
        public string authCode { get; set; }
        public string companyCode { get; set; }
        public string deviceType { get; set; }
        public string deviceName { get; set; }
        public string imageName { get; set; }
        public object lastOperation { get; set; }
        public int orderNumber { get; set; }
    }

    public class RootObject
    {
        public List<List> list { get; set; }
        public bool success { get; set; }
    }
}
