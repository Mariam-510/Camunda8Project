using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamundaProject.Core.Models
{
    public class PublishMessageResponse
    {
        public string MessageKey { get; set; }
        public string MessageName { get; set; }
        public string CorrelationKey { get; set; }
    }
}
