import 'package:flutter/material.dart';
// Ú†ÙˆÙ† main.dart Ùˆ home_page.dart Ù‡Ø± Ø¯Ùˆ ØªÙˆÛŒ lib Ù‡Ø³ØªÙ†:
import 'home_page.dart';

// Ø±Ù†Ú¯â€ŒÙ‡Ø§ÛŒ Ø«Ø§Ø¨Øª
const Color primaryOrange = Color(0xFFFF481F); // Nawax orange
const Color nawaxWhite = Color(0xFFFFFFFF);

void main() {
  runApp(const NawaxRadioApp());
}

class NawaxRadioApp extends StatelessWidget {
  const NawaxRadioApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      debugShowCheckedModeBanner: false,
      title: 'Nawax Radio',
      theme: ThemeData(
        brightness: Brightness.light,
        primaryColor: primaryOrange,
        scaffoldBackgroundColor: primaryOrange,
        colorScheme: ColorScheme.light(
          primary: primaryOrange,
          secondary: nawaxWhite,
          surface: primaryOrange,
        ),
        iconTheme: const IconThemeData(color: Colors.black),
        textTheme: const TextTheme(bodyMedium: TextStyle(color: Colors.black)),
      ),
      home: const HomePage(),
    );
  }
}
