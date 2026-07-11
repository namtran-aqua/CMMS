using System.Collections.Generic;

namespace CMMS.Shared.Dtos.Common
{
    public class PagedResultDto<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
    }
}
