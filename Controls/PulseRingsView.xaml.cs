using Microsoft.Maui.Controls.Shapes;

namespace NexTap.Controls;

public partial class PulseRingsView : Grid
{
	private CancellationTokenSource? _cts;

	public PulseRingsView()
	{
		InitializeComponent();
	}

	/// <summary>Starts the looping pulse. Safe to call repeatedly - restarts cleanly.</summary>
	public void Start()
	{
		Stop();
		_cts = new CancellationTokenSource();

		_ = PulseLoopAsync(RingInner, delayMs: 0, _cts.Token);
		_ = PulseLoopAsync(RingMiddle, delayMs: 500, _cts.Token);
		_ = PulseLoopAsync(RingOuter, delayMs: 1000, _cts.Token);
	}

	public void Stop()
	{
		_cts?.Cancel();
		_cts = null;
	}

	private static async Task PulseLoopAsync(Ellipse ring, int delayMs, CancellationToken token)
	{
		if (delayMs > 0)
			await Task.Delay(delayMs, token).ContinueWith(_ => { }, TaskScheduler.Default);

		while (!token.IsCancellationRequested)
		{
			ring.Scale = 0.55;
			ring.Opacity = 0.6;

			// Soft ease-out mirrors a gentle radar "breath" rather than a mechanical tick.
			var scaleTask = ring.ScaleToAsync(1.35, 1600, Easing.CubicOut);
			var fadeTask = ring.FadeToAsync(0, 1600, Easing.CubicOut);
			await Task.WhenAll(scaleTask, fadeTask);

			if (token.IsCancellationRequested)
				break;
		}

		ring.Opacity = 0;
	}
}
