namespace Skua.Core.Interfaces.ViewModels;

public interface IInputDialogViewModel
{
    bool NumberOnly { get; }
    string DialogTextInput { get; }
    string DialogHint { get; }
    string TextBoxHint { get; }
}