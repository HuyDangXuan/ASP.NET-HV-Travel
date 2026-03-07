# Tài Liệu Logic Đặt Vé (Booking Logic) - HV Travel

Tài liệu này giải thích chi tiết luồng xử lý và các logic nghiệp vụ khi người dùng thực hiện đặt tour trên hệ thống HV Travel.

## 1. Luồng Nghiệp Vụ (Booking Flow)

Quy trình đặt vé trải qua các bước chính sau:

1.  **Chọn Tour**: Người dùng xem danh sách tour tại `PublicTours/Index` và chi tiết tại `PublicTours/Details`.
2.  **Trang Đặt Vé (GET: `Booking/Create`)**:
    - Hệ thống lấy thông tin Tour từ database dựa trên `tourId`.
    - Khởi tạo `BookingViewModel` để hiển thị form nhập liệu.
3.  **Xử Lý Đặt Vé (POST: `Booking/Create`)**:
    - Kiểm tra tính hợp lệ của dữ liệu (ModelState).
    - Tính toán tổng tiền dựa trên số lượng khách và giá tour.
    - Tạo mã đặt vé (`BookingCode`) duy nhất.
    - Lưu bản chụp thông tin tour (`TourSnapshot`) để đảm bảo thông tin không đổi nếu tour gốc cập nhật giá/tên sau này.
    - Lưu vào database với trạng thái mặc định: `Status = "Pending"`, `PaymentStatus = "Unpaid"`.
4.  **Thanh Toán (GET: `Booking/Payment`)**:
    - Người dùng chọn phương thức thanh toán (Tiền mặt, Chuyển khoản, Thẻ tín dụng).
5.  **Xác Nhận Thanh Toán (POST: `Booking/ProcessPayment`)**:
    - Cập nhật trạng thái Booking dựa trên phương thức đã chọn.
    - Chuyển hướng đến trang Thành công hoặc Thất bại.

---

## 2. Logic Chi Tiết

### 2.1. Tính Toán Giá (Pricing Calculation)
Tổng số tiền (`TotalAmount`) được tính toán phía server để đảm bảo tính chính xác:
- **Công thức gốc**: `Subtotal = (AdultPrice * AdultCount) + (ChildPrice * ChildCount) + (InfantPrice * InfantCount)`
- **Giảm giá**: Nếu có `Discount`, `Total = Subtotal * (1 - Discount/100)`.

### 2.2. Mã Đặt Vé (Booking Code)
Mã được sinh tự động theo định dạng: `HV + {yyyyMMddHHmmss} + {3 số ngẫu nhiên}`.
*Ví dụ: HV20260228161500123*

### 2.3. Chụp Hình Thông Tin Tour (Tour Snapshot)
Để tránh việc thay đổi dữ liệu tour gốc (như đổi tên hoặc đổi giá) làm ảnh hưởng đến đơn hàng cũ, hệ thống lưu một bản snapshot gồm:
- Mã tour (`Code`)
- Tên tour (`Name`)
- Ngày khởi hành (`StartDate`)
- Thời lượng (`Duration`)

### 2.4. Quản Lý Trạng Thái (Status Management)

| Phương Thức | Status (Trạng thái đơn) | PaymentStatus (Thanh toán) | Ghi chú |
| :--- | :--- | :--- | :--- |
| **Tiền mặt (Cash)** | `Confirmed` | `Unpaid` | Đơn được xác nhận, chờ thu tiền tại văn phòng. |
| **Khác (Banking/...).** | `Paid` | `Pending` | Đã thanh toán (mô phỏng), chờ đối soát web. |

---

## 3. Cấu Trúc Dữ Liệu

### Booking Entity (`HVTravel.Domain.Entities.Booking`)
- `BookingCode`: Mã định danh đơn hàng.
- `TourSnapshot`: Thông tin tour tại thời điểm đặt.
- `ContactInfo`: Thông tin liên hệ (Tên, Email, SĐT).
- `ParticipantsCount`: Tổng số khách.
- `TotalAmount`: Tổng tiền cuối cùng.
- `HistoryLog`: Lưu vết lịch sử thay đổi (Ai tác động, lúc nào, nội dung gì).

### BookingViewModel (`HVTravel.Web.Models.BookingViewModel`)
- Sử dụng để mapping dữ liệu từ form UI.
- Có các thuộc tính validation như `[Required]`, `[EmailAddress]`, `[Range]`.

---

## 4. Lưu Ý Phát Triển
- Logic tính tiền hiện tại nằm trực tiếp trong `BookingController`. Nếu quy trình phức tạp hơn (thuế, phí dịch vụ phụ), nên tách ra `BookingService`.
- Khi thanh toán thành công, hệ thống hiện tại chưa tích hợp cổng thanh toán thực tế (VNPAY/Momo), mà chỉ đang mô phỏng việc cập nhật trạng thái.
