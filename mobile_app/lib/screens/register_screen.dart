import 'package:flutter/material.dart';
import '../services/auth_service.dart';

class RegisterScreen extends StatefulWidget {
  const RegisterScreen({super.key});

  @override
  State<RegisterScreen> createState() => _RegisterScreenState();
}

class _RegisterScreenState extends State<RegisterScreen> {
  final _formKey = GlobalKey<FormState>();
  final _usernameController = TextEditingController();
  final _fullNameController = TextEditingController();
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();
  final _confirmPasswordController = TextEditingController();

  final AuthService _authService = AuthService();

  String _selectedRole = 'User';
  final List<String> _roles = ['User', 'Staff', 'Admin'];

  bool _isLoading = false;
  bool _obscurePassword = true;

  @override
  void dispose() {
    _usernameController.dispose();
    _fullNameController.dispose();
    _emailController.dispose();
    _passwordController.dispose();
    _confirmPasswordController.dispose();
    super.dispose();
  }

  Future<void> _handleRegister() async {
    if (!_formKey.currentState!.validate()) return;

    if (_passwordController.text != _confirmPasswordController.text) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Mật khẩu xác nhận không khớp!'),
          backgroundColor: Colors.red,
        ),
      );
      return;
    }

    setState(() {
      _isLoading = true;
    });

    final result = await _authService.register(
      username: _usernameController.text.trim(),
      email: _emailController.text.trim(),
      password: _passwordController.text,
      fullName: _fullNameController.text.trim(),
      role: _selectedRole,
    );

    setState(() {
      _isLoading = false;
    });

    if (!mounted) return;

    if (result['success'] == true) {
      showDialog(
        context: context,
        barrierDismissible: false,
        builder: (ctx) => AlertDialog(
          title: const Row(
            children: [
              Icon(Icons.check_circle, color: Colors.green),
              SizedBox(width: 8),
              Text('Đăng ký thành công'),
            ],
          ),
          content: Text(
            'Tài khoản "${_usernameController.text.trim()}" đã được tạo với quyền [$_selectedRole]!\nBạn có thể đăng nhập ngay.',
          ),
          actions: [
            ElevatedButton(
              onPressed: () {
                Navigator.pop(ctx); // Close dialog
                Navigator.pop(context); // Back to Login screen
              },
              child: const Text('Đến trang Đăng nhập'),
            ),
          ],
        ),
      );
    } else {
      showDialog(
        context: context,
        builder: (ctx) => AlertDialog(
          title: const Row(
            children: [
              Icon(Icons.error_outline, color: Colors.red),
              SizedBox(width: 8),
              Text('Đăng ký thất bại'),
            ],
          ),
          content: Text(result['message'] ?? 'Có lỗi xảy ra trong quá trình đăng ký.'),
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
      appBar: AppBar(
        title: const Text('Đăng Ký Tài Khoản'),
        backgroundColor: Colors.transparent,
        elevation: 0,
        foregroundColor: const Color(0xFF1E293B),
      ),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.symmetric(horizontal: 24.0, vertical: 12.0),
          child: Form(
            key: _formKey,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                const Text(
                  'Tạo tài khoản mới',
                  style: TextStyle(
                    fontSize: 22,
                    fontWeight: FontWeight.bold,
                    color: Color(0xFF1E293B),
                  ),
                ),
                const SizedBox(height: 4),
                Text(
                  'Phân quyền tài khoản trong hệ thống Identity',
                  style: TextStyle(
                    fontSize: 13,
                    color: Colors.grey.shade600,
                  ),
                ),
                const SizedBox(height: 20),

                // Họ và tên
                TextFormField(
                  controller: _fullNameController,
                  decoration: InputDecoration(
                    labelText: 'Họ và tên',
                    prefixIcon: const Icon(Icons.badge_outlined),
                    border: OutlineInputBorder(
                      borderRadius: BorderRadius.circular(12),
                    ),
                  ),
                  validator: (val) {
                    if (val == null || val.trim().isEmpty) {
                      return 'Vui lòng nhập họ và tên';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: 14),

                // Tên đăng nhập
                TextFormField(
                  controller: _usernameController,
                  decoration: InputDecoration(
                    labelText: 'Tên đăng nhập (Username)',
                    prefixIcon: const Icon(Icons.person_outline),
                    border: OutlineInputBorder(
                      borderRadius: BorderRadius.circular(12),
                    ),
                  ),
                  validator: (val) {
                    if (val == null || val.trim().isEmpty) {
                      return 'Vui lòng nhập tên đăng nhập';
                    }
                    if (val.trim().length < 3) {
                      return 'Tên đăng nhập tối thiểu 3 ký tự';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: 14),

                // Email
                TextFormField(
                  controller: _emailController,
                  keyboardType: TextInputType.emailAddress,
                  decoration: InputDecoration(
                    labelText: 'Địa chỉ Email',
                    prefixIcon: const Icon(Icons.email_outlined),
                    border: OutlineInputBorder(
                      borderRadius: BorderRadius.circular(12),
                    ),
                  ),
                  validator: (val) {
                    if (val == null || val.trim().isEmpty) {
                      return 'Vui lòng nhập email';
                    }
                    if (!val.contains('@') || !val.contains('.')) {
                      return 'Email không đúng định dạng';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: 14),

                // Chọn Quyền (Role)
                DropdownButtonFormField<String>(
                  initialValue: _selectedRole,
                  decoration: InputDecoration(
                    labelText: 'Vai trò & Phân quyền (Role)',
                    prefixIcon: const Icon(Icons.security),
                    border: OutlineInputBorder(
                      borderRadius: BorderRadius.circular(12),
                    ),
                    helperText: 'Lựa chọn quyền người dùng trong hệ thống',
                  ),
                  items: _roles.map((role) {
                    IconData iconData;
                    Color color;
                    String label;
                    if (role == 'Admin') {
                      iconData = Icons.admin_panel_settings;
                      color = Colors.red;
                      label = 'Admin (Toàn quyền quản trị)';
                    } else if (role == 'Staff') {
                      iconData = Icons.point_of_sale;
                      color = Colors.orange;
                      label = 'Staff (Nhân viên thu ngân & kho)';
                    } else {
                      iconData = Icons.person;
                      color = Colors.blue;
                      label = 'User (Khách hàng thông thường)';
                    }
                    return DropdownMenuItem<String>(
                      value: role,
                      child: Row(
                        children: [
                          Icon(iconData, color: color, size: 20),
                          const SizedBox(width: 8),
                          Text(label, style: const TextStyle(fontSize: 13)),
                        ],
                      ),
                    );
                  }).toList(),
                  onChanged: (val) {
                    if (val != null) {
                      setState(() {
                        _selectedRole = val;
                      });
                    }
                  },
                ),
                const SizedBox(height: 14),

                // Mật khẩu
                TextFormField(
                  controller: _passwordController,
                  obscureText: _obscurePassword,
                  decoration: InputDecoration(
                    labelText: 'Mật khẩu',
                    prefixIcon: const Icon(Icons.lock_outline),
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
                    helperText: 'Ít nhất 6 ký tự, gồm chữ hoa, chữ thường và số (VD: User@123)',
                  ),
                  validator: (val) {
                    if (val == null || val.isEmpty) {
                      return 'Vui lòng nhập mật khẩu';
                    }
                    if (val.length < 6) {
                      return 'Mật khẩu phải từ 6 ký tự';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: 14),

                // Xác nhận mật khẩu
                TextFormField(
                  controller: _confirmPasswordController,
                  obscureText: _obscurePassword,
                  decoration: InputDecoration(
                    labelText: 'Xác nhận lại mật khẩu',
                    prefixIcon: const Icon(Icons.lock_reset),
                    border: OutlineInputBorder(
                      borderRadius: BorderRadius.circular(12),
                    ),
                  ),
                  validator: (val) {
                    if (val == null || val.isEmpty) {
                      return 'Vui lòng xác nhận lại mật khẩu';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: 24),

                // Nút Đăng ký
                ElevatedButton(
                  onPressed: _isLoading ? null : _handleRegister,
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
                          'ĐĂNG KÝ TÀI KHOẢN',
                          style: TextStyle(
                            fontSize: 15,
                            fontWeight: FontWeight.bold,
                            letterSpacing: 0.5,
                          ),
                        ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
