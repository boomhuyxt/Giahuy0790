# HỆ THỐNG POS & BÁN HÀNG TIỆN LỢI - QUẢN LÝ BÀI ĐĂNG
### ASP.NET Core 10 Web API & Flutter Mobile App (Identity & Phân quyền JWT)

Dự án cung cấp giải pháp toàn diện bao gồm:
1. **Backend Web API (.NET 10)**: Tích hợp ASP.NET Core Identity, xác thực JWT phân quyền (Role-based Authorization), Repository Pattern, kết nối cơ sở dữ liệu Supabase PostgreSQL (hỗ trợ Session Pooler), tải ảnh trực tiếp, và đóng gói Docker container chạy trên cổng **8888**.
2. **Web Frontend (Razor Views + Dark Mode)**: Giao diện quản lý Sản phẩm và Bài đăng (Facebook-like feed) với cơ chế chuyển đổi Dark/Light mode tự động và gọi AJAX RESTful API.
3. **Mobile App (Flutter)**: Ứng dụng di động phân quyền cho **Admin**, **Staff** và **User**, tích hợp `jwt_decode`, `flutter_dotenv`, và `shared_preferences` để tự động đăng nhập (Auto-login).

---

## 1. THÔNG TIN TÀI KHOẢN & PHÂN QUYỀN (ROLES)

Hệ thống được cấu hình sẵn cơ chế Seeder tự động nạp 3 tài khoản mặc định đại diện cho 3 phân quyền khi khởi động:

| Tài khoản (Username) | Mật khẩu (Password) | Vai trò (Role) | Phạm vi quyền hạn & Giao diện trên Mobile App |
| :--- | :--- | :--- | :--- |
| **`admin`** | `Admin@123` | **Admin** *(Quản trị viên)* | - Toàn quyền quản trị hệ thống, quản lý người dùng & cấp quyền.<br>- Quản lý danh mục sản phẩm, duyệt và quản lý toàn bộ bài đăng.<br>- Xem báo cáo doanh thu & hóa đơn POS.<br>- **Màn hình Mobile**: `AdminDashboardScreen` (Giao diện Quản trị viền đỏ). |
| **`staff`** | `Staff@123` | **Staff** *(Nhân viên POS & Kho)* | - Bán hàng tại quầy thu ngân (POS) và in hóa đơn.<br>- Quét mã vạch (Barcode Scanner) kiểm tra giá sản phẩm.<br>- Tra cứu và kiểm kê hàng tồn kho.<br>- **Màn hình Mobile**: `StaffPosScreen` (Giao diện Thu ngân viền cam). |
| **`user`** | `User@123` | **User** *(Khách hàng)* | - Xem danh sách sản phẩm tiện lợi & tìm kiếm sản phẩm.<br>- Xem và tương tác bảng tin bài viết chia sẻ phong cách Facebook.<br>- Giỏ hàng và lịch sử đơn hàng cá nhân.<br>- **Màn hình Mobile**: `UserHomeScreen` (Giao diện Mua sắm viền xanh). |

> 💡 **Mẹo:** Trên màn hình đăng nhập của ứng dụng di động Flutter có sẵn 3 nút nhấn nhanh **Admin / Staff / User**. Bạn chỉ cần bấm vào nút tương ứng là form tự động điền tài khoản và mật khẩu để test nhanh.

---

## 2. HƯỚNG DẪN CHẠY DỰ ÁN

