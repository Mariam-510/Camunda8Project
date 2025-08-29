using CamundaProject.Core.Models.RestRequestModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamundaProject.Core.Interfaces.Services.Camounda
{
    public interface ICamundaRestService
    {
        Task<object> StartProcessInstanceAsync(StartProcessRequest request);
        Task<object> SearchUserTasksAsync(UserTaskSearchRequest request);
        Task<object> CompleteUserTaskAsync(string userTaskKey, CompleteUserTaskRequest request);

    }
}
