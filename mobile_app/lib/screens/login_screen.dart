import 'package:flutter/material.dart';
import '../config/app_config.dart';
import '../models/user_token_model.dart';
import '../services/auth_service.dart';
import 'admin_dashboard_screen.dart';
import 'register_screen.dart';
import 'staff_pos_screen.dart';
import 'user_home_screen.dart';

class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key});

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final _formKey = GlobalKey<FormState>();
  final _usernameController = TextEditingController();
  final _passwordController = TextEditingController();
  final AuthService _authService = AuthService();

  bool _isLoading = false;
  bool _obscurePassword = true;

  @override
  void dispose() {
    _usernameController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  void _fillAccount(String username, String password) {
    setState(() {
      _usernameController.text = username;
      _passwordController.text = password;
    });
  }

  Future<void> _handleLogin() async {
    if (!_formKey.currentState!.validate()) return;

    setState(() {
      _isLoading = true;
    });

    final result = await _authService.login(
      _usernameController.text.trim(),
      _passwordController.text,
    );

    setState(() {
      _isLoading = false;
    });

    if (!mounted) return;

    if (result['success'] == true) {
      final UserTokenModel user = result['user'];

      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            'Đăng nhập thành công! Vai trò: ${user.role} (${user.fullName})',
          ),
          backgroundColor: Colors.green.shade700,
          behavior: SnackBarBehavior.floating,
        ),
      );

      // Phân quyền điều hướng màn hình dựa vào Role giải mã từ JWT Token
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
    } else {
      showDialog(
        context: context,
        builder: (ctx) => AlertDialog(
          title: const Row(
            children: [
              Icon(Icons.error_outline, color: Colors.red),
              SizedBox(width: 8),
              Text('Đăng nhập thất bại'),
            ],
          ),
          content: Text(result['message'] ?? 'Vui lòng kiểm tra lại thông tin.'),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(ctx),
              child: const Text('Đóng'),
            ),
          ],
        ),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.symmetric(horizontal: 24.0, vertical: 20.0),
            child: Form(
              key: _formKey,
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  // Logo & Tiêu đề
                  Center(
                    child: Container(
                      padding: const EdgeInsets.all(18),
                      decoration: BoxDecoration(
                        color: Colors.blue.shade50,
                        shape: BoxShape.circle,
                      ),
                      child: Icon(
                        Icons.lock_person_rounded,
                        size: 56,
                        color: Colors.blue.shade800,
                      ),
                    ),
                  ),
                  const SizedBox(height: 16),
                  const Text(
                    'Đăng Nhập Hệ Thống',
                    textAlign: TextAlign.center,
                    style: TextStyle(
                      fontSize: 24,
                      fontWeight: FontWeight.bold,
                      color: Color(0xFF1E293B),
                    ),
                  ),
                  const SizedBox(height: 6),
                  Text(
                    'Xác thực ASP.NET Core Identity & JWT Phân quyền',
                    textAlign: TextAlign.center,
                    style: TextStyle(
                      fontSize: 13,
                      color: Colors.grey.shade600,
                    ),
                  ),
                  const SizedBox(height: 24),

                  // Nút gán tài khoản mẫu nhanh để tiện kiểm thử (Admin, Staff, User)
                  Container(
                    padding: const EdgeInsets.all(12),
                    decoration: BoxDecoration(
                      color: Colors.grey.shade100,
                      borderRadius: BorderRadius.circular(12),
                      border: Border.all(color: Colors.grey.shade300),
                    ),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Row(
                          children: [
                            Icon(Icons.bolt, size: 18, color: Colors.amber.shade800),
                            const SizedBox(width: 6),
                            const Text(
                              'Tài khoản kiểm thử nhanh (Click để điền):',
                              style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold),
                            ),
                          ],
                        ),
                        const SizedBox(height: 8),
                        Wrap(
                          spacing: 8,
                          runSpacing: 6,
                          children: [
                            ActionChip(
                              avatar: const Icon(Icons.admin_panel_settings, size: 16, color: Colors.red),
                              label: const Text('Admin', style: TextStyle(fontSize: 12)),
                              onPressed: () => _fillAccount('admin', 'Admin@123'),
                            ),
                            ActionChip(
                              avatar: const Icon(Icons.badge, size: 16, color: Colors.orange),
                              label: const Text('Staff', style: TextStyle(fontSize: 12)),
                              onPressed: () => _fillAccount('staff', 'Staff@123'),
                            ),
                            ActionChip(
                              avatar: const Icon(Icons.person, size: 16, color: Colors.blue),
                              label: const Text('User', style: TextStyle(fontSize: 12)),
                              onPressed: () => _fillAccount('user', 'User@123'),
                            ),
                          ],
                        ),
                      ],
                    ),
                  ),

                  const SizedBox(height: 20),

                  // Tên đăng nhập
                  TextFormField(
                    controller: _usernameController,
                    decoration: InputDecoration(
                      labelText: 'Tên đăng nhập hoặc Email',
                      prefixIcon: const Icon(Icons.account_circle_outlined),
                      border: OutlineInputBorder(
                        borderRadius: BorderRadius.circular(12),
                      ),
                    ),
                    validator: (val) {
                      if (val == null || val.trim().isEmpty) {
                        return 'Vui lòng nhập tên đăng nhập';
                      }
                      return null;
                    },
                  ),
                  const SizedBox(height: 16),

                  // Mật khẩu
                  TextFormField(
                    controller: _passwordController,
                    obscureText: _obscurePassword,
                    decoration: InputDecoration(
                      labelText: 'Mật khẩu',
                      prefixIcon: const Icon(Icons.key_outlined),
                      suffixIcon: IconButton(
                        icon: Icon(
                          _obscurePassword ? Icons.visibility_off : Icons.visibility,
                        ),
                        onPressed: () {
                          setState(() {
                            _obscurePassword = !_obscurePassword;
                          });
                        },
                      ),
                      border: OutlineInputBorder(
                        borderRadius: BorderRadius.circular(12),
                      ),
                    ),
                    validator: (val) {
                      if (val == null || val.isEmpty) {
                        return 'Vui lòng nhập mật khẩu';
                      }
                      return null;
                    },
                  ),
                  const SizedBox(height: 24),

                  // Nút Đăng nhập
                  ElevatedButton(
                    onPressed: _isLoading ? null : _handleLogin,
                    style: ElevatedButton.styleFrom(
                      backgroundColor: const Color(0xFF2563EB),
                      foregroundColor: Colors.white,
                      padding: const EdgeInsets.symmetric(vertical: 14),
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(12),
                      ),
                    ),
                    child: _isLoading
                        ? const SizedBox(
                            height: 20,
                            width: 20,
                            child: CircularProgressIndicator(
                              strokeWidth: 2,
                              color: Colors.white,
                            ),
                          )
                        : const Text(
                            'ĐĂNG NHẬP',
                            style: TextStyle(
                              fontSize: 15,
                              fontWeight: FontWeight.bold,
                              letterSpacing: 1,
                            ),
                          ),
                  ),

                  const SizedBox(height: 16),

                  // Chuyển sang màn hình Đăng ký
                  Row(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      const Text('Chưa có tài khoản?'),
                      TextButton(
                        onPressed: () {
                          Navigator.push(
                            context,
                            MaterialPageRoute(
                              builder: (_) => const RegisterScreen(),
                            ),
                          );
                        },
                        child: const Text('Đăng ký ngay'),
                      ),
                    ],
                  ),

                  const SizedBox(height: 20),

                  // Hiển thị API Endpoint hiện tại từ .env
                  Container(
                    padding: const EdgeInsets.all(8),
                    decoration: BoxDecoration(
                      color: Colors.blue.shade50,
                      borderRadius: BorderRadius.circular(8),
                    ),
                    child: Text(
                      'API Server (dotenv): ${AppConfig.baseUrl}',
                      textAlign: TextAlign.center,
                      style: TextStyle(
                        fontSize: 11,
                        color: Colors.blue.shade900,
                        fontFamily: 'monospace',
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
