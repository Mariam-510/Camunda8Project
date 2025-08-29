using CamundaProject.Core.Models.RequestModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamundaProject.Core.Interfaces.Services.Camounda
{
    public interface ICamundaService
    {
        //-------------------------------------------------------------------------------------
        // Process Definition Operations (Zeebe compatible)
        //-------------------------------------------------------------------------------------
        
        Task<string> StartProcessInstanceAsync(string processDefinitionKey, VariableRequest variableRequest);
        Task DeployProcessDefinition(string resourcePath);

        //-------------------------------------------------------------------------------------
        // Process Instance Operations (Zeebe compatible)
        //-------------------------------------------------------------------------------------
       
        Task CancelProcessInstanceAsync(string processInstanceKey);

        //-------------------------------------------------------------------------------------
        // Message Operations
        //-------------------------------------------------------------------------------------

        Task PublishMessageAsync(string messageName, string correlationKey, VariableRequest variableRequest);

        //-------------------------------------------------------------------------------------
        // Utility Operations
        //-------------------------------------------------------------------------------------

        Task<string> GetTopologyAsync();
        Task<bool> TestConnectionAsync();
    }

}