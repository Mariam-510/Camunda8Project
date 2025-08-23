using CamundaProject.Core.Models.CamundaModels;
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
        
        Task<string> StartProcessInstanceAsync(string processDefinitionKey, Dictionary<string, object> variables);
        Task DeployProcessDefinition(string resourcePath);
        Task<List<ProcessDefinition>> GetProcessDefinitionsAsync(); // Empty
        Task<ProcessDefinition> GetProcessDefinitionByKeyAsync(string key); // Empty
        Task<List<Deployment>> GetDeploymentsAsync(); // Empty

        //-------------------------------------------------------------------------------------
        // Process Instance Operations (Zeebe compatible)
        //-------------------------------------------------------------------------------------
       
        Task CancelProcessInstanceAsync(string processInstanceKey);
        Task<List<ProcessInstance>> GetProcessInstancesAsync(); // Empty - Limited in Zeebe 

        //-------------------------------------------------------------------------------------
        // Job Operations (Zeebe equivalent of tasks)
        //-------------------------------------------------------------------------------------

        Task<List<Job>> GetActiveJobsAsync(string jobType = null); // Empty
        Task CompleteJobAsync(string jobKey, Dictionary<string, object> variables); // Empty
        Task FailJobAsync(string jobKey, string errorMessage); // Empty

        //-------------------------------------------------------------------------------------
        // Message Operations
        //-------------------------------------------------------------------------------------

        Task PublishMessageAsync(string messageName, string correlationKey, Dictionary<string, object> variables);

        //-------------------------------------------------------------------------------------
        // Incident Operations (Limited in Zeebe without Operate)
        //-------------------------------------------------------------------------------------

        Task<List<Incident>> GetIncidentsAsync(); // Empty

        //-------------------------------------------------------------------------------------
        // Utility Operations
        //-------------------------------------------------------------------------------------

        Task<string> GetTopologyAsync();
        Task<bool> TestConnectionAsync();
    }

}