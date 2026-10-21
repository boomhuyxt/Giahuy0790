import 'package:flutter/material.dart';
import '../services/auth_service.dart';
import '../models/user_token_model.dart';
import 'admin_dashboard_screen.dart';
import 'staff_pos_screen.dart';
import 'user_home_screen.dart';
import 'login_screen.dart';

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
    await Future.delayed(const Duration(milliseconds: 1000));

    if (!mounted) return;

    final UserTokenModel? user = await _authService.checkAutoLogin();

    if (!mounted) return;

    if (user != null) {
      _navigateToRoleScreen(user);
    } else {
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
      backgroundColor: const Color(0xFFF5F5F7),
      body: Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Container(
              width: 76,
              height: 76,
              decoration: BoxDecoration(
                color: const Color(0xFF0071E3),
                borderRadius: BorderRadius.circular(22),
                boxShadow: const [
                  BoxShadow(
                    color: Color(0x330071E3),
                    blurRadius: 20,
                    offset: Offset(0, 8),
                  ),
                ],
              ),
              child: const Center(
                child: Text(
                  'KC',
                  style: TextStyle(
                    color: Colors.white,
                    fontSize: 32,
                    fontWeight: FontWeight.bold,
                    letterSpacing: -1.5,
                  ),
                ),
              ),
            ),
            const SizedBox(height: 24),
            const Text(
              'KidCare',
              style: TextStyle(
                fontSize: 26,
                fontWeight: FontWeight.bold,
                letterSpacing: -0.5,
                color: Color(0xFF1D1D1F),
              ),
            ),
            const SizedBox(height: 6),
            const Text(
              'Quản Lý & Đồng Hành Cùng Trẻ',
              style: TextStyle(
                fontSize: 13,
                color: Color(0xFF86868B),
              ),
            ),
            const SizedBox(height: 48),
            const SizedBox(
              width: 24,
              height: 24,
              child: CircularProgressIndicator(
                strokeWidth: 2.5,
                valueColor: AlwaysStoppedAnimation<Color>(Color(0xFF0071E3)),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
