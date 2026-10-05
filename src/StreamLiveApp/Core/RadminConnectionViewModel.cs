using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace StreamLiveApp
{
    /// <summary>
    /// Aba "Radmin VPN" das configurações. Vive na thread de UI (os awaits voltam para ela); o
    /// trabalho pesado — UI Automation, serviço, adaptador — é do <see cref="IRadminController"/>,
    /// que roda fora dela.
    ///
    /// A senha nunca vira propriedade: o PasswordBox não faz binding de propósito, e ela só
    /// passa por aqui como parâmetro do <see cref="ConnectAsync"/>.
    /// </summary>
    public sealed class RadminConnectionViewModel : INotifyPropertyChanged
    {
        private readonly IRadminController _controller;
        private readonly RadminCredentialStore _store;
        private readonly CancellationTokenSource _lifetime = new();

        /// <summary>
        /// Pergunta antes de desligar a VPN no meio de uma live (a sua ou a que você assiste):
        /// derrubaria todo mundo. Devolve false para cancelar.
        /// </summary>
        public Func<bool> ConfirmDisconnect { get; set; } = () => true;

        /// <summary>Aviso fora da aba (rodapé da janela), para quando ela está fechada.</summary>
        public event Action<string>? AttentionNeeded;

        public event PropertyChangedEventHandler? PropertyChanged;

        public RadminConnectionViewModel(IRadminController controller, RadminCredentialStore store)
        {
            _controller = controller;
            _store = store;
        }

        // ---------------------------------------------------------------- estado

        private RadminStatus _status = new(RadminState.Checking);
        public RadminStatus Status
        {
            get => _status;
            private set
            {
                if (!SetProperty(ref _status, value)) return;
                RaiseDerived();
            }
        }

        public RadminState State => Status.State;

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (!SetProperty(ref _isBusy, value)) return;
                RaiseDerived();
            }
        }

        private string _busyText = "";
        /// <summary>O que está acontecendo agora ("Conectando…"), mostrado no lugar do status.</summary>
        public string BusyText { get => _busyText; private set => SetProperty(ref _busyText, value); }

        public string StatusText => IsBusy ? BusyText : State switch
        {
            RadminState.Checking => "Verificando o Radmin…",
            RadminState.NotInstalled => "Radmin VPN não encontrado",
            RadminState.ServiceStopped => "Serviço parado",
            RadminState.Off => "Desligado",
            RadminState.On => Status.Address != null ? $"Ligado · {Status.Address}" : "Ligado",
            RadminState.Connected => Status.Address != null ? $"Conectado · {Status.Address}" : "Conectado",
            _ => "Erro"
        };

        /// <summary>Linha de apoio do status: o que fazer, ou em que rede você está.</summary>
        public string StatusDetail => IsBusy ? "" : State switch
        {
            RadminState.NotInstalled => RadminController.DescribeState(RadminState.NotInstalled),
            RadminState.ServiceStopped => RadminController.DescribeState(RadminState.ServiceStopped),
            RadminState.Off => "Clique em Conectar para ligar o Radmin e entrar na rede.",
            RadminState.Connected when Status.Network != null => $"Você está na rede {Status.Network}.",
            RadminState.On => "O Radmin está ligado. Conecte para conferir se você está na rede.",
            _ => ""
        };

        public bool ShowInstallLink => State == RadminState.NotInstalled && !IsBusy;
        public bool ShowStartService => State == RadminState.ServiceStopped && !IsBusy;
        public bool CanEdit => !IsBusy && State is not (RadminState.NotInstalled or RadminState.ServiceStopped);
        public bool CanConnect => CanEdit && State != RadminState.Checking;
        public bool CanDisconnect => !IsBusy && State is RadminState.On or RadminState.Connected;
        public string ConnectButtonText => IsBusy && _busyKind == BusyKind.Connect ? "Conectando…" : "Conectar";

        private string _message = "";
        /// <summary>Resultado da última ação (sucesso ou erro). Erro fica até a próxima ação.</summary>
        public string Message { get => _message; private set => SetProperty(ref _message, value); }

        private bool _messageIsError;
        public bool MessageIsError { get => _messageIsError; private set => SetProperty(ref _messageIsError, value); }

        // ---------------------------------------------------------------- campos

        private string _networkName = "";
        public string NetworkName
        {
            get => _networkName;
            set
            {
                if (!SetProperty(ref _networkName, value ?? "")) return;
                // O erro some assim que o campo fica válido, já durante a digitação.
                if (NetworkNameError.Length > 0 && _networkName.Trim().Length > 0) NetworkNameError = "";
            }
        }

        private string _networkNameError = "";
        public string NetworkNameError { get => _networkNameError; private set => SetProperty(ref _networkNameError, value); }

        private string _passwordError = "";
        public string PasswordError { get => _passwordError; private set => SetProperty(ref _passwordError, value); }

        private bool _rememberNetwork;
        /// <summary>
        /// Desmarcar apaga na hora o que estava salvo. Marcar só grava depois de uma conexão
        /// que deu certo — não faz sentido lembrar uma senha que o Radmin recusou.
        /// </summary>
        public bool RememberNetwork
        {
            get => _rememberNetwork;
            set
            {
                if (!SetProperty(ref _rememberNetwork, value) || _loading) return;
                if (!value && _store.HasSaved)
                {
                    _store.Forget();
                    ShowMessage("Rede esquecida. Nome e senha foram apagados deste computador.", isError: false);
                }
            }
        }

        private bool _loading;

        /// <summary>Validação ao sair do campo (não a cada tecla).</summary>
        public void ValidateNetworkName()
        {
            NetworkNameError = NetworkName.Trim().Length == 0 ? "Digite o nome da rede, exatamente como quem a criou passou." : "";
        }

        public void PasswordChanged(bool hasPassword)
        {
            if (hasPassword) PasswordError = "";
        }

        // ---------------------------------------------------------------- ações

        private enum BusyKind { None, Connect, Disconnect, Service }
        private BusyKind _busyKind;

        /// <summary>Carrega a rede lembrada e confere o Radmin. Devolve a senha salva para o PasswordBox.</summary>
        public string? LoadSaved()
        {
            var saved = _store.Load();
            _loading = true;
            try
            {
                RememberNetwork = saved != null;
                if (saved != null) NetworkName = saved.NetworkName;
            }
            finally
            {
                _loading = false;
            }
            return saved?.Password;
        }

        public async Task RefreshAsync()
        {
            if (IsBusy) return;
            try
            {
                Status = await _controller.GetStatusAsync(NetworkName, _lifetime.Token);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                DiagnosticLog.Error("Radmin", "Verificar o Radmin falhou", ex);
                Status = new RadminStatus(RadminState.Error);
                ShowMessage(RadminController.DescribeState(RadminState.Error) + " Tente de novo em instantes.", isError: true);
            }

            if (State == RadminState.ServiceStopped)
                AttentionNeeded?.Invoke("O serviço da Radmin VPN está parado. Inicie em Configurações > Radmin VPN.");
            else if (State == RadminState.NotInstalled)
                AttentionNeeded?.Invoke("Radmin VPN não encontrado. Veja como instalar em Configurações > Radmin VPN.");
        }

        /// <returns>True quando conectou (a UI volta a ocultar a senha).</returns>
        public async Task<bool> ConnectAsync(string password)
        {
            if (IsBusy) return false;

            ValidateNetworkName();
            PasswordError = password.Length == 0 ? "Digite a senha da rede." : "";
            if (NetworkNameError.Length > 0 || PasswordError.Length > 0) return false;

            var name = NetworkName.Trim();
            Begin(BusyKind.Connect, "Conectando… o Radmin pode aparecer por um instante");
            try
            {
                var result = await _controller.ConnectAsync(name, password, _lifetime.Token);
                if (result.Status != null) Status = result.Status;
                ShowMessage(result.Message, isError: !result.Success);

                if (result.Success && RememberNetwork && !_store.Save(new RadminCredential(name, password)))
                    ShowMessage(result.Message + " Mas não foi possível lembrar a rede neste computador.", isError: true);

                return result.Success;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            finally
            {
                End();
                if (Status.State != RadminState.Connected) await RefreshAsync();
            }
        }

        public async Task DisconnectAsync()
        {
            if (IsBusy || !ConfirmDisconnect()) return;

            Begin(BusyKind.Disconnect, "Desligando o Radmin…");
            try
            {
                var result = await _controller.DisconnectAsync(_lifetime.Token);
                if (result.Status != null) Status = result.Status;
                ShowMessage(result.Message, isError: !result.Success);
            }
            catch (OperationCanceledException) { }
            finally
            {
                End();
            }
        }

        public async Task StartServiceAsync()
        {
            if (IsBusy) return;

            Begin(BusyKind.Service, "Iniciando o serviço… aceite o aviso do Windows");
            try
            {
                var result = await _controller.StartServiceAsync(_lifetime.Token);
                ShowMessage(result.Message, isError: !result.Success);
            }
            catch (OperationCanceledException) { }
            finally
            {
                End();
                await RefreshAsync();
            }
        }

        /// <summary>Fechamento: cancela o que estiver rodando e faz a limpeza escolhida.</summary>
        public void Shutdown(bool disconnectOnExit, bool closeOnExit)
        {
            _lifetime.Cancel();
            _controller.CleanupOnExit(disconnectOnExit, closeOnExit, TimeSpan.FromSeconds(3));
        }

        private void Begin(BusyKind kind, string text)
        {
            _busyKind = kind;
            BusyText = text;
            Message = "";
            IsBusy = true;
        }

        private void End()
        {
            _busyKind = BusyKind.None;
            IsBusy = false;
        }

        private void ShowMessage(string message, bool isError)
        {
            MessageIsError = isError;
            Message = message;
        }

        // ---------------------------------------------------------------- notificação

        private void RaiseDerived()
        {
            foreach (var name in new[]
            {
                nameof(State), nameof(StatusText), nameof(StatusDetail), nameof(ShowInstallLink), nameof(ShowStartService),
                nameof(CanEdit), nameof(CanConnect), nameof(CanDisconnect), nameof(ConnectButtonText)
            })
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }

        private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            return true;
        }
    }
}
