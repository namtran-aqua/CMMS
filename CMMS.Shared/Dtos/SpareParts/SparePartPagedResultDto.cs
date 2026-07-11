using System.Collections.Generic;

namespace CMMS.Shared.Dtos.SpareParts
{
    public class SparePartPagedResultDto
    {
        public List<SparePartDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int LowStockCount { get; set; }
    }
}
