# TASK LIST -- QR CODE MANAGEMENT & BARCODE ID

## 0. Mục tiêu

Xây dựng chức năng **QR Code Management** trên CMMS Website để:

-   Cấp `BarcodeID` cho Spare Part, Coded Spare Part và Equipment đang
    tồn tại trong database.
-   Tự động generate QR Code dựa trên `BarcodeID`.
-   Generate label gồm QR Code + BarcodeID + tên item.
-   Chọn nhiều item và export QR/label hàng loạt thành PDF để in và dán.
-   Reprint QR/label của item đã có BarcodeID.
-   Đảm bảo BarcodeID là identity cố định của record và không bị thay
    đổi trong lifecycle.
-   Record mới tự động được cấp BarcodeID.
-   Scanner App dùng BarcodeID để scan và truy vấn item qua API.

------------------------------------------------------------------------

# 1. BARCODE ID STANDARD

## 1.1. Nguyên tắc

`BarcodeID` là identity riêng của hệ thống CMMS.

BarcodeID phải:

-   UNIQUE.
-   Không được reuse.
-   Không thay đổi trong suốt lifecycle của record.
-   Không phụ thuộc Part Code.
-   Không phụ thuộc Serial Code.
-   Không phụ thuộc Equipment Code.
-   Không cho user nhập thủ công khi tạo record.
-   Không dùng SQL ID làm BarcodeID.
-   Không dùng một BarcodeID cho nhiều coded items.

## 1.2. Cấu trúc BarcodeID

BarcodeID phải bao gồm **Company + Department + Item Type + Sequential Number**.

Cấu trúc:

```text
VF + DEPARTMENT + TYPE + 6 DIGITS
```

Trong đó:

```text
VF = VietNam Factory
MNT = Maintenance
```

### Maintenance – Coded Spare Part

```text
VFMNTCP000001
VFMNTCP000002
VFMNTCP000003
```

Cấu trúc:

```text
VF + MNT + CP + 6 digits
```

`CP` = Coded Spare Part.

### Maintenance – Non-Coded Spare Part

```text
VFMNTNCP000001
VFMNTNCP000002
VFMNTNCP000003
```

Cấu trúc:

```text
VF + MNT + NCP + 6 digits
```

`NCP` = Non-Coded Spare Part.

### Maintenance – Equipment

```text
VFMNTEQ000001
VFMNTEQ000002
VFMNTEQ000003
```

Cấu trúc:

```text
VF + MNT + EQ + 6 digits
```

`EQ` = Equipment.

Người dùng nhìn BarcodeID có thể nhận biết:

```text
VFMNTCP...  → Maintenance / Coded Spare Part
VFMNTNCP... → Maintenance / Non-Coded Spare Part
VFMNTEQ...  → Maintenance / Equipment
```

### Department mở rộng trong tương lai

Cấu trúc phải hỗ trợ các Department khác ngoài Maintenance.

Ví dụ Department = IT:

```text
VFITCP000001
VFITNCP000001
VFITEQ000001
```

Không hard-code `MNT` vào logic BarcodeID. Department phải là giá trị cấu hình/dữ liệu của hệ thống.

BarcodeID không chứa Part Code, Equipment Code, Serial Code hoặc tên item.

## 1.3. Sequential Number

Sequential number phải:

-   Tăng dần.
-   Không reuse số đã từng được cấp.
-   Không phụ thuộc SQL `ID`.
-   Không dùng `MAX(ID) + 1` trực tiếp nếu có concurrent request.
-   Được backend generate.
-   Được database bảo vệ bằng UNIQUE constraint.

------------------------------------------------------------------------

# 2. QR CODE CONTENT STANDARD

QR Code chỉ chứa đúng `BarcodeID`.

Ví dụ:

``` text
VFEQ000001
```

Không chứa:

``` text
Equipment Code
Equipment Name
Serial
Part Code
Part Name
Location
JSON
URL
```

Flow:

``` text
Scan QR
    ↓
VFEQ000001
    ↓
API
    ↓
Find BarcodeID
    ↓
Return current item
```

------------------------------------------------------------------------

# 3. QR LABEL STANDARD

QR Code và QR Label là hai khái niệm khác nhau.

QR bên trong chỉ chứa BarcodeID.

Label in ra gồm:

``` text
┌──────────────────────────────┐
│                              │
│          QR CODE             │
│                              │
│        VFEQ000001            │
│                              │
│  THERMO FORMING MACHINE No1  │
│                              │
└──────────────────────────────┘
```

