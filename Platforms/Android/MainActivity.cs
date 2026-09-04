using System.Text;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Nfc;
using Android.Nfc.Tech;
using Android.OS;
using NexTap.Services;

namespace NexTap;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
        ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    private NfcAdapter? _nfcAdapter;
    private PendingIntent? _pendingIntent;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        _nfcAdapter = NfcAdapter.GetDefaultAdapter(this);

        var intent = new Intent(this, GetType()).AddFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);
        var flags = OperatingSystem.IsAndroidVersionAtLeast(31)
            ? PendingIntentFlags.Mutable | PendingIntentFlags.UpdateCurrent
            : PendingIntentFlags.UpdateCurrent;
        _pendingIntent = PendingIntent.GetActivity(this, 0, intent, flags);
        NfcTagBridge.ScanStateChanged += OnScanStateChanged;
    }

    protected override void OnDestroy()
    {
        NfcTagBridge.ScanStateChanged -= OnScanStateChanged;
        base.OnDestroy();
    }

    protected override void OnResume()
    {
        base.OnResume();
        UpdateForegroundDispatch(NfcTagBridge.IsScanArmed);
    }

    private void OnScanStateChanged(bool armed)
    {
        RunOnUiThread(() => UpdateForegroundDispatch(armed));
    }

    private void UpdateForegroundDispatch(bool armed)
    {
        if (_nfcAdapter is null || _pendingIntent is null)
            return;

        try
        {
            if (armed && _nfcAdapter.IsEnabled)
                _nfcAdapter.EnableForegroundDispatch(this, _pendingIntent, null, null);
            else
                _nfcAdapter.DisableForegroundDispatch(this);
        }
        catch (Java.Lang.IllegalStateException) { }
    }

    protected override void OnPause()
    {
        if (_nfcAdapter is not null)
        {
            try { _nfcAdapter.DisableForegroundDispatch(this); }
            catch (Java.Lang.IllegalStateException) { }
        }
        base.OnPause();
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        HandleNfcIntent(intent);
    }

    private static void HandleNfcIntent(Intent? intent)
    {
        if (intent is null || !NfcTagBridge.IsScanArmed)
            return;

        // Foreground dispatch normally gives us ACTION_TAG_DISCOVERED, but
        // accepting all NFC discovery actions makes the app reliable for
        // NDEF and technology-specific tags too.
        var tag = GetTag(intent);
        if (tag is null)
            return;

        var uidBytes = tag.GetId();
        var uidHex = uidBytes is { Length: > 0 }
            ? Convert.ToHexString(uidBytes)
            : string.Empty;

        var techList = string.Join(", ", (tag.GetTechList() ?? Array.Empty<string>())
            .Select(t => t.Split('.').Last()));

        var ndefContent = ReadNdefContent(tag);
        NfcTagBridge.RaiseTagDiscovered(uidHex, techList, ndefContent);
    }

    private static Tag? GetTag(Intent intent)
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
            return intent.GetParcelableExtra(NfcAdapter.ExtraTag, Java.Lang.Class.FromType(typeof(Tag))) as Tag;

#pragma warning disable CA1422
        return intent.GetParcelableExtra(NfcAdapter.ExtraTag) as Tag;
#pragma warning restore CA1422
    }

    private static string? ReadNdefContent(Tag tag)
    {
        try
        {
            var ndef = Android.Nfc.Tech.Ndef.Get(tag);
            if (ndef is null)
                return null;

            ndef.Connect();
            try
            {
                var message = ndef.CachedNdefMessage;
                if (message is null)
                    return null;

                var values = message.GetRecords()
                    .Select(DecodeNdefRecord)
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .ToList();

                return values.Count == 0 ? null : string.Join("\n", values);
            }
            finally
            {
                try { ndef.Close(); } catch { }
            }
        }
        catch
        {
            // A tag can be readable for UID/technology while its NDEF area is
            // unavailable or protected. Scanning should still succeed.
            return null;
        }
    }

    private static string? DecodeNdefRecord(NdefRecord record)
    {
        try
        {
            // Android can map standard URI, Smart Poster and absolute-URI
            // records for us. This is more reliable than manually decoding
            // the URI prefix table.
            var uri = record.ToUri();
            if (uri is not null)
                return uri.ToString();

            var payload = record.GetPayload();
            if (payload is null || payload.Length == 0)
                return null;

            var tnf = record.Tnf;
            var type = record.GetTypeInfo();

            // NFC Forum well-known text record.
            if (tnf == NdefRecord.TnfWellKnown &&
                type is { Length: 1 } && type[0] == (byte)'T' &&
                payload.Length > 1)
            {
                var status = payload[0];
                var languageLength = status & 0x3F;
                var textOffset = 1 + languageLength;

                if (textOffset <= payload.Length)
                    return Encoding.UTF8.GetString(payload, textOffset, payload.Length - textOffset);
            }

            // Unknown/custom records: return a best-effort UTF-8 representation
            // rather than failing the entire NFC scan.
            return Encoding.UTF8.GetString(payload);
        }
        catch
        {
            return null;
        }
    }

}
