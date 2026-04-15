# Phase 1 Progress Recap

## Tổng quan

Tài liệu này ghi lại những gì đã hoàn thành trong chuỗi triển khai phase 1 cho nền dữ liệu lộ trình và phần public tour detail.

Các phần đã làm xong:

- Phase 1.1: khóa mất dữ liệu của schema tour mới và bổ sung admin read-only
- Phase 1.2: hiển thị public route preview từ `Tour.routing`
- Phase 1.2.1: fix lỗi slug-based public tour routing và chuẩn hóa public links theo `slug` trước, `id` sau

Hiện tại dự án đã có nền dữ liệu route, import an toàn hơn, admin kiểm tra được dữ liệu mới, public detail đã tiêu thụ `routing`, và public URL của tour đã hoạt động ổn định với `slug`.

## Phase 1.1 đã làm được gì

### 1. Bảo toàn schema tour mới khi admin sửa tour

- `Admin/Edit` không còn ghi đè toàn bộ document từ form cũ.
- Controller chỉ merge các field legacy đang thực sự editable.
- Các field mới được giữ nguyên:
  - `Slug`
  - `Seo`
  - `CancellationPolicy`
  - `ConfirmationType`
  - `Highlights`
  - `MeetingPoint`
  - `BadgeSet`
  - `Departures`
  - `Routing`
  - `SupplierRef`

Kết quả: tour import từ mockdata schema mới sẽ không bị mất dữ liệu khi admin chỉnh sửa bằng form cũ.

### 2. Giữ create flow cũ nhưng an toàn hơn

- `Admin/Create` vẫn hoạt động với form hiện tại.
- Bổ sung default phía server:
  - tự sinh `Slug` từ `Name` nếu còn trống
  - mặc định `ConfirmationType = "Instant"` nếu chưa có

Kết quả: tour mới tạo từ admin cũ vẫn hợp lệ hơn với public site và schema commerce mới.

### 3. Import được `Tours.json` dạng Mongo extended JSON

- `Admin/Import` đã hỗ trợ:
  - Mongo extended JSON từ mockdata tool
  - JSON thường để tương thích ngược
- Khi import:
  - `Id` luôn được generate lại
  - giữ được `Slug`, `Seo`, `Departures`, `Routing` và các field phase 1

Kết quả: dữ liệu sinh từ `tools/mockdata_generator` có thể đi vào hệ thống đúng shape hơn.

### 4. Admin đã xem được dữ liệu phase 1

Ở admin details đã hiển thị được:

- `Slug`
- SEO metadata
- `ConfirmationType`
- `CancellationPolicy`
- `Highlights`
- `MeetingPoint`
- `BadgeSet`
- `Departures`
- `Routing`

Ngoài ra:

- có fallback rõ khi chưa có `Routing`
- có fallback rõ khi đang dùng `StartDates` thay cho `Departures`

### 5. Admin index có chỉ báo nhanh

Trang danh sách tour đã có:

- badge cho tour có `Routing`
- số lượng `Departures` nếu có

Kết quả: admin nhìn nhanh được tour nào đã route-ready.

### 6. Test bảo vệ behavior mới

Đã thêm test cho:

- edit không làm mất field schema mới
- create sinh default an toàn
- import extended JSON
- details/index render dữ liệu phase 1

## Phase 1.2 đã làm được gì

### 1. Có lớp presentation riêng cho route preview public

Đã thêm model/view-model nội bộ để trình bày route:

- `PublicTourRouteOverview`
- `PublicTourRouteDay`
- `PublicTourRouteStopViewModel`

Đây là lớp rút gọn từ `Tour.routing` để chỉ đưa ra public các field cần thiết.

### 2. Có builder dựng route overview từ `Tour.routing`

Đã thêm service builder để:

- group stop theo `day`
- sort theo `order`
- lấy `DayTitle` từ `Schedule.Day`
- fallback về `Day N` nếu thiếu title
- tính:
  - `HasRouting`
  - `DayCount`
  - `StopCount`
  - `TotalVisitMinutes`

Kết quả: public UI không phải đọc raw `routing` trực tiếp.

### 3. Public tour details đã hiển thị route preview

Trang chi tiết tour public đã có thêm section route overview:

- chỉ hiện khi tour có `routing` hợp lệ
- hiển thị:
  - số ngày
  - số điểm dừng
  - tổng phút tham quan
  - danh sách stop theo từng ngày
- mỗi stop hiện:
  - `Name`
  - `Type`
  - `VisitMinutes`
  - `Note`

Không đưa ra public ở phase này:

- `coordinates.lat/lng`
- `attractionScore`
- `schemaVersion`

Kết quả: `routing` đã trở thành giá trị người dùng nhìn thấy được, không còn chỉ nằm trong dữ liệu backend.

### 4. Public content defaults đã đăng ký section `routing`

