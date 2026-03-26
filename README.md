# HV Travel

Hệ thống quản lý và bán tour du lịch xây dựng trên **ASP.NET Core 8 MVC**, áp dụng kiến trúc phân tầng theo hướng **Domain / Application / Infrastructure / Web**. Repo này bao gồm:

- website public cho khách xem tour và đặt dịch vụ
- khu vực đăng nhập/đăng ký khách hàng
- trang quản trị cho đội ngũ vận hành
- trung tâm chat hỗ trợ realtime giữa khách và admin
- cấu hình Docker để chạy web, tunnel Cloudflare và MongoDB local khi cần

## Tổng quan tính năng

### Public Website

- Trang chủ, giới thiệu, liên hệ
- Danh sách tour và trang chi tiết tour
- Luồng đặt chỗ, tư vấn và thanh toán
- Nội dung public được phục vụ qua `PublicContentService`

### Customer Experience

- Đăng ký / đăng nhập khách hàng
- Ghi nhận thông tin hồ sơ khách
- Hỗ trợ chat realtime với nhân viên
- Tự điền thông tin chat khi khách đã đăng nhập

### Admin Area

- Dashboard thống kê
- Quản lý tours
- Quản lý bookings
- Quản lý customers
- Quản lý payments
- Quản lý users
- Quản lý nội dung
- Hộp thư và màn hình chat hỗ trợ khách hàng theo thời gian thực

### Realtime Support Chat

- SignalR hub tại `/supportChatHub`
- Chat widget phía public
- Inbox/chat center phía admin
- Khách chưa đăng nhập phải nhập thông tin trước khi chat
- Khách đã đăng nhập có thể vào chat trực tiếp
- Hỗ trợ `Enter để gửi`, `Shift + Enter để xuống dòng`

## Kiến trúc dự án

Repo được tách thành 4 project chính:

### `HV-Travel.Domain`

Chứa entity, model và interface lõi của hệ thống.

Entity tiêu biểu:

- `Tour`
- `Booking`
- `Payment`
- `Customer`
- `User`
- `Review`
- `Promotion`
- `Notification`
- `ChatConversation`
- `ChatMessage`
- `ContentSection`
- `SiteSettings`

### `HV-Travel.Application`

Chứa service nghiệp vụ ở tầng application:

- `AuthService`
- `DashboardService`
- `TourService`

### `HV-Travel.Infrastructure`

Chứa phần triển khai hạ tầng:

- MongoDB context
- seeding dữ liệu
- repository implementation

Thành phần đáng chú ý:

- `DbInitializer`
- `Repository`
- `TourRepository`

### `HV-Travel.Web`

Là project ASP.NET Core MVC chứa:

- controller phía public
- area `Admin`
- SignalR hub
- service web layer
- Razor views

Controller public:

- `HomeController`
- `PublicToursController`
- `BookingController`
- `CustomerAuthController`
- `SupportChatController`

Controller admin:

- `DashboardController`
- `ToursController`
- `BookingsController`
- `CustomersController`
- `PaymentsController`
- `UsersController`
- `ContentController`
- `MessagesController`
- `AuthController`

## Công nghệ sử dụng

- **ASP.NET Core 8 MVC**
- **SignalR**
- **MongoDB**
- **DotNetEnv** để nạp biến môi trường từ `.env`
- **Tailwind CSS** trong giao diện
- **xUnit** cho test
- **Docker / Docker Compose**
- **Cloudflare Tunnel**

## Cấu trúc thư mục

```text
.
├── HV-Travel.Domain
├── HV-Travel.Application
├── HV-Travel.Infrastructure
├── HV-Travel.Web
├── HV-Travel.Web.Tests
├── docs
├── scripts
├── Dockerfile
├── docker-compose.yml
├── docker-commands.md
└── HV-Travel.slnx
```

## Yêu cầu môi trường

Để chạy local bằng .NET:

- .NET SDK 8.0
- MongoDB local hoặc MongoDB Atlas

Để chạy bằng Docker:

- Docker Desktop
- Docker Compose

## Biến môi trường

Ứng dụng đọc `.env` từ thư mục hiện tại hoặc ngược lên solution root thông qua `DotNetEnv`.

Những biến quan trọng:

| Biến | Mục đích |
|---|---|
| `HVTravelDatabase__ConnectionString` | Chuỗi kết nối MongoDB |
| `HVTravelDatabase__DatabaseName` | Tên database MongoDB |
| `TUNNEL_TOKEN` | Token Cloudflare Tunnel cũ hoặc tunnel Docker cũ |
| `LOCAL_TUNNEL_TOKEN` | Token Cloudflare Tunnel mới dùng cho local app |
| `CLOUDINARY__CLOUDNAME` | Cloudinary cloud name |
| `CLOUDINARY__UPLOADPRESET` | Cloudinary upload preset |
| `TINYMCE__APIKEY` | API key TinyMCE |
| `SMTP_HOST` | SMTP server |
| `SMTP_PORT` | SMTP port |
| `SMTP_SECURE` | SMTP secure flag |
| `SMTP_USER` | SMTP username |
| `SMTP_PASS` | SMTP password |
| `MAIL_TO` | Email nhận thông báo |
| `MAIL_FROM_NAME` | Tên người gửi mail |

