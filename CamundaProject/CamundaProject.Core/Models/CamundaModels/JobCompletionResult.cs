using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamundaProject.Core.Models.CamundaModels
{
    public class JobCompletionResult
    {
        public bool Success { get; set; }
        public long JobKey { get; set; }
        public long ProcessInstanceKey { get; set; }
        public string Message { get; set; }
    }
}
