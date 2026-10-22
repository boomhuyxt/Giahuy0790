# HỆ THỐNG GIÁM SÁT TRẺ EM, BỘ ĐÀM GIA ĐÌNH & QUẢN LÝ TIỆN LỢI (KIDCARE & POS)
### Đồ án Chuyên ngành: ASP.NET Core 10 Web API, SignalR & Flutter Mobile App
**Tác giả:** Diệp Gia Huy | **MSSV:** 0790  
**Tệp tin Database đính kèm:** [`DiepGiaHuy0790.sql`](file:///C:/Users/boomh/OneDrive/Documents/Giahuy0790/DiepGiaHuy0790.sql)

---

## 1. TỔNG QUAN HỆ THỐNG

Dự án cung cấp một nền tảng giám sát thông minh và tương tác toàn diện giữa Phụ huynh và Trẻ em, kết hợp quản lý tiện ích bán hàng và sinh hoạt gia đình:

1. **Backend Web API (.NET 10)**:
   - Xây dựng trên nền tảng **ASP.NET Core 10**.
   - Quản lý danh tính người dùng và phân quyền bằng **ASP.NET Core Identity** kết hợp **JWT (JSON Web Token)**.
   - Cơ chế bảo mật 2 lớp: Xác thực đăng nhập qua mã **OTP gửi về Email (Gmail SMTP)**.
   - Tương tác thời gian thực hai chiều bằng **SignalR WebSockets** (`LocationHub`).
   - Sử dụng **Entity Framework Core** kết nối cơ sở dữ liệu **PostgreSQL (Supabase)** thông qua cơ chế Transaction & Session Pooler.
   - Kiến trúc **Repository Pattern** tách biệt rõ ràng giữa tầng dữ liệu và tầng nghiệp vụ.
   - Đóng gói container hóa qua **Docker & Docker Compose** chạy độc lập trên cổng **8888** với cấu hình phân quyền root chuẩn cho Linux.

2. **Web Frontend (Razor Pages / MVC & Dark Mode Apple UI)**:
   - Giao diện thiết kế theo ngôn ngữ hiện đại của Apple (Apple Human Interface Guidelines), hỗ trợ chuyển đổi giao diện Sáng / Tối (Dark/Light Mode).
   - Hệ thống **Bộ đàm & Đàm thoại gia đình (Voice Intercom / Walkie-Talkie)**: Thu âm trực tiếp từ microphone trình duyệt, mã hóa sang file định dạng **MP3 128kbps** theo thời gian thực bằng **Web Audio API** và thư viện **LAME.js** (`lame.min.js`), phát và đồng bộ tức thì qua SignalR.
   - Bản đồ theo dõi lộ trình GPS di chuyển của trẻ và thiết lập vùng an toàn Geofencing.

3. **Mobile App (Flutter)**:
   - Ứng dụng di động đa nền tảng viết bằng Flutter (Dart).
   - Phân quyền theo vai trò (**Admin**, **Staff**, **User**), tích hợp `jwt_decode`, `flutter_dotenv` và `shared_preferences` hỗ trợ ghi nhớ đăng nhập (Auto-login).

4. **Cơ sở dữ liệu (Database SQL)**:
   - Tệp tin [`DiepGiaHuy0790.sql`](file:///C:/Users/boomh/OneDrive/Documents/Giahuy0790/DiepGiaHuy0790.sql) chứa toàn bộ kịch bản DDL tạo bảng, chỉ mục (indexes), khóa chính/khóa ngoại và dữ liệu khởi tạo mặc định cho hệ thống PostgreSQL.

---

## 2. PHÂN QUYỀN HỆ THỐNG & TÀI KHOẢN MẪU

Hệ thống được cấu hình sẵn 5 nhóm vai trò (Roles) chính:

| Vai trò (Role) | Mô tả & Phạm vi quyền hạn | Tài khoản mẫu | Mật khẩu mẫu |
| :--- | :--- | :---: | :---: |
| **`Admin`** | Quản trị toàn hệ thống, quản lý người dùng, duyệt dữ liệu, xem toàn bộ hồ sơ trẻ và báo cáo | `admin` | `Admin@123` |
| **`Parents`** | Phụ huynh: Định vị vị trí con, giao nhiệm vụ, giới hạn giờ dùng máy, chặn web/app, gửi voice walkie-talkie | *(Tạo khi đăng ký)* | - |
| **`Kid`** | Trẻ em: Đăng nhập vào thiết bị trẻ, gửi tọa độ GPS định kỳ, gửi tin nhắn thoại bộ đàm, xem nhiệm vụ | *(Gán từ hồ sơ bé)* | - |
| **`Staff`** | Nhân viên quầy thu ngân tiện lợi (POS), kiểm kê hàng hóa | `staff` | `Staff@123` |
| **`User`** | Người dùng thành viên, tra cứu thông tin | `user` | `User@123` |

---

## 3. CƠ SỞ DỮ LIỆU & FILE SQL (`DiepGiaHuy0790.sql`)

### A. Thông tin kết nối CSDL (PostgreSQL / Supabase)
- **Host**: `aws-0-ap-southeast-1.pooler.supabase.com`
- **Port**: `5432` *(Session Pooler)*
- **Database**: `postgres`
- **Cơ chế tự động**: Cờ `"AutoMigrateOnStartup": true` trong `appsettings.json` cho phép ứng dụng tự động kiểm tra và áp dụng migrations mỗi khi khởi động.

### B. Danh sách các bảng chính trong Database

| Tên bảng | Mô tả chi tiết |
| :--- | :--- |
| `AspNetUsers` | Thông tin tài khoản người dùng, họ tên, email, ảnh đại diện, mã OTP đăng nhập |
| `AspNetRoles` | Danh sách nhóm quyền (`Admin`, `Parents`, `Kid`, `Staff`, `User`) |
| `kids` | Hồ sơ trẻ em: Họ tên, biệt danh, ngày sinh, giới tính, mã PIN mở máy, điểm thưởng, mã tài khoản |
| `family_groups` | Nhóm gia đình, mã chia sẻ gia đình (`FamilyCode`) |
| `family_members` | Danh sách thành viên và mối quan hệ (Bố, Mẹ, Con, Người giám hộ) |
| `family_qr_invites` | Mã QR mời tham gia gia đình tạm thời (thời hạn 60 giây) |
| `kid_locations` | Nhật ký tọa độ GPS: Kinh độ, vĩ độ, độ cao, tốc độ, mức pin, trạng thái sạc, địa chỉ |
| `kid_geofences` | Thiết lập Vùng an toàn: Tọa độ tâm, bán kính (mét), cờ cảnh báo vào/ra vùng an toàn |
| `voice_messages` | Tin nhắn thoại / Bộ đàm: Đường dẫn file âm thanh MP3, thời lượng (giây), dung lượng, trạng thái đã nghe |
| `kid_screen_times` | Cấu hình giới hạn thời gian sử dụng thiết bị của trẻ: Tổng phút/ngày, khung giờ cấm, cờ khóa máy tức thì |
| `blocked_items` | Danh sách ứng dụng và trang web bị phụ huynh chặn trên thiết bị trẻ |
| `kid_activities` | Nhật ký sinh hoạt, danh sách bài tập/nhiệm vụ và điểm thưởng tương ứng |

### C. Hướng dẫn nạp Database thủ công bằng `DiepGiaHuy0790.sql`
Nếu muốn tạo một Database mới hoàn toàn trên PostgreSQL / Supabase / pgAdmin:
1. Mở công cụ quản trị CSDL (**DBeaver**, **pgAdmin** hoặc **Supabase SQL Editor**).
2. Mở tệp tin [`DiepGiaHuy0790.sql`](file:///C:/Users/boomh/OneDrive/Documents/Giahuy0790/DiepGiaHuy0790.sql).
3. Nhấn **Execute / Run** để tạo toàn bộ bảng, chỉ mục và dữ liệu phân quyền khởi tạo.

---

## 4. HƯỚNG DẪN KHỞI CHẠY DỰ ÁN

### Yêu cầu môi trường
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- [Docker & Docker Compose](https://www.docker.com/) (nếu chạy qua Docker)
- [Flutter SDK](https://flutter.dev) (nếu chạy Mobile App)

---

### Cách 1: Chạy Backend trực tiếp trên máy cục bộ (.NET CLI)

1. Mở Terminal tại thư mục dự án `Giahuy0790`:
   ```bash
   dotnet restore
   dotnet run
   ```
2. Truy cập ứng dụng:
   - **Tài liệu Swagger API**: [http://localhost:5231/swagger](http://localhost:5231/swagger)
   - **Giao diện Web Quản trị**: [http://localhost:5231](http://localhost:5231)
   - **Bộ đàm thoại gia đình (Voice Intercom)**: [http://localhost:5231/Home/VoiceIntercom](http://localhost:5231/Home/VoiceIntercom)

---

### Cách 2: Chạy Backend trên Docker / Linux (Khuyến nghị cho Server)

Dự án đã được cấu hình tối ưu sẵn tệp tin [`Dockerfile`](file:///C:/Users/boomh/OneDrive/Documents/Giahuy0790/Dockerfile) và [`docker-compose.yml`](file:///C:/Users/boomh/OneDrive/Documents/Giahuy0790/docker-compose.yml) trên cổng **8888**:

1. **Khởi động với Docker Compose:**
   ```bash
   docker compose up -d --build
   ```

2. **Lưu ý phân quyền trên Linux khi upload file:**
   Trên hệ điều hành Linux, để ứng dụng ghi được file ảnh và âm thanh vào volume mount `wwwroot/uploads`:
   ```bash
   sudo chown -R $USER:$USER wwwroot
   sudo chmod -R 777 wwwroot/uploads
   ```

3. **Truy cập ứng dụng:**
   - **Swagger UI**: `http://<IP_SERVER>:8888/swagger`
   - **Web UI**: `http://<IP_SERVER>:8888`

---

### Cách 3: Chạy ứng dụng di động Flutter (`mobile_app`)

1. Di chuyển vào thư mục:
   ```bash
   cd mobile_app
   flutter pub get
   ```
2. Cấu hình địa chỉ IP Backend trong file `mobile_app/.env`:
   ```properties
   # Cho máy ảo Android Emulator kết nối với Backend máy chủ
   API_BASE_URL=http://10.0.2.2:5231/api
   ```
3. Chạy ứng dụng:
   ```bash
   flutter run
   ```

---

## 5. TÀI LIỆU CHI TIẾT CÁC ĐẦU API (RESTful API REFERENCE)

### 1. Nhóm Xác thực & Tài khoản (`/api/auth`)

| Phương thức | Đường dẫn API | Mô tả chức năng | Quyền hạn |
| :---: | :--- | :--- | :---: |
| `POST` | `/api/auth/register` | Đăng ký tài khoản mới (hỗ trợ chọn vai trò: Admin, Parents, Staff, User) | Public |
| `POST` | `/api/auth/login` | Đăng nhập tài khoản, trả về JWT Token và thông tin vai trò | Public |
| `POST` | `/api/auth/send-login-otp` | Gửi mã xác thực OTP 6 số qua Email (Gmail SMTP) để đăng nhập không cần mật khẩu | Public |
| `POST` | `/api/auth/verify-login-otp` | Xác thực mã OTP và nhận JWT Token đăng nhập | Public |
| `GET` | `/api/auth/me` | Lấy thông tin tài khoản và phân quyền của người dùng hiện tại | `Bearer JWT` |

---

### 2. Nhóm Định vị & Giám sát Vị trí Trẻ (`/api/kids/{kidId}/locations`)

| Phương thức | Đường dẫn API | Mô tả chức năng |
| :---: | :--- | :--- |
| `POST` | `/api/kids/{kidId}/locations` | Thiết bị của trẻ gửi tọa độ GPS hiện tại lên hệ thống (tự động phát cảnh báo ra/vào vùng an toàn và gửi realtime qua SignalR) |
| `GET` | `/api/kids/{kidId}/locations/latest` | Lấy thông tin tọa độ, pin, địa chỉ mới nhất của trẻ |
| `GET` | `/api/kids/{kidId}/locations/history?date={yyyy-MM-dd}` | Lấy lịch sử lộ trình di chuyển của trẻ theo ngày |
| `POST` | `/api/kids/{kidId}/locations/simulate?step={n}` | Chạy mô phỏng di chuyển GPS thực tế (phục vụ test) |
| `GET` | `/api/kids/{kidId}/locations/geofences` | Lấy danh sách vùng an toàn đã thiết lập cho trẻ |
| `POST` | `/api/kids/{kidId}/locations/geofences` | Tạo vùng an toàn mới (tọa độ tâm, bán kính mét, cảnh báo vào/ra) |
| `DELETE` | `/api/kids/{kidId}/locations/geofences/{geofenceId}` | Xóa vùng an toàn |

#### Mẫu JSON gửi tọa độ GPS (`POST /api/kids/{kidId}/locations`):
```json
{
  "latitude": 21.028511,
  "longitude": 105.854167,
  "accuracy": 10.0,
  "speed": 15.2,
  "altitude": 15.0,
  "batteryLevel": 88,
  "isCharging": false,
  "address": "Trường Tiểu Học Kim Đồng, Hà Nội"
}
```

---

### 3. Nhóm Bộ đàm & Tin nhắn thoại (`/api/voicemessages`)

| Phương thức | Đường dẫn API | Mô tả chức năng |
| :---: | :--- | :--- |
| `GET` | `/api/voicemessages?kidId={id}` | Lấy danh sách tin nhắn thoại giữa Phụ huynh và Bé |
| `POST` | `/api/voicemessages/send` | Gửi tin nhắn thoại mới (nhận file qua `multipart/form-data`, lưu file MP3 vào server và phát tín hiệu SignalR) |
| `PUT` | `/api/voicemessages/{id}/mark-heard` | Đánh dấu tin nhắn thoại đã được nghe |
| `DELETE` | `/api/voicemessages/{id}` | Xóa tin nhắn thoại và xóa file âm thanh trên đĩa cứng |
| `GET` | `/api/voicemessages/unread-count` | Lấy số lượng tin nhắn thoại mới chưa nghe |

---

### 4. Nhóm Quản lý Hồ sơ Trẻ & Gia đình (`/api/kids` & `/api/family`)

| Phương thức | Đường dẫn API | Mô tả chức năng |
| :---: | :--- | :--- |
| `GET` | `/api/kids` | Lấy danh sách trẻ thuộc sự quản lý của phụ huynh đang đăng nhập |
| `POST` | `/api/kids` | Tạo hồ sơ trẻ mới (họ tên, ngày sinh, mã PIN mở máy) |
| `GET` | `/api/kids/{id}` | Lấy chi tiết hồ sơ bé kèm danh sách cảnh báo vi phạm |
| `GET` | `/api/family/my-group` | Lấy thông tin nhóm gia đình và danh sách các thành viên |
| `POST` | `/api/family/create` | Tạo nhóm gia đình mới và sinh mã `FamilyCode` |
| `POST` | `/api/family/join` | Tham gia vào gia đình bằng mã mời `FamilyCode` |
| `POST` | `/api/family/qr-invite` | Sinh mã QR mời tham gia gia đình (hiệu lực trong 60 giây) |

---

### 5. Nhóm Thời gian sử dụng & Khóa máy (`/api/screentime`)

| Phương thức | Đường dẫn API | Mô tả chức năng |
| :---: | :--- | :--- |
| `GET` | `/api/screentime/{kidId}` | Lấy cấu hình giới hạn thời gian sử dụng thiết bị của bé |
| `POST` | `/api/screentime/{kidId}` | Cập nhật cấu hình thời gian sử dụng (số phút/ngày, khung giờ cấm) |
| `POST` | `/api/screentime/{kidId}/instant-lock` | Khóa máy hoặc mở khóa máy tức thì từ xa |

---

### 6. Nhóm Chặn Web & Ứng dụng (`/api/blockeditems`)

| Phương thức | Đường dẫn API | Mô tả chức năng |
| :---: | :--- | :--- |
| `GET` | `/api/blockeditems/{kidId}` | Lấy danh sách website và ứng dụng bị chặn của bé |
| `POST` | `/api/blockeditems/{kidId}` | Thêm một website hoặc ứng dụng vào danh sách chặn |
| `PUT` | `/api/blockeditems/{id}/toggle` | Bật / Tắt trạng thái chặn |
| `DELETE` | `/api/blockeditems/{id}` | Xóa mục khỏi danh sách chặn |

---

### 7. Nhóm Nhật ký & Nhiệm vụ của trẻ (`/api/kidactivities`)

| Phương thức | Đường dẫn API | Mô tả chức năng |
| :---: | :--- | :--- |
| `GET` | `/api/kidactivities/kid/{kidId}` | Lấy danh sách hoạt động và nhiệm vụ của trẻ |
| `POST` | `/api/kidactivities` | Giao một nhiệm vụ mới cho bé (kèm điểm thưởng `RewardPoints`) |
| `PUT` | `/api/kidactivities/{id}/status` | Cập nhật trạng thái nhiệm vụ (tự động cộng điểm khi chuyển sang `Completed`) |
| `DELETE` | `/api/kidactivities/{id}` | Xóa nhiệm vụ / hoạt động |

---

### 8. Kênh Real-time SignalR (`/hubs/location`)
- **Hub Endpoint**: `/hubs/location`
- **Group tham gia**: `kid_{kidId}`
- **Sự kiện lắng nghe chính**:
  - `ReceiveLocationUpdate`: Nhận tọa độ GPS mới nhất và cảnh báo Geofence.
  - `ReceiveVoiceMessage`: Nhận file âm thanh tin nhắn thoại mới từ bộ đàm.
  - `NewVoiceNotification`: Thông báo có tin nhắn thoại mới cần phát.

---

## 6. CẤU TRÚC THƯ MỤC DỰ ÁN

```text
Giahuy0790/
├── Controllers/                  # Các bộ xử lý RESTful Web API
│   ├── AuthController.cs         # Xác thực JWT, Identity & Email OTP
│   ├── KidsController.cs         # Quản lý hồ sơ trẻ và mã PIN
│   ├── FamilyController.cs       # Nhóm gia đình & QR invite
│   ├── LocationsController.cs    # Định vị GPS, Geofencing & Simulation
│   ├── VoiceMessagesController.cs# Bộ đàm thoại & upload file MP3
│   ├── ScreenTimeController.cs   # Quản lý thời gian sử dụng & khóa máy
│   ├── BlockedItemsController.cs # Chặn Web & Ứng dụng độc hại
│   ├── KidActivitiesController.cs# Nhiệm vụ, nhật ký & điểm thưởng
│   ├── UploadsController.cs      # Upload ảnh đại diện và hồ sơ
│   └── HomeController.cs         # Giao diện Web MVC
├── Data/
│   └── AppDbContext.cs           # Entity Framework Core kết nối Supabase
├── Hubs/
│   └── LocationHub.cs            # SignalR Hub cho GPS và Voice Real-Time
├── Migrations/                   # Các bản ghi chuyển đổi CSDL của EF Core
├── Models/                       # Các thực thể dữ liệu (Entities) & DTOs
├── Views/                        # Giao diện Razor Pages (Apple Design & Dark Mode)
│   ├── Home/
│   │   ├── VoiceIntercom.cshtml  # Giao diện Bộ đàm thoại gia đình MP3
│   │   └── ...
│   └── Shared/
├── wwwroot/                      # Thư mục tài nguyên tĩnh
│   ├── js/lame.min.js            # Thư viện mã hóa MP3 LAME.js
│   └── uploads/                  # Thư mục lưu trữ tệp tin upload
│       ├── voice/                # Lưu file tin nhắn thoại MP3
│       └── avatars/              # Lưu ảnh đại diện
├── DiepGiaHuy0790.sql            # Script SQL tạo CSDL và phân quyền hoàn chỉnh
├── Dockerfile                    # Kịch bản đóng gói container chạy cổng 8888
├── docker-compose.yml            # Điều phối dịch vụ Docker với volume uploads
├── appsettings.json              # Chuỗi kết nối CSDL và cấu hình bảo mật
├── Program.cs                    # Cấu hình dịch vụ, SignalR, Identity, Seeder
└── mobile_app/                   # Mã nguồn ứng dụng di động Flutter
    ├── lib/                      # Logic màn hình, mô hình và dịch vụ
    └── pubspec.yaml              # Thư viện phụ thuộc của Flutter
```

---

## 7. LIÊN HỆ & BẢN QUYỀN
- Đồ án được thiết kế và hoàn thiện bởi: **Diệp Gia Huy (MSSV: 0790)**.
- Mọi thắc mắc kỹ thuật vui lòng liên hệ qua email: `boomhuyxt@gmail.com`.
