import 'dart:convert';

import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;

import 'package:nawax_radio/config/app_config.dart';

class ApiChannel {
  // Backend returns "key" + "title"
  final String key;
  final String name;
  final String description;
  final String emoji;
  final int sortOrder;

  const ApiChannel({
    required this.key,
    required this.name,
    required this.description,
    required this.emoji,
    required this.sortOrder,
  });

  factory ApiChannel.fromJson(Map<String, dynamic> json) {
    return ApiChannel(
      key: (json['key'] ?? json['slug'] ?? '').toString(),
      name: (json['title'] ?? json['name'] ?? '').toString(),
      description: (json['description'] ?? '').toString(),
      emoji: (json['emoji'] ?? '').toString(),
      sortOrder: _toInt(json['sortOrder']) ?? 999,
    );
  }

  static int? _toInt(dynamic v) {
    if (v is int) return v;
    if (v is num) return v.toInt();
    if (v is String) return int.tryParse(v);
    return null;
  }
}

class ChannelsPage extends StatefulWidget {
  final String currentChannelKey;

  const ChannelsPage({super.key, required this.currentChannelKey});

  @override
  State<ChannelsPage> createState() => _ChannelsPageState();
}

class _ChannelsPageState extends State<ChannelsPage> {
  late Future<List<ApiChannel>> _future;

  @override
  void initState() {
    super.initState();
    _future = _fetchChannels();
  }

  // ✅ build a safe URL without relying on AppConfig.url()
  Uri _channelsEndpoint() {
    final base = AppConfig.apiBaseUrl.trim();
    final safeBase = base.endsWith('/')
        ? base.substring(0, base.length - 1)
        : base;
    return Uri.parse('$safeBase/channels');
  }

  Future<List<ApiChannel>> _fetchChannels() async {
    final url = _channelsEndpoint();

    http.Response res;
    try {
      res = await http.get(url);
    } catch (e) {
      throw Exception('Channels API network error: $e\nURL: $url');
    }

    if (res.statusCode != 200) {
      final body = res.body;
      final snippet = body.length > 300 ? body.substring(0, 300) : body;
      throw Exception(
        'Channels API error (${res.statusCode})\nURL: $url\nBody: $snippet',
      );
    }

    final raw = json.decode(res.body);
    if (raw is! List) {
      throw Exception('Invalid channels response (not a list)\nURL: $url');
    }

    final list = raw
        .whereType<Map<String, dynamic>>()
        .map(ApiChannel.fromJson)
        .where((c) => c.key.trim().isNotEmpty)
        .toList();

    list.sort((a, b) => a.sortOrder.compareTo(b.sortOrder));

    return list;
  }

  IconData _iconForKey(String key) {
    switch (key.toLowerCase()) {
      case 'main':
        return Icons.radio;
      case 'party':
        return Icons.celebration;
      case 'rap':
        return Icons.graphic_eq;
      case 'bandari':
      case 'jonobi':
        return Icons.surfing;
      case 'dep':
      case 'blue':
      case 'ghery':
        return Icons.mood_bad;
      case 'energy':
      case 'motivational':
        return Icons.bolt;
      case 'latest':
        return Icons.new_releases;
      case 'genz':
        return Icons.science;
      default:
        return Icons.grid_view;
    }
  }

  String _subtitle(ApiChannel c) {
    final d = c.description.trim();
    if (d.isNotEmpty) return d;
    return 'کانال ${c.key}';
  }

