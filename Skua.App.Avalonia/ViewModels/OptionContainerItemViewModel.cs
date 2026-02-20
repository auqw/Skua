using CommunityToolkit.Mvvm.ComponentModel;
using Skua.Core.Interfaces;
using Skua.Core.Interfaces.ViewModels;
using Skua.Core.Options;
using System;
using System.Collections.Generic;

namespace Skua.App.Avalonia.ViewModels;

public partial class OptionContainerItemViewModel : ObservableObject, IOptionContainerItemViewModel
{
    public OptionContainerItemViewModel(IOptionContainer container, IOption option)
    {
        Container = container;
        Option = option;
        Type = option.Type;
        if (Type.IsEnum)
        {
            string[] enumNames = Enum.GetNames(Type);
            EnumValues = new List<string>(enumNames.Length);
            foreach (string name in enumNames)
                EnumValues.Add(name.Replace('_', ' '));
            SelectedValue = GetValue().ToString()!.Replace('_', ' ');
            return;
        }
        _value = GetValue();
        Category = option.Category;
    }

    [ObservableProperty]
    private object _value;

    [ObservableProperty]
    private List<string>? _enumValues;

    [ObservableProperty]
    private string? _selectedValue;

    public IOptionContainer Container { get; }
    public IOption Option { get; }
    public Type Type { get; }
    public string Category { get; }

    private object GetValue()
    {
        object value = typeof(OptionContainer).GetMethod("Get", new Type[] { typeof(IOption) })?
                .MakeGenericMethod(new Type[] { Option.Type })
                .Invoke(Container, new object[] { Option }) ?? string.Empty;
        return value;
    }
}