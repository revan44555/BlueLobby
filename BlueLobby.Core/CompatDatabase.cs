using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace BlueLobby.Core
{
    public enum CompatStatus { Unknown = 0, Works = 1, NeedsConfig = 2, Unsupported = 3 }

    /// <summary>Madde B: oyun uyumluluk kaydı. Liste toplulukla büyür; burada tohum liste.</summary>
    public sealed class CompatEntry
    {
        public int AppId { get; set; }
        public string Name { get; set; } = string.Empty;
        public CompatStatus Status { get; set; } = CompatStatus.Unknown;
        public string Note { get; set; } = string.Empty;
        public int Votes { get; set; }
    }

    public sealed class CompatResult
    {
        public CompatStatus Status { get; set; } = CompatStatus.Unknown;
        public string Display { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
    }

    /// <summary>Gömülü tohum liste + (gelecekte) çevrimiçi güncelleme desteği.</summary>
    public sealed class CompatDatabase
    {
        // Tohum liste: bilinen LAN dostu oyunlar (appid, isim, durum, not)
        private const string SeedJson = """
        [
          { "AppId": 400,  "Name": "Garry's Mod",            "Status": 1, "Note": "LAN lobisi hazır.", "Votes": 10 },
          { "AppId": 550,  "Name": "Left 4 Dead 2",          "Status": 1, "Note": "LAN lobisi hazır.", "Votes": 10 },
          { "AppId": 240,  "Name": "Counter-Strike: Source", "Status": 1, "Note": "Konsoldan 'map' komutu ile LAN.", "Votes": 8 },
          { "AppId": 220,  "Name": "Half-Life 2",            "Status": 2, "Note": "Lobisi yok — oyunu başlatıp konsoldan 'connect <arkadaş IP>' ile katılın.", "Votes": 7 },
          { "AppId": 620,  "Name": "Portal 2",               "Status": 2, "Note": "Lobi yerine konsoldan connect gerekir.", "Votes": 6 },
          { "AppId": 105600, "Name": "Terraria",             "Status": 1, "Note": "Multiplayer > Host & Play.", "Votes": 9 },
          { "AppId": 42700, "Name": "Call of Duty: MW2 (2009)", "Status": 2, "Note": "LAN bağlantısı konsol komutu ister (iw4mp).", "Votes": 5 },
          { "AppId": 730,  "Name": "Counter-Strike 2",       "Status": 3, "Note": "Sunucu tabanlı — LAN yöntemiyle çalışmaz.", "Votes": 10 },
          { "AppId": 570,  "Name": "Dota 2",                 "Status": 3, "Note": "Tamamen Valve sunuculu — LAN yok.", "Votes": 10 },
          { "AppId": 70,   "Name": "Half-Life",              "Status": 1, "Note": "Klasik LAN desteği.", "Votes": 5 }
        ]
        """;

        private readonly Dictionary<int, CompatEntry> _entries = new();

        public CompatDatabase()
        {
            LoadFromJson(SeedJson);
        }

        /// <summary>JSON metinle listeyi yeniden yükler (çevrimiçi güncelleme buraya bağlanacak).</summary>
        public void LoadFromJson(string json)
        {
            _entries.Clear();
            try
            {
                foreach (CompatEntry e in JsonSerializer.Deserialize<List<CompatEntry>>(json) ?? new List<CompatEntry>())
                {
                    _entries[e.AppId] = e;
                }
            }
            catch
            {
                // Bozuk yük: tohum listeyi koru.
            }
        }

        public CompatResult Lookup(int appId)
        {
            if (!_entries.TryGetValue(appId, out CompatEntry? e))
            {
                return new CompatResult
                {
                    Status = CompatStatus.Unknown,
                    Display = "❓ Bu oyun listede yok — deneyebilirsiniz, sonucu paylaşın.",
                };
            }

            return new CompatResult
            {
                Status = e.Status,
                Display = e.Status switch
                {
                    CompatStatus.Works => "✅ LAN destekliyor",
                    CompatStatus.NeedsConfig => "⚠️ Ek adım gerekiyor",
                    CompatStatus.Unsupported => "✗ Bu yöntemle çalışmaz",
                    _ => "❓ Bilinmiyor",
                },
                Note = e.Note,
            };
        }

        public IReadOnlyList<CompatEntry> All() => _entries.Values.OrderByDescending(e => e.Votes).ToList();
    }
}
