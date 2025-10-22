using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobotServer.Models
{
    public class OperationLog
    {
        public int Id { get; set; }
        public string ApiKey { get; set; }
        public string Payload { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
