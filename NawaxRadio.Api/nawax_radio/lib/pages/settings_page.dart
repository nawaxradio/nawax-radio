import 'package:flutter/material.dart';

class SettingsPage extends StatelessWidget {
  const SettingsPage({super.key});

  @override
  Widget build(BuildContext context) {
    return Directionality(
      textDirection: TextDirection.rtl, // ÙØ§Ø±Ø³ÛŒ Ø±Ø§Ø³Øªâ€ŒØ¨Ù‡â€ŒÚ†Ù¾
      child: Scaffold(
        body: Container(
          decoration: const BoxDecoration(
            gradient: LinearGradient(
              colors: [Color(0xFF000000), Color(0xFF120804), Color(0xFFFF481F)],
              begin: Alignment.topCenter,
              end: Alignment.bottomCenter,
            ),
          ),
          child: SafeArea(
            child: ListView(
              padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 12),
              children: [
                const SizedBox(height: 10),

                // ----------- HEADER ------------
                const Text(
                  "ØªÙ†Ø¸ÛŒÙ…Ø§Øª",
                  style: TextStyle(
                    color: Colors.white,
                    fontSize: 32,
                    fontWeight: FontWeight.bold,
                  ),
                ),
                const SizedBox(height: 6),
                const Text(
                  "ØªØ¬Ø±Ø¨Ù‡â€ŒÛŒ Ø®ÙˆØ¯Øª Ø¯Ø± Ù†Ø§ÙˆØ§Ú©Ø³ Ø±Ø§ Ø´Ø®ØµÛŒâ€ŒØ³Ø§Ø²ÛŒ Ú©Ù†",
                  style: TextStyle(color: Colors.white70, fontSize: 14),
                ),
                const SizedBox(height: 30),

                // ========== ACCOUNT ==========
                _sectionTitle("Ø­Ø³Ø§Ø¨ Ú©Ø§Ø±Ø¨Ø±ÛŒ"),

                _settingsItem(
                  icon: Icons.person,
                  title: "Ù†Ø§Ù… Ú©Ø§Ø±Ø¨Ø±ÛŒ",
                  subtitle: "ØªÙ†Ø¸ÛŒÙ… ÛŒØ§ ÙˆÛŒØ±Ø§ÛŒØ´ Ù†Ø§Ù… Ú©Ø§Ø±Ø¨Ø±ÛŒ",
                  onTap: () {},
                ),

                _settingsItem(
                  icon: Icons.password,
                  title: "Ø±Ù…Ø² Ø¹Ø¨ÙˆØ±",
                  subtitle: "ØªØºÛŒÛŒØ± Ø±Ù…Ø² ÙˆØ±ÙˆØ¯",
                  onTap: () {},
                ),

                _settingsItem(
                  icon: Icons.login,
                  title: "ÙˆØ±ÙˆØ¯ Ø¨Ø§ Ø­Ø³Ø§Ø¨ Ú¯ÙˆÚ¯Ù„",
                  subtitle: "ÙˆØ±ÙˆØ¯ Ø³Ø±ÛŒØ¹ Ùˆ Ø§Ù…Ù† (Ø¨Ù‡â€ŒØ²ÙˆØ¯ÛŒ)",
                  onTap: () {},
                ),

                _settingsItem(
                  icon: Icons.logout,
                  title: "Ø®Ø±ÙˆØ¬ Ø§Ø² Ø­Ø³Ø§Ø¨",
                  subtitle: "Ù‚Ø·Ø¹ Ø§ØªØµØ§Ù„ Ùˆ Ø®Ø±ÙˆØ¬ Ø§Ø² Ø¨Ø±Ù†Ø§Ù…Ù‡",
                  onTap: () {},
                ),

                const SizedBox(height: 20),

                // ========== GENERAL ==========
                _sectionTitle("Ø¹Ù…ÙˆÙ…ÛŒ"),

                _settingsItemDisabled(
                  icon: Icons.language,
                  title: "Ø²Ø¨Ø§Ù† Ø¨Ø±Ù†Ø§Ù…Ù‡",
                  subtitle: "ÙØ¹Ù„Ø§Ù‹ ÙØ§Ø±Ø³ÛŒ (Ø¨Ù‡â€ŒØ²ÙˆØ¯ÛŒ Ú†Ù†Ø¯Ø²Ø¨Ø§Ù†Ù‡)",
                ),

                const SizedBox(height: 20),

                // ========== LEGAL ==========
                _sectionTitle("Ù‚ÙˆØ§Ù†ÛŒÙ† Ùˆ Ù…Ù‚Ø±Ø±Ø§Øª"),

                _settingsItem(
                  icon: Icons.balance,
                  title: "Ú©Ù¾ÛŒâ€ŒØ±Ø§ÛŒØª Ùˆ Ù…Ø§Ù„Ú©ÛŒØª Ù…Ø­ØªÙˆØ§",
                  subtitle: "Ø­Ù‚ÙˆÙ‚ Ø¢Ø«Ø§Ø± Ù…ÙˆØ³ÛŒÙ‚ÛŒ Ùˆ Ù‚ÙˆØ§Ù†ÛŒÙ† Ø§Ù†ØªØ´Ø§Ø±",
                  onTap: () => _openLegalSheet(context),
                ),

                _settingsItem(
                  icon: Icons.privacy_tip,
                  title: "Ù‚ÙˆØ§Ù†ÛŒÙ† Ø§Ø³ØªÙØ§Ø¯Ù‡ Ùˆ Ø­Ø±ÛŒÙ… Ø®ØµÙˆØµÛŒ",
                  subtitle: "Ø­Ù‚ÙˆÙ‚ Ú©Ø§Ø±Ø¨Ø± Ùˆ Ø´Ø±Ø§ÛŒØ· Ø§Ø³ØªÙØ§Ø¯Ù‡ Ø§Ø² Ù†Ø§ÙˆØ§Ú©Ø³",
                  onTap: () => _openTermsSheet(context),
                ),

                const SizedBox(height: 20),

                // ========== BUSINESS ==========
                _sectionTitle("ØªØ¨Ù„ÛŒØºØ§Øª Ùˆ Ù‡Ù…Ú©Ø§Ø±ÛŒ Ø¨Ø§ Ù†Ø§ÙˆØ§Ú©Ø³"),

                _settingsItem(
                  icon: Icons.campaign,
                  title: "ØªØ¨Ù„ÛŒØºØ§Øª Ø¯Ø± Ù†Ø§ÙˆØ§Ú©Ø³",
                  subtitle: "Ø¯Ø±Ø®ÙˆØ§Ø³Øª Ù¾Ø®Ø´ ØªØ¨Ù„ÛŒØº ØµÙˆØªÛŒ Ùˆ Ù‡Ù…Ú©Ø§Ø±ÛŒ ØªØ¬Ø§Ø±ÛŒ",
                  onTap: () => _openAdsSheet(context),
                ),

                _settingsItem(
                  icon: Icons.music_note,
                  title: "Ø§Ø±Ø³Ø§Ù„ Ø¢Ù‡Ù†Ú¯ Ø¨Ø±Ø§ÛŒ Ù¾Ø®Ø´",
                  subtitle: "ÙˆÛŒÚ˜Ù‡ Ù‡Ù†Ø±Ù…Ù†Ø¯Ø§Ù†ØŒ Ø®ÙˆØ§Ù†Ù†Ø¯Ù‡â€ŒÙ‡Ø§ Ùˆ Ù„ÛŒØ¨Ù„â€ŒÙ‡Ø§",
                  onTap: () => _openArtistSheet(context),
                ),

                const SizedBox(height: 20),

                // ========== CONTACT ==========
                _sectionTitle("ØªÙ…Ø§Ø³ Ø¨Ø§ Ù…Ø§"),

                _settingsItem(
                  icon: Icons.email,
                  title: "Ø§ÛŒÙ…ÛŒÙ„ Ù¾Ø´ØªÛŒØ¨Ø§Ù†ÛŒ",
                  subtitle: "radio@nawax.app",
                  onTap: () {},
                ),

                _settingsItem(
                  icon: Icons.camera_alt,
                  title: "Ø§ÛŒÙ†Ø³ØªØ§Ú¯Ø±Ø§Ù…",
                  subtitle: "@nawaxradio",
                  onTap: () {},
                ),

                _settingsItem(
                  icon: Icons.play_circle,
                  title: "ÛŒÙˆØªÛŒÙˆØ¨",
                  subtitle: "Nawax Radio",
                  onTap: () {},
                ),

                const SizedBox(height: 40),
              ],
            ),
          ),
        ),
      ),
    );
  }

  // --------------------------------------------
  // ----------- UI COMPONENTS -------------------
  // --------------------------------------------

  static Widget _sectionTitle(String text) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 10),
      child: Text(
        text,
        style: const TextStyle(
          color: Colors.white70,
          fontSize: 14,
          fontWeight: FontWeight.w500,
        ),
      ),
    );
  }

  static Widget _settingsItem({
    required IconData icon,
    required String title,
    required String subtitle,
    required VoidCallback onTap,
  }) {
    return Container(
      margin: const EdgeInsets.only(bottom: 14),
      decoration: BoxDecoration(
        color: const Color(0xFF101010),
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: Colors.white12),
      ),
      child: ListTile(
        leading: Icon(icon, color: Colors.white, size: 26),
        title: Text(
          title,
          style: const TextStyle(color: Colors.white, fontSize: 16),
        ),
        subtitle: Text(
          subtitle,
          style: const TextStyle(color: Colors.white54, fontSize: 12),
        ),
        trailing: const Icon(
          Icons.arrow_back_ios,
          color: Colors.white38,
          size: 16,
        ),
        onTap: onTap,
      ),
    );
  }

  static Widget _settingsItemDisabled({
    required IconData icon,
    required String title,
    required String subtitle,
  }) {
    return Container(
      margin: const EdgeInsets.only(bottom: 14),
      decoration: BoxDecoration(
        color: const Color(0xFF1A1A1A),
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: Colors.white10),
      ),
      child: const ListTile(
        leading: Icon(Icons.language, color: Colors.white24, size: 26),
        title: Text(
          "Ø²Ø¨Ø§Ù† Ø¨Ø±Ù†Ø§Ù…Ù‡",
          style: TextStyle(color: Colors.white38, fontSize: 16),
        ),
        subtitle: Text(
          "ÙØ¹Ù„Ø§Ù‹ ÙØ§Ø±Ø³ÛŒ (Ø¨Ù‡â€ŒØ²ÙˆØ¯ÛŒ Ú†Ù†Ø¯Ø²Ø¨Ø§Ù†Ù‡)",
          style: TextStyle(color: Colors.white24, fontSize: 12),
        ),
        trailing: Icon(Icons.lock, color: Colors.white30, size: 16),
        enabled: false,
      ),
    );
  }

  // ---------------------------------------------------
  // -------------------- LEGAL -------------------------
  // ---------------------------------------------------

  void _openLegalSheet(BuildContext context) {
    showModalBottomSheet(
      context: context,
      backgroundColor: const Color(0xFF0D0D0D),
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (_) => const Padding(
        padding: EdgeInsets.all(20),
        child: SingleChildScrollView(
          child: Text('''
ðŸŽµ Ù‚ÙˆØ§Ù†ÛŒÙ† Ú©Ù¾ÛŒâ€ŒØ±Ø§ÛŒØª â€” Ù†Ø§ÙˆØ§Ú©Ø³ Ø±Ø§Ø¯ÛŒÙˆ

ØªÙ…Ø§Ù… Ø¢Ù‡Ù†Ú¯â€ŒÙ‡Ø§ÛŒ Ù¾Ø®Ø´â€ŒØ´Ø¯Ù‡ Ø¯Ø± Ù†Ø§ÙˆØ§Ú©Ø³ Ù…ØªØ¹Ù„Ù‚ Ø¨Ù‡ ØµØ§Ø­Ø¨Ø§Ù† Ø§ØµÙ„ÛŒ Ø§Ø«Ø±ØŒ Ù‡Ù†Ø±Ù…Ù†Ø¯Ø§Ù†ØŒ Ù„ÛŒØ¨Ù„â€ŒÙ‡Ø§ Ùˆ Ù†Ø§Ø´Ø±Ø§Ù† Ù‚Ø§Ù†ÙˆÙ†ÛŒ Ø¢Ù†â€ŒÙ‡Ø§Ø³Øª.

Ù†Ø§ÙˆØ§Ú©Ø³ Ù‡ÛŒÚ†â€ŒÚ¯ÙˆÙ†Ù‡ Ø§Ø¯Ø¹Ø§ÛŒ Ù…Ø§Ù„Ú©ÛŒØª Ù†Ø³Ø¨Øª Ø¨Ù‡ Ø¢Ø«Ø§Ø± Ù†Ø¯Ø§Ø±Ø¯ Ù…Ú¯Ø± Ø¯Ø± Ù…ÙˆØ§Ø±Ø¯ÛŒ Ú©Ù‡ Ø¨Ù‡â€ŒØµÙˆØ±Øª Ø±Ø³Ù…ÛŒ Ø«Ø¨Øª Ø´Ø¯Ù‡ Ø¨Ø§Ø´Ø¯.

Ø¢Ù¾Ù„ÙˆØ¯ ÛŒØ§ Ø§Ø±Ø³Ø§Ù„ Ø¢Ø«Ø§Ø± Ø¯Ø§Ø±Ø§ÛŒ Ø­Ù‚ Ù†Ø´Ø± Ø¨Ø¯ÙˆÙ† Ù…Ø¬ÙˆØ² ØµØ§Ø­Ø¨ Ø§Ø«Ø± Ù…Ù…Ù†ÙˆØ¹ Ø§Ø³Øª.

Ø¯Ø± ØµÙˆØ±Øª Ø¯Ø±ÛŒØ§ÙØª Ø¯Ø±Ø®ÙˆØ§Ø³Øª Ø­Ø°Ù (DMCA)ØŒ Ø¢Ù‡Ù†Ú¯ Ø¨Ù‡â€ŒØ³Ø±Ø¹Øª Ø§Ø² Ø³Ø§Ù…Ø§Ù†Ù‡ Ø­Ø°Ù Ù…ÛŒâ€ŒØ´ÙˆØ¯.
''', style: TextStyle(color: Colors.white70, height: 1.6)),
        ),
      ),
    );
  }

  void _openTermsSheet(BuildContext context) {
    showModalBottomSheet(
      context: context,
      backgroundColor: const Color(0xFF0D0D0D),
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (_) => const Padding(
        padding: EdgeInsets.all(20),
        child: SingleChildScrollView(
          child: Text('''
ðŸ“„ Ù‚ÙˆØ§Ù†ÛŒÙ† Ø§Ø³ØªÙØ§Ø¯Ù‡ Ùˆ Ø­Ø±ÛŒÙ… Ø®ØµÙˆØµÛŒ

â€¢ Ù…Ø§ Ø­Ø¯Ø§Ù‚Ù„ Ø§Ø·Ù„Ø§Ø¹Ø§Øª Ù…Ù…Ú©Ù† Ø±Ø§ Ø¨Ø±Ø§ÛŒ Ø¨Ù‡Ø¨ÙˆØ¯ Ø¹Ù…Ù„Ú©Ø±Ø¯ Ø¨Ø±Ù†Ø§Ù…Ù‡ Ø¬Ù…Ø¹â€ŒØ¢ÙˆØ±ÛŒ Ù…ÛŒâ€ŒÚ©Ù†ÛŒÙ….  
â€¢ Ø§Ø·Ù„Ø§Ø¹Ø§Øª Ú©Ø§Ø±Ø¨Ø±Ø§Ù† ÙØ±ÙˆØ®ØªÙ‡ ÛŒØ§ Ù…Ù†ØªÙ‚Ù„ Ù†Ù…ÛŒâ€ŒØ´ÙˆØ¯.  
â€¢ Ù…Ø³Ø¦ÙˆÙ„ÛŒØª Ø¢Ù¾Ù„ÙˆØ¯ ÛŒØ§ Ø§Ø±Ø³Ø§Ù„ Ø¢Ø«Ø§Ø± Ø¯Ø§Ø±Ø§ÛŒ Ø­Ù‚ Ù†Ø´Ø± Ø¨Ø± Ø¹Ù‡Ø¯Ù‡ Ú©Ø§Ø±Ø¨Ø± Ø§Ø³Øª.  
â€¢ Ù‡Ø±Ú¯ÙˆÙ†Ù‡ Ø§Ø³ØªÙØ§Ø¯Ù‡ ØºÛŒØ±Ù…Ø¬Ø§Ø² Ø§Ø² Ø¨Ø±Ù†Ø¯ Ù†Ø§ÙˆØ§Ú©Ø³ Ù…Ù…Ù†ÙˆØ¹ Ù…ÛŒâ€ŒØ¨Ø§Ø´Ø¯.

Ø¨Ø§ Ø§Ø³ØªÙØ§Ø¯Ù‡ Ø§Ø² Ø§ÛŒÙ† Ø¨Ø±Ù†Ø§Ù…Ù‡ØŒ Ø´Ù…Ø§ Ø¨Ø§ Ø´Ø±Ø§ÛŒØ· ÙÙˆÙ‚ Ù…ÙˆØ§ÙÙ‚Øª Ù…ÛŒâ€ŒÚ©Ù†ÛŒØ¯.
''', style: TextStyle(color: Colors.white70, height: 1.6)),
        ),
      ),
    );
  }

  // ---------------- ADS ----------------

  void _openAdsSheet(BuildContext context) {
    showModalBottomSheet(
      context: context,
      backgroundColor: const Color(0xFF0D0D0D),
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (_) => const Padding(
        padding: EdgeInsets.all(20),
        child: Text('''
ðŸ“¢ ØªØ¨Ù„ÛŒØºØ§Øª Ø¯Ø± Ù†Ø§ÙˆØ§Ú©Ø³

Ø§Ù…Ú©Ø§Ù†â€ŒÙ¾Ø°ÛŒØ± Ø¨Ø±Ø§ÛŒ:
â€¢ ØªØ¨Ù„ÛŒØºØ§Øª ØµÙˆØªÛŒ Ø¨ÛŒÙ† Ø¢Ù‡Ù†Ú¯â€ŒÙ‡Ø§  
â€¢ Ø§Ø³Ù¾Ø§Ù†Ø³Ø± Ø¨Ø±Ù†Ø§Ù…Ù‡â€ŒÙ‡Ø§  
â€¢ Ù‡Ù…Ú©Ø§Ø±ÛŒ Ø¨Ø±Ù†Ø¯Ù‡Ø§  
â€¢ Ù…Ø¹Ø±ÙÛŒ Ù‡Ù†Ø±Ù…Ù†Ø¯Ø§Ù† Ùˆ Ø¢Ø«Ø§Ø±

Ø¯Ø±Ø®ÙˆØ§Ø³Øª Ù‡Ù…Ú©Ø§Ø±ÛŒ:
ads@nawax.app
''', style: TextStyle(color: Colors.white70, height: 1.6)),
      ),
    );
  }

  // ------------- ARTISTS -------------

  void _openArtistSheet(BuildContext context) {
    showModalBottomSheet(
      context: context,
      backgroundColor: const Color(0xFF0D0D0D),
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (_) => const Padding(
        padding: EdgeInsets.all(20),
        child: Text('''
ðŸŽ¤ Ø§Ø±Ø³Ø§Ù„ Ø¢Ù‡Ù†Ú¯ Ø¨Ø±Ø§ÛŒ Ù¾Ø®Ø´ Ø¯Ø± Ù†Ø§ÙˆØ§Ú©Ø³

Ù‡Ù†Ø±Ù…Ù†Ø¯Ø§Ù† Ùˆ Ø®ÙˆØ§Ù†Ù†Ø¯Ú¯Ø§Ù† Ù…ÛŒâ€ŒØªÙˆØ§Ù†Ù†Ø¯ Ø¢Ø«Ø§Ø± Ø®ÙˆØ¯ Ø±Ø§ Ø¨Ø±Ø§ÛŒ Ø¨Ø±Ø±Ø³ÛŒ Ø§Ø±Ø³Ø§Ù„ Ú©Ù†Ù†Ø¯.

Ø³Ø¨Ú©â€ŒÙ‡Ø§ÛŒ Ù…ÙˆØ±Ø¯ Ù¾Ø´ØªÛŒØ¨Ø§Ù†ÛŒ:
â€¢ Ù¾Ø§Ù¾  
â€¢ Ø±Ù¾ / Ù‡ÛŒÙ¾â€ŒÙ‡Ø§Ù¾  
â€¢ Ø§Ù„Ú©ØªØ±ÙˆÙ†ÛŒÚ©  
â€¢ Ø³Ù†ØªÛŒ / ÙÙˆÙ„Ú©Ù„ÙˆØ±  
â€¢ Ù‡Ù†Ø±Ù…Ù†Ø¯Ø§Ù† Ù…Ø³ØªÙ‚Ù„  

Ø§Ø±Ø³Ø§Ù„ Ø¢Ø«Ø§Ø±:
artists@nawax.app
''', style: TextStyle(color: Colors.white70, height: 1.6)),
      ),
    );
  }
}
