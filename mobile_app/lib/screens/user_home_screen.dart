import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import '../models/user_token_model.dart';
import '../services/auth_service.dart';
import 'login_screen.dart';

class UserHomeScreen extends StatefulWidget {
  final UserTokenModel user;

  const UserHomeScreen({super.key, required this.user});

  @override
  State<UserHomeScreen> createState() => _UserHomeScreenState();
}

class _UserHomeScreenState extends State<UserHomeScreen> {
  final AuthService _authService = AuthService();

  Future<void> _handleLogout() async {
    final confirm = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Đăng xuất'),
        content: const Text('Bạn có chắc chắn muốn đăng xuất không?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text('Hủy'),
          ),
          ElevatedButton(
            style: ElevatedButton.styleFrom(backgroundColor: Colors.red),
            onPressed: () => Navigator.pop(ctx, true),
            child: const Text('Đăng xuất', style: TextStyle(color: Colors.white)),
          ),
        ],
      ),
    );

    if (confirm == true) {
      await _authService.logout();
      if (!mounted) return;
      Navigator.pushAndRemoveUntil(
        context,
        MaterialPageRoute(builder: (_) => const LoginScreen()),
        (route) => false,
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    final user = widget.user;
    final dateFormat = DateFormat('dd/MM/yyyy HH:mm:ss');
    final String expiryText = user.expiryDate != null
        ? dateFormat.format(user.expiryDate!.toLocal())
        : 'Không xác định';

    return Scaffold(
      appBar: AppBar(
        title: const Text('Trang Chủ Khách Hàng', style: TextStyle(fontWeight: FontWeight.bold)),
        backgroundColor: const Color(0xFF2563EB), // Xanh dương User
        foregroundColor: Colors.white,
        actions: [
          IconButton(
            tooltip: 'Đăng xuất',
            icon: const Icon(Icons.logout),
            onPressed: _handleLogout,
          ),
        ],
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            // Thẻ Thông tin Khách Hàng (Decoded via jwt_decode)
            Card(
              elevation: 3,
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(14),
              ),
              child: Container(
                decoration: BoxDecoration(
                  borderRadius: BorderRadius.circular(14),
                  border: Border.all(color: Colors.blue.shade200, width: 1.5),
                ),
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        Container(
                          padding: const EdgeInsets.all(10),
                          decoration: BoxDecoration(
                            color: Colors.blue.shade100,
                            shape: BoxShape.circle,
                          ),
                          child: const Icon(Icons.person, color: Color(0xFF2563EB), size: 28),
                        ),
                        const SizedBox(width: 12),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Container(
                                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                                decoration: BoxDecoration(
                                  color: const Color(0xFF2563EB),
                                  borderRadius: BorderRadius.circular(6),
                                ),
                                child: Text(
                                  'VAI TRÒ: ${user.role.toUpperCase()}',
                                  style: const TextStyle(
                                    color: Colors.white,
                                    fontSize: 11,
                                    fontWeight: FontWeight.bold,
                                  ),
                                ),
                              ),
                              const SizedBox(height: 4),
                              Text(
                                user.fullName.isNotEmpty ? user.fullName : user.username,
                                style: const TextStyle(
                                  fontSize: 18,
                                  fontWeight: FontWeight.bold,
                                ),
                              ),
                            ],
                          ),
                        ),
                      ],
                    ),
                    const Divider(height: 24),
                    _buildInfoRow(Icons.account_circle, 'Tên người dùng', user.username),
                    _buildInfoRow(Icons.email, 'Email liên hệ', user.email.isNotEmpty ? user.email : 'N/A'),
                    _buildInfoRow(Icons.fingerprint, 'Mã khách hàng ID', user.userId),
                    _buildInfoRow(
                      Icons.timer_outlined,
                      'Thời hạn phiên đăng nhập',
                      expiryText,
                      highlight: true,
                    ),
                  ],
                ),
              ),
            ),

            const SizedBox(height: 20),
            const Text(
              'Trải Nghiệm Mua Sắm & Tương Tác',
              style: TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.bold,
                color: Color(0xFF1E293B),
              ),
            ),
            const SizedBox(height: 12),

            // Grid các chức năng dành cho User
            GridView.count(
              crossAxisCount: 2,
              shrinkWrap: true,
              physics: const NeverScrollableScrollPhysics(),
              crossAxisSpacing: 12,
              mainAxisSpacing: 12,
              childAspectRatio: 1.15,
              children: [
                _buildActionCard(
                  title: 'Danh Sách Sản Phẩm',
                  subtitle: 'Xem & tìm kiếm mặt hàng',
                  icon: Icons.storefront,
                  color: Colors.blue.shade700,
                  onTap: () {
                    ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(content: Text('API: /api/products (Lấy danh sách sản phẩm)')),
                    );
                  },
                ),
                _buildActionCard(
                  title: 'Bảng Tin Bài Viết',
                  subtitle: 'Bài viết chia sẻ dạng Facebook',
                  icon: Icons.feed,
                  color: Colors.deepPurple,
                  onTap: () {
                    ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(content: Text('API: /api/posts (Bảng tin bài đăng)')),
                    );
                  },
                ),
                _buildActionCard(
                  title: 'Giỏ Hàng',
                  subtitle: 'Đơn hàng đang chọn mua',
                  icon: Icons.shopping_basket,
                  color: Colors.teal.shade700,
                  onTap: () {
                    ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(content: Text('Giỏ hàng cá nhân của khách hàng')),
                    );
                  },
                ),
                _buildActionCard(
                  title: 'Lịch Sử Mua Hàng',
                  subtitle: 'Theo dõi tình trạng đơn đặt',
                  icon: Icons.history,
                  color: Colors.orange.shade800,
                  onTap: () {
                    ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(content: Text('Xem danh sách đơn hàng đã đặt')),
                    );
                  },
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildInfoRow(IconData icon, String label, String value, {bool highlight = false}) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, size: 16, color: highlight ? Colors.blue.shade700 : Colors.grey.shade600),
          const SizedBox(width: 8),
          SizedBox(
            width: 145,
            child: Text(
              label,
              style: TextStyle(
                fontSize: 12,
                color: Colors.grey.shade700,
                fontWeight: FontWeight.w500,
              ),
            ),
          ),
          Expanded(
            child: Text(
              value,
              style: TextStyle(
                fontSize: 12,
                fontWeight: highlight ? FontWeight.bold : FontWeight.w600,
                color: highlight ? Colors.blue.shade800 : const Color(0xFF1E293B),
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildActionCard({
    required String title,
    required String subtitle,
    required IconData icon,
    required Color color,
    required VoidCallback onTap,
  }) {
    return Card(
      elevation: 2,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
      ),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(12),
        child: Padding(
          padding: const EdgeInsets.all(12),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Container(
                padding: const EdgeInsets.all(8),
                decoration: BoxDecoration(
                  color: color.withValues(alpha: 0.12),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Icon(icon, color: color, size: 24),
              ),
              const SizedBox(height: 10),
              Text(
                title,
                style: const TextStyle(fontSize: 13, fontWeight: FontWeight.bold),
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
              ),
              const SizedBox(height: 2),
              Text(
                subtitle,
                style: TextStyle(fontSize: 11, color: Colors.grey.shade600),
                maxLines: 2,
                overflow: TextOverflow.ellipsis,
              ),
            ],
          ),
        ),
      ),
    );
  }
}
