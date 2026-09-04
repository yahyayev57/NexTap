namespace NexTap.Services;

/// <summary>
/// Bridges Android NFC intents to the platform-neutral NFC service.
/// Foreground NFC capture is armed only while the user explicitly presses
/// Scan Card, so normal NFC URL tags keep their default Android behavior.
/// </summary>
public static class NfcTagBridge
{
    private static bool _scanArmed;

    public static bool IsScanArmed => _scanArmed;

    public static event Action<string, string, string?>? TagDiscovered;
    public static event Action<bool>? ScanStateChanged;

    public static void ArmScan()
    {
        if (_scanArmed) return;
        _scanArmed = true;
        ScanStateChanged?.Invoke(true);
    }

    public static void DisarmScan()
    {
        if (!_scanArmed) return;
        _scanArmed = false;
        ScanStateChanged?.Invoke(false);
    }

    public static void RaiseTagDiscovered(string uid, string technology, string? ndefContent)
    {
        TagDiscovered?.Invoke(uid, technology, ndefContent);
        DisarmScan();
    }
}
