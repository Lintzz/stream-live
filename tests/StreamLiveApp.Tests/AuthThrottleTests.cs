using StreamLiveApp;
using Xunit;

namespace StreamLiveApp.Tests;

/// <summary>
/// Limite de tentativas de senha por IP. Sem ele, cada AUTH errado só ganhava um desafio novo,
/// e quem passasse pelo portão de amigos testava senhas na velocidade da rede.
/// </summary>
public class AuthThrottleTests
{
    private DateTime _now = new(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc);
    private AuthThrottle NewThrottle() => new(maxFailures: 5, lockout: TimeSpan.FromSeconds(60), clock: () => _now);

    [Fact]
    public void LocksAfterTheFifthFailure()
    {
        var t = NewThrottle();
        for (int i = 0; i < 4; i++)
        {
            Assert.False(t.RecordFailure("26.10.0.5"));
            Assert.False(t.IsLocked("26.10.0.5"));
        }

        Assert.True(t.RecordFailure("26.10.0.5"));
        Assert.True(t.IsLocked("26.10.0.5"));
    }

    [Fact]
    public void LockIsPerIp()
    {
        var t = NewThrottle();
        for (int i = 0; i < 5; i++) t.RecordFailure("26.10.0.5");

        Assert.False(t.IsLocked("26.10.0.9"));
    }

    [Fact]
    public void LockExpiresAndTheCountStartsOver()
    {
        var t = NewThrottle();
        for (int i = 0; i < 5; i++) t.RecordFailure("26.10.0.5");

        _now = _now.AddSeconds(61);

        Assert.False(t.IsLocked("26.10.0.5"));
        Assert.False(t.RecordFailure("26.10.0.5")); // primeira falha de uma nova série
    }

    [Fact]
    public void SuccessClearsTheFailures()
    {
        var t = NewThrottle();
        for (int i = 0; i < 4; i++) t.RecordFailure("26.10.0.5");

        t.RecordSuccess("26.10.0.5");

        Assert.False(t.RecordFailure("26.10.0.5"));
    }
}
