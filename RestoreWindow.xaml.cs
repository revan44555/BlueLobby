using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace SteamLANControlCenter
{
    public partial class RestoreWindow : Window
    {
        public bool Confirmed { get; private set; }

        public RestoreWindow(PatchManifest manifest)
        {
            InitializeComponent();

            DateTime when = DateTime.MinValue;
            if (DateTime.TryParse(manifest.CreatedUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime parsed))
            {
                when = parsed.ToLocalTime();
            }

            TxtHeading.Text = $"Bu oyun için {manifest.Entries.Count} hedef düzenlendi. Geri alma şunları yapacak:";
            TxtDate.Text = when == DateTime.MinValue
                ? "Kayıt tarihi bilinmiyor."
                : $"📅 Düzenleme tarihi: {when:dd MMMM yyyy, HH:mm}";

            var items = new List<Item>();

            foreach (PatchEntry entry in manifest.Entries)
            {
                items.Add(new Item("🧩", $"DLL yedeği geri yüklenir: {Path.GetFileName(entry.DllPath)}"));
            }

            foreach (FileBackup backup in manifest.TextBackups)
            {
                items.Add(new Item("📄", $"Önceki metin dosyası geri yüklenir: {Path.GetFileName(backup.OriginalPath)}"));
            }

            foreach (string file in manifest.CreatedFiles)
            {
                items.Add(new Item("🗑", $"Araç dosyası silinir: {Path.GetFileName(file)}"));
            }

            foreach (string dir in manifest.CreatedDirectories)
            {
                items.Add(new Item("📁", $"Boş kalırsa klasör silinir: {Path.GetFileName(dir)}"));
            }

            if (!string.IsNullOrWhiteSpace(manifest.FirewallRuleName))
            {
                items.Add(new Item("🛡", $"Firewall kuralı kaldırılır: {manifest.FirewallRuleName}"));
            }

            if (items.Count == 0)
            {
                items.Add(new Item("ℹ", "Geri alınacak bir kayıt bulunamadı."));
            }

            LstItems.ItemsSource = items;
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            Confirmed = true;
            DialogResult = true;
            Close();
        }

        private sealed record Item(string Icon, string Text);
    }
}
