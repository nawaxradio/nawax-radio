using System.Collections.Generic;

namespace NawaxRadio.Api.Domain
{
    public static class ChannelStore
    {
        // Source of Truth: Backend-only
        // Keys are English (stable for routes + Song.channel)
        // Titles/Descriptions are Persian (UI for users)
        public static List<Channel> Channels { get; set; } = new List<Channel>
        {
            new Channel
            {
                Id = "1",
                Key = "main",
                Title = "رادیو اصلی",
                Name = "Main Radio",
                Description = "ترکیب هیت‌ها برای همه حال‌وهواها • ۲۴/۷",
                Emoji = "📻",
                SortOrder = 1,
                Filter = new ChannelFilter { Latest = false },
                PlaylistConfig = new PlaylistConfig { MaxSongs = 300 }
            },

            new Channel
            {
                Id = "2",
                Key = "birthday",
                Title = "تولد",
                Name = "Birthday",
                Description = "شاد و مناسب جشن تولد",
                Emoji = "🎂",
                SortOrder = 2,
                Filter = new ChannelFilter { Latest = false },
                PlaylistConfig = new PlaylistConfig { MaxSongs = 300 }
            },

            new Channel
            {
                Id = "3",
                Key = "wedding",
                Title = "عروسی",
                Name = "Wedding",
                Description = "شاد و مجلسی • مخصوص مراسم",
                Emoji = "💍",
                SortOrder = 3,
                Filter = new ChannelFilter { Latest = false },
                PlaylistConfig = new PlaylistConfig { MaxSongs = 300 }
            },

            new Channel
            {
                Id = "4",
                Key = "party",
                Title = "پارتی",
                Name = "Party",
                Description = "انرژی بالا • مهمونی • کلاب",
                Emoji = "🎉",
                SortOrder = 4,
                Filter = new ChannelFilter { Latest = false },
                PlaylistConfig = new PlaylistConfig { MaxSongs = 300 }
            },

            new Channel
            {
                Id = "5",
                Key = "rap",
                Title = "رپ و هیپ‌هاپ",
                Name = "Rap & Hip-Hop",
                Description = "رپ فارسی + هیپ‌هاپ",
                Emoji = "🎤",
                SortOrder = 5,
                Filter = new ChannelFilter { Latest = false },
                PlaylistConfig = new PlaylistConfig { MaxSongs = 300 }
            },

            new Channel
            {
                Id = "6",
                Key = "genz",
                Title = "نسل زد",
                Name = "Gen Z",
                Description = "ترندهای جدید • وایب تند و امروزی",
                Emoji = "🧃",
                SortOrder = 6,
                Filter = new ChannelFilter { Latest = false },
                PlaylistConfig = new PlaylistConfig { MaxSongs = 300 }
            },

            new Channel
            {
                Id = "7",
                Key = "60s",
                Title = "دهه شصتی",
                Name = "60s",
                Description = "نوستالژی و خاطره‌بازی",
                Emoji = "📼",
                SortOrder = 7,
                Filter = new ChannelFilter { Latest = false },
                PlaylistConfig = new PlaylistConfig { MaxSongs = 300 }
            },

            new Channel
            {
                Id = "8",
                Key = "shooti",
                Title = "شوتی",
                Name = "Shooti",
                Description = "جاده‌ای • تند • بی‌وقفه",
                Emoji = "🚗",
                SortOrder = 8,
                Filter = new ChannelFilter { Latest = false },
                PlaylistConfig = new PlaylistConfig { MaxSongs = 300 }
            },

            new Channel
            {
                Id = "9",
                Key = "motivational",
                Title = "انگیزشی",
                Name = "Motivational",
                Description = "تمرکز • انرژی • حال خوب",
                Emoji = "🔥",
                SortOrder = 9,
                Filter = new ChannelFilter { Latest = false },
                PlaylistConfig = new PlaylistConfig { MaxSongs = 300 }
            },

            new Channel
            {
                Id = "10",
                Key = "badphase",
                Title = "بدفاز",
                Name = "Bad Phase",
                Description = "غمگین • سنگین • تاریک",
                Emoji = "🖤",
                SortOrder = 10,
                Filter = new ChannelFilter { Latest = false },
                PlaylistConfig = new PlaylistConfig { MaxSongs = 300 }
            }
        };
    }
}
