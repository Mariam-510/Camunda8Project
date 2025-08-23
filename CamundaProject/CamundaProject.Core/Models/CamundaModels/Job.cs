using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamundaProject.Core.Models.CamundaModels
{
    public class Job
    {
        public long Key { get; set; }
        public string Type { get; set; }
        public long ProcessInstanceKey { get; set; }
        public string BpmnProcessId { get; set; }
        public string ElementId { get; set; }
        public int Retries { get; set; }
        public string State { get; set; } // "ACTIVATABLE", "ACTIVATED", "FAILED", "COMPLETED"
        public DateTime Timestamp { get; set; }
    }

}
