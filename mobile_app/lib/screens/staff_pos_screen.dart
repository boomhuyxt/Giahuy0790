import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import '../models/user_token_model.dart';
import '../services/auth_service.dart';
import 'login_screen.dart';

class StaffPosScreen extends StatefulWidget {
  final UserTokenModel user;

  const StaffPosScreen({super.key, required this.user});

  @override
  State<StaffPosScreen> createState() => _StaffPosScreenState();
}

class _StaffPosScreenState extends State<StaffPosScreen> {
  final AuthService _authService = AuthService();

  Future<void> _handleLogout() async {
    final confirm = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Đăng xuất'),
        content: const Text('Bạn có chắc muốn đăng xuất ca làm việc không?'),
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
        title: const Text('Bàn Thu Ngân & Kho (Staff POS)', style: TextStyle(fontWeight: FontWeight.bold)),
        backgroundColor: const Color(0xFFD97706), // Cam vàng Staff
        foregroundColor: Colors.white,
        actions: [
          IconButton(
            tooltip: 'Đăng xuất ca',
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
            // Thẻ Thông tin Vai trò & Quyền Staff
            Card(
              elevation: 3,
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(14),
              ),
              child: Container(
                decoration: BoxDecoration(
                  borderRadius: BorderRadius.circular(14),
                  border: Border.all(color: Colors.amber.shade300, width: 1.5),
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
                            color: Colors.amber.shade100,
                            shape: BoxShape.circle,
                          ),
                          child: const Icon(Icons.point_of_sale, color: Color(0xFFD97706), size: 28),
                        ),
                        const SizedBox(width: 12),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Container(
                                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                                decoration: BoxDecoration(
                                  color: const Color(0xFFD97706),
                                  borderRadius: BorderRadius.circular(6),
                                ),
                                child: Text(
                                  'QUYỀN HẠN: ${user.role.toUpperCase()}',
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
                    _buildInfoRow(Icons.account_circle, 'Tài khoản ca trực', user.username),
                    _buildInfoRow(Icons.email, 'Email nhân viên', user.email.isNotEmpty ? user.email : 'N/A'),
                    _buildInfoRow(Icons.fingerprint, 'User ID', user.userId),
                    _buildInfoRow(
                      Icons.timer_outlined,
                      'Hết hạn phiên (JWT exp)',
                      expiryText,
                      highlight: true,
                    ),
                  ],
                ),
              ),
            ),

            const SizedBox(height: 20),
            const Text(
              'Nghiệp Vụ Bán Hàng & Quản Lý Kho (Staff POS)',
              style: TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.bold,
                color: Color(0xFF1E293B),
              ),
            ),
            const SizedBox(height: 12),

            // Grid các chức năng POS của Staff
            GridView.count(
              crossAxisCount: 2,
              shrinkWrap: true,
              physics: const NeverScrollableScrollPhysics(),
              crossAxisSpacing: 12,
              mainAxisSpacing: 12,
              childAspectRatio: 1.15,
              children: [
                _buildActionCard(
                  title: 'Bàn Bán Hàng (POS)',
                  subtitle: 'Tạo đơn hàng tại quầy thu ngân',
                  icon: Icons.shopping_cart_checkout,
                  color: Colors.amber.shade800,
                  onTap: () {
                    ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(content: Text('Mở màn hình POS tính tiền thu ngân')),
                    );
                  },
                ),
                _buildActionCard(
                  title: 'Quét Mã Vạch (Barcode)',
                  subtitle: 'Tra cứu nhanh giá sản phẩm',
                  icon: Icons.qr_code_scanner,
                  color: Colors.teal.shade700,
                  onTap: () {
                    ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(content: Text('Tính năng quét barcode bằng camera')),
                    );
                  },
                ),
                _buildActionCard(
                  title: 'Tra Cứu Tồn Kho',
                  subtitle: 'Xem số lượng sản phẩm trong kho',
                  icon: Icons.inventory,
                  color: Colors.indigo,
                  onTap: () {
                    ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(content: Text('Gọi API: /api/products/search?name=...')),
                    );
                  },
                ),
                _buildActionCard(
                  title: 'Lịch Sử Hóa Đơn',
                  subtitle: 'Xem & in lại hóa đơn trong ca',
                  icon: Icons.receipt_long,
                  color: Colors.purple.shade700,
                  onTap: () {
                    ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(content: Text('Danh sách hóa đơn bán lẻ trong ca')),
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
          Icon(icon, size: 16, color: highlight ? Colors.amber.shade900 : Colors.grey.shade600),
          const SizedBox(width: 8),
          SizedBox(
            width: 140,
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
                color: highlight ? Colors.amber.shade900 : const Color(0xFF1E293B),
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
