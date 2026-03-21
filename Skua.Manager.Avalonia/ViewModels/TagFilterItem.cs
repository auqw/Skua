using CommunityToolkit.Mvvm.ComponentModel;

namespace Skua.Manager.Avalonia.ViewModels;

public partial class TagFilterItem : ObservableObject
{
    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private int _count;

    public TagFilterItem(string name, int count)
    {
        _name = name;
        _count = count;
    }
}