## Equipment Label

Tối thiểu:

-   QR Code.
-   BarcodeID.
-   Equipment Name.

Có thể hiển thị thêm:

-   Equipment Code.
-   Serial.

Ví dụ:

``` text
┌──────────────────────────────┐
│          QR CODE             │
│                              │
│        VFEQ000001            │
│                              │
│  THERMO FORMING MACHINE No1  │
│  EQ000001 | SN: 120000000354 │
└──────────────────────────────┘
```

Mục đích là người đi dán có thể đối chiếu label với thiết bị thực tế.

## Non-Coded Spare Part Label

``` text
┌──────────────────────────────┐
│          QR CODE             │
│                              │
│        VFNS000001            │
│                              │
│  ĐẦU ĐỌT / STRAIGHT FITTING  │
└──────────────────────────────┘
```

Có thể hiển thị Part Code.

## Coded Spare Part Label

``` text
┌──────────────────────────────┐
│          QR CODE             │
│                              │
│        VFCS000001            │
│                              │
│          Súng keo            │
│  ITT004 | SN: 1612352        │
└──────────────────────────────┘
```

Mỗi coded item/serial có một BarcodeID riêng.

Ví dụ:

``` text
ITT001 / 12512512 → VFCS000001
ITT004 / 1612352  → VFCS000002
ITT004 / 123      → VFCS000003
```

Không generate một BarcodeID cho toàn bộ Part Code.

------------------------------------------------------------------------

# 4. DATABASE CHANGES

## 4.1. Non-Coded Spare Part

Thêm field:

``` text
BarcodeID
```

Requirements:

-   Nullable trong giai đoạn triển khai ban đầu.
-   UNIQUE.
-   Indexed.
-   Không thay đổi khi Part Code thay đổi.
-   Không cho user edit trực tiếp.

## 4.2. Coded Spare Part

BarcodeID phải nằm ở record của từng coded item / serial.

Ví dụ:

``` text
Part Code  Serial     BarcodeID
ITT004     1612352    VFCS000001
ITT004     123        VFCS000002
ITT004     456        VFCS000003
```

Requirements:

-   Mỗi coded item = một BarcodeID.
-   BarcodeID UNIQUE.
-   Không group theo Part Code.
-   Không dùng Serial Code làm BarcodeID.
-   Không cho user edit BarcodeID.

## 4.3. Equipment

Thêm:

``` text
BarcodeID
```

Requirements:

-   UNIQUE.
-   Indexed.
-   Một Equipment = một BarcodeID.
-   Không thay đổi khi Equipment Code thay đổi.
-   Không cho user edit trực tiếp.

------------------------------------------------------------------------

# 5. BARCODE ID GENERATION SERVICE

Tạo backend service chịu trách nhiệm generate BarcodeID.

Ví dụ:

``` text
BarcodeIdService
```

Các chức năng:

``` text
GenerateEquipmentBarcodeId()
GenerateCodedSparePartBarcodeId()
GenerateNonCodedSparePartBarcodeId()
```

Service phải đảm bảo:

-   Không duplicate.
-   Không reuse.
-   Thread/concurrency safe.
-   Không phụ thuộc SQL ID.
-   Không cho client tự truyền BarcodeID khi create.
-   Database UNIQUE constraint là lớp bảo vệ cuối cùng.

------------------------------------------------------------------------

# 6. EXISTING RECORDS -- GENERATE BARCODE ID

Đây là chức năng của menu:

``` text
QR Code Management
```

Không gọi đây là migration.

Các record hiện tại có thể đang:

``` text
BarcodeID = NULL
```

User vào:

``` text
QR Code Management
    ↓
Generate QR Code
```

Hệ thống hiển thị các record chưa có BarcodeID.

Ví dụ:

``` text
☐ EQ000001  THERMO FORMING MACHINE No1
☐ EQ000002  THERMO FORMING MACHINE No2
☐ EQ000003  CNC MACHINE No1
```

User chọn item:

``` text
[ Generate Barcode ID ]
```

Backend cấp:

``` text
EQ000001 → VFEQ000001
EQ000002 → VFEQ000002
EQ000003 → VFEQ000003
```

Sau khi cấp, BarcodeID được lưu vào database.

------------------------------------------------------------------------

# 7. EXISTING ITEM MAPPING

Khi generate BarcodeID cho existing records:

