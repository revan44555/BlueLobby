using System;
using System.Collections.Generic;

namespace BlueLobby.Core
{
    public sealed class AppSettings
    {
        public int SchemaVersion { get; set; } = 2;
        public string GameDir { get; set; } = string.Empty;
        public string ExeName { get; set; } = string.Empty;
        public bool WizardDone { get; set; }
        public bool TouchMode { get; set; }
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
        public override string ToString() => $"{Name} — {Ip}";
    }

    public enum PatchTransactionState
    {
        Preparing = 0,
        Committed = 1,
        Restoring = 2,
        RollingBack = 3,
        RolledBack = 4,
    }

    public sealed class PatchManifest
    {
        public int SchemaVersion { get; set; } = 1;
        public string TransactionId { get; set; } = Guid.NewGuid().ToString("N");
        public PatchTransactionState State { get; set; } = PatchTransactionState.Preparing;
        public string GameDir { get; set; } = string.Empty;
        public string ExePath { get; set; } = string.Empty;
        public string CreatedUtc { get; set; } = string.Empty;
        public string CommittedUtc { get; set; } = string.Empty;
        public List<PatchEntry> Entries { get; set; } = new();
        public List<string> CreatedFiles { get; set; } = new();
        public List<ManagedFile> CreatedFileRecords { get; set; } = new();
        public List<string> CreatedDirectories { get; set; } = new();
        public List<FileBackup> TextBackups { get; set; } = new();
        public string FirewallRuleName { get; set; } = string.Empty;
        public string FirewallExePath { get; set; } = string.Empty;
        public bool FirewallRuleCreatedByUs { get; set; }
    }

    public sealed class PatchEntry
    {
        public string DllPath { get; set; } = string.Empty;
        public string BackupPath { get; set; } = string.Empty;
        public bool Is64Bit { get; set; }
        public BinaryArchitecture Architecture { get; set; }
        public string OriginalSha256 { get; set; } = string.Empty;
        public string BackupSha256 { get; set; } = string.Empty;
        public string PatchedSha256 { get; set; } = string.Empty;
    }

    public sealed class ManagedFile
    {
        public string Path { get; set; } = string.Empty;
        public string ManagedSha256 { get; set; } = string.Empty;
    }

    public sealed class FileBackup
    {
        public string OriginalPath { get; set; } = string.Empty;
        public string BackupPath { get; set; } = string.Empty;
        public string OriginalSha256 { get; set; } = string.Empty;
        public string BackupSha256 { get; set; } = string.Empty;
        public string ManagedContentSha256 { get; set; } = string.Empty;
    }

    public sealed class ApiDllTarget
    {
        public string Path { get; set; } = string.Empty;
        public bool Is64Bit { get; set; }
        public bool IsSharedObject { get; set; }
        public BinaryArchitecture Architecture { get; set; } = BinaryArchitecture.Unknown;
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
