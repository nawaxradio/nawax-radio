//home_page.dart
import 'dart:async';
import 'dart:convert';
import 'package:flutter/foundation.dart' show kIsWeb;
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'package:just_audio/just_audio.dart';
import 'package:nawax_radio/config/app_config.dart';
import 'package:nawax_radio/pages/channels_page.dart';
import 'package:nawax_radio/pages/settings_page.dart';
import 'package:nawax_radio/widgets/organic_pulse_visualizer.dart';

class HomePage extends StatefulWidget {
  const HomePage({super.key});

  @override
  State<HomePage> createState() => _HomePageState();
}

class _HomePageState extends State<HomePage> {
  String _currentChannelKey = 'main';

  final AudioPlayer _player = AudioPlayer();

  String _songTitle = '';
  String _songSinger = '';
  bool _isJingle = false;

  bool _userUnlockedAudio = false;
  bool _isFetchingNext = false;
  bool _hasPreparedTrack = false;

  bool _isLoadingTrack = false;
  String _errorText = '';

  late final StreamSubscription<PlayerState> _playerStateSub;

  // ---------------- AutoNext lock & generation ----------------
  int _playGeneration = 0; // هر بار تعویض کانال/تعویض ترک ++
  bool _autoNextInFlight = false; // قفل AutoNext
  String? _lastPreparedUrl; // جلوگیری از setUrl تکراری

  @override
  void initState() {
    super.initState();

    _playerStateSub = _player.playerStateStream.listen((state) async {
      if (state.processingState == ProcessingState.completed) {
        await _handleAutoNext();
      }
    });

    // روی وب autoplay ممنوعه: فقط آماده می‌کنیم (بدون play)
    _playNextFromRadio(autoplay: false, forceReload: true);
  }

  Uri _radioNowEndpoint() =>
      Uri.parse('${AppConfig.apiBaseUrl}/radio/$_currentChannelKey/now');

  String _radioStreamUrl() =>
      '${AppConfig.apiBaseUrl}/radio/$_currentChannelKey/stream';

  // URL یکتا برای مجبور کردن player به reload (مخصوصاً وب)
  String _radioStreamUrlUnique() {
    final ts = DateTime.now().millisecondsSinceEpoch;
    return '${AppConfig.apiBaseUrl}/radio/$_currentChannelKey/stream?t=$ts';
  }

  // ---------------- JSON helpers ----------------
  String _s(dynamic v) => (v is String) ? v.trim() : '';

  Map<String, dynamic>? _m(dynamic v) {
    if (v is Map<String, dynamic>) return v;
    if (v is Map) return v.map((k, val) => MapEntry(k.toString(), val));
    return null;
  }

  Map<String, dynamic> _nowPlayingRoot(Map<String, dynamic> root) {
    final np = _m(root['nowPlaying']);
    return np ?? root;
  }

  void _applyMetadata(Map<String, dynamic> root) {
    final np = _nowPlayingRoot(root);

    // ✅ اولویت: متادیتای واقعی که باید بک‌اند بده
    final title = _s(np['title']);
    final artist = _s(np['artist']);

    // fallback فعلی بک‌اند
    final name = _s(np['name']);
    final singer = _s(np['singer']);

    final isJingle = (np['isJingle'] is bool)
        ? (np['isJingle'] as bool)
        : false;

    String finalTitle = title.isNotEmpty ? title : name;
    String finalArtist = artist.isNotEmpty ? artist : singer;

    if (title.isEmpty && finalTitle.isNotEmpty) {
      finalTitle = _prettifyFromFilename(finalTitle);
    }

    _songTitle = finalTitle;
    _songSinger = finalArtist.isNotEmpty ? finalArtist : 'Unknown';
    _isJingle = isJingle == true;
  }

  String _prettifyFromFilename(String raw) {
    var s = raw;

    s = s.replaceAll('.mp3', '').replaceAll('.wav', '').replaceAll('.m4a', '');
    s = s.replaceAll(RegExp(r'[_\-]+'), ' ').trim();
    s = s.replaceAll(RegExp(r'\b(320|256|192|128)\b'), '');
    s = s.replaceAll(
      RegExp(
        r'\b(official|lyrics|lyric|audio|video|remix|mix)\b',
        caseSensitive: false,
      ),
      '',
    );
    s = s.replaceAll(RegExp(r'\s{2,}'), ' ').trim();

    return s;
  }

