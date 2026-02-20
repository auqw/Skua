using CommunityToolkit.Mvvm.ComponentModel;
using Skua.App.Avalonia.ViewModels.CoreBotsOptions.Options;
using Skua.App.Avalonia.ViewModels.Options;
using Skua.Shared.Avalonia.ViewModels.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace Skua.App.Avalonia.ViewModels.CoreBotsOptions;

public class CBOOtherOptionsViewModel : ObservableObject, IManageCBOptions
{
    public CBOOtherOptionsViewModel(List<CBOOptionItemContainerViewModel> options)
    {
        Options = options;
        DefaultValues = new();
        foreach (CBOOptionItemContainerViewModel container in Options)
            foreach (DisplayOptionItemViewModelBase option in container.Items)
                DefaultValues.Add(option.Tag, option.Value!);
    }

    public Dictionary<string, object> DefaultValues { get; }
    public List<CBOOptionItemContainerViewModel> Options { get; }

    public StringBuilder Save(StringBuilder builder)
    {
        foreach (CBOOptionItemContainerViewModel container in Options)
        {
            foreach (DisplayOptionItemViewModelBase option in container.Items)
                builder.AppendLine($"{option.Tag}: {option.Value}");
        }

        return builder;
    }

    public void SetValues(Dictionary<string, string> values)
    {
        foreach (CBOOptionItemContainerViewModel container in Options)
        {
            foreach (DisplayOptionItemViewModelBase option in container.Items)
            {
                if (values.TryGetValue(option.Tag, out string? value) && !string.IsNullOrWhiteSpace(value))
                {
                    option.Value = Convert.ChangeType(value, option.DisplayType);
                    continue;
                }
                option.Value = DefaultValues[option.Tag];
            }
        }
    }
}