-   BarcodeID phải được cấp trực tiếp cho đúng record được chọn.
-   Không tạo danh sách QR rời rồi cho người dùng dán tự do.
-   Không yêu cầu người dùng mapping QR với item sau đó.
-   Label phải được generate từ chính record trong database.

Ví dụ:

``` text
EQ000001
    ↓
VFEQ000001
    ↓
QR Label
    ↓
THERMO FORMING MACHINE No1
```

Khi export PDF, label phải giữ nguyên mapping này.

------------------------------------------------------------------------

# 8. GENERATE QR CODE

Sau khi record đã có BarcodeID:

``` text
Generate QR
```

không được tạo BarcodeID mới.

Ví dụ:

``` text
EQ000001 → VFEQ000001
```

Nếu user Generate QR nhiều lần, BarcodeID vẫn là:

``` text
VFEQ000001
```

Chỉ generate lại QR image/label.

Không được:

``` text
EQ000001 → VFEQ000002
```

------------------------------------------------------------------------

# 9. QR CODE MANAGEMENT UI

Tạo menu:

``` text
QR Code Management
```

Có thể gồm:

``` text
Generate QR Code
Reprint QR
```

## Generate QR Code page

Hiển thị:

-   Entity Type.
-   Search.
-   Barcode Status.
-   Item list.
-   Checkbox selection.
-   BarcodeID.
-   Item Code.
-   Item Name.
-   Serial nếu là coded.
-   Status.

Filter:

``` text
Entity Type:
[ All ]

Barcode Status:
[ Not Generated ]

Search:
[ Part Code / Equipment Code / Name / Serial ]
```

Item list:

``` text
☐ Barcode ID    Type          Code       Name
☐ -             Equipment     EQ000001   THERMO FORMING MACHINE No1
☐ -             Equipment     EQ000002   THERMO FORMING MACHINE No2
☐ -             Coded SP      ITT004     Súng keo
☐ -             Non-Coded SP  RAR0001    STRAIGHT FITTING
```

Buttons:

``` text
[ Generate Barcode ID ]
```

Sau khi BarcodeID đã tồn tại:

``` text
[ Generate QR ]
[ Export PDF ]
```

------------------------------------------------------------------------

# 10. BULK GENERATION

Cho phép chọn nhiều record.

Ví dụ:

``` text
5 Equipment
10 Coded Spare Parts
20 Non-Coded Spare Parts
```

User chọn tất cả:

``` text
[ Generate Barcode ID ]
```

Backend cấp BarcodeID cho từng record.

Requirements:

-   Không duplicate.
-   Record đã có BarcodeID thì không cấp lại.
-   Xử lý transaction phù hợp.
-   Return danh sách kết quả.

Ví dụ:

``` text
Generated: 35
Skipped: 2
Failed: 0
```

------------------------------------------------------------------------

# 11. EXPORT PDF

Flow:

``` text
Select items
    ↓
Generate QR
    ↓
Preview
    ↓
Export PDF
```

PDF phải giữ đúng mapping:

``` text
QR
BarcodeID
Name
Code / Serial nếu cần
```

Ví dụ:

``` text
Label 1
VFEQ000001
THERMO FORMING MACHINE No1
EQ000001

Label 2
VFEQ000002
THERMO FORMING MACHINE No2
EQ000002

Label 3
VFCS000001
Súng keo
ITT004 | 1612352
```

Không regenerate BarcodeID chỉ vì export PDF.

------------------------------------------------------------------------

# 12. REPRINT

Nếu BarcodeID đã tồn tại:

``` text
VFEQ000001
```

cho phép:

``` text
Reprint QR
```

Reprint phải dùng chính BarcodeID hiện tại.

Không tạo BarcodeID mới.

------------------------------------------------------------------------

# 13. NEW RECORD FLOW

Sau khi tính năng được triển khai, record mới phải tự động được cấp
BarcodeID.

## Equipment

``` text
Add Equipment
    ↓
Save
    ↓
Backend Generate VFEQxxxxxx
    ↓
Save DB
```

## Coded Spare Part

``` text
Create Coded Item / Serial
    ↓
Backend Generate VFCSxxxxxx
    ↓
Save DB
```

## Non-Coded Spare Part

``` text
Create Spare Part / Inventory Record
    ↓
Backend Generate VFNSxxxxxx
    ↓
Save DB
```

User không nhập BarcodeID.

------------------------------------------------------------------------

# 14. BARCODE LIFECYCLE

