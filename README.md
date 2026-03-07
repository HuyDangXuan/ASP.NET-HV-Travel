# ✈️ HV Travel - Hệ Thống Quản Lý Du Lịch Cao Cấp (Comprehensive Review)

![HV-Travel](https://img.shields.io/badge/ASP.NET_Core-8.0-blue?style=for-the-badge&logo=dotnet) ![MongoDB](https://img.shields.io/badge/MongoDB-4EA94B?style=for-the-badge&logo=mongodb&logoColor=white) ![TailwindCSS](https://img.shields.io/badge/Tailwind_CSS-38B2AC?style=for-the-badge&logo=tailwind-css&logoColor=white)

HV Travel là một hệ thống web toàn diện, hiệu suất cao dành cho các công ty du lịch và lữ hành. Dự án được triển khai trên nền tảng **ASP.NET Core 8.0 MVC** kết hợp với **MongoDB** cho thiết kế dữ liệu NoSQL linh hoạt, tuân thủ chặt chẽ kiến trúc **Clean Architecture** và **Repository Pattern**. Giao diện người dùng được xây dựng hoàn toàn tinh tế với **Tailwind CSS**, mang lại trải nghiệm UX/UI hiện đại với hỗ trợ *Dark Mode* toàn diện.

---

## 🏗 Kiến Trúc Hệ Thống (Clean Architecture)

Dự án được cấu trúc thành 4 tầng riêng biệt, tuân thủ nguyên tắc Dependency Inversion:

### 1. 🛡️ Tầng Lõi Nghiệp Vụ (`HV-Travel.Domain`)
Nơi chứa các thực thể (Entities) cốt lõi và các Interfaces cơ bản. Không có bất kỳ dependency nào hướng ra bên ngoài.
*   **Entities:**
    *   `User`: Quản lý tài khoản quản trị (Role, Status, PasswordHash).
    *   `Customer`: Lưu trữ hồ sơ, lịch sử giao dịch và phân khúc khách hàng (VIP, Potential, v.v.).
    *   `Tour`: Lưu chi tiết sản phẩm du lịch (Tên, Mã, Giá, Địa điểm, Trạng thái, Lưu trữ mềm - Soft Delete).
    *   `Booking`: Đơn đặt chỗ của khách, liên kết thông tin `TourSnapshot`, tổng số tiền và số người tham gia.
    *   `Payment`: Lưu vết các giao dịch tài chính liên quan đến Booking.
    *   `Review`, `Promotion`, `Notification`: Các module thành phần hỗ trợ hoạt động kinh doanh.
*   **Interfaces**: `IRepository<T>` định nghĩa các thao tác CRUD chung.

### 2. ⚙️ Tầng Ứng Dụng (`HV-Travel.Application`)
Chứa Business Logic, DTOs và định nghĩa Services.
*   **Services**: `AuthService`, `DashboardService`, `TourService`. Xử lý các nghiệp vụ phức tạp mà Repositories thông thường không đảm nhận (Thống kê KPI, xác thực người dùng).

### 3. 💾 Tầng Hạ Tầng (`HV-Travel.Infrastructure`)
Triển khai kỹ thuật của Interfaces được định nghĩa ở tầng Core.
*   **Data Access**: `MongoDbContext.cs` để kết nối database thông qua chuỗi kết nối ở `appsettings.json`.
*   **Repositories**: `MongoRepository<T>` triển khai các lệnh bất đồng bộ (`GetAllAsync`, `GetByIdAsync`, `InsertAsync`, `UpdateAsync`, `DeleteAsync`, `FindAsync`) cho mọi collection tương ứng của Model.
*   **Seeding**: `DbInitializer.cs` đảm nhận tạo dữ liệu mấu hoặc thiết lập tài khoản Admin khởi tạo đầu tiên.

### 4. 🎨 Tầng Trình Diễn (`HV-Travel.Web`)
Chịu trách nhiệm tương tác bằng ASP.NET Core MVC (Controller + View).
*   **Admin Area (`/Admin/`)**: Trang dành cho nhân viên quản trị (Controllers: `Dashboard`, `Tours`, `Bookings`, `Customers`, `Payments`, `Users`, `Auth`).
*   **Giao Diện Phía Dưới**: View dùng Razor Engine với cấu trúc bố cục Layout (`_Layout.cshtml`), Header (`_Header.cshtml`) và giao diện nhất quán. Hỗ trợ AJAX cho các thao tác mượt mà và Modal/Popups thao tác UI.

---

## 🚀 Tính Năng Nổi Bật

### 🔐 1. Quản Lý Quyền & Xác Thực (Authentication & Authorization)
*   **Cookie Authentication**: Quản lý phiên đăng nhập an toàn với Cookie (`.AspNetCore.Cookies`).
*   **Role-based Access Control (RBAC)**: Phân quyền đa cấp độ bao gồm `Admin`, `Manager`, `Staff`, và `Guide`. Ví dụ: `Guide` có thể giới hạn truy cập chức năng Chỉnh sửa so với `Admin`.

### 📊 2. Dashboard Tiên Tiến & KPI Metrics
*   Trang chủ Dashboard tổng hợp dữ liệu qua `DashboardService`.
*   Cung cấp thẻ thông tin (KPI cards) như tổng doanh thu, biểu đồ đăng ký đặt tour mới, và danh sách các thay đổi trạng thái theo thời gian thực.
*   Thống kê toàn cục trên phạm vi hệ thống.

### 🧩 3. Panels Bộ Lọc & Tìm Kiếm Thông Minh (Smart Filtering)
Mọi trang danh sách (Tours, Bookings, Customers, Payments, Users) đều được chuẩn hóa hệ thống bảng với các tính năng sau:
*   **Thanh Tìm Kiếm (Search)**: Tra cứu nhanh mã ID, Tên, hay Email.
*   **Panel Bộ Lọc Trạng Thái Động**: Cung cấp thẻ lọc theo Tab (hoặc Dropdown/Radio) như "Đang Xử Lý", "Hoàn Tiền", "Đã Thanh Toán", "Thành Công/Thất Bại".
*   **Sắp Xếp Cột Mũi Tên (Sorting)**: Cho phép nhấp vào tiêu đề bảng (Header) để đổi hướng sắp xếp ASC / DESC (Ngày tạo, Giá tiền, Trạng thái).
*   **Bộ nhớ Filters**: Giá trị truy vấn được giữ nguyên khi điều hướng và sắp xếp cùng lúc qua biến `Query String`.

### 🛒 4. Tính Năng Nghiệp Vụ Chính
*   **Tours**: Quản lý ảnh bìa, lịch trình, lưu lượng nhóm (`MaxParticipants`), và gắn Tag trạng thái mượt mà bằng UI.
*   **Bookings**: Cung cấp cái nhìn toàn cảnh về đơn yêu cầu, kết xuất (Export - UI) chi tiết giá và trạng thái Booking (`Pending`, `Paid`, `Cancelled`).
*   **Customers**: Phân tích lịch sử chi tiêu, tự động định danh (Tags phân khúc VIP/Potential) để có kế hoạch sale phù hợp.
*   **Payments**: Phân biệt nguồn thu (Transactions) & nguồn chi (Expenses/Refunds). Tự động mapping Data từ Payment Status.

### 🌗 5. UX/UI Đẳng Cấp & Dark Mode
*   Toàn bộ hệ thống kế thừa cấu hình `Tailwind CSS 3+` (hoạt động thông qua CDN hoặc Pre-compiled).
*   **Theme Switcher**: Hỗ trợ chuyển đổi Sáng/Tối mượt mà qua các Icon mặt trời/mặt trăng ở góc Navbar, ghi nhớ qua `localStorage`.
*   **Visual Elements**: Biểu tượng Material Symbols hiện đại, các Badge trạng thái đầy màu sắc với Dot Indicator (Chấm tròn), Glassmorphism (Kính lờ) và Micro-animations chuyển cảnh tinh tế.

---

## 💻 Hướng Dẫn Kỹ Thuật (Developer Guide)

### Yêu Cầu Chạy Tools
1.  **SDK**: `.NET 8.0` SDK.
2.  **Database**: MongoDB Server (chạy port mặc định `27017` hoặc khai báo Mongo Atlas URl).

### ⚙️ Biến Môi Trường (`appsettings.json`)
Cấu trúc cơ bản yêu cầu khi setup:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "MongoDb": "mongodb://localhost:27017"
  },
  "DatabaseSettings": {
    "DatabaseName": "HVTravelDb"
  }
}
```

### 🏃🏾 Cài đặt và Chạy hệ thống local

1.  **Restore gói Dependencies:**
    ```bash
    dotnet restore
    ```
2.  **Khởi chạy Dự án:**
    Sử dụng Terminal ở Root folder để watch (Hot reload):
    ```bash
    cd HV-Travel.Web
    dotnet watch run
    ```
3.  **Tài Khoản Test (Seeded by DbInitializer):**
    *   **User/Email**: `admin@hvtravel.com`
    *   **Password**: `admin123`
4.  Truy cập hệ thống tại: `http://localhost:5028/` hoặc `https://localhost:7190/` (Dựa trên launchSettings.json).

