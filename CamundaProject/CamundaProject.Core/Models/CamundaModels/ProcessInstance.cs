using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamundaProject.Core.Models.CamundaModels
{
    public class ProcessInstance
    {
        public long ProcessInstanceKey { get; set; }
        public string BpmnProcessId { get; set; }
        public long Version { get; set; }
        public string State { get; set; } // "ACTIVE", "COMPLETED", "CANCELED"
        public DateTime StartTime { get; set; }
    }

}
