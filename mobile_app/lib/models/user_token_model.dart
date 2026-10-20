import 'package:jwt_decode/jwt_decode.dart';

/// Đại diện cho thông tin người dùng được giải mã từ JWT Token
class UserTokenModel {
  final String userId;
  final String username;
  final String email;
  final String fullName;
  final String role; // Quyền người dùng: Admin, Staff, User
  final DateTime? expiryDate;
  final bool isExpired;
  final String rawToken;

  UserTokenModel({
    required this.userId,
    required this.username,
    required this.email,
    required this.fullName,
    required this.role,
    this.expiryDate,
    required this.isExpired,
    required this.rawToken,
  });

  /// Hàm Factory sử dụng jwt_decode để giải mã chuỗi JWT gửi về từ Server
  factory UserTokenModel.fromJwt(String token) {
    // 1. Giải mã Payload của Token
    final Map<String, dynamic> payload = Jwt.parseJwt(token);

    // 2. Kiểm tra thời gian hiệu lực của Token
    final bool expired = Jwt.isExpired(token);
    final DateTime? expiry = Jwt.getExpiryDate(token);

    // 3. Trích xuất thông tin người dùng & quyền hạn (Role)
    final String id = payload['nameid'] ?? payload['sub'] ?? '';
    final String uName = payload['unique_name'] ?? payload['name'] ?? '';
    final String uEmail = payload['email'] ?? '';
    final String fName = payload['FullName'] ?? uName;

    // Xử lý quyền Role (hỗ trợ cả schema chuẩn Microsoft và key ngắn)
    dynamic rawRole = payload['role'] ??
        payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];

    String userRole = 'User';
    if (rawRole is List && rawRole.isNotEmpty) {
      userRole = rawRole.first.toString();
    } else if (rawRole is String && rawRole.isNotEmpty) {
      userRole = rawRole;
    }

    return UserTokenModel(
      userId: id,
      username: uName,
      email: uEmail,
      fullName: fName,
      role: userRole,
      expiryDate: expiry,
      isExpired: expired,
      rawToken: token,
    );
  }

  bool get isAdmin => role.toLowerCase() == 'admin';
  bool get isStaff => role.toLowerCase() == 'staff';
  bool get isUser => role.toLowerCase() == 'user';
}
