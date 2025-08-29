using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamundaProject.Core.Models.EmailModels
{
    public class EmailModel
    {
        public string To { get; set; }
        public string? Subject { get; set; }
        public string? Body { get; set; }
    }
}
