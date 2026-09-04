using Android.App;
using Android.Nfc.CardEmulators;
using Android.OS;
using NexTap.Services;

namespace NexTap;

/// <summary>
/// Android instantiates this itself whenever a reader selects one of our
/// registered AIDs (see apduservice.xml) - it is NOT resolved from the DI
/// container, so it talks to the rest of the app through the static
/// NfcEmulationBridge.
///
/// This service answers on two, very different, AIDs:
///
///  - F0010203040506 (custom "badge" AID): a minimal demo that echoes the
///    scanned card's raw UID back to readers that speak ISO-DEP/APDU
///    (corporate badge systems using DESFire or iCLASS SE, for example).
///    Only works against readers that check for this specific placeholder
///    AID - see the comment in apduservice.xml before shipping this to a
///    real reader.
///
///  - D2760000850101 (the NFC Forum's registered NDEF Tag Application
///    AID): implements the actual Type 4 Tag protocol - Capability
///    Container + NDEF file, selected/read the same way a physical NTAG/
///    URL sticker is. Any phone's stock NFC stack (NexTap installed or
///    not) already knows how to read this, so a "website link" card just
///    opens the link on a second phone with no app search, no install
///    prompt. This never emits an Android Application Record - AARs are
///    exactly what makes Android chase down/install a specific app
///    instead of just following the tag's content, which is the behavior
///    this exists to avoid.
///
/// A reader that only checks a raw MIFARE Classic/EM4100 UID during
/// anticollision will never get this far either way - Android doesn't
/// expose a way to spoof that layer on unrooted devices.
/// </summary>
[Service(Exported = true, Permission = "android.permission.BIND_NFC_SERVICE")]
[IntentFilter(new[] { "android.nfc.cardemulation.action.HOST_APDU_SERVICE" },
	Categories = new[] { "android.intent.category.DEFAULT" })]
[MetaData("android.nfc.cardemulation.host_apdu_service", Resource = "@xml/apduservice")]
public class NexTapHostApduService : HostApduService
{
	// Status words (ISO 7816-4).
	private static readonly byte[] StatusOk = { 0x90, 0x00 };
	private static readonly byte[] StatusFileNotFound = { 0x6A, 0x82 };
	private static readonly byte[] StatusWrongP1P2 = { 0x6A, 0x86 };
	private static readonly byte[] StatusInsNotSupported = { 0x6D, 0x00 };

	// NFC Forum-assigned AID for the Type 4 Tag NDEF Application.
	private static readonly byte[] NdefAppAid =
		{ 0xD2, 0x76, 0x00, 0x00, 0x85, 0x01, 0x01 };

	private static readonly byte[] BadgeAid =
		{ 0xF0, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06 };

	// Standard Type 4 Tag file IDs for the CC and NDEF files.
	private static readonly byte[] CapabilityContainerFileId = { 0xE1, 0x03 };
	private static readonly byte[] NdefFileId = { 0xE1, 0x04 };

	private const byte SelectIns = 0xA4;
	private const byte ReadBinaryIns = 0xB0;

	private enum Session
	{
		None,
		Badge,
		NdefApp,
	}

	private enum SelectedFile
	{
		None,
		CapabilityContainer,
		NdefFile,
	}

	private Session _session = Session.None;
	private SelectedFile _selectedFile = SelectedFile.None;

	public override byte[] ProcessCommandApdu(byte[]? commandApdu, Bundle? extras)
	{
		// Any APDU exchange at all means a reader is actively engaging us -
		// that's the "approach" moment the ApproachPage is waiting for.
		NfcEmulationBridge.NotifyPresented();

		if (commandApdu is null || commandApdu.Length < 4)
			return StatusInsNotSupported;

		var ins = commandApdu[1];

		if (ins == SelectIns)
			return HandleSelect(commandApdu);

		if (ins == ReadBinaryIns)
			return HandleReadBinary(commandApdu);

		// Unknown instruction. The old demo behavior blindly returned
		// success (plus the UID) for literally anything once the badge AID
		// was selected - keep that for badge sessions so existing reader
		// setups built against it keep working, but don't do it for the
		// NDEF app session, where an unrecognized command should fail
		// cleanly instead of corrupting a real tag read.
		if (_session == Session.Badge)
			return BuildBadgeResponse();

		return StatusInsNotSupported;
	}

