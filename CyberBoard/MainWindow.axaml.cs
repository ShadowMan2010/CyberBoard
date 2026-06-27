using Avalonia.Controls;

namespace CyberBoard;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnKeyDown(Avalonia.Input.KeyEventArgs e)
    {
        base.OnKeyDown(e);
        var vm = DataContext as ViewModels.MainViewModel;
        if (vm == null) return;

        switch (e.Key)
        {
            case Avalonia.Input.Key.Z when e.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Control):
                if (e.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Shift))
                    vm.RedoCommand.Execute(null);
                else
                    vm.UndoCommand.Execute(null);
                e.Handled = true;
                break;
            case Avalonia.Input.Key.S when e.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Control):
                vm.SaveDocumentCommand.Execute(null);
                e.Handled = true;
                break;
            case Avalonia.Input.Key.N when e.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Control):
                vm.NewDocumentCommand.Execute(null);
                e.Handled = true;
                break;
            case Avalonia.Input.Key.OemPlus when e.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Control):
                vm.ZoomInCommand.Execute(null);
                e.Handled = true;
                break;
            case Avalonia.Input.Key.OemMinus when e.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Control):
                vm.ZoomOutCommand.Execute(null);
                e.Handled = true;
                break;
            case Avalonia.Input.Key.D0 when e.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Control):
                vm.ZoomToFitCommand.Execute(null);
                e.Handled = true;
                break;
        }
    }
}
