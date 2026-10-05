using StreamLiveApp.Services;
using Xunit;

namespace StreamLiveApp.Tests;

/// <summary>
/// IP digitado errado na lista de amigos fazia o amigo aparecer sempre offline, sem pista do
/// motivo. A regra é estrita de propósito: o IPAddress.TryParse aceita formas antigas como
/// "26.10" (lida como 26.0.0.10), que ninguém digita querendo dizer isso.
/// </summary>
public class FriendIpValidationTests
{
    [Theory]
    [InlineData("26.10.0.5")]
    [InlineData("192.168.0.10")]
    [InlineData("0.0.0.0")]
    [InlineData("255.255.255.255")]
    public void AcceptsDottedIpv4(string ip) => Assert.True(FriendsService.IsValidFriendIp(ip));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("26.10")]
    [InlineData("26.10.0")]
    [InlineData("26.10.0.5.1")]
    [InlineData("26.10.0.256")]
    [InlineData("26.10.0.-1")]
    [InlineData("26.10.0.05")]
    [InlineData("26.10..5")]
    [InlineData("26.10.0.5 ")]
    [InlineData("26,10,0,5")]
    public void RejectsAnythingElse(string ip) => Assert.False(FriendsService.IsValidFriendIp(ip));
}
