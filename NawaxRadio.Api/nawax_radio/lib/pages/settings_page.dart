import 'package:flutter/material.dart';

class SettingsPage extends StatelessWidget {
  const SettingsPage({super.key});

  @override
  Widget build(BuildContext context) {
    return Directionality(
      textDirection: TextDirection.rtl, // فارسی راست‌به‌چپ
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
                  "تنظیمات",
                  style: TextStyle(
                    color: Colors.white,
                    fontSize: 32,
                    fontWeight: FontWeight.bold,
                  ),
                ),
                const SizedBox(height: 6),
                const Text(
                  "تجربه‌ی خودت در ناواکس را شخصی‌سازی کن",
                  style: TextStyle(color: Colors.white70, fontSize: 14),
                ),
                const SizedBox(height: 30),

                // ========== ACCOUNT ==========
                _sectionTitle("حساب کاربری"),

                _settingsItem(
                  icon: Icons.person,
                  title: "نام کاربری",
                  subtitle: "تنظیم یا ویرایش نام کاربری",
                  onTap: () {},
                ),

                _settingsItem(
                  icon: Icons.password,
                  title: "رمز عبور",
                  subtitle: "تغییر رمز ورود",
                  onTap: () {},
                ),

                _settingsItem(
                  icon: Icons.login,
                  title: "ورود با حساب گوگل",
                  subtitle: "ورود سریع و امن (به‌زودی)",
                  onTap: () {},
                ),

                _settingsItem(
                  icon: Icons.logout,
                  title: "خروج از حساب",
                  subtitle: "قطع اتصال و خروج از برنامه",
                  onTap: () {},
                ),

                const SizedBox(height: 20),

                // ========== GENERAL ==========
                _sectionTitle("عمومی"),

                _settingsItemDisabled(
                  icon: Icons.language,
                  title: "زبان برنامه",
                  subtitle: "فعلاً فارسی (به‌زودی چندزبانه)",
                ),

                const SizedBox(height: 20),

                // ========== LEGAL ==========
                _sectionTitle("قوانین و مقررات"),

                _settingsItem(
                  icon: Icons.balance,
                  title: "کپی‌رایت و مالکیت محتوا",
                  subtitle: "حقوق آثار موسیقی و قوانین انتشار",
                  onTap: () => _openLegalSheet(context),
                ),

                _settingsItem(
                  icon: Icons.privacy_tip,
                  title: "قوانین استفاده و حریم خصوصی",
                  subtitle: "حقوق کاربر و شرایط استفاده از ناواکس",
                  onTap: () => _openTermsSheet(context),
                ),

                const SizedBox(height: 20),

                // ========== BUSINESS ==========
                _sectionTitle("تبلیغات و همکاری با ناواکس"),

                _settingsItem(
                  icon: Icons.campaign,
                  title: "تبلیغات در ناواکس",
                  subtitle: "درخواست پخش تبلیغ صوتی و همکاری تجاری",
                  onTap: () => _openAdsSheet(context),
                ),

                _settingsItem(
                  icon: Icons.music_note,
                  title: "ارسال آهنگ برای پخش",
                  subtitle: "ویژه هنرمندان، خواننده‌ها و لیبل‌ها",
                  onTap: () => _openArtistSheet(context),
                ),

                const SizedBox(height: 20),

                // ========== CONTACT ==========
                _sectionTitle("تماس با ما"),

                _settingsItem(
                  icon: Icons.email,
                  title: "ایمیل پشتیبانی",
                  subtitle: "radio@nawax.app",
                  onTap: () {},
                ),

                _settingsItem(
                  icon: Icons.camera_alt,
                  title: "اینستاگرام",
                  subtitle: "@nawaxradio",
                  onTap: () {},
                ),

                _settingsItem(
                  icon: Icons.play_circle,
                  title: "یوتیوب",
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
          "زبان برنامه",
          style: TextStyle(color: Colors.white38, fontSize: 16),
        ),
        subtitle: Text(
          "فعلاً فارسی (به‌زودی چندزبانه)",
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
🎵 قوانین کپی‌رایت — ناواکس رادیو

تمام آهنگ‌های پخش‌شده در ناواکس متعلق به صاحبان اصلی اثر، هنرمندان، لیبل‌ها و ناشران قانونی آن‌هاست.

ناواکس هیچ‌گونه ادعای مالکیت نسبت به آثار ندارد مگر در مواردی که به‌صورت رسمی ثبت شده باشد.

آپلود یا ارسال آثار دارای حق نشر بدون مجوز صاحب اثر ممنوع است.

در صورت دریافت درخواست حذف (DMCA)، آهنگ به‌سرعت از سامانه حذف می‌شود.
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
📄 قوانین استفاده و حریم خصوصی

• ما حداقل اطلاعات ممکن را برای بهبود عملکرد برنامه جمع‌آوری می‌کنیم.  
• اطلاعات کاربران فروخته یا منتقل نمی‌شود.  
• مسئولیت آپلود یا ارسال آثار دارای حق نشر بر عهده کاربر است.  
• هرگونه استفاده غیرمجاز از برند ناواکس ممنوع می‌باشد.

با استفاده از این برنامه، شما با شرایط فوق موافقت می‌کنید.
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
📢 تبلیغات در ناواکس

امکان‌پذیر برای:
• تبلیغات صوتی بین آهنگ‌ها  
• اسپانسر برنامه‌ها  
• همکاری برندها  
• معرفی هنرمندان و آثار

درخواست همکاری:
ads@nawaxradio.com
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
🎤 ارسال آهنگ برای پخش در ناواکس

هنرمندان و خوانندگان می‌توانند آثار خود را برای بررسی ارسال کنند.

سبک‌های مورد پشتیبانی:
• پاپ  
• رپ / هیپ‌هاپ  
• الکترونیک  
• سنتی / فولکلور  
• هنرمندان مستقل  

ارسال آثار:
artists@nawaxradio.com
''', style: TextStyle(color: Colors.white70, height: 1.6)),
      ),
    );
  }
}
