using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamundaProject.Core.Models.RestRequestModels
{
    public class CompleteUserTaskRequest
    {
        public Dictionary<string, object>? Variables { get; set; }
        public string? Action { get; set; }
    }
    
}