---

## 🛠 Những Bản Cập Nhật Chú Ý Gần Đây
*(Thông tin cung cấp dựa trên Tracking của Git/Các file log)*
1.  **Chuẩn Hóa Bộ Lọc (Standardizing Filter Panels)**: Hệ thống bộ lọc đã được đồng bộ hóa trên `Tours`, `Bookings`, `Payments`, và `Users` theo giao diện của `Customers`. Loại bỏ thành công lỗi kẹt input ẩn (duplicate hidden `status`).
2.  **Table Sorting**: Controllers đã được cập nhật tham số `sortOrder` động, kèm chức năng tích hợp bộ lọc cũ (duy trì `searchString`, `statusFilter`).
3.  **UI Xác Thực (Auth UI)**: Áp dụng Dark Mode và Gradient Layout vào các trang Identity như Login (Đăng nhập) và Account Recovery.

---
*Dành cho nhóm phát triển HV Travel - Phá vỡ rào cản nền tảng công nghệ trong ngành lữ hành!*

---

## 📱 Hướng Dẫn Mapping Dữ Liệu (Mobile App ↔ Backend ASP.NET)

Dự án này sử dụng cấu trúc dữ liệu tối ưu cho quản trị nghiệp vụ. Khi phát triển Mobile App (React Native), cần lưu ý các quy tắc mapping dữ liệu sau để đảm bảo đồng bộ với API trả về từ Backend. Toàn bộ API Response mặc định sử dụng format `camelCase`, và ID của MongoDB (`_id`) được biểu diễn qua `id` (kiểu string).

