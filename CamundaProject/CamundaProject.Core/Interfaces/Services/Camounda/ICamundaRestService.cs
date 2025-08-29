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
        Task<string> StartProcessInstanceAsync(StartProcessRequest request);
    }
}