  // ---------------- AUTO NEXT (LOCKED) ----------------
  Future<void> _handleAutoNext() async {
    if (!_userUnlockedAudio) return;

    // قفل همزمانی: فقط یک بار
    if (_autoNextInFlight) return;
    _autoNextInFlight = true;

    final genAtStart = _playGeneration;

    try {
      debugPrint('✅ AUTO NEXT TRIGGERED');

      // اگر وسطش کانال/پخش عوض شد، این next بی‌اثر
      if (genAtStart != _playGeneration) return;

      // ✅ 1) tell backend to advance
      final res = await http.post(
        Uri.parse('${AppConfig.apiBaseUrl}/radio/$_currentChannelKey/next'),
      );

      if (res.statusCode < 200 || res.statusCode >= 300) {
        debugPrint('❌ NEXT failed (${res.statusCode}): ${res.body}');
        return; // نرو setUrl جدید، چون بک‌اند next نداده
      }

      // ✅ 2) reload for player
      await _playNextFromRadio(autoplay: true, forceReload: true);
    } catch (e) {
      debugPrint('❌ AutoNext error: $e');
    } finally {
      if (genAtStart == _playGeneration) {
        _autoNextInFlight = false;
      }
    }
  }

  // ---------------- RADIO FLOW ----------------
  Future<void> _playNextFromRadio({
    required bool autoplay,
    bool forceReload = false,
  }) async {
    // جلوگیری از همزمانی (fetch)
    if (_isFetchingNext) return;
    _isFetchingNext = true;

    if (mounted) {
      setState(() {
        _isLoadingTrack = true;
        _errorText = '';
      });
    }

    try {
      final nowRes = await http.get(_radioNowEndpoint());
      if (nowRes.statusCode != 200) {
        if (mounted) {
          setState(() {
            _errorText = 'Radio NOW error (${nowRes.statusCode})';
          });
        }
        return;
      }

      debugPrint('apiBaseUrl => ${AppConfig.apiBaseUrl}');
      debugPrint('NOW BODY => ${nowRes.body}');

      final data = json.decode(nowRes.body) as Map<String, dynamic>;

      // 1) metadata
      _applyMetadata(data);

      // 2) انتخاب URL برای پخش
      final np = _nowPlayingRoot(data);
      final audioUrlRaw = _s(np['audioUrl']);

      String rawUrl;

      // ✅ WEB: فقط stream (برای دور زدن CORS گوگل استوریج)
      if (kIsWeb) {
        rawUrl = forceReload ? _radioStreamUrlUnique() : _radioStreamUrl();
      } else {
        // ✅ MOBILE: audioUrl مستقیم (اگر موجود نبود fallback به stream)
        if (audioUrlRaw.isNotEmpty) {
          rawUrl = audioUrlRaw;
        } else {
          rawUrl = forceReload ? _radioStreamUrlUnique() : _radioStreamUrl();
        }
      }

      final urlForPlayer = Uri.encodeFull(rawUrl);
      debugPrint('🎧 setUrl => $urlForPlayer');

      // اگر URL همان قبلی است و forceReload هم نیست، دوباره setUrl نزن
      if (!forceReload && _lastPreparedUrl == urlForPlayer) {
        if (autoplay && _userUnlockedAudio) {
          await _safePlay();
        }
        if (mounted) setState(() {});
        return;
      }

      _lastPreparedUrl = urlForPlayer;

      _hasPreparedTrack = false;
      await _player.setUrl(urlForPlayer);
      _hasPreparedTrack = true;

      if (autoplay && _userUnlockedAudio) {
        await _safePlay();
      }

      if (mounted) setState(() {});
    } catch (e) {
      debugPrint('❌ stream prepare error: $e');
      if (mounted) {
        setState(() {
          _errorText = 'stream prepare error: $e\nurl: ${_radioStreamUrl()}';
        });
      }
    } finally {
      _isFetchingNext = false;
      if (mounted) {
        setState(() {
          _isLoadingTrack = false;
        });
      }
    }
  }

