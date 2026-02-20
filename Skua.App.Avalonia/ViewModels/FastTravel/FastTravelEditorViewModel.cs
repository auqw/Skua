using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Skua.Core.Interfaces;
using Skua.Core.Interfaces.ViewModels;

namespace Skua.App.Avalonia.ViewModels.FastTravel;

public partial class FastTravelEditorViewModel : ObservableObject
{
    public FastTravelEditorViewModel(IMapService mapService, IRelayCommand<object> travel)
    {
        _mapService = mapService;
        _travel = new(travel);
    }

    public FastTravelEditorViewModel(IMapService mapService, IFastTravelItemViewModel fastTravel)
    {
        _mapService = mapService;
        _travel = new(
            fastTravel.DescriptionName,
            fastTravel.MapName,
            fastTravel.Cell,
            fastTravel.Pad,
            fastTravel.TravelCommand);
    }

    private readonly IMapService _mapService;

    [ObservableProperty]
    private FastTravelItemViewModel _travel;

    [RelayCommand]
    private void GetCurrent()
    {
        (Travel.MapName, Travel.Cell, Travel.Pad) = _mapService.GetCurrentLocation();
    }
}