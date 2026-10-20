import 'package:flutter_dotenv/flutter_dotenv.dart';

/// Quản lý tập trung các đường dẫn API thông qua flutter_dotenv
class AppConfig {
  /// Lấy base URL từ file .env (mặc định nếu không tìm thấy)
  static String get baseUrl {
    return dotenv.env['API_BASE_URL'] ?? 'http://10.0.2.2:5231/api';
  }

  // Các đầu API xác thực (Identity Auth)
  static String get loginUrl => '$baseUrl/auth/login';
  static String get registerUrl => '$baseUrl/auth/register';
  static String get profileUrl => '$baseUrl/auth/me';

  // Các đầu API nghiệp vụ
  static String get productsUrl => '$baseUrl/products';
  static String get postsUrl => '$baseUrl/posts';
  static String get uploadProductImageUrl => '$baseUrl/uploads/product';
  static String get uploadPostImageUrl => '$baseUrl/uploads/post';
}
