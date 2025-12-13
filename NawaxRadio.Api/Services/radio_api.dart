import 'dart:convert';
import 'package:http/http.dart' as http;

class RadioApi {
  // برای Flutter Web/Android/iOS روی localhost کار نمی‌کنه مگر همون دستگاهی باشی که API روش ران میشه.
  // فعلاً همون رو گذاشتم چون گفتی localhost.
  static const String baseUrl = 'http://localhost:5246';

  static Future<RadioTrack> getNextTrack(String channel) async {
    final uri = Uri.parse('$baseUrl/radio/$channel/stream');

    final res = await http.get(
      uri,
      headers: const {'Accept': 'application/json'},
    );

    if (res.statusCode != 200) {
      throw Exception(
        'Failed to load radio stream: ${res.statusCode} ${res.body}',
      );
    }

    final decoded = jsonDecode(res.body);
    if (decoded is! Map<String, dynamic>) {
      throw Exception('Invalid response format. Expected JSON object.');
    }

    return RadioTrack.fromJson(decoded);
  }
}

class RadioTrack {
  final String audioUrl;
  final String songId;
  final String name;
  final String singer;
  final String channel;
  final bool isJingle;

  const RadioTrack({
    required this.audioUrl,
    required this.songId,
    required this.name,
    required this.singer,
    required this.channel,
    required this.isJingle,
  });

  factory RadioTrack.fromJson(Map<String, dynamic> json) {
    final audioUrl = (json['audioUrl'] ?? '').toString();
    final songId = (json['songId'] ?? '').toString();

    if (audioUrl.isEmpty) {
      throw Exception('audioUrl is missing/empty in response');
    }
    if (songId.isEmpty) {
      throw Exception('songId is missing/empty in response');
    }

    return RadioTrack(
      audioUrl: audioUrl,
      songId: songId,
      name: (json['name'] ?? '').toString(),
      singer: (json['singer'] ?? '').toString(),
      channel: (json['channel'] ?? '').toString(),
      isJingle: (json['isJingle'] as bool?) ?? false,
    );
  }
}