  Future<void> _safePlay() async {
    try {
      await _player.play();
    } catch (e) {
      debugPrint('❌ play blocked: $e');
      if (mounted) {
        setState(() {
          _errorText = kIsWeb
              ? 'Browser blocked audio. Tap Play once.'
              : 'Play error: $e';
        });
      }
    }
  }

  Future<void> _unlockAndStart() async {
    if (_userUnlockedAudio) return;

    if (mounted) {
      setState(() {
        _errorText = '';
      });
    }

    if (_isFetchingNext) {
      await Future.delayed(const Duration(milliseconds: 150));
    }

    if (!_hasPreparedTrack) {
      await _playNextFromRadio(autoplay: false, forceReload: true);
    }

    _userUnlockedAudio = true;
    await _safePlay();

    if (mounted) setState(() {});
  }

  void _seekRelative(int seconds) {
    final pos = _player.position;
    Duration newPos = pos + Duration(seconds: seconds);
    if (newPos < Duration.zero) newPos = Duration.zero;
    _player.seek(newPos);
  }

  String get _channelTitle => _currentChannelKey.toUpperCase();

  Future<void> _openChannels() async {
    final selected = await Navigator.push<String>(
      context,
      MaterialPageRoute(
        builder: (_) => ChannelsPage(currentChannelKey: _currentChannelKey),
      ),
    );

    if (selected != null && selected != _currentChannelKey) {
      _currentChannelKey = selected;

      // invalidate old next calls & reset locks
      _playGeneration++;
      _autoNextInFlight = false;
      _lastPreparedUrl = null;

      await _player.stop();
      _hasPreparedTrack = false;

      await _playNextFromRadio(autoplay: _userUnlockedAudio, forceReload: true);

      if (mounted) setState(() {});
    }
  }

  Widget _buildProgressBar() {
    return StreamBuilder<Duration>(
      stream: _player.positionStream,
      builder: (context, snapshot) {
        final position = snapshot.data ?? Duration.zero;
        final total = _player.duration ?? Duration.zero;

        final hasTotal = total.inMilliseconds > 0;
        final progress = hasTotal
            ? (position.inMilliseconds / total.inMilliseconds).clamp(0.0, 1.0)
            : 0.0;

        return ClipRRect(
          borderRadius: BorderRadius.circular(999),
          child: SizedBox(
            height: 6,
            child: LinearProgressIndicator(
              value: hasTotal
                  ? progress
                  : null, // اگر duration نداریم indeterminate
              backgroundColor: Colors.white.withOpacity(0.25),
              valueColor: const AlwaysStoppedAnimation(Colors.black),
            ),
          ),
        );
      },
    );
  }

  Widget _buildStatusRow() {
    return SizedBox(
      height: 32,
      child: Center(
        child: AnimatedSwitcher(
          duration: const Duration(milliseconds: 180),
          child: _isLoadingTrack
              ? const SizedBox(
                  key: ValueKey('loading'),
                  height: 18,
                  width: 18,
                  child: CircularProgressIndicator(strokeWidth: 2),
                )
              : (_errorText.isNotEmpty
                    ? Text(
                        _errorText,
                        key: const ValueKey('error'),
                        textAlign: TextAlign.center,
                        style: const TextStyle(
                          fontSize: 12,
                          color: Colors.black,
                          fontWeight: FontWeight.w600,
                        ),
                      )
                    : const SizedBox(key: ValueKey('empty'))),
        ),
      ),
    );
  }

