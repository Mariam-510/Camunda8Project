using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamundaProject.Core.Models.CamundaModels
{
    public class Deployment
    {
        public long Key { get; set; }
        public List<ProcessDefinition> ProcessDefinitions { get; set; } = new List<ProcessDefinition>();
        public DateTime DeployedAt { get; set; }
    }

}