Khuyến nghị:

- không commit `.env` thật lên remote public
- dùng `.env.example` nếu bạn muốn chuẩn hóa onboarding cho team

## Chạy local bằng .NET

Restore package:

```bash
dotnet restore
```

Chạy web app:

```bash
cd HV-Travel.Web
dotnet watch run
```

Các URL local theo `launchSettings.json`:

- `http://localhost:5028`
- `https://localhost:7190`

## Tài khoản seed mặc định

Seeder hiện tạo sẵn tài khoản admin mặc định:

- Email: `admin@hvtravel.com`
- Password: `admin123`

Lưu ý:

- đây là tài khoản seed cho môi trường phát triển
- nên đổi hoặc vô hiệu hóa ở môi trường production

## Chạy bằng Docker

### Build image

```powershell
docker compose build
```

### Chạy mặc định web + Cloudflare Tunnel

```powershell
docker compose up -d
```

Với cấu hình hiện tại:

- `hv-travel-web` được publish ra host tại `http://localhost:5028`
- `tunnel` chạy cùng stack
- `mongodb` **không** chạy mặc định

### Chạy Docker ở chế độ hot reload

Để sửa code local và thấy thay đổi ngay trong container, dùng thêm file override dev:

```powershell
docker compose -f docker-compose.yml -f docker-compose.dev.yml up -d hv-travel-web
```

Chế độ này:

- mount source code từ máy local vào container
- chạy `dotnet watch` trong container
- vẫn publish web ra `http://localhost:5028`

Nếu muốn chạy kèm MongoDB local:

```powershell
docker compose -f docker-compose.yml -f docker-compose.dev.yml --profile local-db up -d mongodb hv-travel-web
```

Xem log hot reload:

```powershell
docker compose -f docker-compose.yml -f docker-compose.dev.yml logs -f hv-travel-web
```

### Chạy thêm MongoDB local

MongoDB local đang nằm trong profile `local-db`:

```powershell
docker compose --profile local-db up -d
```

Hoặc chỉ chạy MongoDB local:

```powershell
docker compose --profile local-db up -d mongodb
```

MongoDB local được map ra:

- `localhost:27018`

### Cloudflare Tunnel

Repo đã có service `tunnel` dùng:

```yaml
cloudflare/cloudflared:latest
tunnel --no-autoupdate run --token ${LOCAL_TUNNEL_TOKEN}
```

Hostname public được điều khiển từ cấu hình tunnel trên Cloudflare, không phải bởi lệnh `docker compose build`.

Khi tunnel được cấu hình đúng, app có thể được truy cập qua domain public của bạn, ví dụ:

- `https://hv-travel.fshdx2105.id.vn/`

### Các lệnh Docker thường dùng

Xem file:

[docker-commands.md](./docker-commands.md)

## Kiểm thử

Project test hiện có:

- `HV-Travel.Web.Tests`

Chạy test:

```bash
dotnet test HV-Travel.Web.Tests/HV-Travel.Web.Tests.csproj
```

Một số nhóm test hiện có:

- validate bootstrap của support chat
- validate checkbox điều khoản
- regression test cho keyboard shortcut trong chat

## File đáng chú ý

- [HV-Travel.Web/Program.cs](./HV-Travel.Web/Program.cs)
  Nơi đăng ký service, auth scheme, SignalR hub và seeding

- [HV-Travel.Infrastructure/Data/DbInitializer.cs](./HV-Travel.Infrastructure/Data/DbInitializer.cs)
  Seeder dữ liệu và tài khoản mặc định

- [docker-compose.yml](./docker-compose.yml)
  Cấu hình Docker Compose cho web, tunnel và MongoDB local

- [Dockerfile](./Dockerfile)
  Multi-stage build cho ASP.NET Core

- [HV-Travel.Web/README_BOOKING.md](./HV-Travel.Web/README_BOOKING.md)
  Tài liệu chi tiết riêng cho luồng booking

## Ghi chú vận hành

- Ứng dụng hiện có một số warning nullability khi build, nhưng vẫn build và chạy được
- Data Protection key trong container hiện chưa được persist ra volume riêng
- Nếu dùng Cloudflare Tunnel trong Docker, service đích nên là `http://hv-travel-web:8080`
- Nếu dùng MongoDB Atlas, không cần bật service `mongodb`

## Hướng phát triển đề xuất

- thêm `.env.example`
- thêm migration/seed guide rõ ràng hơn cho môi trường mới
- bổ sung test cho controller và view model quan trọng
- chuẩn hóa tài liệu triển khai production
- persist Data Protection keys khi chạy Docker

## License

Hiện repo chưa khai báo license riêng trong README. Nếu đây là dự án nội bộ, nên bổ sung ghi chú sử dụng nội bộ hoặc thêm file `LICENSE` phù hợp.
