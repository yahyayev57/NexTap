namespace NexTap.Services;

/// <summary>
/// NexTapHostApduService (Android platform code) can't easily be resolved
/// from the DI container - Android instantiates it itself when a reader
/// selects our AID. This tiny static bridge lets NfcService register a
/// callback for "presented to a reader" that the platform service invokes.
/// </summary>
public static class NfcEmulationBridge
{
	public static string? ArmedCardUid { get; private set; }

	/// <summary>
	/// Raw NDEF message bytes to serve over the Type 4 Tag NDEF Application
	/// AID (see NexTapHostApduService). Null for plain UID/badge cards.
	/// </summary>
	public static byte[]? ArmedNdefMessage { get; private set; }

	private static Action? _onPresented;

	public static void Arm(string cardUid, byte[]? ndefMessage, Action onPresented)
	{
		ArmedCardUid = cardUid;
		ArmedNdefMessage = ndefMessage;
		_onPresented = onPresented;
	}

	public static void Disarm()
	{
		ArmedCardUid = null;
		ArmedNdefMessage = null;
		_onPresented = null;
	}

	/// <summary>Called by NexTapHostApduService when a reader selects our AID.</summary>
	public static void NotifyPresented()
	{
		_onPresented?.Invoke();
	}
}
