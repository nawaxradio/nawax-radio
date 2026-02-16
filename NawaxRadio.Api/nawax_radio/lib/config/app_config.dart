import 'package:flutter/foundation.dart';

class AppConfig {
  // Compile-time value:
  // flutter run -d chrome --dart-define=API_BASE=https://api.nawaxradio.com
  static const String apiBaseUrlProd = String.fromEnvironment(
    'API_BASE',
    defaultValue: 'https://api.nawaxradio.com',
  );

  // Optional: keep a dev base if you want local API sometimes
  static const String apiBaseUrlDev = 'http://127.0.0.1:5246';

  static bool forceProd = true;

  static String get apiBaseUrl {
    if (forceProd) return apiBaseUrlProd;
    return kReleaseMode ? apiBaseUrlProd : apiBaseUrlDev;
  }
}
