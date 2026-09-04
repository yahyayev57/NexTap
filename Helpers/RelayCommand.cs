using System.Windows.Input;

namespace NexTap.Helpers;

public class RelayCommand : ICommand
{
	private readonly Action<object?> _execute;
	private readonly Func<object?, bool>? _canExecute;

	public RelayCommand(Action execute, Func<bool>? canExecute = null)
		: this(_ => execute(), canExecute is null ? null : _ => canExecute())
	{
	}

	public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
	{
		_execute = execute ?? throw new ArgumentNullException(nameof(execute));
		_canExecute = canExecute;
	}

	public event EventHandler? CanExecuteChanged;

	public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

	public void Execute(object? parameter) => _execute(parameter);

	public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

/// <summary>Async version - fires the task and guards against re-entrancy while it's running.</summary>
public class AsyncRelayCommand : ICommand
{
	private readonly Func<object?, Task> _execute;
	private readonly Func<object?, bool>? _canExecute;
	private bool _isRunning;

	public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
		: this(_ => execute(), canExecute is null ? null : _ => canExecute())
	{
	}

	public AsyncRelayCommand(Func<object?, Task> execute, Func<object?, bool>? canExecute = null)
	{
		_execute = execute ?? throw new ArgumentNullException(nameof(execute));
		_canExecute = canExecute;
	}

	public event EventHandler? CanExecuteChanged;

	public bool CanExecute(object? parameter) => !_isRunning && (_canExecute?.Invoke(parameter) ?? true);

	public async void Execute(object? parameter)
	{
		_isRunning = true;
		CanExecuteChanged?.Invoke(this, EventArgs.Empty);
		try
		{
			await _execute(parameter);
		}
		finally
		{
			_isRunning = false;
			CanExecuteChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

/// <summary>Typed convenience wrapper - avoids casting `object?` back to T in every handler.</summary>
public class AsyncRelayCommand<T> : ICommand
{
	private readonly Func<T?, Task> _execute;
	private readonly Func<T?, bool>? _canExecute;
	private bool _isRunning;

	public AsyncRelayCommand(Func<T?, Task> execute, Func<T?, bool>? canExecute = null)
	{
		_execute = execute ?? throw new ArgumentNullException(nameof(execute));
		_canExecute = canExecute;
	}

	public event EventHandler? CanExecuteChanged;

	public bool CanExecute(object? parameter) => !_isRunning && (_canExecute?.Invoke((T?)parameter) ?? true);

	public async void Execute(object? parameter)
	{
		_isRunning = true;
		CanExecuteChanged?.Invoke(this, EventArgs.Empty);
		try
		{
			await _execute((T?)parameter);
		}
		finally
		{
			_isRunning = false;
			CanExecuteChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
