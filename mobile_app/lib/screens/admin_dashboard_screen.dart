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
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(18)),
        title: const Text('Đăng xuất', style: TextStyle(fontWeight: FontWeight.bold)),
        content: const Text('Bạn có chắc chắn muốn đăng xuất khỏi hệ thống không?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text('Hủy', style: TextStyle(color: Color(0xFF86868B))),
          ),
          ElevatedButton(
            style: ElevatedButton.styleFrom(
              backgroundColor: const Color(0xFF1D1D1F),
              foregroundColor: Colors.white,
            ),
            onPressed: () => Navigator.pop(ctx, true),
            child: const Text('Đăng xuất'),
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
      backgroundColor: const Color(0xFFF5F5F7),
      appBar: AppBar(
        title: const Text('Admin Dashboard', style: TextStyle(fontWeight: FontWeight.bold)),
        backgroundColor: Colors.white,
        foregroundColor: const Color(0xFF1D1D1F),
        actions: [
          IconButton(
            tooltip: 'Đăng xuất',
            icon: const Icon(Icons.logout, color: Color(0xFF1D1D1F)),
            onPressed: _handleLogout,
          ),
        ],
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            // Thẻ Thông tin Vai trò & Quyền (Apple Style)
            Container(
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(18),
                border: Border.all(color: const Color(0x14000000)),
              ),
              padding: const EdgeInsets.all(18),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Container(
                        padding: const EdgeInsets.all(10),
                        decoration: BoxDecoration(
                          color: const Color(0xFF0071E3).withValues(alpha: 0.1),
                          shape: BoxShape.circle,
                        ),
                        child: const Icon(Icons.shield_outlined, color: Color(0xFF0071E3), size: 26),
                      ),
                      const SizedBox(width: 12),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Container(
                              padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                              decoration: BoxDecoration(
                                color: const Color(0xFF0071E3),
                                borderRadius: BorderRadius.circular(980),
                              ),
                              child: Text(
                                user.role.toUpperCase(),
                                style: const TextStyle(
                                  color: Colors.white,
                                  fontSize: 10,
                                  fontWeight: FontWeight.bold,
                                  letterSpacing: 0.5,
                                ),
                              ),
                            ),
                            const SizedBox(height: 4),
                            Text(
                              user.fullName.isNotEmpty ? user.fullName : user.username,
                              style: const TextStyle(
                                fontSize: 17,
                                fontWeight: FontWeight.bold,
                                color: Color(0xFF1D1D1F),
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                  const Divider(height: 24, color: Color(0x14000000)),
                  _buildInfoRow(Icons.account_circle_outlined, 'Tài khoản', user.username),
                  _buildInfoRow(Icons.email_outlined, 'Email', user.email.isNotEmpty ? user.email : 'N/A'),
                  _buildInfoRow(Icons.fingerprint, 'User ID', user.userId),
                  _buildInfoRow(
                    Icons.timer_outlined,
                    'Hiệu lực Token',
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
                          color: const Color(0xFF0071E3),
                          size: 18,
                        ),
                        const SizedBox(width: 4),
                        Text(
                          _showTokenDetail ? 'Ẩn chuỗi JWT Token gốc' : 'Xem chuỗi JWT Token gốc',
                          style: const TextStyle(
                            fontSize: 12,
                            color: Color(0xFF0071E3),
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
                        color: const Color(0xFFF5F5F7),
                        borderRadius: BorderRadius.circular(10),
                        border: Border.all(color: const Color(0x14000000)),
                      ),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.stretch,
                        children: [
                          SelectableText(
                            user.rawToken,
                            style: const TextStyle(fontSize: 10, fontFamily: 'monospace', color: Color(0xFF1D1D1F)),
                            maxLines: 4,
                          ),
                          const SizedBox(height: 6),
                          Align(
                            alignment: Alignment.centerRight,
                            child: TextButton.icon(
                              icon: const Icon(Icons.copy, size: 14, color: Color(0xFF0071E3)),
                              label: const Text('Sao chép Token', style: TextStyle(fontSize: 11, color: Color(0xFF0071E3))),
                              onPressed: () {
                                Clipboard.setData(ClipboardData(text: user.rawToken));
                                ScaffoldMessenger.of(context).showSnackBar(
                                  const SnackBar(
                                    content: Text('Đã sao chép JWT Token'),
                                    backgroundColor: Color(0xFF0071E3),
                                  ),
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

            const SizedBox(height: 24),
            const Text(
              'Chức Năng Quản Trị Viên',
              style: TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.bold,
                color: Color(0xFF1D1D1F),
                letterSpacing: -0.3,
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
                  title: 'Quản Lý Hồ Sơ',
                  subtitle: 'Hồ sơ các bé & tài khoản',
                  icon: Icons.family_restroom_outlined,
                  onTap: () {
                    ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(content: Text('Chuyển tới quản lý hồ sơ trẻ'), backgroundColor: Color(0xFF0071E3)),
                    );
                  },
                ),
                _buildActionCard(
                  title: 'Nhiệm Vụ & Điểm',
                  subtitle: 'Theo dõi sinh hoạt & sao',
                  icon: Icons.checklist_outlined,
                  onTap: () {
                    ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(content: Text('Quản lý nhiệm vụ & thưởng sao'), backgroundColor: Color(0xFF0071E3)),
                    );
                  },
                ),
                _buildActionCard(
                  title: 'Phân Quyền User',
                  subtitle: 'Admin, Parents, Kid',
                  icon: Icons.manage_accounts_outlined,
                  onTap: () {
                    ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(content: Text('Phân quyền người dùng'), backgroundColor: Color(0xFF0071E3)),
                    );
                  },
                ),
                _buildActionCard(
                  title: 'Báo Cáo & Thống Kê',
                  subtitle: 'Tổng quan hoạt động',
                  icon: Icons.analytics_outlined,
                  onTap: () {
                    ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(content: Text('Báo cáo thống kê'), backgroundColor: Color(0xFF0071E3)),
                    );
                  },
                ),
                _buildActionCard(
                  title: 'Định Vị GPS',
                  subtitle: 'Giám sát vị trí & Vùng an toàn',
                  icon: Icons.location_on_outlined,
                  onTap: () {
                    ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(content: Text('Giám sát vị trí GPS trẻ em thời gian thực'), backgroundColor: Color(0xFF0071E3)),
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
      padding: const EdgeInsets.symmetric(vertical: 5),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, size: 16, color: highlight ? const Color(0xFF0071E3) : const Color(0xFF86868B)),
          const SizedBox(width: 8),
          SizedBox(
            width: 120,
            child: Text(
              label,
              style: const TextStyle(
                fontSize: 12,
                color: Color(0xFF86868B),
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
                color: highlight ? const Color(0xFF0071E3) : const Color(0xFF1D1D1F),
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
    required VoidCallback onTap,
  }) {
    return Container(
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: const Color(0x14000000)),
      ),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(16),
        child: Padding(
          padding: const EdgeInsets.all(14),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Container(
                padding: const EdgeInsets.all(8),
                decoration: BoxDecoration(
                  color: const Color(0xFF0071E3).withValues(alpha: 0.1),
                  borderRadius: BorderRadius.circular(10),
                ),
                child: Icon(icon, color: const Color(0xFF0071E3), size: 22),
              ),
              const SizedBox(height: 10),
              Text(
                title,
                style: const TextStyle(fontSize: 13, fontWeight: FontWeight.bold, color: Color(0xFF1D1D1F)),
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
              ),
              const SizedBox(height: 2),
              Text(
                subtitle,
                style: const TextStyle(fontSize: 11, color: Color(0xFF86868B)),
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
