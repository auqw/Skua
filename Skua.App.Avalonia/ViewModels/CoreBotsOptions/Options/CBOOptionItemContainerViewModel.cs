using CommunityToolkit.Mvvm.ComponentModel;
using Skua.App.Avalonia.ViewModels.Options;
using System.Collections.Generic;

namespace Skua.App.Avalonia.ViewModels.CoreBotsOptions.Options;

public class CBOOptionItemContainerViewModel : ObservableObject
{
    public CBOOptionItemContainerViewModel(string category, List<DisplayOptionItemViewModelBase> items)
    {
        Category = category;
        Items = items;
    }

    public CBOOptionItemContainerViewModel(string category, DisplayOptionItemViewModelBase item)
    {
        Category = category;
        Items = new() { item };
    }

    public string Category { get; }
    public List<DisplayOptionItemViewModelBase> Items { get; }
}