``` text
Create record
    ↓
Generate BarcodeID
    ↓
Generate QR
    ↓
Print
    ↓
Attach to item
    ↓
Scan
    ↓
View / Edit / Outbound / Maintenance
```

Nếu thay đổi:

``` text
Part Code
Part Name
Equipment Code
Equipment Name
Serial
Location
```

BarcodeID không thay đổi.

Nếu reprint QR:

``` text
Same BarcodeID
```

Nếu item retired/deleted:

-   Không reuse BarcodeID.
-   BarcodeID không được cấp lại cho item khác.

------------------------------------------------------------------------

# 15. SCANNER APP INTEGRATION

Scanner App không tự generate BarcodeID.

Scanner App:

``` text
Scan QR
    ↓
Read BarcodeID
    ↓
Call API
    ↓
Find record
```

Ví dụ:

``` text
POST /api/qr/scan
```

Request:

``` json
{
  "barcodeId": "VFEQ000001"
}
```

Backend trả về entity tương ứng.

Không để Scanner App tự quyết định entity chỉ dựa trên prefix. Prefix
chỉ phục vụ human-readable; backend vẫn phải xác định record thực tế từ
database.

------------------------------------------------------------------------

# 16. LABEL VERIFICATION / PHYSICAL DEPLOYMENT

Đối với item hiện hữu:

``` text
Existing DB Record
        ↓
Generate BarcodeID
        ↓
Generate QR Label từ record đó
        ↓
Export PDF
        ↓
In Label
        ↓
Đối chiếu Code / Name / Serial
        ↓
Dán lên đúng item
        ↓
Scan Verification
```

Không dùng quy trình:

``` text
Generate QR 0001, 0002, 0003
        ↓
Dán tự do
        ↓
Mapping thủ công sau
```

------------------------------------------------------------------------

# 17. SECURITY / PERMISSION

Chỉ user có permission phù hợp mới được:

-   Generate BarcodeID.
-   Generate QR.
-   Bulk Generate.
-   Export PDF.
-   Reprint QR.

Không cho phép:

-   Edit BarcodeID.
-   Delete BarcodeID.
-   Reuse BarcodeID.
-   Tự nhập BarcodeID khi create record.

------------------------------------------------------------------------

# 18. AUDIT LOG

Nếu CMMS có audit log, ghi nhận:

``` text
Generate BarcodeID
Generate QR
Reprint QR
```

Thông tin:

-   User.
-   Date/Time.
-   Entity Type.
-   Record ID.
-   BarcodeID.
-   Action.

------------------------------------------------------------------------

# 19. TEST DATA

## Maintenance – Non-Coded

```text
RAR0001 → VFMNTNCP000001
RAR0002 → VFMNTNCP000002
RAR0003 → VFMNTNCP000003
```

## Maintenance – Coded

```text
ITT001 / 12512512 → VFMNTCP000001
ITT004 / 1612352  → VFMNTCP000002
ITT004 / 123      → VFMNTCP000003
```

## Maintenance – Equipment

```text
EQ000001 → VFMNTEQ000001
EQ000002 → VFMNTEQ000002
EQ000003 → VFMNTEQ000003
```

# 20. TEST CASES

## BarcodeID

-   Existing record BarcodeID = NULL.
-   Generate one BarcodeID.
-   Generate bulk BarcodeID.
-   Existing BarcodeID không bị generate lại.
-   Duplicate BarcodeID.
-   Concurrent generation.
-   Sequential number.
-   BarcodeID không reuse.
-   BarcodeID không đổi sau Edit.
-   BarcodeID không đổi sau đổi Part Code.
-   BarcodeID không đổi sau đổi Equipment Code.
-   BarcodeID không đổi sau các thay đổi thông tin không liên quan.

## QR

-   Generate QR.
-   QR chứa đúng BarcodeID.
-   QR không chứa thêm data.
-   Scan QR bằng Scanner App.
-   QR không tồn tại.
-   Reprint QR.
-   Generate QR nhiều lần vẫn cùng BarcodeID.
-   Export PDF.
-   Bulk export PDF.

## Label

-   QR hiển thị đúng.
-   BarcodeID hiển thị đúng.
-   Name hiển thị đúng.
-   Code/Serial hiển thị đúng.
-   Mapping giữa label và record chính xác.

## Existing Equipment

-   Generate QR cho Equipment cũ.
-   Export PDF.
-   Dùng label đối chiếu Equipment Code/Name/Serial.
-   Scan QR.
-   App trả đúng Equipment.

