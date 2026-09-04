namespace NexTap.Models;

/// <summary>Plain colors available for wallet cards.</summary>
public enum CardCover
{
	Blue = 0,
	Purple = 1,
	Emerald = 2,
	Orange = 3,
	Crimson = 4,
	Midnight = 5
}

public static class CardCoverExtensions
{
	public static string DisplayName(this CardCover cover) => cover switch
	{
		CardCover.Blue => "Blue",
		CardCover.Purple => "Purple",
		CardCover.Emerald => "Emerald",
		CardCover.Orange => "Orange",
		CardCover.Crimson => "Crimson",
		CardCover.Midnight => "Midnight",
		_ => "Blue"
	};

	public static string AccentColorKey(this CardCover cover) => cover switch
	{
		CardCover.Blue => "CardBlue",
		CardCover.Purple => "CardPurple",
		CardCover.Emerald => "CardEmerald",
		CardCover.Orange => "CardOrange",
		CardCover.Crimson => "CardCrimson",
		CardCover.Midnight => "CardMidnight",
		_ => "CardBlue"
	};
}
