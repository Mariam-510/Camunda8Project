using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CamundaProject.Core.Models.RestCamundaModels
{
    public class ProcessInstanceResponse
    {
        [JsonPropertyName("processInstanceKey")]
        public long ProcessInstanceKey { get; set; }

        [JsonPropertyName("bpmnProcessId")]
        public string BpmnProcessId { get; set; } = string.Empty;

        [JsonPropertyName("version")]
        public int Version { get; set; }

        [JsonPropertyName("variables")]
        public Dictionary<string, object>? Variables { get; set; }
    }
}