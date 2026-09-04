namespace NexTap.Services;

public record NfcScanResult(string Uid, string Technology, string? NdefContent = null);

public interface INfcService
{
    bool IsNfcAvailable { get; }
    bool IsNfcEnabled { get; }
    Task<NfcScanResult?> ScanCardAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Arms emulation for a card. When <paramref name="linkContent"/> is set
    /// (e.g. a saved website URL), the reading device is served a standard
    /// NFC Forum Type 4 Tag NDEF message over the well-known NDEF Tag
    /// Application AID - the same thing a physical URL sticker sends - so
    /// any phone opens it directly with no NexTap install required. Cards
    /// without link content fall back to the raw UID/custom-AID badge echo.
    /// </summary>
    void ArmEmulation(string cardUid, string? linkContent, Action onPresented);
    void DisarmEmulation();
}
