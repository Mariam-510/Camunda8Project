using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamundaProject.Core.Models.RestRequestModels
{
    public class UserTaskSearchRequest
    {
        public UserTaskFilter? Filter { get; set; }
        public List<SortField>? Sort { get; set; }
        public PageInfo? Page { get; set; }
    }

    public class UserTaskFilter
    {
        public string? State { get; set; }
        public string? Assignee { get; set; }
        public string? ElementId { get; set; }
        public string? CandidateGroup { get; set; }
        public string? CandidateUser { get; set; }
        public string? TenantIds { get; set; }
        public string? ProcessDefinitionId { get; set; }
        public string? Key { get; set; }
        public string? ProcessDefinitionKey { get; set; }
        public string? ProcessInstanceKey { get; set; }
    }

    public class SortField
    {
        public string? Field { get; set; }
        public string? Order { get; set; } // "asc" or "desc"
    }

    public class PageInfo
    {
        public int? From { get; set; }
        public int? Limit { get; set; }
        public List<object>? SearchAfter { get; set; }
        public List<object>? SearchBefore { get; set; }
    }
}