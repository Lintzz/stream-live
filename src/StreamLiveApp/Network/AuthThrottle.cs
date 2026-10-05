using System;
using System.Collections.Generic;

namespace StreamLiveApp
{
    /// <summary>
    /// Limite de tentativas de senha da sala, por IP. Sem ele, cada AUTH errado só ganhava um
    /// desafio novo, e quem passasse pelo portão de amigos (ou qualquer um, com a lista
    /// desligada) testava senhas na velocidade da rede.
    ///
    /// É por IP, e não por conexão, porque reconectar zeraria a contagem. Na Radmin o IP
    /// identifica a máquina, então o bloqueio não pega outro amigo.
    /// </summary>
    internal sealed class AuthThrottle
    {
        private readonly int _maxFailures;
        private readonly TimeSpan _lockout;
        private readonly Func<DateTime> _clock;
        private readonly Dictionary<string, (int Failures, DateTime LockedUntil)> _state = new();
        private readonly object _lock = new();

        public AuthThrottle(int maxFailures = 5, TimeSpan? lockout = null, Func<DateTime>? clock = null)
        {
            _maxFailures = maxFailures;
            _lockout = lockout ?? TimeSpan.FromSeconds(60);
            _clock = clock ?? (() => DateTime.UtcNow);
        }

        public bool IsLocked(string ip)
        {
            lock (_lock)
            {
                return _state.TryGetValue(ip, out var s) && s.LockedUntil > _clock();
            }
        }

        /// <summary>Conta um erro; devolve true se este erro bloqueou o IP.</summary>
        public bool RecordFailure(string ip)
        {
            lock (_lock)
            {
                var now = _clock();
                _state.TryGetValue(ip, out var s);

                // Bloqueio vencido: a contagem recomeça do zero.
                if (s.LockedUntil != default && s.LockedUntil <= now) s = default;

                s.Failures++;
                bool locked = s.Failures >= _maxFailures;
                if (locked) s.LockedUntil = now + _lockout;
                _state[ip] = s;
                return locked;
            }
        }

        public void RecordSuccess(string ip)
        {
            lock (_lock) { _state.Remove(ip); }
        }
    }
}