	private byte[] HandleSelect(byte[] apdu)
	{
		var p1 = apdu[2];
		var p2 = apdu[3];
		var data = ExtractData(apdu);

		// Select by AID (P1=0x04, P2=0x00).
		if (p1 == 0x04 && p2 == 0x00 && data is not null)
		{
			if (data.AsSpan().SequenceEqual(NdefAppAid))
			{
				_selectedFile = SelectedFile.None;
				_session = NfcEmulationBridge.ArmedNdefMessage is null
					? Session.None
					: Session.NdefApp;
				return _session == Session.NdefApp ? StatusOk : StatusFileNotFound;
			}

			if (data.AsSpan().SequenceEqual(BadgeAid))
			{
				_selectedFile = SelectedFile.None;
				_session = Session.Badge;
				return StatusOk;
			}

			return StatusFileNotFound;
		}

		// Select by file ID (P1=0x00, P2=0x0C) - only meaningful once the
		// NDEF Tag Application itself has been selected.
		if (p1 == 0x00 && p2 == 0x0C && data is not null && _session == Session.NdefApp)
		{
			if (data.AsSpan().SequenceEqual(CapabilityContainerFileId))
			{
				_selectedFile = SelectedFile.CapabilityContainer;
				return StatusOk;
			}

			if (data.AsSpan().SequenceEqual(NdefFileId))
			{
				_selectedFile = SelectedFile.NdefFile;
				return StatusOk;
			}

			return StatusFileNotFound;
		}

		return StatusWrongP1P2;
	}

	private byte[] HandleReadBinary(byte[] apdu)
	{
		if (_session != Session.NdefApp || _selectedFile == SelectedFile.None)
			return StatusFileNotFound;

		var ndefMessage = NfcEmulationBridge.ArmedNdefMessage;
		if (ndefMessage is null)
			return StatusFileNotFound;

		var fileBytes = _selectedFile == SelectedFile.CapabilityContainer
			? BuildCapabilityContainer(ndefMessage.Length)
			: BuildNdefFile(ndefMessage);

		// Offset is the 15-bit value packed into P1 (low 7 bits) and P2.
		var offset = ((apdu[2] & 0x7F) << 8) | apdu[3];
		var le = apdu.Length > 4 ? apdu[4] : 0;
		var length = le == 0 ? 256 : le;

		if (offset > fileBytes.Length)
			return StatusWrongP1P2;

		length = Math.Min(length, fileBytes.Length - offset);

		var response = new byte[length + StatusOk.Length];
		Buffer.BlockCopy(fileBytes, offset, response, 0, length);
		Buffer.BlockCopy(StatusOk, 0, response, length, StatusOk.Length);
		return response;
	}

	private byte[] BuildBadgeResponse()
	{
		var armedUid = NfcEmulationBridge.ArmedCardUid;
		if (string.IsNullOrEmpty(armedUid))
			return StatusOk;

		// Minimal demo response: echo the stored UID's bytes back followed by
		// the success status word. Real deployments will need to shape this
		// to whatever data/format the target reader actually expects.
		var uidBytes = Convert.FromHexString(armedUid);
		var response = new byte[uidBytes.Length + StatusOk.Length];
		Buffer.BlockCopy(uidBytes, 0, response, 0, uidBytes.Length);
		Buffer.BlockCopy(StatusOk, 0, response, uidBytes.Length, StatusOk.Length);
		return response;
	}

	/// <summary>
	/// Standard 15-byte Type 4 Tag Capability Container: CCLEN, mapping
	/// version, max R-APDU/C-APDU data sizes, then the NDEF File Control
	/// TLV pointing at file E104 with read-only access.
	/// </summary>
	private static byte[] BuildCapabilityContainer(int ndefMessageLength)
	{
		var maxNdefFileSize = Math.Min(0xFFFE, ndefMessageLength + 2);

		return new byte[]
		{
			0x00, 0x0F,             // CCLEN = 15 bytes
			0x20,                   // Mapping version 2.0
			0x00, 0xF6,             // MLe: max data in a READ BINARY response
			0x00, 0xF6,             // MLc: max data in an UPDATE BINARY command
			0x04, 0x06,             // NDEF File Control TLV: tag=04, len=06
			NdefFileId[0], NdefFileId[1],
			(byte)(maxNdefFileSize >> 8), (byte)maxNdefFileSize,
			0x00,                   // Read access: allowed
			0xFF,                   // Write access: not allowed (read-only tag)
		};
	}

	/// <summary>2-byte big-endian NLEN length prefix followed by the NDEF message bytes.</summary>
	private static byte[] BuildNdefFile(byte[] ndefMessage)
	{
		var file = new byte[2 + ndefMessage.Length];
		file[0] = (byte)(ndefMessage.Length >> 8);
		file[1] = (byte)ndefMessage.Length;
		Buffer.BlockCopy(ndefMessage, 0, file, 2, ndefMessage.Length);
		return file;
	}

	private static byte[]? ExtractData(byte[] apdu)
	{
		// CLA INS P1 P2 Lc <data...> [Le] - short-form APDU, Lc at index 4.
		if (apdu.Length < 5)
			return null;

		var lc = apdu[4];
		if (lc == 0 || apdu.Length < 5 + lc)
			return null;

		var data = new byte[lc];
		Buffer.BlockCopy(apdu, 5, data, 0, lc);
		return data;
	}

	public override void OnDeactivated(DeactivationReason reason)
	{
		// Reader moved out of range or another AID was selected.
		_session = Session.None;
		_selectedFile = SelectedFile.None;
	}
}
