namespace NexTap.Controls;

public partial class CheckmarkView : AbsoluteLayout
{
	public CheckmarkView()
	{
		InitializeComponent();
	}

	/// <summary>Plays the bounce-in + draw-on sequence once. Call after the page has appeared.</summary>
	public async Task PlayAsync()
	{
		Circle.Scale = 0;
		StrokeShort.ScaleX = 0;
		StrokeLong.ScaleX = 0;

		// Soft overshoot bounce for the circle - feels alive without being cartoonish.
		await Circle.ScaleToAsync(1.0, 420, Easing.SpringOut);

		// Quick flick, then a longer confident sweep - mirrors how a hand actually draws a check.
		await StrokeShort.ScaleXToAsync(1.0, 140, Easing.CubicOut);
		await StrokeLong.ScaleXToAsync(1.0, 220, Easing.CubicOut);
	}
}
