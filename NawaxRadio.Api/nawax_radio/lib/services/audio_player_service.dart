import 'dart:async';
import 'package:just_audio/just_audio.dart';
import 'package:http/http.dart' as http;

class AudioPlayerService {
  final AudioPlayer player;
  final String apiBase;

  StreamSubscription<PlayerState>? _stateSub;
  String? _currentChannel;

  AudioPlayerService({required this.player, required this.apiBase});

  /// Call this once when channel changes.
  Future<void> playChannel(String channel) async {
    _currentChannel = channel;

    // Ensure listener is attached only once.
    _ensureAutoNextListener();

    // Set stream url and play
    final streamUrl = '$apiBase/radio/$channel/stream';
    await player.setUrl(streamUrl);
    await player.play();
  }

  void _ensureAutoNextListener() {
    if (_stateSub != null) return; // already attached

    _stateSub = player.playerStateStream.listen((state) async {
      if (state.processingState == ProcessingState.completed) {
        final ch = _currentChannel;
        if (ch == null || ch.isEmpty) return;

        // 1) tell backend to rotate
        await http.post(Uri.parse('$apiBase/radio/$ch/next'));

        // 2) reload stream
        await player.setUrl('$apiBase/radio/$ch/stream');
        await player.play();
      }
    });
  }

  Future<void> dispose() async {
    await _stateSub?.cancel();
    _stateSub = null;
    await player.dispose();
  }
}