### 1. Sự Khác Biệt Cấu Trúc Chính
*   **User vs Customer:** Backend tách biệt hoàn toàn `User` (Dành cho Admin/Nhân viên nội bộ) và `Customer` (Dành cho khách hàng cuối). Mobile App **chỉ sử dụng entity `Customer`** để quản lý tài khoản người dùng, không gọi vào bảng `User`.
*   **Không có Collection Danh mục (Category/City):** Backend thiết kế NoSQL tối ưu số lần đọc bằng cách nhúng trực tiếp.
    *   `Category` được lưu dạng chuỗi giá trị trực tiếp (VD: `"Adventure"`, `"Luxury"`) trong document `Tour`.
    *   `City` được nhúng thành object `destination: { city, country, region }` trong bảng `Tour`. Mobile filter bằng cách query string trực tiếp.
*   **Thuộc tính Hình ảnh (Images):** Không tách rời `thumbnail` và `gallery`. Gộp chung thành mảng `images: string[]` trên entity `Tour`. Ảnh đầu tiên (`images[0]`) mặc định là ảnh đại diện.
*   **Cấu trúc Giá (Pricing):** Entity `Tour` sử dụng object `price: { adult, child, infant, currency, discount }`. Backend đã gộp `newPrice` và `discount` cũ vào chung thuộc tính `discount` (tính theo %). Thuộc tính vé trẻ em dùng `child` và `infant` (không dùng `children`/`baby`).

### 2. Mapping Các Thực Thể Giao Dịch
*   **Tour Duration:** Thay vì chuỗi text thuần túy, frontend cần map qua object `duration: { days, nights, text }` để linh hoạt tính toán logic UI.
*   **Hành Khách (Passengers):** Khi tạo `Booking`, không truyền số lượng đơn thuần mà phải truyền mảng object chi tiết `passengers: [{ fullName, type: "Adult"|"Child"|"Infant" }]`. Việc giới hạn số chỗ dựa trên `tour.maxParticipants` và `tour.currentParticipants`.
*   **Payment (Thanh Toán):** Tách biệt hoàn toàn khỏi `Booking`. Phương thức thanh toán được định nghĩa qua `paymentMethod` (`"CreditCard"`, `"BankTransfer"`, `"Cash"`). Mobile App cần gọi API tạo Payment riêng biệt sau khi Booking thành công.
*   **Reviews (Đánh giá):** Entity độc lập, liên kết qua `tourId` và `customerId`. Không embed vào bảng `Tour` để tránh phình to kích thước document. Khi fetch cần nối (populate) thông tin khách hàng.

*(Xem chi tiết từng field trong tài liệu `data_mapping_analysis.md`)*
