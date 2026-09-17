using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using CMMS.Data.Connection;
using CMMS.Shared.Dtos.Scanner;
using Dapper;
using System;

namespace CMMS.Server.Controllers.Scanner
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ScannerController : ControllerBase
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public ScannerController(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        [HttpGet("{barcodeId}")]
        public async Task<IActionResult> GetItemByBarcodeId(string barcodeId)
        {
            if (string.IsNullOrEmpty(barcodeId)) return BadRequest("Barcode ID is required");

            using var connection = _connectionFactory.CreateConnection();
            
            // 1. Try to find in Tbl_SparePart
            var spSql = @"
                SELECT 
                    SPID as Id, 
                    CASE WHEN IsCoded = 1 THEN 'CodedSparePart' ELSE 'SparePart' END as EntityType, 
                    SparePartBarcode as BarcodeId, 
                    PartCode as Code, 
                    PartName as Name, 
                    '' as Serial, 
                    PartModel as Specification, 
                    Inventory as Quantity, 
                    MinStock, 
                    Unit, 
                    Price, 
                    (SELECT LocName FROM Tbl_FactoryLocation WHERE LocID = Tbl_SparePart.LocID) as Location, 
                    (SELECT SupplierName FROM Tbl_SparePartSuppliers WHERE SupplierID = Tbl_SparePart.SupplierID) as Supplier, 
                    (SELECT DeptCode FROM vw_FactoryDepartment WHERE DeptID = Tbl_SparePart.DeptID) as Department, 
                    DATEDIFF(day, CreateDate, GETDATE()) as AgeDays, 
                    FORMAT(CreateDate, 'yyyy-MM-dd') as ImportDate, 
                    'Available' as Status, 
                    Note
                FROM Tbl_SparePart 
                WHERE SparePartBarcode = @barcodeId";

            var item = await connection.QuerySingleOrDefaultAsync<ScannerItemDto>(spSql, new { barcodeId });

            if (item != null)
                return Ok(item);

            // 2. Try to find in Tbl_EquipmentInfo
            var eqSql = @"
                SELECT 
                    EQID as Id, 
                    'Equipment' as EntityType, 
                    EquipmentBarcode as BarcodeId, 
                    EquipmentCode as Code, 
                    EquipmentName as Name, 
                    EquipmentSerial as Serial, 
                    EquipmentModel as Specification, 
                    1 as Quantity, 
                    1 as MinStock, 
                    'pcs' as Unit, 
                    BuyPrice as Price, 
                    (SELECT LocName FROM Tbl_FactoryLocation WHERE LocID = Tbl_EquipmentInfo.LocID) as Location, 
                    (SELECT SupplierName FROM Tbl_SparePartSuppliers WHERE SupplierID = Tbl_EquipmentInfo.VendorID) as Supplier, 
                    (SELECT DeptCode FROM vw_FactoryDepartment WHERE DeptID = Tbl_EquipmentInfo.DeptId) as Department, 
                    DATEDIFF(day, BuyDate, GETDATE()) as AgeDays, 
                    FORMAT(BuyDate, 'yyyy-MM-dd') as ImportDate, 
                    CAST(IsActive as VARCHAR) as Status, 
                    EquipmentNote as Note
                FROM Tbl_EquipmentInfo 
                WHERE EquipmentBarcode = @barcodeId";

            var eqItem = await connection.QuerySingleOrDefaultAsync<ScannerItemDto>(eqSql, new { barcodeId });

            if (eqItem != null)
            {
                eqItem.Status = eqItem.Status == "True" || eqItem.Status == "1" ? "Active" : "Inactive";
                return Ok(eqItem);
            }

            return NotFound(new { message = "Item not found" });
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateItem([FromBody] ScannerItemDto dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.BarcodeId))
                return BadRequest("Invalid data");

            using var connection = _connectionFactory.CreateConnection();

            if (dto.EntityType == "Equipment")
            {
                var sql = @"
                    UPDATE Tbl_EquipmentInfo 
                    SET 
                        EquipmentName = @Name,
                        EquipmentModel = @Specification,
                        BuyPrice = @Price,
                        EquipmentNote = @Note
                    WHERE EquipmentBarcode = @BarcodeId";
                await connection.ExecuteAsync(sql, dto);
            }
            else // SparePart / CodedSparePart
            {
                var sql = @"
                    UPDATE Tbl_SparePart 
                    SET 
                        PartName = @Name,
                        PartCode = @Code,
                        PartModel = @Specification,
                        MinStock = @MinStock,
                        Unit = @Unit,
                        Price = @Price,
                        Note = @Note
                    WHERE SparePartBarcode = @BarcodeId";
                await connection.ExecuteAsync(sql, dto);
            }

            return Ok(new { success = true });
        }
    }
}


