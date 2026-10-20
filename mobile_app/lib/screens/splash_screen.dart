import 'package:flutter/material.dart';
import '../services/auth_service.dart';
import '../models/user_token_model.dart';
import 'admin_dashboard_screen.dart';
import 'staff_pos_screen.dart';
import 'user_home_screen.dart';
import 'login_screen.dart';

/// Màn hình Splash kiểm tra Tự động Đăng nhập (Auto-Login)
/// Đọc token từ SharedPreferences, giải mã JWT bằng jwt_decode,
/// nếu token hợp lệ thì chuyển hướng thẳng tới màn hình theo đúng Role.
class SplashScreen extends StatefulWidget {
  const SplashScreen({super.key});

  @override
  State<SplashScreen> createState() => _SplashScreenState();
}

class _SplashScreenState extends State<SplashScreen> {
  final AuthService _authService = AuthService();

  @override
  void initState() {
    super.initState();
    _checkLogin();
  }

  Future<void> _checkLogin() async {
    // Chờ 1.2s tạo hiệu ứng chuyển cảnh mượt mà
    await Future.delayed(const Duration(milliseconds: 1200));

    if (!mounted) return;

    // Kiểm tra token đã lưu trong máy qua SharedPreferences
    final UserTokenModel? user = await _authService.checkAutoLogin();

    if (!mounted) return;

    if (user != null) {
      // Đã có Token hợp lệ và chưa hết hạn -> Điều hướng theo Role
      _navigateToRoleScreen(user);
    } else {
      // Chưa đăng nhập hoặc Token đã hết hạn -> Chuyển sang màn hình Đăng nhập
      Navigator.pushReplacement(
        context,
        MaterialPageRoute(builder: (_) => const LoginScreen()),
      );
    }
  }

  void _navigateToRoleScreen(UserTokenModel user) {
    Widget targetScreen;
    if (user.isAdmin) {
      targetScreen = AdminDashboardScreen(user: user);
    } else if (user.isStaff) {
      targetScreen = StaffPosScreen(user: user);
    } else {
      targetScreen = UserHomeScreen(user: user);
    }

    Navigator.pushReplacement(
      context,
      MaterialPageRoute(builder: (_) => targetScreen),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Container(
        decoration: const BoxDecoration(
          gradient: LinearGradient(
            begin: Alignment.topLeft,
            end: Alignment.bottomRight,
            colors: [Color(0xFF1E3A8A), Color(0xFF3B82F6), Color(0xFF60A5FA)],
          ),
        ),
        child: Center(
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Container(
                padding: const EdgeInsets.all(24),
                decoration: BoxDecoration(
                  color: Colors.white.withValues(alpha: 0.15),
                  shape: BoxShape.circle,
                  border: Border.all(color: Colors.white.withValues(alpha: 0.3), width: 2),
                ),
                child: const Icon(
                  Icons.storefront_rounded,
                  size: 72,
                  color: Colors.white,
                ),
              ),
              const SizedBox(height: 24),
              const Text(
                'POS & STORE SYSTEM',
                style: TextStyle(
                  fontSize: 26,
                  fontWeight: FontWeight.bold,
                  letterSpacing: 1.5,
                  color: Colors.white,
                ),
              ),
              const SizedBox(height: 8),
              Text(
                'ASP.NET Core Identity & JWT Role Auth',
                style: TextStyle(
                  fontSize: 14,
                  color: Colors.white.withValues(alpha: 0.9),
                ),
              ),
              const SizedBox(height: 40),
              const CircularProgressIndicator(
                valueColor: AlwaysStoppedAnimation<Color>(Colors.white),
              ),
              const SizedBox(height: 16),
              Text(
                'Đang kiểm tra phiên đăng nhập...',
                style: TextStyle(
                  fontSize: 13,
                  color: Colors.white.withValues(alpha: 0.8),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
