using System;
using System.Collections.Generic;

namespace BlueLobby
{
    public sealed class AppSettings
    {
        public string GameDir { get; set; } = string.Empty;
        public string ExeName { get; set; } = string.Empty;
        public string PlayerName { get; set; } = "Player1";
        public string AppId { get; set; } = "480";
        public string FriendIp { get; set; } = string.Empty;
        public string Language { get; set; } = "english";
        public bool UnlockDlcTemplate { get; set; }
        public bool AddFirewallRule { get; set; } = true;
        public bool UseCustomBroadcast { get; set; } = true;
        public string Theme { get; set; } = "gece";
        public string UiLanguage { get; set; } = "tr";
        public bool ShowMascot { get; set; } = true;
        public List<FriendEntry> Friends { get; set; } = new();
    }

    public sealed class FriendEntry
    {
        public string Name { get; set; } = string.Empty;
        public string Ip { get; set; } = string.Empty;
        public string AddedUtc { get; set; } = string.Empty;
    }

    public sealed class PatchManifest
    {
        public string GameDir { get; set; } = string.Empty;
        public string CreatedUtc { get; set; } = string.Empty;
        public List<PatchEntry> Entries { get; set; } = new();
        public List<string> CreatedFiles { get; set; } = new();
        public List<string> CreatedDirectories { get; set; } = new();
        public List<FileBackup> TextBackups { get; set; } = new();
        public string FirewallRuleName { get; set; } = string.Empty;
        public string FirewallExePath { get; set; } = string.Empty;
    }

    public sealed class PatchEntry
    {
        public string DllPath { get; set; } = string.Empty;
        public string BackupPath { get; set; } = string.Empty;
        public bool Is64Bit { get; set; }
    }

    public sealed class FileBackup
    {
        public string OriginalPath { get; set; } = string.Empty;
        public string BackupPath { get; set; } = string.Empty;
    }

    public sealed class ApiDllTarget
    {
        public string Path { get; set; } = string.Empty;
        public bool Is64Bit { get; set; }
    }

    public sealed class RestoreResult
    {
        public int RestoredDllBackups { get; set; }
        public int RestoredTextBackups { get; set; }
        public int RemovedCreatedFiles { get; set; }
        public bool RemovedCreatedDirectories { get; set; }
        public bool FirewallRuleRemoved { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
