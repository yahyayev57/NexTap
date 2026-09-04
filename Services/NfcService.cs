using System.Linq;
using System.Text;
using Android.Nfc;

namespace NexTap.Services;

public class NfcService : INfcService, IDisposable
{
    private readonly object _sync = new();
    private TaskCompletionSource<NfcScanResult?>? _pendingScan;
    private CancellationTokenRegistration _cancellationRegistration;

    public NfcService()
    {
        NfcTagBridge.TagDiscovered += OnTagDiscovered;
    }

    public bool IsNfcAvailable => NfcAdapter.GetDefaultAdapter(Platform.AppContext) is not null;

    public bool IsNfcEnabled => NfcAdapter.GetDefaultAdapter(Platform.AppContext)?.IsEnabled ?? false;

    public Task<NfcScanResult?> ScanCardAsync(CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            _pendingScan?.TrySetResult(null);
            _cancellationRegistration.Dispose();

            var tcs = new TaskCompletionSource<NfcScanResult?>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pendingScan = tcs;
            NfcTagBridge.ArmScan();

            if (cancellationToken.CanBeCanceled)
            {
                _cancellationRegistration = cancellationToken.Register(() =>
                {
                    lock (_sync)
                    {
                        if (ReferenceEquals(_pendingScan, tcs))
                        {
                            _pendingScan = null;
                            _cancellationRegistration.Dispose();
                            NfcTagBridge.DisarmScan();
                            tcs.TrySetResult(null);
                        }
                    }
                });
            }

            return tcs.Task;
        }
    }

    public void ArmEmulation(string cardUid, string? linkContent, Action onPresented) =>
        NfcEmulationBridge.Arm(cardUid, BuildNdefMessage(linkContent), onPresented);

    public void DisarmEmulation() => NfcEmulationBridge.Disarm();

    /// <summary>
    /// Builds the raw NDEF message bytes to serve from the Type 4 Tag NDEF
    /// file. Absolute URIs (the common "website link" card case) become a
    /// well-known URI record via the platform's own abbreviation table;
    /// anything else becomes a well-known plain-text record. Deliberately
    /// never adds an Android Application Record - that's what forces a
    /// reading phone to chase down/install a specific app instead of just
    /// following the link, which is the exact behavior this is fixing.
    /// </summary>
    private static byte[]? BuildNdefMessage(string? linkContent)
    {
        if (string.IsNullOrWhiteSpace(linkContent))
            return null;

        var content = linkContent.Trim();
        var record = TryBuildUriRecord(content) ?? BuildTextRecord(content);

        return new NdefMessage(new[] { record }).ToByteArray();
    }

    private static NdefRecord? TryBuildUriRecord(string content)
    {
        if (!Uri.TryCreate(content, UriKind.Absolute, out var uri))
            return null;

        if (uri.Scheme is not ("http" or "https" or "tel" or "mailto" or "sms" or "geo"))
            return null;

        return NdefRecord.CreateUri(content);
    }

    private static NdefRecord BuildTextRecord(string content)
    {
        // Well-known plain-text record: status byte (UTF-8, "en" length)
        // + language code + text, per the NFC Forum Text RTD.
        var lang = Encoding.ASCII.GetBytes("en");
        var text = Encoding.UTF8.GetBytes(content);
        var payload = new byte[1 + lang.Length + text.Length];
        payload[0] = (byte)lang.Length;
        Buffer.BlockCopy(lang, 0, payload, 1, lang.Length);
        Buffer.BlockCopy(text, 0, payload, 1 + lang.Length, text.Length);

        // RtdText binds as IList<byte> in this SDK, not byte[] - the
        // constructor below needs an actual array.
        return new NdefRecord(NdefRecord.TnfWellKnown, NdefRecord.RtdText.ToArray(), null, payload);
    }

    private void OnTagDiscovered(string uid, string technology, string? ndefContent)
    {
        TaskCompletionSource<NfcScanResult?>? tcs;
        lock (_sync)
        {
            tcs = _pendingScan;
            _pendingScan = null;
            _cancellationRegistration.Dispose();
            NfcTagBridge.DisarmScan();
        }

        tcs?.TrySetResult(new NfcScanResult(uid, technology, ndefContent));
    }

    public void Dispose()
    {
        NfcTagBridge.TagDiscovered -= OnTagDiscovered;
        lock (_sync)
        {
            _cancellationRegistration.Dispose();
            _pendingScan?.TrySetResult(null);
            _pendingScan = null;
            NfcTagBridge.DisarmScan();
        }
    }
}
