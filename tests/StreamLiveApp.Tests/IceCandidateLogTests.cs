using StreamLiveApp;
using Xunit;

namespace StreamLiveApp.Tests;

/// <summary>
/// O log não registrava candidato ICE nenhum, e por isso "o som vai e a imagem não" só se
/// diagnosticava por eliminação. O que interessa na linha crua que o outro lado manda é o
/// endereço: ele diz se o amigo anunciou o IP da VPN (26.x, o único em que dois amigos se
/// alcançam) ou só o da LAN da casa dele.
/// </summary>
public class IceCandidateLogTests
{
    [Fact]
    public void CandidatoDaVpnAparecePorInteiro()
    {
        var descricao = StreamManager.DescribeCandidateAttribute(
            "candidate:1 1 udp 2130706431 26.30.144.54 51234 typ host generation 0");

        Assert.Equal("host udp 26.30.144.54:51234", descricao);
    }

    [Fact]
    public void CandidatoDaLanTambemEDescrito()
    {
        var descricao = StreamManager.DescribeCandidateAttribute(
            "candidate:2 1 udp 2130706431 192.168.15.10 51235 typ host");

        Assert.Equal("host udp 192.168.15.10:51235", descricao);
    }

    [Fact]
    public void PrefixoDoAtributoEOpcional()
    {
        // Alguns lados mandam a linha sem o "candidate:"; o formato posicional é o mesmo.
        var descricao = StreamManager.DescribeCandidateAttribute(
            "3 1 udp 1694498815 200.1.2.3 40000 typ srflx raddr 192.168.15.10 rport 51234");

        Assert.Equal("srflx udp 200.1.2.3:40000", descricao);
    }

    [Fact]
    public void CandidatoIPv6VemEntreColchetes()
    {
        // Com todas as interfaces no ICE, os IPv6 aparecem de verdade. Sem o colchete a linha
        // terminava em ":babd:57802" e não dava para saber onde acabava o endereço.
        var descricao = StreamManager.DescribeCandidateAttribute(
            "candidate:1269947708 1 udp 2113940223 2804:1b3:a9c1:148d:e1c9:35fb:f03a:babd 57802 typ host generation 0");

        Assert.Equal("host udp [2804:1b3:a9c1:148d:e1c9:35fb:f03a:babd]:57802", descricao);
    }

    [Fact]
    public void LinhaSemTipoNaoPerdeOEndereco()
    {
        var descricao = StreamManager.DescribeCandidateAttribute("candidate:4 1 udp 100 10.0.0.7 5000");

        Assert.Equal("? udp 10.0.0.7:5000", descricao);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CandidatoVazioNaoQuebraOLog(string? atributo)
    {
        Assert.Equal("?", StreamManager.DescribeCandidateAttribute(atributo));
    }

    [Fact]
    public void LinhaCurtaDemaisVaiCruaParaOLog()
    {
        // Melhor registrar o que chegou do que engolir: se o formato mudar, o log mostra.
        Assert.Equal("candidate:5 1 udp", StreamManager.DescribeCandidateAttribute("candidate:5 1 udp"));
    }
}
