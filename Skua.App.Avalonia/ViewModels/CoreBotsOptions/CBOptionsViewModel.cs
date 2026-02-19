using CommunityToolkit.Mvvm.ComponentModel;
using Skua.App.Avalonia.ViewModels.Options;
using Skua.Core.Interfaces;
using Skua.Shared.Avalonia.ViewModels.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace Skua.App.Avalonia.ViewModels.CoreBotsOptions;

public class CBOptionsViewModel : ObservableObject, IManageCBOptions
{
    private readonly IDialogService _dialogService;

    public CBOptionsViewModel(List<DisplayOptionItemViewModelBase> options, IDialogService dialogService)
    {
        Options = options;
        _dialogService = dialogService;
        DefaultValues = new();
        foreach (DisplayOptionItemViewModelBase option in Options)
            DefaultValues.Add(option.Tag, option.Value!);
    }

    public List<DisplayOptionItemViewModelBase> Options { get; }

    private Dictionary<string, object> DefaultValues { get; }

    public StringBuilder Save(StringBuilder builder)
    {
        foreach (DisplayOptionItemViewModelBase option in Options)
        {
            if (option.Tag == "PrivateRooms" && !(bool)option.Value && _dialogService.ShowMessageBox("Whilst we do offer the option, we highly recommend staying in private rooms while botting. Bot in public at your own risk.\r\n Confirm the use of Public Rooms?", "Public Room Warning", true) == false)
            {
                builder.AppendLine($"{option.Tag}: {true}");
                continue;
            }

            if (option.Tag == "PrivateRoomNr" && long.TryParse((string?)option.Value?.ToString(), out long room) && room > int.MaxValue)
            {
                if (_dialogService.ShowMessageBox($"Private room number cannot be greater than {int.MaxValue}. It will be reset to 100000.", "Room Number Warning", true) == false)
                {
                    continue;
                }
                option.Value = 100000;
            }
            builder.AppendLine($"{option.Tag}: {option.Value}");
        }

        return builder;
    }

    public void SetValues(Dictionary<string, string> values)
    {
        foreach (DisplayOptionItemViewModelBase option in Options)
        {
            if (values.TryGetValue(option.Tag, out string? value) && !string.IsNullOrWhiteSpace(value))
            {
                try
                {
                    option.Value = option.DisplayType == typeof(int)
                        ? long.TryParse(value, out long longValue) ? longValue is > int.MaxValue or < int.MinValue ? 100000 : (int)longValue : 100000
                        : Convert.ChangeType(value, option.DisplayType);
                }
                catch (Exception)
                {
                    option.Value = DefaultValues[option.Tag];
                }
            }
            else
            {
                option.Value = DefaultValues[option.Tag];
            }
        }
    }
}