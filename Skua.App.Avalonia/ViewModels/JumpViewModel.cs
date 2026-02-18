using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Skua.Core.Interfaces;
using Skua.Core.Utils;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Skua.App.Avalonia.ViewModels;

public partial class JumpViewModel : BotControlViewModelBase
{
    public JumpViewModel(IMapService mapService)
        : base("Jump")
    {
        _mapService = mapService;
        Pads = _mapService.Pads;
    }

    private readonly IMapService _mapService;
    private bool _suppressAutoJump = false;

    [ObservableProperty]
    private string _selectedCell = string.Empty;

    async partial void OnSelectedCellChanged(string value)
    {
        if (!string.IsNullOrEmpty(value) && !_suppressAutoJump)
        {
            if (!string.IsNullOrEmpty(SelectedCell) && SelectedCell == "Enter")
            {
                SelectedPad = "Spawn";
            }
            else if (string.IsNullOrEmpty(SelectedPad))
            {
                SelectedPad = "Left";
            }
            await JumpTo();
        }
    }

    [ObservableProperty]
    private string _selectedPad = string.Empty;

    [ObservableProperty]
    private RangedObservableCollection<string> _cells = new();

    public ImmutableList<string> Pads { get; }

    [RelayCommand]
    private void GetCurrent()
    {
        _suppressAutoJump = true;
        (string? cell, string? pad) = _mapService.GetCurrentCell();
        SelectedCell = cell;
        SelectedPad = pad;
        _suppressAutoJump = false;
    }

    [RelayCommand]
    private async Task JumpTo()
    {
        await Task.Run(() => _mapService.Jump(SelectedCell, SelectedPad));
    }

    [RelayCommand]
    public void UpdateCells()
    {
        Cells.ReplaceRange(_mapService.Cells);
    }
}