  @override
  void dispose() {
    _playerStateSub.cancel();
    _player.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    const nawaxOrange = Color(0xFFFF481F);

    return Scaffold(
      backgroundColor: nawaxOrange,
      body: SafeArea(
        child: Column(
          children: [
            const SizedBox(height: 16),

            // ---------------- Header (Channels left / Settings right) ----------------
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 16),
              child: Row(
                children: [
                  IconButton(
                    icon: const Icon(
                      Icons.grid_view_rounded,
                      color: Colors.black,
                    ),
                    onPressed: _openChannels,
                  ),
                  const Spacer(),
                  Column(
                    children: const [
                      Text(
                        'NAWAX',
                        style: TextStyle(
                          fontSize: 42,
                          fontWeight: FontWeight.w900,
                          letterSpacing: 4,
                          color: Colors.white,
                        ),
                      ),
                      Text(
                        'RADIO',
                        style: TextStyle(
                          fontSize: 10,
                          fontWeight: FontWeight.w700,
                          letterSpacing: 2,
                          color: Colors.black,
                        ),
                      ),
                    ],
                  ),
                  const Spacer(),
                  IconButton(
                    icon: const Icon(
                      Icons.settings_rounded,
                      color: Colors.black,
                    ),
                    onPressed: () {
                      Navigator.push(
                        context,
                        MaterialPageRoute(builder: (_) => const SettingsPage()),
                      );
                    },
                  ),
                ],
              ),
            ),

            const SizedBox(height: 24),

            Text(
              _channelTitle,
              style: const TextStyle(
                fontSize: 28,
                fontWeight: FontWeight.w800,
                letterSpacing: 2,
                color: Colors.black,
              ),
            ),

            const SizedBox(height: 8),

            Text(
              (_songTitle.isNotEmpty) ? _songTitle : 'Loading…',
              textAlign: TextAlign.center,
              maxLines: 2,
              overflow: TextOverflow.ellipsis,
              style: const TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.w600,
                color: Colors.black,
              ),
            ),

            Text(
              (_songSinger.isNotEmpty) ? _songSinger : '—',
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: const TextStyle(fontSize: 12, color: Colors.black),
            ),

            const SizedBox(height: 8),

            if (_isJingle)
              const Text(
                'JINGLE',
                style: TextStyle(
                  fontSize: 11,
                  fontWeight: FontWeight.w800,
                  letterSpacing: 2,
                  color: Colors.black,
                ),
              ),

            // ---------------- Stable status row (no layout shift) ----------------
            _buildStatusRow(),

            // ---------------- Visualizer (UNCHANGED) ----------------
            Expanded(
              child: Center(
                child: StreamBuilder<PlayerState>(
                  stream: _player.playerStateStream,
                  builder: (context, snapshot) {
                    final isPlaying = snapshot.data?.playing ?? false;
                    return OrganicPulseVisualizer(
                      width: 260,
                      height: 120,
                      barColor: Colors.black,
                      bars: 24,
                      maxBarHeight: 80,
                      spacing: 4,
                      isActive: isPlaying,
                    );
                  },
                ),
              ),
            ),

            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 16),
              child: _buildProgressBar(),
            ),

            const SizedBox(height: 16),

            // ---------------- Controls ----------------
            Container(
              height: 80,
              margin: const EdgeInsets.symmetric(horizontal: 16),
              decoration: BoxDecoration(
                color: Colors.black,
                borderRadius: BorderRadius.circular(40),
              ),
              padding: const EdgeInsets.symmetric(horizontal: 18),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  IconButton(
                    onPressed: () => _seekRelative(-10),
                    icon: const Icon(
                      Icons.replay_10_rounded,
                      color: Colors.white,
                    ),
                    iconSize: 28,
                  ),

                  StreamBuilder<PlayerState>(
                    stream: _player.playerStateStream,
                    builder: (context, snapshot) {
                      final isPlaying = snapshot.data?.playing ?? false;

                      return GestureDetector(
                        onTap: () async {
                          if (!_userUnlockedAudio) {
                            await _unlockAndStart();
                            return;
                          }

                          if (isPlaying) {
                            await _player.pause();
                          } else {
                            await _safePlay();
                          }

                          if (mounted) setState(() {});
                        },
                        child: Icon(
                          isPlaying ? Icons.pause : Icons.play_arrow,
                          color: Colors.white,
                          size: 40,
                        ),
                      );
                    },
                  ),

                  IconButton(
                    onPressed: () => _seekRelative(10),
                    icon: const Icon(
                      Icons.forward_10_rounded,
                      color: Colors.white,
                    ),
                    iconSize: 28,
                  ),
                ],
              ),
            ),

            const SizedBox(height: 16),
          ],
        ),
      ),
    );
  }
}
