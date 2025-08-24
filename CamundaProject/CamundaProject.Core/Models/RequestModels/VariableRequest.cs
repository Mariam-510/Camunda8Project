using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamundaProject.Core.Models.RequestModels
{
    public class VariableRequest
    {
        public Dictionary<string, object> Variables { get; set; } = new Dictionary<string, object>();
    }

}