Đã mở rộng `publicTourDetails` để có thêm section content `routing` với các field tối thiểu:

- `eyebrowText`
- `title`
- `description`
- `dayCountFormat`
- `stopCountFormat`
- `visitMinutesFormat`
- `typeLabel`

Kết quả: route preview vẫn đi theo pattern content-driven hiện có của dự án.

### 5. Sanitize dữ liệu route trước khi render

`PublicTextSanitizer` đã normalize:

- `Routing.Stops[].Name`
- `Routing.Stops[].Type`
- `Routing.Stops[].Note`

Kết quả: dữ liệu route ra public sạch hơn và bớt rủi ro hiển thị lỗi định dạng.

### 6. Test cho route preview public

Đã thêm test cho:

- route overview builder
- sanitize route fields
- wiring controller
- content defaults có section `routing`
- public detail có route overview và không lộ field nhạy cảm

## Phase 1.2.1 đã thay đổi gì

### 1. Fix lỗi crash khi mở tour bằng slug

Nguyên nhân cũ:

- `PublicToursController.Details` luôn gọi `GetByIdAsync(id)` trước
- khi `id` thực ra là slug như `kham-pha-thai-binh-...`, Mongo cố parse thành `_id` kiểu `ObjectId`
- request bị ném `FormatException` trước khi kịp fallback sang `GetBySlugAsync`

Đã sửa:

- nếu identifier không phải ObjectId hợp lệ, controller bỏ qua lookup theo id và gọi thẳng theo slug
- nếu identifier có dạng ObjectId hợp lệ, controller thử theo id trước rồi mới fallback theo slug

Kết quả: URL dạng slug không còn làm vỡ trang chi tiết tour.

### 2. Chuẩn hóa helper lấy public identifier của tour

Đã thêm helper dùng chung:

- `PublicTourIdentifierHelper.GetDetailIdentifier(tour)`

Rule:

- ưu tiên `Slug`
- fallback `Id` nếu `Slug` trống

Kết quả: không còn lặp lại ternary `slug ? id` ở nhiều nơi.

### 3. Chuẩn hóa các public link sang slug-first

Các vị trí tạo link tới `PublicTours/Details` đã được thống nhất dùng helper chung:

- public tour card
- carousel tour card
- destinations page
- booking create page

Kết quả:

- click từ danh sách tour
- click từ carousel
- click từ destinations
- click từ booking create

đều ưu tiên tạo URL bằng `slug` nếu có.

### 4. Canonical URL cũng dùng cùng chiến lược

`CanonicalUrl` trong `PublicToursController.Details` đã dùng cùng helper public identifier.

Kết quả:

- link người dùng click
- canonical URL

được đồng bộ theo cùng một rule, tránh sai lệch giữa các chỗ build URL.

### 5. Thêm regression tests cho slug routing

Đã bổ sung test cho các trường hợp:

- slug hợp lệ về business nhưng không phải ObjectId: resolve qua slug, không ném exception
- identifier có dạng ObjectId: resolve theo id trước
- identifier có dạng ObjectId nhưng không có bản ghi id: vẫn fallback sang slug nếu cần
- canonical URL dùng cùng chiến lược slug-first
- các view public link đều dùng helper chung

Kết quả kiểm thử:

- `PublicTourSlugRoutingTests`: pass
- full `HV-Travel.Web.Tests`: pass

## Sau các phase này, dự án đang có gì

Dự án hiện đã có:

- dữ liệu `routing` gắn trực tiếp vào `Tour`
- import dữ liệu schema mới ổn hơn
- admin xem được dữ liệu `routing` và `departures`
- admin sửa tour mà không làm mất schema mới
- public detail dùng `routing` để hiển thị route preview
- public URL của tour hoạt động theo `slug` trước, `id` sau
- controller detail xử lý an toàn cả slug thường lẫn chuỗi có dạng ObjectId

## Dự án chưa có gì ở thời điểm này

Chưa có:

- bản đồ, marker, polyline
- ETA giao thông
- heuristic traffic delay
- thuật toán TSP
- tối ưu đa tiêu chí hoàn chỉnh
- admin editor cho `routing`
- admin editor cho `departures`
- route-aware AI chat
- recommendation engine đầy đủ

## Ý nghĩa của các phase đã làm

Chuỗi phase này giải quyết 4 việc nền tảng:

1. Dữ liệu route đã có chỗ đứng ổn định trong hệ thống.
2. Schema mới không còn ở trạng thái “có nhưng dễ mất”.
3. `routing` đã bắt đầu được tiêu thụ ở giao diện public.
4. `slug` đã trở thành public identifier vận hành ổn định cho tour detail, thay vì chỉ tồn tại trong data model.

Đây là nền cần thiết trước khi đi tiếp sang các bước như admin editor cho route data, recommendation engine, ETA estimation, hoặc tối ưu lộ trình đa tiêu chí.
