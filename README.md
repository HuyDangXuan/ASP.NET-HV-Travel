# HV Travel - Hệ Thống Quản Lý Du Lịch

HV Travel là một ứng dụng web hiện đại, toàn diện được thiết kế để quản lý các công ty du lịch cao cấp. Được xây dựng với **ASP.NET Core** và tuân thủ các nguyên tắc **Clean Architecture**, hệ thống cung cấp một nền tảng mạnh mẽ để quản lý tour du lịch, đặt chỗ (booking), khách hàng và các giao dịch tài chính.

## 🚀 Tính Năng Chính

### 🌟 Bảng Điều Khiển & Tiện Ích
-   **Dashboard Tương Tác:** Tổng quan thời gian thực về hiệu quả kinh doanh với các thẻ KPI và biểu đồ trực quan.
-   **Chế Độ Tối (Dark Mode):** Hỗ trợ Dark Mode đồng bộ trên toàn bộ các trang, bao gồm cả màn hình đăng nhập, với khả năng lưu trạng thái bằng `localStorage`.
-   **Thiết Kế Responsive:** Tối ưu hóa hiển thị trên nhiều loại thiết bị, sử dụng Tailwind CSS.

### 📦 Quản Lý Tour
-   **Thao Tác CRUD:** Quy trình cụ thể để tạo, chỉnh sửa và quản lý tour.
-   **Bộ Lọc Nâng Cao:** Lọc tour theo Thành phố, Danh mục, Khoảng giá và Thời lượng sử dụng các thẻ UI tương tác.
-   **Xóa Mềm & Lưu Trữ:** Cơ chế xóa an toàn để bảo toàn dữ liệu (Soft Delete).
-   **Nội Dung Phong Phú:** Hỗ trợ mô tả chi tiết tour, hình ảnh và lịch trình.

### 🔐 Xác Thực & Bảo Mật
-   **Luồng Xác Thực An Toàn:** Tích hợp các trang Đăng nhập, Đăng ký, Quên mật khẩu và Tài khoản chờ duyệt.
-   **Phân Quyền:** Giao diện quản trị được bảo vệ bởi cơ chế xác thực.
-   **Giao Diện Hiện Đại:** Sử dụng hiệu ứng Glassmorphism và hình ảnh chất lượng cao trên các trang xác thực.

### 👥 Quản Lý Khách Hàng & Đặt Tour
-   **Hồ Sơ Khách Hàng:** Xem chi tiết thông tin và lịch sử của khách hàng.
-   **Theo Dõi Đặt Tour:** Giám sát trạng thái và chi tiết các booking.
-   **Xử Lý Thanh Toán:** Quản lý giao dịch, hoàn tiền và chi phí.

## 🛠️ Công Nghệ Sử Dụng

-   **Backend:** ASP.NET Core (Clean Architecture)
-   **Frontend:** ASP.NET Core MVC / Razor Views
-   **Styling:** Tailwind CSS (thông qua CDN hoặc xử lý tiền kỳ)
-   **Cơ Sở Dữ Liệu:** MongoDB
-   **Hạ Tầng:** Dependency Injection, Repository Pattern

## 📂 Cấu Trúc Dự Án

-   **`HV-Travel.Domain`**: Các thực thể nghiệp vụ cốt lõi (Entities) và interfaces.
-   **`HV-Travel.Application`**: Logic nghiệp vụ, services và DTOs.
-   **`HV-Travel.Infrastructure`**: Triển khai truy cập dữ liệu (MongoDB context, repositories).
-   **`HV-Travel.Web`**: Tầng giao diện (Controllers, Views, Static files).

## ⚡ Hướng Dẫn Cài Đặt

### Yêu Cầu Tiên Quyết
-   [.NET SDK](https://dotnet.microsoft.com/download) (Phiên bản 8.0 trở lên được khuyến nghị)
-   [MongoDB](https://www.mongodb.com/try/download/community) (Local hoặc Atlas)

### Cài Đặt

1.  **Clone repository:**
    ```bash
    git clone <repository-url>
    cd ASP.NET-HV-Travel
    ```

2.  **Cấu Hình Môi Trường:**
    -   Đảm bảo chuỗi kết nối MongoDB của bạn được thiết lập chính xác trong file `appsettings.json` hoặc biến môi trường.
    -   Ví dụ cấu hình trong `appsettings.json` (Lưu ý: Không chia sẻ thông tin nhạy cảm):
        ```json
        {
          "ConnectionStrings": {
            "MongoDb": "<Your_MongoDB_Connection_String>"
          }
        }
        ```

3.  **Chạy Ứng Dụng:**
    Di chuyển đến thư mục dự án Web và chạy lệnh:
    ```bash
    cd HV-Travel.Web
    dotnet run
    ```
    Hoặc sử dụng `dotnet watch run` để hot reload trong quá trình phát triển.

4.  **Truy Cập Ứng Dụng:**
    Mở trình duyệt và truy cập vào địa chỉ `https://localhost:7198` (hoặc cổng được hiển thị trên terminal).

## 🎨 Điểm Nổi Bật Về UI/UX
-   **Giao Diện Nhất Quán:** Bảng màu chuẩn (`primary`, `surface-dark`, `background-dark`) được sử dụng xuyên suốt.
-   **Tương Tác:** Hiệu ứng hover, chuyển cảnh mượt mà và các thẻ lọc động.
-   **Độ Hoàn Thiện:** Thanh cuộn tùy chỉnh, lớp phủ glassmorphism và typography tinh tế (Be Vietnam Pro).

---
*Được phát triển cho HV Travel.*
