using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamundaProject.Core.Models
{
    public class StartProcessWithResultRequest
    {
        public Dictionary<string, object> Variables { get; set; } = new();
        public int? TimeoutSeconds { get; set; }
    }

}
