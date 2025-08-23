using CamundaProject.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamundaProject.Core.Interfaces.Services
{
    public interface ICamundaService
    {
        Task<string> StartProcessInstanceAsync(string processDefinitionKey, Dictionary<string, object> variables);
        Task DeployProcessDefinition(string resourcePath);
        Task CancelProcessInstanceAsync(string processInstanceKey);
        Task PublishMessageAsync(string messageName, string correlationKey, Dictionary<string, object> variables);
        Task<string> GetTopologyAsync();
    }

}
