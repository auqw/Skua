using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;

namespace Skua.Core.Interfaces.ViewModels;

public interface IFastTravelItemViewModel : INotifyPropertyChanged
{
    string DescriptionName { get; }
    string MapName { get; }
    string Cell { get; }
    string Pad { get; }
    IRelayCommand<object> TravelCommand { get; }
}