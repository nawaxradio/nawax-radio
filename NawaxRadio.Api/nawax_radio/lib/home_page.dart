import 'dart:async';
import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'package:just_audio/just_audio.dart';

import 'package:nawax_radio/pages/channels_page.dart';
import 'package:nawax_radio/pages/settings_page.dart';
import 'package:nawax_radio/widgets/organic_pulse_visualizer.dart';

// ================== API CONFIG ==================
const String _apiBaseUrl = 'http://localhost:5246';

class HomePage extends StatefulWidget {
  const HomePage({super.key});

  @override
  State<HomePage> createState() => _HomePageState();
}

class _HomePageState extends State<HomePage> {
  // کانال فعلی
  String _currentChannelKey = 'main';

  // پلیر
  final AudioPlayer _player = AudioPlayer();

  // اطلاعات آهنگ فعلی
  String _songTitle = '';
  String _songSinger = '';
  bool _isJingle = false;

  // Web autoplay lock
  bool _userUnlockedAudio = false;

  // جلوگیری از درخواست همزمان
  bool _isFetchingNext = false;

  // برای نمایش وضعیت
  bool _isLoadingTrack = false;
  String _errorText = '';

  late final StreamSubscription<PlayerState> _playerStateSub;

  // ================== INIT ==================
  @override
  void initState() {
    super.initState();

    _playerStateSub = _player.playerStateStream.listen((state) async {
      // وقتی آهنگ تموم شد، بعدی رو فقط وقتی مجازیم autoplay کنیم بگیر
      if (state.processingState == ProcessingState.completed) {
        if (_userUnlockedAudio) {
          await _playNextFromRadio(autoplay: true);
        }
      }
    });

    // مهم: روی وب، autoplay ممنوعه. پس اول فقط track رو "آماده" کن (بدون play)
    _playNextFromRadio(autoplay: false);
  }

  // ================== RADIO STREAM ==================
  Future<void> _playNextFromRadio({required bool autoplay}) async {
    if (_isFetchingNext) return;
    _isFetchingNext = true;

    setState(() {
      _isLoadingTrack = true;
      _errorText = '';
    });

    try {
      final url = Uri.parse('$_apiBaseUrl/radio/$_currentChannelKey/stream');
      final res = await http.get(url);

      if (res.statusCode != 200) {
        setState(() {
          _errorText = 'Radio API error (${res.statusCode})';
        });
        return;
      }

      final data = json.decode(res.body) as Map<String, dynamic>;

      final audioUrl = data['audioUrl'] as String?;
      if (audioUrl == null || audioUrl.isEmpty) {
        setState(() {
          _errorText = 'No audioUrl from server';
        });
        return;
      }

      // metadata
      _songTitle = (data['name'] ?? '').toString();
      _songSinger = (data['singer'] ?? '').toString();
      _isJingle = (data['isJingle'] ?? false) == true;

      // آماده کردن آهنگ
      await _player.setUrl(audioUrl);

      // اگر اجازه autoplay داریم، پلی کن
      if (autoplay && _userUnlockedAudio) {
        await _safePlay();
      }

      setState(() {});
    } catch (e) {
      setState(() {
        _errorText = 'radio error: $e';
      });
      debugPrint('❌ radio error: $e');
    } finally {
      _isFetchingNext = false;
      if (mounted) {
        setState(() {
          _isLoadingTrack = false;
        });
      }
    }
  }

  // یک پلی امن: اگر وب اجازه نداد، unlock رو false نگه می‌داریم
  Future<void> _safePlay() async {
    try {
      await _player.play();
    } catch (e) {
      // روی وب اگر بدون تعامل user پلی کنیم اینجا می‌خوره
      debugPrint('❌ play blocked: $e');
      setState(() {
        _errorText =
            'Audio is blocked by browser. Tap Play once to start the radio.';
      });
    }
  }

  // اولین تعامل کاربر برای باز شدن autoplay وب
  Future<void> _unlockAndStart() async {
    if (_userUnlockedAudio) return;

    setState(() {
      _errorText = '';
    });

    // تلاش برای پلی: اگر آماده نباشه، اول یک track بگیر
    if (_player.duration == null) {
      await _playNextFromRadio(autoplay: false);
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

  // ================== PROGRESS BAR ==================
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

  // ================== DISPOSE ==================
  @override
  void dispose() {
    _playerStateSub.cancel();
    _player.dispose();
    super.dispose();
  }

  // ================== BUILD ==================
  @override
  Widget build(BuildContext context) {
    const nawaxOrange = Color(0xFFFF481F);

    return Scaffold(
      backgroundColor: nawaxOrange,
      body: SafeArea(
        child: Column(
          children: [
            const SizedBox(height: 16),

            // -------- Header --------
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

            // -------- Controls --------
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
                          // اگر اولین بار روی وب است، اول unlock کن
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

                  // کانال عوض شد: یک track جدید بگیر
                  // اگر قبلا unlock شده بود، autoplay کن
                  await _playNextFromRadio(autoplay: _userUnlockedAudio);

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
