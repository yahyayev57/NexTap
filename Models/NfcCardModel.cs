namespace NexTap.Models;

/// <summary>
/// A single saved NFC credential in the wallet.
///
/// NOTE ON REAL-WORLD SCANNING: <see cref="Uid"/> and <see cref="Technology"/>
/// are populated from an actual tag scan (see INfcService.ScanCardAsync).
/// Re-presenting that UID to a reader via Host Card Emulation is only
/// possible on Android for ISO-DEP (Type A/B, AID-routed) targets - stock
/// Android does not allow spoofing a card's factory UID for plain
/// MIFARE Classic-style readers. See ApproachViewModel for details.
///
/// NOTE ON <see cref="NdefContent"/>: when set, emulation serves it as a
/// standard Type 4 Tag NDEF message (see NexTapHostApduService) instead of
/// echoing <see cref="Uid"/> - that's what lets a "website link" card open
/// directly on a phone that doesn't have NexTap installed.
/// </summary>
public class NfcCardModel
{
	public string Id { get; set; } = Guid.NewGuid().ToString("N");

	public string Name { get; set; } = string.Empty;

	public CardCover Cover { get; set; } = CardCover.Blue;

	/// <summary>Hex UID captured from the physical card during scanning.</summary>
	public string Uid { get; set; } = string.Empty;

	/// <summary>e.g. "IsoDep", "MifareClassic", "NfcA" - informational only.</summary>
	public string Technology { get; set; } = string.Empty;

	/// <summary>Optional NDEF data read from the physical tag, such as a URL.</summary>
	public string? NdefContent { get; set; }

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	public DateTime? LastUsedAt { get; set; }
}
