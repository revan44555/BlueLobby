using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using BlueLobby.Platform;

namespace BlueLobby.Core
{
    /// <summary>
    /// UI'dan bağımsız uygulama state'i. Avalonia view şu an geriye dönük uyumluluk için
    /// code-behind kullanmaya devam ediyor; yeni ekranlar bu VM üzerinden ilerlemelidir.
    /// </summary>
    public sealed class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly GameDiscoveryService _discovery;
        private CancellationTokenSource? _discoveryCts;
        private long _discoveryGeneration;
        private string _gameDirectory = string.Empty;
        private string _executablePath = string.Empty;
        private bool _isBusy;
        private string _status = "Hazır";

        public MainViewModel(IPlatformServices platform)
        {
            _discovery = new GameDiscoveryService(platform);
            DiscoverGamesCommand = new AsyncCommand(DiscoverGamesAsync, () => !IsBusy);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public IReadOnlyList<DiscoveredGame> Games { get; private set; } = Array.Empty<DiscoveredGame>();
        public ICommand DiscoverGamesCommand { get; }

        public string GameDirectory
        {
            get => _gameDirectory;
            set => Set(ref _gameDirectory, value);
        }

        public string ExecutablePath
        {
            get => _executablePath;
            set => Set(ref _executablePath, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (Set(ref _isBusy, value) && DiscoverGamesCommand is AsyncCommand command)
                    command.RaiseCanExecuteChanged();
            }
        }

        public string Status
        {
            get => _status;
            private set => Set(ref _status, value);
        }

        public async Task DiscoverGamesAsync()
        {
            long generation = Interlocked.Increment(ref _discoveryGeneration);
            _discoveryCts?.Cancel();
            _discoveryCts?.Dispose();
            _discoveryCts = new CancellationTokenSource();
            CancellationToken ct = _discoveryCts.Token;
            IsBusy = true;
            Status = "Oyun kütüphaneleri taranıyor…";
            try
            {
                IReadOnlyList<DiscoveredGame> games = await _discovery.DiscoverAsync(ct);
                if (generation != Volatile.Read(ref _discoveryGeneration) || ct.IsCancellationRequested) return;
                Games = games;
                OnPropertyChanged(nameof(Games));
                Status = Games.Count == 0 ? "Kurulu oyun bulunamadı" : $"{Games.Count} oyun bulundu";
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // Eski taramanın sonucu yeni taramanın durumunu ezmesin.
                if (generation == Volatile.Read(ref _discoveryGeneration))
                    Status = "Tarama iptal edildi";
            }
            catch (Exception ex)
            {
                if (generation == Volatile.Read(ref _discoveryGeneration))
                    Status = $"Tarama başarısız: {ex.Message}";
            }
            finally
            {
                if (generation == Volatile.Read(ref _discoveryGeneration))
                    IsBusy = false;
            }
        }

        public void SelectGame(DiscoveredGame game)
        {
            if (game == null) throw new ArgumentNullException(nameof(game));
            GameDirectory = game.InstallPath;
        }

        private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(name);
            return true;
        }

        private void OnPropertyChanged(string? name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public void Dispose()
        {
            Interlocked.Increment(ref _discoveryGeneration);
            _discoveryCts?.Cancel();
            _discoveryCts?.Dispose();
            _discoveryCts = null;
        }
    }

    public sealed class AsyncCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool>? _canExecute;
        private bool _running;

        public AsyncCommand(Func<Task> execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => !_running && (_canExecute?.Invoke() ?? true);

        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter)) return;
            _running = true;
            RaiseCanExecuteChanged();
            try { await _execute(); }
            finally { _running = false; RaiseCanExecuteChanged(); }
        }

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
