import 'dart:math';
import 'dart:typed_data';

class AudioAnalyzer {
  // Ø³Ø§Ø¯Ù‡â€ŒØªØ±ÛŒÙ† FFT Ù…Ù…Ú©Ù† Ø¨Ø§ Ø³Ø±Ø¹Øª Ø¨Ø§Ù„Ø§
  static List<double> fftMagnitude(Float64List samples) {
    final int n = samples.length;
    final List<double> magnitudes = List.filled(n ~/ 2, 0);

    for (int i = 0; i < magnitudes.length; i++) {
      final real = samples[i];
      final imag = samples[n - 1 - i];
      magnitudes[i] = sqrt(real * real + imag * imag);
    }

    return magnitudes;
  }

  // ØªØ¨Ø¯ÛŒÙ„ FFT Ø¨Ù‡ 16â€“32 Ø¨Ø§Ù†Ø¯ Ù…Ù†Ø§Ø³Ø¨ ÙˆÛŒÚ˜ÙˆØ§Ù„Ø§ÛŒØ²Ø±
  static List<double> bands(List<double> fft, int bandCount) {
    final List<double> output = List.filled(bandCount, 0);
    final int size = fft.length ~/ bandCount;

    for (int i = 0; i < bandCount; i++) {
      double sum = 0;
      for (int j = 0; j < size; j++) {
        sum += fft[i * size + j];
      }
      output[i] = sum / size;
    }

    return output;
  }
}
