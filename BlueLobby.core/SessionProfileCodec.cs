using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace BlueLobby.Core
{
    /// <summary>
    /// Madde 5: paylaşılabilir oturum profilleri. Ayar setini sıkıştırılmış JSON'dan
    /// base64url token'a çevirir. Asla makineye özel yol içermez (GameDir taşınmaz).
    /// </summary>
    public static class SessionProfileCodec
    {
        public const int CurrentVersion = 1;
        public const string Prefix = "BL1.";

        // Token'a taşınan alanlar (yalnızca ayarlar)
        private sealed class TokenPayload
        {
            public int Version { get; set; }
            public string PlayerName { get; set; } = string.Empty;
            public string AppId { get; set; } = string.Empty;
            public string Language { get; set; } = "english";
            public bool BroadcastOn { get; set; } = true;
            public bool DlcTemplateOn { get; set; }
        }

        public static string Encode(GameProfile profile)
        {
            var payload = new TokenPayload
            {
                Version = CurrentVersion,
                PlayerName = profile.PlayerName,
                AppId = profile.AppId,
                Language = profile.Language,
                BroadcastOn = profile.BroadcastOn,
                DlcTemplateOn = profile.DlcTemplateOn,
            };

            byte[] json = JsonSerializer.SerializeToUtf8Bytes(payload);
            using var ms = new MemoryStream();
            using (var gz = new GZipStream(ms, CompressionMode.Compress, leaveOpen: true))
            {
                gz.Write(json, 0, json.Length);
            }
            return Prefix + Base64Url(ms.ToArray());
        }

        /// <summary>Token çözülür; geçersiz/sürüm uyumsuz ise null döner. Asla istisna fırlatmaz.</summary>
        public static GameProfile? Decode(string token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token) || !token.StartsWith(Prefix, StringComparison.Ordinal))
                {
                    return null;
                }

                byte[] compressed = Base64UrlDecode(token.Substring(Prefix.Length));
                using var ms = new MemoryStream(compressed);
                using var gz = new GZipStream(ms, CompressionMode.Decompress);
                using var outMs = new MemoryStream();
                gz.CopyTo(outMs);
                TokenPayload? payload = JsonSerializer.Deserialize<TokenPayload>(outMs.ToArray());
                if (payload == null || payload.Version > CurrentVersion)
                {
                    return null;
                }

                return new GameProfile
                {
                    PlayerName = payload.PlayerName ?? string.Empty,
                    AppId = payload.AppId ?? string.Empty,
                    Language = string.IsNullOrWhiteSpace(payload.Language) ? "english" : payload.Language,
                    BroadcastOn = payload.BroadcastOn,
                    DlcTemplateOn = payload.DlcTemplateOn,
                };
            }
            catch
            {
                return null;
            }
        }

        private static string Base64Url(byte[] data) =>
            Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        private static byte[] Base64UrlDecode(string text)
        {
            string padded = text.Replace('-', '+').Replace('_', '/');
            padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
            return Convert.FromBase64String(padded);
        }
    }
}
