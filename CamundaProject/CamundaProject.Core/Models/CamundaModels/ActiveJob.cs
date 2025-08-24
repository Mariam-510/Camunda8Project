using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamundaProject.Core.Models.CamundaModels
{
    public class ActiveJob
    {
        public long JobKey { get; set; }
        public long ProcessInstanceKey { get; set; }
        public string JobType { get; set; }
        public string ElementId { get; set; }
        public Dictionary<string, object> Variables { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = "Active"; // Active, Completed, Failed
    }
}