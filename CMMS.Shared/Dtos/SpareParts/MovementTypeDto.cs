using System;

namespace CMMS.Shared.Dtos.SpareParts
{
    public class MovementTypeDto
    {
        public int MovementTypeID { get; set; }
        public string MovementTypeName { get; set; } = string.Empty;
        public int? FACID { get; set; }
    }
}
