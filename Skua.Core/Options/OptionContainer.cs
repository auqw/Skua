using CommunityToolkit.Mvvm.ComponentModel;
using Skua.Core.Interfaces;
using Skua.Core.Interfaces.ViewModels;

namespace Skua.Core.Options;

public class OptionContainer : ObservableObject, IOptionContainer
{
    private readonly IDialogService _dialogService;

    public List<IOption> Options { get; } = new List<IOption>();

    public Dictionary<string, List<IOption>> MultipleOptions { get; } = new();

    public Dictionary<IOption, string> OptionValues { get; } = new();
    public virtual string OptionsFile { get; set; } = string.Empty;

    public OptionContainer(IDialogService dialogService)
    {
        _dialogService = dialogService;
    }

    public void SetDefaults()
    {
        Options.ForEach(o => OptionValues[o] = o.DefaultValue?.ToString() ?? string.Empty);
        foreach (IOption option in MultipleOptions.SelectMany(kvp => kvp.Value))
            OptionValues[option] = option.DefaultValue?.ToString() ?? string.Empty;
    }

    public T? Get<T>(string name) where T : IConvertible
    {
        return Get<T>(Options.Find(o => o.Name == name));
    }

    public T? Get<T>(string category, string name) where T : IConvertible
    {
        return category == "Options" ? Get<T>(name) : Get<T>(MultipleOptions[category].Find(o => o.Name == name));
    }

    public T? Get<T>(IOption? option) where T : IConvertible
    {
        return option is null
            ? default
            : OptionValues.TryGetValue(option, out string? value)
            ? string.IsNullOrEmpty(value)
                ? default
                : typeof(T).IsEnum
                ? (T)Enum.Parse(typeof(T), value.Replace(' ', '_'))
                : (T)Convert.ChangeType(value, typeof(T))
            : default;
    }

    public string GetDirect(IOption? option)
    {
        return option is null ? string.Empty : OptionValues[option];
    }

    public void Set(string name, object value)
    {
        Set(Options.Find(o => o.Name == name), value);
    }

    public void Set(string category, string name, object value)
    {
        Set(MultipleOptions[category].Find(o => o.Name == name), value);
    }

    public void Set<T>(IOption? option, T value)
    {
        if (option is null)
            return;
        OptionValues[option] = value?.ToString() ?? string.Empty;
        if (!option.Transient)
            Save();
    }

    public void Configure()
    {
        SetDefaults();
        Load();
        _dialogService.ShowDialog(this, SaveOptions);
    }

    private void SaveOptions(IOptionContainerViewModel vm)
    {
        foreach (IOptionContainerItemViewModel optionViewModel in vm.Options)
            Set(optionViewModel.Option, optionViewModel.Type.IsEnum ? optionViewModel.SelectedValue : optionViewModel.Value);
    }

    public void Load()
    {
        if (!File.Exists(OptionsFile))
        {
            return;
        }

        foreach (string line in File.ReadLines(OptionsFile))
        {
            string[] parts = line.Trim().Split(new char[] { ':', '=' }, 3);
            switch (parts.Length)
            {
                case 3:
                    {
                        IOption? option = parts[0] switch
                        {
                            "Options" => Options.Find(o => o.Name == parts[1]),
                            _ => MultipleOptions[parts[0]].Find(o => o.Name == parts[1]),
                        };
                        if (option is not null)
                            OptionValues[option] = parts[2];
                        break;
                    }
                case 2:
                    {
                        IOption? option = Options.Find(o => o.Name == parts[0]);
                        if (option is not null)
                            OptionValues[option] = parts[1];
                        break;
                    }
            }
        }
        OnPropertyChanged(nameof(MultipleOptions));
        OnPropertyChanged(nameof(OptionValues));
        OnPropertyChanged(nameof(Options));
    }

    public void Save()
    {
        List<string> linesToSave = new();
        linesToSave.AddRange(Options.Where(o => !o.Transient).Select(o => $"Options:{o.Name}={OptionValues[o]}"));
        foreach (KeyValuePair<string, List<IOption>> item in MultipleOptions)
            linesToSave.AddRange(item.Value.Where(o => !o.Transient).Select(o => $"{item.Key}:{o.Name}={OptionValues[o]}"));
        File.WriteAllLines(OptionsFile, linesToSave);
    }
}