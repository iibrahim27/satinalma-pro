using System.Windows.Input;

namespace SatinalmaPro.ViewModels;

public sealed class RelayCommand(Action<object?> calistir, Func<object?, bool>? kosul = null) : ICommand
{
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => kosul?.Invoke(parameter) ?? true;
    public void Execute(object? parameter) => calistir(parameter);
    public void Bildir() => CommandManager.InvalidateRequerySuggested();
}
