using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamundaProject.Core.Models
{
    public class ProcessDefinition
    {
        public string ProcessDefinitionKey { get; set; }
        public string BpmnProcessId { get; set; }
        public string ResourceName { get; set; }
        public int Version { get; set; }
    }

}