  @override
  Widget build(BuildContext context) {
    return Directionality(
      textDirection: TextDirection.rtl,
      child: Scaffold(
        body: Container(
          decoration: const BoxDecoration(
            gradient: LinearGradient(
              colors: [Color(0xFF000000), Color(0xFF050505), Color(0xFF1A0905)],
              begin: Alignment.topCenter,
              end: Alignment.bottomCenter,
            ),
          ),
          child: SafeArea(
            child: Stack(
              children: [
                Padding(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 20,
                    vertical: 12,
                  ),
                  child: Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      const Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            'کانال‌ها',
                            style: TextStyle(
                              color: Colors.white,
                              fontSize: 28,
                              fontWeight: FontWeight.bold,
                              letterSpacing: 1.2,
                            ),
                          ),
                          SizedBox(height: 4),
                          Text(
                            'حال و هوای گوشیت رو انتخاب کن',
                            style: TextStyle(
                              color: Colors.white54,
                              fontSize: 12,
                            ),
                          ),
                        ],
                      ),
                      Row(
                        children: [
                          GestureDetector(
                            onTap: () {
                              setState(() {
                                _future = _fetchChannels();
                              });
                            },
                            child: Container(
                              padding: const EdgeInsets.all(6),
                              decoration: BoxDecoration(
                                shape: BoxShape.circle,
                                border: Border.all(color: Colors.white24),
                              ),
                              child: const Icon(
                                Icons.refresh,
                                color: Colors.white,
                                size: 20,
                              ),
                            ),
                          ),
                          const SizedBox(width: 10),
                          GestureDetector(
                            onTap: () => Navigator.pop(context),
                            child: Container(
                              padding: const EdgeInsets.all(6),
                              decoration: BoxDecoration(
                                shape: BoxShape.circle,
                                border: Border.all(color: Colors.white24),
                              ),
                              child: const Icon(
                                Icons.close,
                                color: Colors.white,
                                size: 20,
                              ),
                            ),
                          ),
                        ],
                      ),
                    ],
                  ),
                ),

                Positioned.fill(
                  top: 70,
                  child: FutureBuilder<List<ApiChannel>>(
                    future: _future,
                    builder: (context, snapshot) {
                      if (snapshot.connectionState == ConnectionState.waiting) {
                        return const Center(
                          child: SizedBox(
                            height: 22,
                            width: 22,
                            child: CircularProgressIndicator(strokeWidth: 2),
                          ),
                        );
                      }

                      if (snapshot.hasError) {
                        if (kDebugMode) {
                          debugPrint('❌ channels error: ${snapshot.error}');
                          debugPrint('✅ apiBaseUrl: ${AppConfig.apiBaseUrl}');
                          debugPrint('✅ channels url: ${_channelsEndpoint()}');
                        }
                        return Center(
                          child: Padding(
                            padding: const EdgeInsets.symmetric(horizontal: 24),
                            child: Column(
                              mainAxisSize: MainAxisSize.min,
                              children: [
                                const Text(
                                  'خطا در دریافت کانال‌ها',
                                  style: TextStyle(
                                    color: Colors.white,
                                    fontSize: 16,
                                    fontWeight: FontWeight.w700,
                                  ),
                                ),
                                const SizedBox(height: 8),
                                Text(
                                  '${snapshot.error}',
                                  textAlign: TextAlign.center,
                                  style: const TextStyle(
                                    color: Colors.white54,
                                    fontSize: 12,
                                  ),
                                ),
                                const SizedBox(height: 14),
                                ElevatedButton(
                                  style: ElevatedButton.styleFrom(
                                    backgroundColor: const Color(0xFFFF481F),
                                    foregroundColor: Colors.black,
                                  ),
                                  onPressed: () {
                                    setState(() {
                                      _future = _fetchChannels();
                                    });
                                  },
                                  child: const Text('تلاش مجدد'),
                                ),
                              ],
                            ),
                          ),
                        );
                      }

                      final channels = snapshot.data ?? const <ApiChannel>[];

                      if (channels.isEmpty) {
                        return const Center(
                          child: Text(
                            'هیچ کانالی پیدا نشد',
                            style: TextStyle(color: Colors.white70),
                          ),
                        );
                      }

                      return ListView.builder(
                        padding: const EdgeInsets.symmetric(
                          horizontal: 20,
                          vertical: 16,
                        ),
                        itemCount: channels.length,
                        itemBuilder: (context, index) {
                          final ch = channels[index];
                          final isActive = ch.key == widget.currentChannelKey;

                          return GestureDetector(
                            onTap: () => Navigator.pop(context, ch.key),
                            child: Container(
                              margin: const EdgeInsets.only(bottom: 16),
                              decoration: BoxDecoration(
                                color: isActive
                                    ? const Color(0x22FF481F)
                                    : const Color(0xFF101010),
                                borderRadius: BorderRadius.circular(16),
                                border: Border.all(
                                  color: isActive
                                      ? const Color(0xFFFF481F)
                                      : Colors.white10,
                                  width: 1.2,
                                ),
                              ),
                              child: Row(
                                children: [
                                  AnimatedContainer(
                                    duration: const Duration(milliseconds: 200),
                                    width: 4,
                                    height: 70,
                                    decoration: BoxDecoration(
                                      color: isActive
                                          ? const Color(0xFFFF481F)
                                          : Colors.transparent,
                                      borderRadius: const BorderRadius.only(
                                        topRight: Radius.circular(16),
                                        bottomRight: Radius.circular(16),
                                      ),
                                    ),
                                  ),
                                  const SizedBox(width: 12),
                                  Padding(
                                    padding: const EdgeInsets.symmetric(
                                      vertical: 12.0,
                                    ),
                                    child: Row(
                                      children: [
                                        Container(
                                          width: 42,
                                          alignment: Alignment.center,
                                          child: Text(
                                            ch.emoji.isEmpty ? '🎧' : ch.emoji,
                                            style: const TextStyle(
                                              fontSize: 22,
                                            ),
                                          ),
                                        ),
                                        const SizedBox(width: 6),
                                        Icon(
                                          _iconForKey(ch.key),
                                          color: isActive
                                              ? const Color(0xFFFF481F)
                                              : Colors.white70,
                                          size: 26,
                                        ),
                                        const SizedBox(width: 14),
                                        Column(
                                          crossAxisAlignment:
                                              CrossAxisAlignment.start,
                                          children: [
                                            Text(
                                              ch.name.isEmpty
                                                  ? ch.key
                                                  : ch.name,
                                              style: TextStyle(
                                                color: Colors.white,
                                                fontSize: 18,
                                                fontWeight: isActive
                                                    ? FontWeight.bold
                                                    : FontWeight.w500,
                                              ),
                                            ),
                                            const SizedBox(height: 2),
                                            Text(
                                              _subtitle(ch),
                                              style: const TextStyle(
                                                color: Colors.white54,
                                                fontSize: 12,
                                              ),
                                            ),
                                            const SizedBox(height: 4),
                                            if (isActive)
                                              const Text(
                                                'در حال پخش',
                                                style: TextStyle(
                                                  color: Color(0xFFFF481F),
                                                  fontSize: 11,
                                                ),
                                              ),
                                          ],
                                        ),
                                      ],
                                    ),
                                  ),
                                ],
                              ),
                            ),
                          );
                        },
                      );
                    },
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
