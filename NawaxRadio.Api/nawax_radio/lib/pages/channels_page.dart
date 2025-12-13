import 'package:flutter/material.dart';

class RadioChannel {
  final String key;
  final String title;
  final String subtitle;
  final IconData icon;

  const RadioChannel({
    required this.key,
    required this.title,
    required this.subtitle,
    required this.icon,
  });
}

class ChannelsPage extends StatelessWidget {
  final String currentChannelKey;

  const ChannelsPage({super.key, required this.currentChannelKey});

  List<RadioChannel> get _channels => const [
    RadioChannel(
      key: 'main',
      title: 'رادیوی اصلی',
      subtitle: 'ترکیب هیت‌ها ۲۴/۷',
      icon: Icons.radio,
    ),
    RadioChannel(
      key: 'party',
      title: 'پارتی',
      subtitle: 'انرژی بالا • مهمونی • کلاب',
      icon: Icons.celebration,
    ),
    RadioChannel(
      key: 'rap',
      title: 'رپ / هیپ‌هاپ',
      subtitle: 'بیت • فلو • بار',
      icon: Icons.graphic_eq,
    ),
    // قبلاً shooti بود → الان jonobi تا با /radio/jonobi مچ شود
    RadioChannel(
      key: 'jonobi',
      title: 'جنوبی / بندری',
      subtitle: 'حال خوب • بندری • جنوبی',
      icon: Icons.surfing,
    ),
    RadioChannel(
      key: 'blue',
      title: 'مود آبی',
      subtitle: 'دیپ • غمگین • احساسی',
      icon: Icons.mood_bad,
    ),
    RadioChannel(
      key: 'motivational',
      title: 'انگیزشی',
      subtitle: 'ورزش • تمرکز • قدرت',
      icon: Icons.bolt,
    ),
    RadioChannel(
      key: 'latest',
      title: 'آخرین‌ها',
      subtitle: 'جدیدترین ترک‌ها',
      icon: Icons.new_releases,
    ),
  ];

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
                // ---------------- HEADER ----------------
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
                ),

                // ---------------- LIST ----------------
                Positioned.fill(
                  top: 70,
                  child: ListView.builder(
                    padding: const EdgeInsets.symmetric(
                      horizontal: 20,
                      vertical: 16,
                    ),
                    itemCount: _channels.length,
                    itemBuilder: (context, index) {
                      final ch = _channels[index];
                      final bool isActive = ch.key == currentChannelKey;

                      return GestureDetector(
                        onTap: () {
                          Navigator.pop(context, ch.key);
                        },
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
                              // نوار نارنجی کنار کانال فعال
                              AnimatedContainer(
                                duration: const Duration(milliseconds: 200),
                                width: 4,
                                height: 64,
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
                                    Icon(
                                      ch.icon,
                                      color: isActive
                                          ? const Color(0xFFFF481F)
                                          : Colors.white70,
                                      size: 28,
                                    ),
                                    const SizedBox(width: 14),
                                    Column(
                                      crossAxisAlignment:
                                          CrossAxisAlignment.start,
                                      children: [
                                        Text(
                                          ch.title,
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
                                          ch.subtitle,
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
