using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace BlueLobby.Core
{
    /// <summary>
    /// Paylaşılabilir oturum profili. Token makineye özel yol içermez.
    /// </summary>
    public static class SessionProfileCodec
    {
        public const int CurrentVersion = 1;
        public const string Prefix = "BL1.";
        private const int MaxTokenChars = 32768;
        private const int MaxCompressedBytes = 24576;
        private const int MaxDecompressedBytes = 131072;

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
            string token = Prefix + Base64Url(ms.ToArray());
            if (token.Length > MaxTokenChars) throw new InvalidDataException("Profil tokenı izin verilen boyutu aşıyor.");
            return token;
        }

        public static GameProfile? Decode(string token)
        {
            try
            {
                token = token?.Trim() ?? string.Empty;
                if (token.Length == 0 || token.Length > MaxTokenChars || !token.StartsWith(Prefix, StringComparison.Ordinal))
                    return null;

                byte[] compressed = Base64UrlDecode(token.Substring(Prefix.Length));
                if (compressed.Length > MaxCompressedBytes) return null;

                using var input = new MemoryStream(compressed, writable: false);
                using var gz = new GZipStream(input, CompressionMode.Decompress);
                using var output = new MemoryStream();
                byte[] buffer = new byte[8192];
                int total = 0;
                while (true)
                {
                    int read = gz.Read(buffer, 0, buffer.Length);
                    if (read == 0) break;
                    total += read;
                    if (total > MaxDecompressedBytes) return null;
                    output.Write(buffer, 0, read);
                }

                TokenPayload? payload = JsonSerializer.Deserialize<TokenPayload>(output.ToArray());
                if (payload == null || payload.Version <= 0 || payload.Version > CurrentVersion) return null;

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
            if (text.Length > MaxTokenChars) throw new InvalidDataException("Token fazla büyük.");
            string padded = text.Replace('-', '+').Replace('_', '/');
            int padding = (4 - padded.Length % 4) % 4;
            padded = padded.PadRight(padded.Length + padding, '=');
            return Convert.FromBase64String(padded);
        }
    }
}
