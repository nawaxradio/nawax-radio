import 'dart:math';
import 'package:flutter/material.dart';

/// ÛŒÚ© ÙˆÛŒÚ˜ÙˆØ§Ù„Ø§ÛŒØ²Ø± Ø³Ø§Ø¯Ù‡ Ø´Ø¨ÛŒÙ‡ Ø³Ø§Ù†Ø¯ Ø¨Ø§Ø±Ù‡Ø§ÛŒ Ú©Ù„Ø§Ø³ÛŒÚ©
/// ÙˆÙ‚ØªÛŒ isActive = true Ø¨Ø§Ø´Ù‡ Ø§Ù†ÛŒÙ…ÛŒØ´Ù† Ù…ÛŒÙ„Ù‡â€ŒÙ‡Ø§ ØªÚ©Ø±Ø§Ø± Ù…ÛŒØ´Ù‡
class OrganicPulseVisualizer extends StatefulWidget {
  final double width;
  final double height;
  final Color barColor;
  final int bars; // ØªØ¹Ø¯Ø§Ø¯ Ù…ÛŒÙ„Ù‡â€ŒÙ‡Ø§
  final double maxBarHeight;
  final double spacing;
  final bool isActive;

  const OrganicPulseVisualizer({
    super.key,
    this.width = 260,
    this.height = 120,
    this.barColor = Colors.black,
    this.bars = 24,
    this.maxBarHeight = 100,
    this.spacing = 4,
    this.isActive = false,
  });

  @override
  State<OrganicPulseVisualizer> createState() => _OrganicPulseVisualizerState();
}

class _OrganicPulseVisualizerState extends State<OrganicPulseVisualizer>
    with SingleTickerProviderStateMixin {
  late AnimationController _controller;

  @override
  void initState() {
    super.initState();

    _controller = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 1200),
    );

    if (widget.isActive) {
      _controller.repeat();
    }
  }

  @override
  void didUpdateWidget(covariant OrganicPulseVisualizer oldWidget) {
    super.didUpdateWidget(oldWidget);

    if (widget.isActive && !_controller.isAnimating) {
      _controller.repeat();
    } else if (!widget.isActive && _controller.isAnimating) {
      _controller.stop();
    }
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: widget.width,
      height: widget.height,
      child: AnimatedBuilder(
        animation: _controller,
        builder: (context, _) {
          return CustomPaint(
            painter: _BarsPainter(
              progress: _controller.value,
              bars: widget.bars,
              maxBarHeight: widget.maxBarHeight,
              spacing: widget.spacing,
              color: widget.barColor,
            ),
          );
        },
      ),
    );
  }
}

class _BarsPainter extends CustomPainter {
  final double progress;
  final int bars;
  final double maxBarHeight;
  final double spacing;
  final Color color;

  _BarsPainter({
    required this.progress,
    required this.bars,
    required this.maxBarHeight,
    required this.spacing,
    required this.color,
  });

  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()
      ..color = color
      ..style = PaintingStyle.fill;

    final totalSpacing = spacing * (bars - 1);
    final barWidth = (size.width - totalSpacing) / bars;

    final midY = size.height / 2;

    for (int i = 0; i < bars; i++) {
      // ÛŒÚ© Ù…ÙˆØ¬ Ø³ÛŒÙ†ÙˆØ³ÛŒ Ù†Ø±Ù… Ø¨Ø±Ø§ÛŒ Ù‡Ø± Ù…ÛŒÙ„Ù‡ Ø¨Ø§ Ø§Ø®ØªÙ„Ø§Ù ÙØ§Ø²
      final phase = progress * 2 * pi + i * 0.4;
      final normalized = (sin(phase) + 1) / 2; // 0..1

      // Ú©Ù…ÛŒ Ù…Ø­Ø¯ÙˆØ¯Ø´ Ù…ÛŒâ€ŒÚ©Ù†ÛŒÙ… Ú©Ù‡ Ø®ÛŒÙ„ÛŒ Ø¯ÛŒÙˆÙˆÙ†Ù‡ Ø¨Ø§Ù„Ø§ Ù¾Ø§ÛŒÛŒÙ† Ù†Ø±Ù‡
      final barHeight = 10 + normalized * maxBarHeight;

      final left = i * (barWidth + spacing);
      final right = left + barWidth;

      // Ù…ÛŒÙ„Ù‡ Ø§Ø² ÙˆØ³Ø· Ø¨Ù‡ Ø¨Ø§Ù„Ø§ Ùˆ Ù¾Ø§ÛŒÛŒÙ†
      final top = midY - barHeight / 2;
      final bottom = midY + barHeight / 2;

      final rrect = RRect.fromLTRBR(
        left,
        top,
        right,
        bottom,
        const Radius.circular(8),
      );

      canvas.drawRRect(rrect, paint);
    }
  }

  @override
  bool shouldRepaint(covariant _BarsPainter oldDelegate) {
    return oldDelegate.progress != progress ||
        oldDelegate.color != color ||
        oldDelegate.bars != bars;
  }
}
