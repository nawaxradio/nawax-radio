import 'package:flutter/foundation.dart';

class AppConfig {
  static const String apiBaseUrlDev = 'http://127.0.0.1:5246';
  static const String apiBaseUrlProd = 'https://nawaxradio-api.liara.run';

  // ✅ default false. Only force prod with --dart-define=FORCE_PROD=true
  static const bool forceProd = bool.fromEnvironment(
    'FORCE_PROD',
    defaultValue: false,
  );

  static String get apiBaseUrl {
    if (forceProd) return apiBaseUrlProd;
    return kReleaseMode ? apiBaseUrlProd : apiBaseUrlDev;
  }
}
