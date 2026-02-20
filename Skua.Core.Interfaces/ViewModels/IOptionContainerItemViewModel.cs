using System.ComponentModel;

namespace Skua.Core.Interfaces.ViewModels;

public interface IOptionContainerItemViewModel : INotifyPropertyChanged
{
    IOption Option { get; }
    Type Type { get; }
    string SelectedValue { get; }
    object Value { get; }
}