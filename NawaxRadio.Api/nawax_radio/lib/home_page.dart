//home_page.dart
import 'dart:async';
import 'dart:convert';
import 'package:nawax_radio/services/audio_player_service.dart';
import 'package:flutter/foundation.dart' show kIsWeb;
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'package:just_audio/just_audio.dart';
import 'package:nawax_radio/config/app_config.dart';
import 'package:nawax_radio/pages/channels_page.dart';
import 'package:nawax_radio/pages/settings_page.dart';
import 'package:nawax_radio/widgets/organic_pulse_visualizer.dart';

late final AudioPlayer _player;
late final AudioPlayerService _audioSvc;

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
  int _playGeneration = 0; // Ù‡Ø± Ø¨Ø§Ø± ØªØ¹ÙˆÛŒØ¶ Ú©Ø§Ù†Ø§Ù„/ØªØ¹ÙˆÛŒØ¶ ØªØ±Ú© ++
  bool _autoNextInFlight = false; // Ù‚ÙÙ„ AutoNext
  String? _lastPreparedUrl; // Ø¬Ù„ÙˆÚ¯ÛŒØ±ÛŒ Ø§Ø² setUrl ØªÚ©Ø±Ø§Ø±ÛŒ

  @override
  void initState() {
    super.initState();

    _playerStateSub = _player.playerStateStream.listen((state) async {
      if (state.processingState == ProcessingState.completed) {
        await _handleAutoNext();
      }
    });

    // Ø±ÙˆÛŒ ÙˆØ¨ autoplay Ù…Ù…Ù†ÙˆØ¹Ù‡: ÙÙ‚Ø· Ø¢Ù…Ø§Ø¯Ù‡ Ù…ÛŒâ€ŒÚ©Ù†ÛŒÙ… (Ø¨Ø¯ÙˆÙ† play)
    _playNextFromRadio(autoplay: false, forceReload: true);
  }

  Uri _radioNowEndpoint() =>
      Uri.parse('${AppConfig.apiBaseUrl}/radio/$_currentChannelKey/now');

  String _radioStreamUrl() =>
      '${AppConfig.apiBaseUrl}/radio/$_currentChannelKey/stream';

  // URL ÛŒÚ©ØªØ§ Ø¨Ø±Ø§ÛŒ Ù…Ø¬Ø¨ÙˆØ± Ú©Ø±Ø¯Ù† player Ø¨Ù‡ reload (Ù…Ø®ØµÙˆØµØ§Ù‹ ÙˆØ¨)
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

    // âœ… Ø§ÙˆÙ„ÙˆÛŒØª: Ù…ØªØ§Ø¯ÛŒØªØ§ÛŒ ÙˆØ§Ù‚Ø¹ÛŒ Ú©Ù‡ Ø¨Ø§ÛŒØ¯ Ø¨Ú©â€ŒØ§Ù†Ø¯ Ø¨Ø¯Ù‡
    final title = _s(np['title']);
    final artist = _s(np['artist']);

    // fallback ÙØ¹Ù„ÛŒ Ø¨Ú©â€ŒØ§Ù†Ø¯
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

    // Ù‚ÙÙ„ Ù‡Ù…Ø²Ù…Ø§Ù†ÛŒ: ÙÙ‚Ø· ÛŒÚ© Ø¨Ø§Ø±
    if (_autoNextInFlight) return;
    _autoNextInFlight = true;

    final genAtStart = _playGeneration;

    try {
      debugPrint('âœ… AUTO NEXT TRIGGERED');

      // Ø§Ú¯Ø± ÙˆØ³Ø·Ø´ Ú©Ø§Ù†Ø§Ù„/Ù¾Ø®Ø´ Ø¹ÙˆØ¶ Ø´Ø¯ØŒ Ø§ÛŒÙ† next Ø¨ÛŒâ€ŒØ§Ø«Ø±
      if (genAtStart != _playGeneration) return;

      // âœ… 1) tell backend to advance
      final res = await http.post(
        Uri.parse('${AppConfig.apiBaseUrl}/radio/$_currentChannelKey/next'),
      );

      if (res.statusCode < 200 || res.statusCode >= 300) {
        debugPrint('âŒ NEXT failed (${res.statusCode}): ${res.body}');
        return; // Ù†Ø±Ùˆ setUrl Ø¬Ø¯ÛŒØ¯ØŒ Ú†ÙˆÙ† Ø¨Ú©â€ŒØ§Ù†Ø¯ next Ù†Ø¯Ø§Ø¯Ù‡
      }

      // âœ… 2) reload for player
      await _playNextFromRadio(autoplay: true, forceReload: true);
    } catch (e) {
      debugPrint('âŒ AutoNext error: $e');
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
    // Ø¬Ù„ÙˆÚ¯ÛŒØ±ÛŒ Ø§Ø² Ù‡Ù…Ø²Ù…Ø§Ù†ÛŒ (fetch)
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

      // 2) Ø§Ù†ØªØ®Ø§Ø¨ URL Ø¨Ø±Ø§ÛŒ Ù¾Ø®Ø´
      final np = _nowPlayingRoot(data);
      final audioUrlRaw = _s(np['audioUrl']);

      String rawUrl;

      // âœ… WEB: ÙÙ‚Ø· stream (Ø¨Ø±Ø§ÛŒ Ø¯ÙˆØ± Ø²Ø¯Ù† CORS Ú¯ÙˆÚ¯Ù„ Ø§Ø³ØªÙˆØ±ÛŒØ¬)
      if (kIsWeb) {
        rawUrl = forceReload ? _radioStreamUrlUnique() : _radioStreamUrl();
      } else {
        // âœ… MOBILE: audioUrl Ù…Ø³ØªÙ‚ÛŒÙ… (Ø§Ú¯Ø± Ù…ÙˆØ¬ÙˆØ¯ Ù†Ø¨ÙˆØ¯ fallback Ø¨Ù‡ stream)
        if (audioUrlRaw.isNotEmpty) {
          rawUrl = audioUrlRaw;
        } else {
          rawUrl = forceReload ? _radioStreamUrlUnique() : _radioStreamUrl();
        }
      }

      final urlForPlayer = Uri.encodeFull(rawUrl);
      debugPrint('ðŸŽ§ setUrl => $urlForPlayer');

      // Ø§Ú¯Ø± URL Ù‡Ù…Ø§Ù† Ù‚Ø¨Ù„ÛŒ Ø§Ø³Øª Ùˆ forceReload Ù‡Ù… Ù†ÛŒØ³ØªØŒ Ø¯ÙˆØ¨Ø§Ø±Ù‡ setUrl Ù†Ø²Ù†
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
      debugPrint('âŒ stream prepare error: $e');
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
      debugPrint('âŒ play blocked: $e');
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

  Widget _buildProgressBar() {
    return StreamBuilder<Duration>(
      stream: _player.positionStream,
      builder: (context, snapshot) {
        final position = snapshot.data ?? Duration.zero;
        final total = _player.duration ?? Duration.zero;

        double progress = 0;
        if (total.inMilliseconds > 0) {
          progress = position.inMilliseconds / total.inMilliseconds;
        }

        return SizedBox(
          height: 4,
          child: LinearProgressIndicator(
            value: progress.clamp(0, 1),
            backgroundColor: Colors.white,
            valueColor: const AlwaysStoppedAnimation(Colors.black),
          ),
        );
      },
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
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 24),
              child: Row(
                children: [
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
                    icon: const Icon(Icons.settings, color: Colors.black),
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
              _songTitle,
              textAlign: TextAlign.center,
              style: const TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.w600,
                color: Colors.black,
              ),
            ),
            Text(
              _songSinger,
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
            if (_isLoadingTrack)
              const Padding(
                padding: EdgeInsets.only(top: 8),
                child: SizedBox(
                  height: 18,
                  width: 18,
                  child: CircularProgressIndicator(strokeWidth: 2),
                ),
              ),
            if (_errorText.isNotEmpty)
              Padding(
                padding: const EdgeInsets.only(top: 8),
                child: Text(
                  _errorText,
                  textAlign: TextAlign.center,
                  style: const TextStyle(
                    fontSize: 12,
                    color: Colors.black,
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ),
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
            Container(
              height: 80,
              margin: const EdgeInsets.symmetric(horizontal: 16),
              decoration: BoxDecoration(
                color: Colors.black,
                borderRadius: BorderRadius.circular(40),
              ),
              padding: const EdgeInsets.symmetric(horizontal: 32),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  GestureDetector(
                    onTap: () => _seekRelative(-10),
                    child: const Text(
                      '-10S',
                      style: TextStyle(color: Colors.white),
                    ),
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
                  GestureDetector(
                    onTap: () => _seekRelative(10),
                    child: const Text(
                      '+10S',
                      style: TextStyle(color: Colors.white),
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 24),
            GestureDetector(
              onTap: () async {
                final selected = await Navigator.push<String>(
                  context,
                  MaterialPageRoute(
                    builder: (_) =>
                        ChannelsPage(currentChannelKey: _currentChannelKey),
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

                  await _playNextFromRadio(
                    autoplay: _userUnlockedAudio,
                    forceReload: true,
                  );

                  if (mounted) setState(() {});
                }
              },
              child: const Icon(Icons.grid_view, size: 40, color: Colors.black),
            ),
            const SizedBox(height: 16),
          ],
        ),
      ),
    );
  }
}
