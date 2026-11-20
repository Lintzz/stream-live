using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StreamLiveApp.Services
{
    /// <summary>Resultado de uma sondagem de status: o amigo respondeu? está em live?</summary>
    public readonly record struct FriendStatus(bool IsOnline, bool IsStreaming)
    {
        public static readonly FriendStatus Offline = new(false, false);
    }

    /// <summary>
    /// Descobre se um amigo está online e transmitindo. Vivia dentro do MainWindow; aqui é
    /// código de rede puro, sem dependência de UI — e por isso testável isoladamente.
    /// </summary>
    public static class FriendStatusService
    {
        // Timeouts curtos de propósito: o alvo é a LAN virtual do Radmin, onde uma máquina
        // que existe responde em milissegundos. O que custa caro é o amigo offline, e é ele
        // que precisa caber dentro do intervalo de atualização.
        private static readonly TimeSpan ConnectTimeout = TimeSpan.FromMilliseconds(800);
        private static readonly TimeSpan ResponseTimeout = TimeSpan.FromMilliseconds(1200);

        public static async Task<FriendStatus> CheckAsync(string ip, int port = 8080)
        {
            if (string.IsNullOrWhiteSpace(ip)) return FriendStatus.Offline;

            try
            {
                // Uma conexão por sondagem, não duas. Antes abríamos um TcpClient só para
                // descobrir se a porta respondia e, logo depois, um ClientWebSocket para a
                // mesma porta — dois handshakes por amigo a cada 5s. O próprio handshake do
                // WebSocket já responde à pergunta "o app está de pé?": se ele completa, está.
                using var ws = new ClientWebSocket();
                var wsConnectTask = ws.ConnectAsync(new Uri($"ws://{ip}:{port}"), CancellationToken.None);
                if (await Task.WhenAny(wsConnectTask, Task.Delay(ConnectTimeout)) != wsConnectTask)
                {
                    Observe(wsConnectTask);
                    return FriendStatus.Offline;
                }

                // Uma conexão recusada (amigo offline, ou você fora da lista dele) faz o
                // ConnectAsync falhar — a task já completou, então await propaga a exceção
                // para o catch abaixo em vez de nos deixar afirmar que ele está online.
                await wsConnectTask;

                bool isStreaming = await ReadStatusAsync(ws);

                try { await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None); } catch { }

                return new FriendStatus(IsOnline: true, IsStreaming: isStreaming);
            }
            catch
            {
                return FriendStatus.Offline;
            }
        }

        /// <summary>
        /// Avisa um amigo de que este app mudou de estado, para ele sondar na hora em vez de
        /// esperar até 5 s pela sondagem periódica. Vai como STATUS_CHECK marcado: um app
        /// antigo só responde o status e ignora o resto, e um tipo novo de mensagem cairia no
        /// fluxo da live dele. Devolve se o aviso chegou; falhar não importa, porque a
        /// sondagem periódica continua valendo.
        /// </summary>
        public static async Task<bool> NotifyChangedAsync(string ip, int port = 8080)
        {
            if (string.IsNullOrWhiteSpace(ip)) return false;

            try
            {
                using var ws = new ClientWebSocket();
                var connectTask = ws.ConnectAsync(new Uri($"ws://{ip}:{port}"), CancellationToken.None);
                if (await Task.WhenAny(connectTask, Task.Delay(ConnectTimeout)) != connectTask)
                {
                    Observe(connectTask);
                    return false;
                }
                await connectTask;

                var notice = new SignalingMessage { Type = "STATUS_CHECK", Data = SignalingServer.StatusChangedNotice };
                var bytes = Encoding.UTF8.GetBytes(SignalingMessage.Serialize(notice));
                await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);

                // Espera a resposta antes de fechar: fechar logo depois do envio pode derrubar
                // a conexão antes de o outro lado ler a mensagem.
                await ReadStatusAsync(ws, sendCheck: false);

                try { await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None); } catch { }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static async Task<bool> ReadStatusAsync(ClientWebSocket ws, bool sendCheck = true)
        {
            if (sendCheck)
            {
                var checkMsg = new SignalingMessage { Type = "STATUS_CHECK" };
                var bytes = Encoding.UTF8.GetBytes(SignalingMessage.Serialize(checkMsg));
                await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
            }

            // Um host transmitindo manda outros quadros no mesmo socket; a resposta pode não vir
            // de primeira, então lemos até achar o STATUS_RESPONSE (ou estourar o tempo).
            var deadline = DateTime.UtcNow + ResponseTimeout;
            var buffer = new byte[8192];

            while (DateTime.UtcNow < deadline)
            {
                var receiveTask = ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                var remaining = deadline - DateTime.UtcNow;
                if (remaining <= TimeSpan.Zero) { Observe(receiveTask); break; }

                if (await Task.WhenAny(receiveTask, Task.Delay(remaining)) != receiveTask)
                {
                    Observe(receiveTask);
                    break;
                }

                var result = receiveTask.Result;
                if (result.MessageType != WebSocketMessageType.Text) continue;

                var responseText = Encoding.UTF8.GetString(buffer, 0, result.Count);
                var responseMsg = SignalingMessage.Deserialize(responseText);
                if (responseMsg != null && responseMsg.Type == "STATUS_RESPONSE")
                {
                    return responseMsg.Data == "STREAMING";
                }
            }

            return false;
        }

        /// <summary>
        /// Marca como observada a task que perdeu a corrida para o timeout. Sem isso o socket
        /// abortado no Dispose vira uma exceção não observada relançada pelo finalizador.
        /// </summary>
        private static void Observe(Task task)
        {
            task.ContinueWith(t => { _ = t.Exception; },
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
        }
    }
}
