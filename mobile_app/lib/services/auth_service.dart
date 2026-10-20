import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';
import '../config/app_config.dart';
import '../models/user_token_model.dart';

/// Service quản lý Xác thực tài khoản, Lưu trữ JWT qua SharedPreferences,
/// và Giải mã Token để xác định danh tính & quyền hạn.
class AuthService {
  static const String _tokenKey = 'jwt_token';

  // Singleton pattern
  static final AuthService _instance = AuthService._internal();
  factory AuthService() => _instance;
  AuthService._internal();

  /// Đăng nhập tài khoản
  /// Gửi username/password đến Backend API, nhận JWT token,
  /// lưu vào SharedPreferences và giải mã thông tin quyền hạn.
  Future<Map<String, dynamic>> login(String username, String password) async {
    try {
      final response = await http.post(
        Uri.parse(AppConfig.loginUrl),
        headers: {
          'Content-Type': 'application/json; charset=UTF-8',
          'Accept': 'application/json',
        },
        body: jsonEncode({
          'username': username.trim(),
          'password': password,
        }),
      ).timeout(const Duration(seconds: 15));

      final data = jsonDecode(response.body);

      if (response.statusCode == 200 && data['token'] != null) {
        final String token = data['token'];

        // 1. Lưu token vào máy thông qua SharedPreferences để hỗ trợ tự động đăng nhập
        await saveToken(token);

        // 2. Dùng jwt_decode để giải mã token
        final userModel = UserTokenModel.fromJwt(token);

        return {
          'success': true,
          'message': data['message'] ?? 'Đăng nhập thành công',
          'token': token,
          'user': userModel,
        };
      } else {
        return {
          'success': false,
          'message': data['message'] ?? 'Tài khoản hoặc mật khẩu không chính xác',
        };
      }
    } catch (e) {
      return {
        'success': false,
        'message': 'Không thể kết nối đến máy chủ: $e\nKiểm tra lại cấu hình .env (${AppConfig.baseUrl})',
      };
    }
  }

  /// Đăng ký tài khoản mới (hỗ trợ phân quyền vai trò: Admin, Staff, User)
  Future<Map<String, dynamic>> register({
    required String username,
    required String email,
    required String password,
    required String fullName,
    String role = 'User',
  }) async {
    try {
      final response = await http.post(
        Uri.parse(AppConfig.registerUrl),
        headers: {
          'Content-Type': 'application/json; charset=UTF-8',
          'Accept': 'application/json',
        },
        body: jsonEncode({
          'username': username.trim(),
          'email': email.trim(),
          'password': password,
          'fullName': fullName.trim(),
          'role': role,
        }),
      ).timeout(const Duration(seconds: 15));

      final data = jsonDecode(response.body);

      if (response.statusCode == 200) {
        return {
          'success': true,
          'message': data['message'] ?? 'Đăng ký tài khoản thành công',
        };
      } else {
        return {
          'success': false,
          'message': data['message'] ?? 'Đăng ký thất bại',
        };
      }
    } catch (e) {
      return {
        'success': false,
        'message': 'Không thể kết nối đến máy chủ: $e',
      };
    }
  }

  /// Lưu chuỗi JWT token vào bộ nhớ máy (SharedPreferences)
  Future<void> saveToken(String token) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(_tokenKey, token);
  }

  /// Lấy token đã lưu trong bộ nhớ máy
  Future<String?> getToken() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getString(_tokenKey);
  }

  /// Xóa token khỏi bộ nhớ máy khi người dùng Đăng xuất (Logout)
  Future<void> logout() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove(_tokenKey);
  }

  /// Kiểm tra tự động đăng nhập (Auto-login)
  /// Đọc token từ SharedPreferences -> Giải mã JWT -> Kiểm tra thời hạn -> Trả về User
  Future<UserTokenModel?> checkAutoLogin() async {
    try {
      final token = await getToken();
      if (token == null || token.isEmpty) {
        return null;
      }

      final user = UserTokenModel.fromJwt(token);

      // Nếu token đã hết hạn thì xóa và yêu cầu đăng nhập lại
      if (user.isExpired) {
        await logout();
        return null;
      }

      return user;
    } catch (e) {
      await logout();
      return null;
    }
  }
}