### Yêu cầu môi trường
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Flutter SDK](https://flutter.dev) (phiên bản >= 3.0.0)
- [Docker Desktop](https://www.docker.com/) (nếu chạy bằng Docker)

---

### Cách 1: Chạy Backend trực tiếp qua .NET CLI

1. Mở cửa sổ dòng lệnh tại thư mục gốc của dự án (`Giahuy0790`):
   ```bash
   dotnet restore
   dotnet run
   ```
2. Ứng dụng sẽ khởi chạy tại:
   - **Giao diện Web & Swagger API**: [http://localhost:5231/swagger](http://localhost:5231/swagger)
   - **Giao diện Quản lý Web (Có Dark Mode)**: [http://localhost:5231](http://localhost:5231)

---

### Cách 2: Chạy Backend qua Docker Container (Cổng 8888)

Dự án đã có sẵn [Dockerfile](file:///C:/Users/boomh/OneDrive/Documents/Giahuy0790/Dockerfile) cấu hình đa tầng (multi-stage build) chạy độc lập trên cổng `8888`:

1. **Build Docker Image:**
   ```bash
   docker build -t pos-backend:10.0 .
   ```

2. **Chạy Docker Container:**
   ```bash
   docker run -d -p 8888:8888 --name pos_container pos-backend:10.0
   ```

3. Truy cập hệ thống:
   - **Swagger UI**: [http://localhost:8888/swagger](http://localhost:8888/swagger)
   - **Web UI**: [http://localhost:8888](http://localhost:8888)

---

### Cách 3: Chạy Ứng dụng Di Động Flutter (`mobile_app`)

1. **Di chuyển vào thư mục ứng dụng di động:**
   ```bash
   cd mobile_app
   ```

2. **Tải các gói thư viện phụ thuộc:**
   ```bash
   flutter pub get
   ```

3. **Cấu hình địa chỉ IP Backend trong file `.env`:**
   Mở file [`mobile_app/.env`](file:///C:/Users/boomh/OneDrive/Documents/Giahuy0790/mobile_app/.env) và chọn đúng URL tùy theo môi trường chạy:
   ```properties
   # 1. Nếu chạy trên Android Emulator:
   API_BASE_URL=http://10.0.2.2:5231/api

   # 2. Nếu chạy trên Windows App hoặc Chrome Web:
   # API_BASE_URL=http://localhost:5231/api

   # 3. Nếu chạy qua Docker:
   # API_BASE_URL=http://10.0.2.2:8888/api

   # 4. Nếu chạy trên máy thật kết nối cùng mạng WiFi:
   # API_BASE_URL=http://192.168.1.x:5231/api
   ```

4. **Khởi chạy ứng dụng Flutter:**
   ```bash
   flutter run
   ```

---

## 3. CƠ CHẾ BẢO MẬT & XỬ LÝ PHÍA FLUTTER

### A. Quản lý URL API tách biệt với `flutter_dotenv`
- File `.env` lưu trữ các đường dẫn API và biến môi trường nhạy cảm, không bị mã hóa cứng (hard-coded) trong mã Dart.
- Lớp `AppConfig` nạp dữ liệu từ `.env` giúp dễ dàng chuyển đổi môi trường dev, staging hoặc production mà không cần sửa code.

### B. Giải mã Token & Kiểm tra Thời hạn với `jwt_decode`
- Khi người dùng đăng nhập thành công, Server trả về một chuỗi **JWT Token**.
- Lớp `UserTokenModel.fromJwt(token)` sử dụng `jwt_decode` để:
  1. Trích xuất Payload: User ID (`nameid`/`sub`), Username, Email, Họ tên (`FullName`).
  2. Trích xuất vai trò: `role` (Admin, Staff, User).
  3. Kiểm tra hạn sử dụng: `Jwt.isExpired(token)` và `Jwt.getExpiryDate(token)`.
- Dựa vào kết quả này, ứng dụng sẽ tự động chuyển hướng đến đúng giao diện chức năng tương ứng của người dùng.

### C. Lưu trữ Token & Tự động Đăng nhập với `shared_preferences`
- Token sau khi nhận từ API được lưu trực tiếp vào bộ nhớ cục bộ của thiết bị thông qua `SharedPreferences`.
- Mỗi lần mở app, `SplashScreen` sẽ đọc Token này lên:
  - Nếu **có Token và Token chưa hết hạn**: Tự động chuyển thẳng vào màn hình làm việc tương ứng với quyền của tài khoản.
  - Nếu **chưa có hoặc Token đã hết hạn**: Tự động xóa dữ liệu và chuyển về màn hình Đăng nhập (`LoginScreen`).
- Khi người dùng chọn **Đăng xuất (Logout)**: Hệ thống sẽ gọi lệnh xóa token trong `SharedPreferences` và đưa người dùng về `LoginScreen`.

---

## 4. TÀI LIỆU RESTful API

### 1. Nhóm Xác thực & Phân quyền (`/api/auth`)

| Phương thức | Đường dẫn API | Mô tả chức năng | Yêu cầu quyền |
| :---: | :--- | :--- | :---: |
| `POST` | `/api/auth/register` | Đăng ký tài khoản mới (hỗ trợ chỉ định role: Admin, Staff, User) | Public |
| `POST` | `/api/auth/login` | Đăng nhập hệ thống, nhận JWT Token và thông tin vai trò | Public |
| `GET` | `/api/auth/me` | Lấy thông tin tài khoản đang đăng nhập từ Token | `Bearer JWT` |

#### Payload mẫu Đăng ký (`POST /api/auth/register`):
```json
{
  "username": "nguyenvana",
  "email": "vana@example.com",
  "password": "Password@123",
  "fullName": "Nguyễn Văn A",
  "role": "Staff"
}
```

#### Response mẫu Đăng nhập (`POST /api/auth/login`):
```json
{
  "message": "Đăng nhập thành công",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "username": "admin",
  "fullName": "System Administrator",
  "roles": ["Admin"]
}
```

---

### 2. Nhóm Quản lý Sản phẩm (`/api/products`)

| Phương thức | Đường dẫn API | Mô tả chức năng |
| :---: | :--- | :--- |
| `GET` | `/api/products` | Lấy toàn bộ danh sách sản phẩm |
| `GET` | `/api/products/{id}` | Lấy chi tiết sản phẩm theo ID |
| `GET` | `/api/products/search?name={tên}` | Tìm kiếm sản phẩm theo tên (không phân biệt hoa thường) |
| `POST` | `/api/products` | Thêm mới sản phẩm |
| `PUT` | `/api/products/{id}` | Chỉnh sửa thông tin sản phẩm |
| `DELETE` | `/api/products/{id}` | Xóa sản phẩm theo ID |

#### Payload mẫu Thêm Sản Phẩm:
```json
{
  "name": "Bánh mì que Pate Hải Phòng",
  "price": 15000,
  "image": "/uploads/products/banhmi.jpg",
  "description": "Bánh mì que giòn rụm với pate thơm ngon đặc sản"
}
```

---

### 3. Nhóm Quản lý Bài Đăng Bảng Tin (`/api/posts`)

| Phương thức | Đường dẫn API | Mô tả chức năng |
| :---: | :--- | :--- |
| `GET` | `/api/posts` | Lấy danh sách bài đăng (tự động xếp mới nhất lên đầu) |
| `GET` | `/api/posts/{id}` | Lấy thông tin chi tiết bài viết theo ID |
| `POST` | `/api/posts` | Đăng bài viết mới (tương tự Facebook post) |
| `PUT` | `/api/posts/{id}` | Cập nhật bài viết |
| `DELETE` | `/api/posts/{id}` | Xóa bài viết |

#### Payload mẫu Đăng Bài:
```json
{
  "title": "Chương trình khuyến mãi mùa hè 2026",
  "content": "Giảm giá 20% cho toàn bộ sản phẩm nước giải khát!",
  "author": "Ban Quản Trị",
  "imageUrl": "/uploads/posts/khuyenmai.jpg"
}
```

---

### 4. Nhóm Tải lên Hình Ảnh (`/api/uploads`)

- `POST /api/uploads/product`: Nhận file ảnh qua `multipart/form-data` (key: `file`), tự động lưu vào thư mục `wwwroot/uploads/products/` và trả về đường dẫn URL.
- `POST /api/uploads/post`: Nhận file ảnh qua `multipart/form-data` (key: `file`), tự động lưu vào thư mục `wwwroot/uploads/posts/` và trả về đường dẫn URL.

---

## 5. CẤU TRÚC THƯ MỤC TOÀN BỘ DỰ ÁN

```text
Giahuy0790/
├── Controllers/                  # Controller xử lý các đầu API
│   ├── AuthController.cs         # Xác thực Identity & tạo JWT
│   ├── ProductsController.cs     # CRUD & Tìm kiếm sản phẩm
│   ├── PostsController.cs        # CRUD Bài viết mạng xã hội
│   ├── UploadsController.cs      # Upload ảnh sản phẩm & bài viết
│   └── HomeController.cs         # Giao diện Web MVC
├── Data/
│   └── AppDbContext.cs           # EF Core IdentityDbContext nối Supabase
├── Models/
│   ├── ApplicationUser.cs        # Model người dùng mở rộng từ IdentityUser
│   ├── Product.cs                # Model bảng Product
│   ├── Post.cs                   # Model bảng Post
│   └── DTOs/                     # Data Transfer Objects (Login, Register)
├── Repositories/                 # Repository Pattern phân tách logic truy vấn
│   ├── IRepository.cs / Repository.cs
│   ├── IProductRepository.cs / ProductRepository.cs
│   └── IPostRepository.cs / PostRepository.cs
├── Views/                        # Giao diện Web Razor Views (Dark Mode)
│   ├── Home/
│   │   ├── Index.cshtml          # Quản lý Sản phẩm
│   │   └── Posts.cshtml          # Quản lý Bài đăng (Facebook-like)
│   └── Shared/                   # Layout & thanh chuyển Dark Mode
├── wwwroot/                      # Thư mục tài nguyên tĩnh
│   └── uploads/                  # Nơi lưu trữ ảnh tải lên (products & posts)
├── appsettings.json              # Chuỗi kết nối Supabase & Khóa bí mật JWT
├── Dockerfile                    # Kịch bản đóng gói container chạy cổng 8888
├── Program.cs                    # Cấu hình dịch vụ, JWT, Identity & Data Seeder
└── mobile_app/                   # Dự án ứng dụng di động Flutter
    ├── .env                      # File cấu hình biến môi trường API_BASE_URL
    ├── pubspec.yaml              # Khai báo jwt_decode, flutter_dotenv, shared_preferences
    └── lib/
        ├── config/               # Cấu hình AppConfig lấy từ dotenv
        ├── models/               # Model UserTokenModel giải mã JWT
        ├── services/             # AuthService gọi API, lưu cache máy
        ├── screens/              # Giao diện Splash, Login, Register, Admin, Staff, User
        └── main.dart             # Điểm bắt đầu ứng dụng di động
```

---

## 6. THÔNG TIN CƠ SỞ DỮ LIỆU (SUPABASE POSTGRESQL)

Dự án kết nối trực tiếp đến hệ thống cơ sở dữ liệu Supabase thông qua **Session Pooler**:
- **Host**: `aws-0-ap-southeast-1.pooler.supabase.com`
- **Port**: `5432` *(Session Mode - hỗ trợ đầy đủ các thao tác Migration & Identity DDL)*
- **Database**: `postgres`
- **Username**: `postgres.fddbvowrgywnlcingnnc`
- **Cơ chế tự động**: Cờ `"AutoMigrateOnStartup": true` trong `appsettings.json` giúp ứng dụng tự động kiểm tra và chạy migration EF Core mỗi khi khởi động server, đảm bảo bảng dữ liệu luôn được đồng bộ.

---

## 7. LIÊN HỆ & TÁC GIẢ
- Dự án phát triển phục vụ học phần Lập trình Di động & Web API.
- Tác giả: **Gia Huy** (MSSV: `0790`).
