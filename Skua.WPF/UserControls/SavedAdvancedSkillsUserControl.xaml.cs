using Skua.Core.Models.Skills;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;

namespace Skua.WPF.UserControls;

/// <summary>
/// Interaction logic for SavedAdvancedSkillsUserControl.xaml
/// </summary>
public partial class SavedAdvancedSkillsUserControl : UserControl
{
    public SavedAdvancedSkillsUserControl()
    {
        InitializeComponent();
    }

    private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is Core.ViewModels.SavedAdvancedSkillsViewModel vm)
        {
            vm.LoadAvailableClassesCommand.Execute(null);
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ICollectionView view = CollectionViewSource.GetDefaultView(SkillsList.ItemsSource);
        view.Filter = o =>
        {
            return o is AdvancedSkill skill && skill.ClassName.Contains(SearchBox.Text, StringComparison.OrdinalIgnoreCase);
        };
    }

    private void UserControl_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.C && (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == System.Windows.Input.ModifierKeys.Control)
        {
            CopySelected();
            e.Handled = true;
        }
        else if (e.Key == System.Windows.Input.Key.V && (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == System.Windows.Input.ModifierKeys.Control)
        {
            PasteSelected();
            e.Handled = true;
        }
    }

    private List<AdvancedSkill> _clipboard = new();

    private void CopySelected()
    {
        _clipboard.Clear();
        foreach (object? item in SkillsList.SelectedItems)
        {
            if (item is AdvancedSkill skill)
            {
                _clipboard.Add(new AdvancedSkill
                {
                    ClassName = skill.ClassName,
                    Skills = skill.Skills,
                    SkillTimeoutMs = skill.SkillTimeoutMs,
                    ClassUseMode = skill.ClassUseMode,
                    SkillUseMode = skill.SkillUseMode
                });
            }
        }
    }

    private void PasteSelected()
    {
        if (DataContext is Core.ViewModels.SavedAdvancedSkillsViewModel vm)
        {
            foreach (AdvancedSkill skill in _clipboard)
            {
                vm.LoadedSkills.Add(skill);
            }
        }
    }
}