------------------------------------------------------------------------

# 21. CODE STRUCTURE

Backend nên có service tương tự:

``` text
Services/
└── Barcode/
    ├── IBarcodeIdService.cs
    ├── BarcodeIdService.cs
    ├── IQRCodeService.cs
    └── QRCodeService.cs
```

Có thể có API:

``` text
/api/barcode
/api/qr
```

Ví dụ:

``` text
POST /api/barcode/generate
POST /api/barcode/generate-bulk
POST /api/qr/generate
POST /api/qr/generate-bulk
GET  /api/qr/preview/{barcodeId}
```

Nếu project hiện tại đã có convention khác, phải tuân thủ architecture
hiện tại, không tự ý tạo architecture song song.

------------------------------------------------------------------------

# 22. IMPORTANT ARCHITECTURE RULE

CMMS Website và Scanner App không được tự quản lý hai nguồn BarcodeID.

Kiến trúc:

``` text
CMMS Website
      ↓
CMMS Backend / API
      ↓
SQL Server
      ↑
Scanner App
      ↓
Scan BarcodeID
```

Backend/Database là single source of truth cho BarcodeID.

Không làm:

``` text
CMMS Website → tự generate
Scanner App → tự generate
```

------------------------------------------------------------------------

# 23. IMPLEMENTATION ORDER

## Phase 1 -- Database

-   Add BarcodeID.
-   Nullable ban đầu.
-   Unique constraint/index.
-   Kiểm tra coded item mapping.
-   Kiểm tra existing records.

## Phase 2 -- Backend

-   BarcodeIdService.
-   QRCodeService.
-   Generate single.
-   Generate bulk.
-   Auto generate cho record mới.
-   Permission.
-   Concurrency protection.

## Phase 3 -- CMMS Website

-   QR Code Management menu.
-   Generate QR Code page.
-   Search/filter.
-   Select items.
-   Generate BarcodeID.
-   Generate QR.
-   Preview.
-   Export PDF.
-   Reprint.

## Phase 4 -- Existing Records

-   Generate BarcodeID cho các record đang NULL.
-   Generate label.
-   Export PDF.
-   Kiểm tra mapping.
-   In và dán.
-   Scan verification.

## Phase 5 -- Scanner App

-   Scan BarcodeID.
-   Call API.
-   Display item.
-   View/Edit.
-   Outbound/Maintenance theo entity type.

------------------------------------------------------------------------

# 24. KHÔNG THỰC HIỆN

Trong task này không:

-   Thay đổi Part Code.
-   Thay đổi Equipment Code.
-   Thay đổi Serial Code.
-   Tạo QR dựa trên SQL ID.
-   Tạo QR dựa trên Part Code.
-   Tạo QR dựa trên Equipment Code.
-   Tạo QR dựa trên Serial Code.
-   Lưu QR image vào database.
-   Cho user nhập BarcodeID.
-   Cho user edit BarcodeID.
-   Generate BarcodeID mới khi Reprint.
-   Generate BarcodeID mới khi Export PDF.
-   Cho Scanner App tự generate BarcodeID.
-   Tạo mapping thủ công giữa QR rời và item sau khi in.

------------------------------------------------------------------------

# 25. DEFINITION OF DONE

-   [ ] Database có BarcodeID cho Non-Coded Spare Part.
-   [ ] Database có BarcodeID cho từng Coded Spare Part item/Serial.
-   [ ] Database có BarcodeID cho Equipment.
-   [ ] BarcodeID theo format VF + Department + Type + 6 digits.
-   [ ] BarcodeID UNIQUE.
-   [ ] BarcodeID không reuse.
-   [ ] Existing records có thể được cấp BarcodeID từ CMMS Website.
-   [ ] Record mới tự động được cấp BarcodeID.
-   [ ] QR chỉ chứa BarcodeID.
-   [ ] QR Label chứa QR + BarcodeID + Name.
-   [ ] Equipment label có đủ thông tin để đối chiếu khi đi dán.
-   [ ] Có Generate QR hàng loạt.
-   [ ] Có Preview.
-   [ ] Có Export PDF.
-   [ ] Có Reprint.
-   [ ] Reprint không thay đổi BarcodeID.
-   [ ] Scanner App scan được QR.
-   [ ] Scanner App truy vấn đúng item qua API.
-   [ ] Không có logic generate BarcodeID trong Mobile App.
-   [ ] Không có BarcodeID duplicate.
-   [ ] Có test cho existing records và new records.
