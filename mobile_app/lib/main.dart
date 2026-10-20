import 'package:flutter/material.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'screens/login_screen.dart';
import 'screens/register_screen.dart';
import 'screens/splash_screen.dart';

Future<void> main() async {
  // Đảm bảo Flutter Binding sẵn sàng trước khi nạp file môi trường .env
  WidgetsFlutterBinding.ensureInitialized();

  // 1. Nạp biến môi trường từ file .env qua thư viện flutter_dotenv
  // Tách biệt hoàn toàn API endpoints khỏi mã nguồn Dart
  try {
    await dotenv.load(fileName: ".env");
    debugPrint("Đã tải cấu hình .env thành công. API_BASE_URL: ${dotenv.env['API_BASE_URL']}");
  } catch (e) {
    debugPrint("Không thể nạp file .env: $e");
  }

  runApp(const MyApp());
}

class MyApp extends StatelessWidget {
  const MyApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'POS & Auth System',
      debugShowCheckedModeBanner: false,
      theme: ThemeData(
        useMaterial3: true,
        colorScheme: ColorScheme.fromSeed(
          seedColor: const Color(0xFF2563EB),
          primary: const Color(0xFF2563EB),
          secondary: const Color(0xFFD97706),
        ),
        appBarTheme: const AppBarTheme(
          elevation: 0,
          centerTitle: false,
        ),
        inputDecorationTheme: InputDecorationTheme(
          filled: true,
          fillColor: Colors.grey.shade50,
          border: OutlineInputBorder(
            borderRadius: BorderRadius.circular(12),
          ),
          enabledBorder: OutlineInputBorder(
            borderRadius: BorderRadius.circular(12),
            borderSide: BorderSide(color: Colors.grey.shade300),
          ),
          focusedBorder: OutlineInputBorder(
            borderRadius: BorderRadius.circular(12),
            borderSide: const BorderSide(color: Color(0xFF2563EB), width: 1.8),
          ),
        ),
      ),
      initialRoute: '/',
      routes: {
        '/': (context) => const SplashScreen(),
        '/login': (context) => const LoginScreen(),
        '/register': (context) => const RegisterScreen(),
      },
    );
  }
}
