import 'dart:math';
import 'package:flutter/material.dart';

/// یک ویژوالایزر ساده شبیه ساند بارهای کلاسیک
/// وقتی isActive = true باشه انیمیشن میله‌ها تکرار میشه
class OrganicPulseVisualizer extends StatefulWidget {
  final double width;
  final double height;
  final Color barColor;
  final int bars; // تعداد میله‌ها
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
      // یک موج سینوسی نرم برای هر میله با اختلاف فاز
      final phase = progress * 2 * pi + i * 0.4;
      final normalized = (sin(phase) + 1) / 2; // 0..1

      // کمی محدودش می‌کنیم که خیلی دیوونه بالا پایین نره
      final barHeight = 10 + normalized * maxBarHeight;

      final left = i * (barWidth + spacing);
      final right = left + barWidth;

      // میله از وسط به بالا و پایین
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
