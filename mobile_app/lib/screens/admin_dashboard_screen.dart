import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:intl/intl.dart';
import '../models/user_token_model.dart';
import '../services/auth_service.dart';
import 'login_screen.dart';

class AdminDashboardScreen extends StatefulWidget {
  final UserTokenModel user;

  const AdminDashboardScreen({super.key, required this.user});

  @override
  State<AdminDashboardScreen> createState() => _AdminDashboardScreenState();
}

class _AdminDashboardScreenState extends State<AdminDashboardScreen> {
  final AuthService _authService = AuthService();
  bool _showTokenDetail = false;

  Future<void> _handleLogout() async {
    final confirm = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Đăng xuất'),
        content: const Text('Bạn có chắc chắn muốn đăng xuất khỏi hệ thống không?'),
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
      // Xóa Token trong SharedPreferences
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
        title: const Text('Admin Dashboard', style: TextStyle(fontWeight: FontWeight.bold)),
        backgroundColor: const Color(0xFF991B1B), // Đỏ đậm Admin
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
            // Thẻ Thông tin Vai trò & Quyền (Decoded via jwt_decode)
            Card(
              elevation: 3,
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(14),
              ),
              child: Container(
                decoration: BoxDecoration(
                  borderRadius: BorderRadius.circular(14),
                  border: Border.all(color: Colors.red.shade200, width: 1.5),
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
                            color: Colors.red.shade100,
                            shape: BoxShape.circle,
                          ),
                          child: const Icon(Icons.shield_rounded, color: Color(0xFF991B1B), size: 28),
                        ),
                        const SizedBox(width: 12),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Container(
                                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                                decoration: BoxDecoration(
                                  color: const Color(0xFF991B1B),
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
                    _buildInfoRow(Icons.account_circle, 'Tài khoản', user.username),
                    _buildInfoRow(Icons.email, 'Email', user.email.isNotEmpty ? user.email : 'N/A'),
                    _buildInfoRow(Icons.fingerprint, 'User ID (Identity)', user.userId),
                    _buildInfoRow(
                      Icons.timer_outlined,
                      'Hiệu lực Token (exp)',
                      expiryText,
                      highlight: true,
                    ),

                    const SizedBox(height: 10),
                    // Nút xem chi tiết Token JWT
                    InkWell(
                      onTap: () {
                        setState(() {
                          _showTokenDetail = !_showTokenDetail;
                        });
                      },
                      child: Row(
                        children: [
                          Icon(
                            _showTokenDetail ? Icons.expand_less : Icons.expand_more,
                            color: Colors.blue.shade700,
                            size: 20,
                          ),
                          const SizedBox(width: 4),
                          Text(
                            _showTokenDetail ? 'Ẩn chuỗi JWT Token gốc' : 'Xem chuỗi JWT Token gốc (jwt_decode)',
                            style: TextStyle(
                              fontSize: 12,
                              color: Colors.blue.shade700,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                        ],
                      ),
                    ),

                    if (_showTokenDetail) ...[
                      const SizedBox(height: 8),
                      Container(
                        padding: const EdgeInsets.all(10),
                        decoration: BoxDecoration(
                          color: Colors.grey.shade100,
                          borderRadius: BorderRadius.circular(8),
                          border: Border.all(color: Colors.grey.shade300),
                        ),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.stretch,
                          children: [
                            SelectableText(
                              user.rawToken,
                              style: const TextStyle(fontSize: 10, fontFamily: 'monospace'),
                              maxLines: 4,
                            ),
                            const SizedBox(height: 6),
                            Align(
                              alignment: Alignment.centerRight,
                              child: TextButton.icon(
                                icon: const Icon(Icons.copy, size: 14),
                                label: const Text('Sao chép Token', style: TextStyle(fontSize: 11)),
                                onPressed: () {
                                  Clipboard.setData(ClipboardData(text: user.rawToken));
                                  ScaffoldMessenger.of(context).showSnackBar(
                                    const SnackBar(content: Text('Đã sao chép JWT Token vào Clipboard')),
                                  );
                                },
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ],
                ),
              ),
            ),

            const SizedBox(height: 20),
            const Text(
              'Chức Năng Dành Cho Quản Trị Viên (Admin Only)',
              style: TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.bold,
                color: Color(0xFF1E293B),
              ),
            ),
            const SizedBox(height: 12),

            // Grid các chức năng của Admin
            GridView.count(
              crossAxisCount: 2,
              shrinkWrap: true,
              physics: const NeverScrollableScrollPhysics(),
              crossAxisSpacing: 12,
              mainAxisSpacing: 12,
              childAspectRatio: 1.15,
              children: [
                _buildActionCard(
                  title: 'Quản Lý Người Dùng',
                  subtitle: 'Phân quyền Admin, Staff, User',
                  icon: Icons.manage_accounts,
                  color: Colors.red.shade700,
                  onTap: () {
                    ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(content: Text('Tính năng quản lý người dùng & phân quyền')),
                    );
                  },
                ),
                _buildActionCard(
                  title: 'Quản Lý Sản Phẩm',
                  subtitle: 'Thêm, sửa, xóa, tìm kiếm',
                  icon: Icons.inventory_2,
                  color: Colors.indigo,
                  onTap: () {
                    ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(content: Text('API: /api/products (Thêm, Sửa, Xóa)')),
                    );
                  },
                ),
                _buildActionCard(
                  title: 'Quản Lý Bài Đăng',
                  subtitle: 'Bảng tin diễn đàn & phê duyệt',
                  icon: Icons.dynamic_feed,
                  color: Colors.blue.shade700,
                  onTap: () {
                    ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(content: Text('API: /api/posts (Bài viết Facebook-like)')),
                    );
                  },
                ),
                _buildActionCard(
                  title: 'Báo Cáo Doanh Thu',
                  subtitle: 'Thống kê POS bán hàng',
                  icon: Icons.analytics,
                  color: Colors.teal.shade700,
                  onTap: () {
                    ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(content: Text('Xem báo cáo doanh số & hóa đơn')),
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
          Icon(icon, size: 16, color: highlight ? Colors.green.shade700 : Colors.grey.shade600),
          const SizedBox(width: 8),
          SizedBox(
            width: 130,
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
                color: highlight ? Colors.green.shade800 : const Color(0xFF1E293B),
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
