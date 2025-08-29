using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamundaProject.Core.Models.EmailModels
{
    public class EmailMessage
    {
        public string RequestId { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string Status { get; set; } = "pending";
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
