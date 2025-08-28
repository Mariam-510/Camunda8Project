using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamundaProject.Core.Models.RestRequestModels
{
    public class StartInstruction
    {
        public string Type { get; set; } = "startBeforeElement";
        public string ElementId { get; set; } = string.Empty;
        public Dictionary<string, object>? Variables { get; set; }
    }
}
