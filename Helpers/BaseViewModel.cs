using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NexTap.Helpers;

public abstract class BaseViewModel : INotifyPropertyChanged
{
	private bool _isBusy;
	public bool IsBusy
	{
		get => _isBusy;
		set => SetProperty(ref _isBusy, value);
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	protected bool SetProperty<T>(ref T backingField, T value, [CallerMemberName] string? propertyName = null)
	{
		if (EqualityComparer<T>.Default.Equals(backingField, value))
			return false;

		backingField = value;
		OnPropertyChanged(propertyName);
		return true;
	}

	protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
