using CamundaProject.Core.Models.CamundaModels;
using CamundaProject.Core.Models.RequestModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamundaProject.Core.Interfaces.Services
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

        //-------------------------------------------------------------------------------------
        // Job Functions
        //-------------------------------------------------------------------------------------

        Task<JobCompletionResult> CompleteJobAsync(long jobKey, Dictionary<string, object> variables);
        Task<bool> FailJobAsync(long jobKey, string errorMessage, int retries = 3);
        List<ActiveJob> GetActiveJobs(long? processInstanceKey = null, string jobType = null);
    }

}