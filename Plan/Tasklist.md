Phase 0 - Chốt kiến trúc
- QR identity: BarcodeID
- Format QR: SP{BarcodeID} / EQ{BarcodeID}
- QR = 1 Spare Part record / 1 Equipment
- Không lưu ảnh QR trong DB
- API Architecture: .NET MAUI -> ASP.NET Core API + SQL Server

Phase 1 - Chuẩn bị Database CMMS
1.1. Spare Part
- Kiểm tra bảng Spare Part hiện tại
- Xác định Coded Part / Non-coded Part
- Xác định bảng chứa Serial Code (SP hoặc EQ)
- Thêm BarcodeID và set là UNIQUE
- Cho phép NULL trong giai đoạn migration
- Tạo index cho BarcodeID để tăng tốc độ truy vấn
- Kiểm tra relationship giữa Part Code và Serial Code
1.2. Equipment
- Kiểm tra bảng Equipment hiện tại
- Thêm BarcodeID và set là UNIQUE
- Cho phép NULL trong giai đoạn migration
- Tạo index cho BarcodeID để tăng tốc độ truy vấn
1.3. Backend
- Tạo service generate BarcodeID
- Đảm bảo BarcodeID không bao giờ bị reuse
- Tự tạo BarcodeID khi tạo Spare Part và Equipment mới
- Không thay đổi BarcodeID khi Part Code / Equipment Code thay đổi
- Tạo BarcodeID cho những Spare Part / Equipment cũ 

Phase 2 - Generate QR Image / Label
- Tạo QR Generator trên CMMS Backend
- QR chứa SP+BarcodeID / EQ+BarcodeID
- Test scan bằng điện thoại
- Tạo template label SP / EQ
┌─────────────────┐
│    QR CODE      │             
│                 │
│   SP-000123     │
│   Bearing 6204  │
└─────────────────┘
┌─────────────────┐
│    QR CODE      │
│                 │
│   EQ-000123     │
│   CNC Machine   │
└─────────────────┘
- Export PDF hàng loạt
- Print selected
- Print single QR
- Reprint QR

Phase 3 - QR Migration thực tế
- Generate QR cho Spare Part / Equipment cũ
- Kiểm tra duplicate BarcodeID
- Kiểm tra record nào còn NULL
- Export danh sách QR
- In QR hàng loạt

Phase 4 - API cho Scanner
4.1. Authentication
- ITSM login API
- Access Token
- Refresh Token
- Token expiration
- Logout
- Permission
4.2. QR Scan
- Scan
    POST /api/qr/scan
- Request:
    {
        "barcodeId": "SP-000123"
    }
- Response:
    + Spare Part
    {
        "status": "success",
        "barcodeID": "SP000123",
        "entityType": "Spare Part",
        "partCode": "000123",
        "partName": "Bearing 6204",
        "location": "Location A",
        "availableQty": 10
        "actions": [
            "VIEW",
            "EDIT",
            "EXPORT_OUTBOUND"
        ]
    }
    + Equipment
    {
        "status": "success",
        "barcodeID": "EQ000123",
        "entityType": "Equipment",
        "partCode": "000123",
        "partName": "Bearing 6204",
        "location": "Location A",
        "availableQty": 10
        "actions": [
            "VIEW",
            "EDIT",
            "EXPORT_OUTBOUND"
        ]
    }
4.3. Spare Part API
a. View
- Get Spare Part Info
b. Edit
- Edit basic info
- Không cho edit qty, remaing qty, inventory transactions, serial code
- Save
- Permission
c. Export Outbound
- Export Non-coded Spare Part
     + API Check Stock
     + Validate Quantity > 0
     + Validate Quantity <= Available Qty
     + FIFO by AGE
     + Determine inventory batches
     + Create outbound transaction
     + Deduct remaining quantity
     + Update status
     + Save Export Type
     + Save user
     + Save date/time
     + Generate Outbound No
     + Transaction rollback if failed
- Export Coded Spare Part
    + Get available serial list
    + Search serial
    + Validate selected Serial
    + Validate Serial status = Available
    + Qty automatically = 1
    + Save Export Type
    + Create Outbound
    + Update Serial status
    + Generate Outbound No
4.4. Equipment API
a. View
- Get Equipment Info
b. Edit
- Edit basic info
- Save
- Permission
c. Maintenance
- Create Maintenance Record
    + Maintenance Type
    + Description
    + Date
    + Cost
    + User/Technician
    + Save
- Permission

Phase 5 - Mobile App .NET MAUI
5.1. Project
- Create .NET MAUI project
- Android target
- Configure API URL
- Configure authentication
- Configure secure token storage
5.2. Login
- Login = tài khoản ITSM
- Login UI
- Login API
- Token management
- Logout

Phase 6 - Scanner & UI
6.1. Home Screen
- Home
- Scan button
- Username
- Logout/Profile nếu cần
6.2. QR Scanner
- Camera permission
- QR Scanner
- Detect QR
- Parse BarcodeID
- Call /api/qr/scan
- Loading
- Invalid QR
- QR not found
- Unauthorized
- Network error
Fallback
- Manual ID input
- Upload image -> QR recognition

Phase 7 - Mobile Non-Coded Part UI
7.1. View
- Part Code
- Part Name
- Import Date
- Import Qty
- Remaining Qty
- Age
- Status
- Edit button
- Export Outbound button
7.2. Export
- Type = Export Purpose
- Qty input
- Stock validation
- Confirm
- Success

Phase 8 - Mobile Coded Part UI
8.1. View
- Coded Part View
- Total Qty
- Available Qty
- Serial List
- Search Serial
- Scroll
- Pagination / Lazy Loading
- Serial Status
- Edit button
- Export button
8.2. Export
- Serial Search
- Serial Selection
- Qty fixed = 1
- Export Type
- Confirm
- Success

Phase 9 - Mobile Equipment UI
9.1. View
- Equipment Code
- Equipment Name
- QR Code
- Asset No
- Status
- Edit button
- Maintenance button
9.2. Edit
- Edit basic info
- Save
- No inventory field
9.3. Maintenance
- Input
- Validation
- Save
- Success

Phase 10 -  Testing
10.1. QR
- Existing Spare Part QR
- Existing Equipment QR
- New Spare Part QR
- New Equipment QR
- Part Code changed → QR still works
- Equipment Code changed → QR still works
- Duplicate BarcodeID
- Invalid BarcodeID
- Retired/deleted record
10.2. Spare Part
- Non-Coded View
- Non-Coded Edit
- Coded View
- Serial Search
- Serial Scroll
- Coded Edit
- Quantity cannot be edited
10.3. Outbound
- Non-Coded export
- Insufficient stock
- FIFO
- Multiple AGE batches
- Coded export
- Serial unavailable
- Serial already exported
- Qty always 1
- Export Type
- Cancel
- Confirm
- Duplicate/concurrent export
10.4. Equipment
- View
- Edit
- Maintenance
- Save
- No Export button
10.5. Mobile
- Login
- Scan
- Manual input
- Upload image
- Network error
- API timeout
- Permission
- Token expiration
- APK installation
