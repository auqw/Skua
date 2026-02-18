using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Skua.Core.Interfaces;
using Skua.Core.Messaging;
using Skua.Core.Models.Skills;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Skua.App.Avalonia.ViewModels.AdvancedSkills;

public class ClassModeItem
{
    public string ClassName { get; set; } = "";
    public string Mode { get; set; } = "";
    public override string ToString()
    {
        return $"{ClassName} ({Mode})";
    }
}

public partial class SavedAdvancedSkillsViewModel : ObservableRecipient
{
    public SavedAdvancedSkillsViewModel(IAdvancedSkillContainer advancedSkillContainer)
    {
        _advancedSkillContainer = advancedSkillContainer;
        RefreshSkillsCommand = new RelayCommand(_advancedSkillContainer.LoadSkills);
        ResetSkillsSetCommand = new RelayCommand(_advancedSkillContainer.ResetSkillsSets);
    }

    protected override void OnActivated()
    {
        Messenger.Register<SavedAdvancedSkillsViewModel, SaveAdvancedSkillMessage>(this, SaveSkill);
        Messenger.Register<SavedAdvancedSkillsViewModel, PropertyChangedMessage<List<AdvancedSkill>>>(this, AdvancedSkillsChanged);
    }

    private readonly IAdvancedSkillContainer _advancedSkillContainer;

    [ObservableProperty]
    private AdvancedSkill? _selectedSkill;

    [ObservableProperty]
    private ObservableCollection<string> _availableClasses = new();

    [ObservableProperty]
    private ObservableCollection<string> _availableModes = new();

    [ObservableProperty]
    private string? _selectedClassName;

    [ObservableProperty]
    private string? _selectedMode;

    private ObservableCollection<AdvancedSkill>? _loadedSkills;

    public ObservableCollection<AdvancedSkill> LoadedSkills
    {
        get
        {
            _loadedSkills ??= new ObservableCollection<AdvancedSkill>(_advancedSkillContainer.LoadedSkills);
            return _loadedSkills;
        }
        set
        {
            _loadedSkills = value;
            OnPropertyChanged(nameof(LoadedSkills));
        }
    }

    public IRelayCommand RefreshSkillsCommand { get; }
    public IRelayCommand ResetSkillsSetCommand { get; }

    [RelayCommand]
    private void LoadAvailableClasses()
    {
        AvailableClasses.Clear();
        Dictionary<string, List<string>> classModeDictionary = _advancedSkillContainer.GetAvailableClassModes();
        List<string> sortedKeys = new(classModeDictionary.Keys);
        sortedKeys.Sort();
        foreach (string className in sortedKeys)
        {
            AvailableClasses.Add(className);
        }
    }

    partial void OnSelectedClassNameChanged(string? value)
    {
        if (value == null)
        {
            AvailableModes.Clear();
            return;
        }

        AvailableModes.Clear();
        Dictionary<string, List<string>> classModeDictionary = _advancedSkillContainer.GetAvailableClassModes();
        if (classModeDictionary.TryGetValue(value, out List<string>? modes))
        {
            List<string> sortedModes = new(modes);
            sortedModes.Sort();
            foreach (string mode in sortedModes)
            {
                AvailableModes.Add(mode);
            }
        }
        SelectedMode = null;
    }

    [RelayCommand]
    private void LoadSelectedClassMode()
    {
        if (string.IsNullOrEmpty(SelectedClassName) || string.IsNullOrEmpty(SelectedMode))
            return;

        AdvancedSkill? skill = _advancedSkillContainer.GetClassModeSkills(SelectedClassName, SelectedMode);
        if (skill != null)
        {
            SelectedSkill = skill;
        }
    }

    [RelayCommand]
    private void RemoveSelected()
    {
        if (SelectedSkill is null)
            return;

        _advancedSkillContainer.Remove(SelectedSkill);
    }

    [RelayCommand]
    private void EditSelected()
    {
        if (SelectedSkill is null)
            return;

        Messenger.Send<EditAdvancedSkillMessage>(new(SelectedSkill));
    }


    private void SaveSkill(SavedAdvancedSkillsViewModel recipient, SaveAdvancedSkillMessage message)
    {
        recipient._advancedSkillContainer.TryOverride(message.AdvSkill);
        Task.Delay(1000).ContinueWith(_ =>
        {
            recipient._loadedSkills = null;
            recipient.OnPropertyChanged(nameof(recipient.LoadedSkills));
        });
    }

    private void AdvancedSkillsChanged(SavedAdvancedSkillsViewModel recipient, PropertyChangedMessage<List<AdvancedSkill>> message)
    {
        if (message.PropertyName == nameof(IAdvancedSkillContainer.LoadedSkills))
        {
            recipient._loadedSkills = null;
            recipient.OnPropertyChanged(nameof(recipient.LoadedSkills));
        }
    }
}