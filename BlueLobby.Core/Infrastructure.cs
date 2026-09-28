using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace BlueLobby.Core
{
    public static class FileIntegrity
    {
        public static string Sha256(string path)
        {
            using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
        }

        public static bool MatchesSha256(string path, string? expected)
        {
            if (!File.Exists(path) || string.IsNullOrWhiteSpace(expected)) return false;
            try
            {
                return string.Equals(Sha256(path), expected, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        public static void CopyDurably(string source, string destination)
        {
            string temp = destination + ".bluelobby-tmp-" + Guid.NewGuid().ToString("N");
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destination) ?? Directory.GetCurrentDirectory());
                UnixFileMode? unixMode = null;
                if (!OperatingSystem.IsWindows())
                {
                    try { unixMode = File.GetUnixFileMode(source); } catch { }
                }

                using (FileStream input = new(source, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (FileStream output = new(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    input.CopyTo(output);
                    output.Flush(flushToDisk: true);
                }

                if (unixMode.HasValue)
                {
                    try { File.SetUnixFileMode(temp, unixMode.Value); } catch { }
                }

                File.Move(temp, destination, overwrite: true);
            }
            catch
            {
                TryDelete(temp);
                throw;
            }
        }

        public static void WriteBytesDurably(string path, byte[] bytes)
        {
            string temp = path + ".bluelobby-tmp-" + Guid.NewGuid().ToString("N");
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? Directory.GetCurrentDirectory());
                using (FileStream stream = new(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(flushToDisk: true);
                }
                File.Move(temp, path, overwrite: true);
            }
            catch
            {
                TryDelete(temp);
                throw;
            }
        }

        public static void WriteTextDurably(string path, string content)
        {
            WriteBytesDurably(path, new UTF8Encoding(false).GetBytes(content));
        }

        public static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch
            {
                // Temizlik best-effort; asıl hata korunur.
            }
        }
    }

    public static class PathSafety
    {
        public static string Canonicalize(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return string.Empty;
            string full = Path.GetFullPath(path.Trim());
            string root = Path.GetPathRoot(full) ?? string.Empty;
            if (full.Length > root.Length) full = full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return full;
        }

        public static bool IsInside(string root, string path)
        {
            string rootFull = Canonicalize(root);
            string pathFull = Canonicalize(path);
            if (string.IsNullOrWhiteSpace(rootFull) || string.IsNullOrWhiteSpace(pathFull)) return false;

            StringComparison comparison = OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

            if (string.Equals(rootFull, pathFull, comparison)) return true;
            string prefix = rootFull.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                ? rootFull
                : rootFull + Path.DirectorySeparatorChar;
            return pathFull.StartsWith(prefix, comparison);
        }

        public static void EnsureInside(string root, string path, string description)
        {
            if (!IsInside(root, path))
            {
                throw new InvalidDataException($"Güvenlik kontrolü başarısız: {description} oyun klasörünün dışına işaret ediyor.");
            }
        }

        public static void EnsureNoReparsePoint(string root, string path, string description)
        {
            string rootFull = Canonicalize(root);
            string current = Canonicalize(path);
            while (!string.IsNullOrWhiteSpace(current) && IsInside(rootFull, current))
            {
                try
                {
                    if (File.Exists(current) || Directory.Exists(current))
                    {
                        FileAttributes attributes = File.GetAttributes(current);
                        if ((attributes & FileAttributes.ReparsePoint) != 0)
                            throw new InvalidDataException($"Güvenlik kontrolü başarısız: {description} bir symbolic link/junction üzerinden erişiliyor.");
                    }
                }
                catch (FileNotFoundException) { }
                catch (DirectoryNotFoundException) { }

                if (string.Equals(current, rootFull, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)) break;
                string? parent = Path.GetDirectoryName(current);
                if (string.IsNullOrWhiteSpace(parent) || string.Equals(parent, current, StringComparison.Ordinal)) break;
                current = parent;
            }
        }
    }

    public enum BinaryArchitecture
    {
        Unknown = 0,
        X86 = 1,
        X64 = 2,
        Arm64 = 3,
    }

    public static class BinaryArchitectureDetector
    {
        public static BinaryArchitecture Detect(string path)
        {
            if (!File.Exists(path)) return BinaryArchitecture.Unknown;
            try
            {
                using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                byte[] head = new byte[8192];
                int read = stream.Read(head, 0, head.Length);
                return Detect(head.AsSpan(0, read));
            }
            catch
            {
                // Bilinmeyen/okunamayan binary format güvenli biçimde Unknown kabul edilir.
                return BinaryArchitecture.Unknown;
            }
        }

        public static BinaryArchitecture Detect(ReadOnlySpan<byte> head)
        {
            if (head.Length < 64) return BinaryArchitecture.Unknown;

            if (head[0] == (byte)'M' && head[1] == (byte)'Z')
            {
                int peOffset = BitConverter.ToInt32(head.Slice(0x3C, 4));
                if (peOffset < 0 || peOffset + 6 >= head.Length) return BinaryArchitecture.Unknown;
                if (head[peOffset] != (byte)'P' || head[peOffset + 1] != (byte)'E' || head[peOffset + 2] != 0 || head[peOffset + 3] != 0)
                    return BinaryArchitecture.Unknown;

                ushort machine = BitConverter.ToUInt16(head.Slice(peOffset + 4, 2));
                return machine switch
                {
                    0x014c => BinaryArchitecture.X86,
                    0x8664 => BinaryArchitecture.X64,
                    0xAA64 => BinaryArchitecture.Arm64,
                    _ => BinaryArchitecture.Unknown,
                };
            }

            if (head[0] == 0x7F && head[1] == (byte)'E' && head[2] == (byte)'L' && head[3] == (byte)'F')
            {
                byte elfClass = head[4];
                byte dataEncoding = head[5];
                if (elfClass is not (1 or 2) || dataEncoding != 1) return BinaryArchitecture.Unknown;

                ushort machine = BitConverter.ToUInt16(head.Slice(18, 2));
                return machine switch
                {
                    0x03 => BinaryArchitecture.X86,
                    0x3E => BinaryArchitecture.X64,
                    0xB7 => BinaryArchitecture.Arm64,
                    _ => BinaryArchitecture.Unknown,
                };
            }

            return BinaryArchitecture.Unknown;
        }

        public static bool Matches(BinaryArchitecture actual, BinaryArchitecture expected)
        {
            return expected == BinaryArchitecture.Unknown || actual == expected;
        }
    }
}
