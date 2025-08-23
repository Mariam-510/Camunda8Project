using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamundaProject.Core.Models.CamundaModels
{
    public class ProcessDefinition
    {
        public string BpmnProcessId { get; set; }
        public long Version { get; set; }
        public long ProcessDefinitionKey { get; set; }
        public string ResourceName { get; set; }
        public string DeploymentId { get; set; }
    }

}
