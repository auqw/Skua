using System.ComponentModel;

namespace Skua.Core.Interfaces.ViewModels;

public interface IOptionContainerViewModel : INotifyPropertyChanged
{
    string Title { get; }
    IOptionContainer Container { get; }
    List<IOptionContainerItemViewModel> Options { get; }
    IOptionContainerItemViewModel SelectedOption { get; set; }
}