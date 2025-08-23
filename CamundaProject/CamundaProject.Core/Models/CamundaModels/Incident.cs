using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamundaProject.Core.Models.CamundaModels
{
    public class Incident
    {
        public long Key { get; set; }
        public string ErrorType { get; set; }
        public string ErrorMessage { get; set; }
        public long ProcessInstanceKey { get; set; }
        public string ElementId { get; set; }
        public DateTime CreatedTime { get; set; }
    }

}
