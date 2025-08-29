using CamundaProject.Core.Models.RequestModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamundaProject.Core.Models.RestRequestModels
{
    public class StartProcessRequest
    {
        public string? ProcessDefinitionKey { get; set; }
        public string? ProcessDefinitionId { get; set; }
        public int? Version { get; set; }
        public VariableRequest VariableRequest { get; set; } = new VariableRequest();
    }

}