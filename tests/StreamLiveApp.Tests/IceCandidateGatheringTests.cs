using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using StreamLiveApp;
using Xunit;

namespace StreamLiveApp.Tests;

/// <summary>
/// O app nasceu com <c>new RTCPeerConnection(null)</c>, e com configuração nula o SIPSorcery
/// anuncia como candidato ICE só os endereços da placa que o Windows usa para sair à internet.
/// Numa máquina com Radmin VPN isso é a Ethernet de casa — justamente o único endereço que o
/// amigo do outro lado não alcança. O 26.x da VPN, que é por onde a live realmente passa, nunca
/// entrava na lista, e a conexão só fechava quando o outro lado, por sorte, anunciava o dele:
/// funcionava com uns amigos, morria nos 16s do FAILED_TIMEOUT_PERIOD com outros.
///
/// Este teste trava a correção no lugar onde ela importa — os candidatos que o
/// <see cref="StreamManager"/> de fato põe na sinalização.
/// </summary>
public class IceCandidateGatheringTests
{
    private static readonly TimeSpan GatheringTimeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task CandidatosCobremTodasAsPlacasDaMaquina()
    {
        var esperados = LocalIPv4Addresses();
        Assert.NotEmpty(esperados); // sem rede não há o que testar

        var anunciados = await GatherAdvertisedAddressesAsync();

        // Em CI, com uma placa só, isto é trivialmente verdade. Numa máquina com VPN é o teste
        // inteiro: antes da correção só o endereço da rota padrão aparecia aqui.
        var faltando = esperados.Except(anunciados).ToList();
        Assert.True(
            faltando.Count == 0,
            $"placas sem candidato ICE: {string.Join(", ", faltando)} | anunciados: {string.Join(", ", anunciados)}");
    }

    /// <summary>
    /// Roda um <see cref="StreamManager"/> de viewer de verdade e recolhe os endereços dos
    /// candidatos que ele manda pela sinalização — o mesmo caminho que leva ao amigo.
    /// </summary>
    private static async Task<List<string>> GatherAdvertisedAddressesAsync()
    {
        using var manager = new StreamManager();
        var anunciados = new List<string>();

        manager.OnLocalSdpReady += (_, json) =>
        {
            var msg = SignalingMessage.Deserialize(json);
            if (msg?.Type != "ice" || string.IsNullOrEmpty(msg.Data)) return;

            // O Data é o JSON do candidato; dentro dele o atributo SDP traz o endereço na
            // quinta posição — o mesmo campo que o DescribeCandidateAttribute lê para o log.
            var atributo = ExtractCandidateAttribute(msg.Data);
            var campos = atributo?.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (campos is not { Length: >= 6 }) return;

            var endereco = campos[4];
            lock (anunciados) { if (!anunciados.Contains(endereco)) anunciados.Add(endereco); }
        };

        await manager.CreatePeerConnection("host");

        // A coleta é assíncrona: o RtpIceChannel emite os candidatos numa tarefa própria. Sai
        // cedo quando todo IPv4 da máquina já apareceu — contar candidatos não serve, porque
        // com todas as interfaces ligadas vêm também os IPv6 de cada uma.
        var esperados = LocalIPv4Addresses();
        var limite = DateTime.UtcNow + GatheringTimeout;
        while (DateTime.UtcNow < limite)
        {
            lock (anunciados) { if (!esperados.Except(anunciados).Any()) break; }
            await Task.Delay(100);
        }

        manager.Stop();
        lock (anunciados) { return anunciados.ToList(); }
    }

    private static string? ExtractCandidateAttribute(string candidateJson)
    {
        using var doc = System.Text.Json.JsonDocument.Parse(candidateJson);
        return doc.RootElement.TryGetProperty("candidate", out var attr) ? attr.GetString() : null;
    }

    private static List<string> LocalIPv4Addresses()
    {
        var enderecos = new List<string>();
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.OperationalStatus != OperationalStatus.Up) continue;
            if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

            foreach (var unicast in nic.GetIPProperties().UnicastAddresses)
            {
                if (unicast.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                if (IPAddress.IsLoopback(unicast.Address)) continue;
                enderecos.Add(unicast.Address.ToString());
            }
        }
        return enderecos;
    }
}
