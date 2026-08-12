namespace ExamGuard.App.Services;

/// <summary>
/// Simple brute-force deterrent: after a number of failed password attempts the
/// dialog refuses input for a cooldown period.
/// </summary>
public sealed class LockoutGuard
{
    private readonly int _maxAttempts;
    private readonly int _cooldownSeconds;
    private int _failures;
    private DateTime _lockedUntil = DateTime.MinValue;

    public LockoutGuard(int maxAttempts = 3, int cooldownSeconds = 30)
    {
        _maxAttempts = maxAttempts;
        _cooldownSeconds = cooldownSeconds;
    }

    public bool IsLocked => DateTime.UtcNow < _lockedUntil;

    public int RemainingSeconds => IsLocked ? (int)(_lockedUntil - DateTime.UtcNow).TotalSeconds + 1 : 0;

    public void RegisterFailure()
    {
        _failures++;
        if (_failures >= _maxAttempts)
        {
            _lockedUntil = DateTime.UtcNow.AddSeconds(_cooldownSeconds);
            _failures = 0;
        }
    }

    public void Reset() => _failures = 0;
}
