using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Skua.App.Avalonia.ViewModels.Dialogs;
using Skua.Core.Interfaces;
using Skua.Core.Messaging;
using Skua.Core.Models.Skills;
using Skua.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Skua.App.Avalonia.ViewModels.AdvancedSkills;

public partial class AdvancedSkillEditorViewModel : ObservableRecipient
{
    public AdvancedSkillEditorViewModel(IDialogService dialogService)
    {
        ClassUseModes = ClassUseModeExtensions.ToArray();
        UseRules = new();
        ClearSkillsCommand = new RelayCommand(CurrentSkillsList.Clear);
        _dialogService = dialogService;
    }

    protected override void OnActivated()
    {
        Messenger.Register<AdvancedSkillEditorViewModel, EditAdvancedSkillMessage>(this, EditSkill);
    }

    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private int _currentSkillTimeout = 100;

    [ObservableProperty]
    private bool _useWaitModeBool;

    [ObservableProperty]
    private RangedObservableCollection<SkillItemViewModel> _currentSkillsList = new();

    [ObservableProperty]
    private SkillItemViewModel? _selectedSkill;

    [ObservableProperty]
    private int _selectedSkillIndex;

    [ObservableProperty]
    private int _selectedClassUseMode;

    [ObservableProperty]
    private string _currentClassName = string.Empty;

    [ObservableProperty]
    private bool _resetComboOnTargetChange;

    public string[] ClassUseModes { get; }
    public SkillRulesViewModel UseRules { get; }
    public IRelayCommand ClearSkillsCommand { get; }

    [RelayCommand]
    private void EditSkill()
    {
        if (SelectedSkill is null)
            return;
        SkillRuleEditorDialogViewModel toEdit = new(new(SelectedSkill.UseRules));
        if (_dialogService.ShowDialog(toEdit) == true)
            SelectedSkill.UseRules = toEdit.UseRules;
    }

    [RelayCommand]
    private void SaveSkills()
    {
        List<string> skillStrings = new(_currentSkillsList.Count);
        foreach (SkillItemViewModel skill in _currentSkillsList)
            skillStrings.Add(skill.Convert());
        string skills = string.Join(" | ", skillStrings);
        string modeString = SelectedClassUseMode >= 0 && SelectedClassUseMode < ClassUseModes.Length ? ClassUseModes[SelectedClassUseMode] : "Base";
        AdvancedSkill advSkill = new(CurrentClassName, skills, CurrentSkillTimeout, modeString, UseWaitModeBool ? SkillUseMode.WaitForCooldown : SkillUseMode.UseIfAvailable, ResetComboOnTargetChange);
        OnPropertyChanged(nameof(CurrentSkillsList));
        Messenger.Send<SaveAdvancedSkillMessage>(new(advSkill));
    }

    [RelayCommand]
    private void AddSkillToCurrent(string? value)
    {
        if (value is null)
            return;
        if (!int.TryParse(value, out int result))
            return;
        SkillItemViewModel info = new(result, UseRules);
        CurrentSkillsList.Add(info);
    }

    [RelayCommand]
    private void MoveSkillDown()
    {
        MoveSkill(1);
    }

    [RelayCommand]
    private void MoveSkillUp()
    {
        MoveSkill(-1);
    }

    private void MoveSkill(int direction)
    {
        if (SelectedSkill is null)
        {
            SelectedSkillIndex = 0;
            return;
        }

        int newIndex = SelectedSkillIndex + direction;

        if (newIndex < 0 || newIndex >= _currentSkillsList.Count)
            return;

        CurrentSkillsList.Swap(newIndex, SelectedSkillIndex);
        SelectedSkillIndex = newIndex;
    }

    [RelayCommand]
    private void SelectSkillDown()
    {
        SelectSkill(1);
    }

    [RelayCommand]
    private void SelectSkillUp()
    {
        SelectSkill(-1);
    }

    private void SelectSkill(int direction)
    {
        if (SelectedSkill is null)
        {
            SelectedSkillIndex = 0;
            return;
        }

        int newIndex = SelectedSkillIndex + direction;
        if (newIndex < 0 || newIndex >= _currentSkillsList.Count)
            return;

        SelectedSkillIndex = newIndex;
    }

    [RelayCommand]
    private void RemoveSkill()
    {
        if (SelectedSkill is null)
            return;
        int index = SelectedSkillIndex;
        CurrentSkillsList.RemoveAt(SelectedSkillIndex);
        SelectedSkillIndex = index - 1;
    }

    private void EditSkill(AdvancedSkillEditorViewModel recipient, EditAdvancedSkillMessage message)
    {
        recipient.CurrentSkillsList.Clear();
        recipient.CurrentSkillTimeout = message.AdvSkill.SkillTimeout;
        recipient.UseWaitModeBool = message.AdvSkill.SkillUseMode != SkillUseMode.UseIfAvailable;
        recipient.ResetComboOnTargetChange = message.AdvSkill.ResetComboOnTargetChange;
        recipient.CurrentClassName = message.AdvSkill.ClassName;
        recipient.SelectedClassUseMode = (int)message.AdvSkill.ClassUseMode;
        recipient.CurrentSkillsList.AddRange(message.AdvSkill.Skills.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).Select(s => new SkillItemViewModel(s.Trim())));
    }
}