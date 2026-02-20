using CommunityToolkit.Mvvm.ComponentModel;
using Skua.Core.Skills;
using System;

namespace Skua.App.Avalonia.ViewModels.AdvancedSkills;

public partial class AuraCheckViewModel : ObservableRecipient
{
    public AuraCheckViewModel()
    {
    }

    public AuraCheckViewModel(AuraCheckViewModel check)
    {
        AuraName = check.AuraName;
        StackCount = check.StackCount;
        IsGreater = check.IsGreater;
        AuraTargetIndex = check.AuraTargetIndex;
    }

    [ObservableProperty]
    private string _auraName = string.Empty;

    private float _stackCount;

    public float StackCount
    {
        get => _stackCount;
        set
        {
            if (value < 0)
                return;
            SetProperty(ref _stackCount, value);
        }
    }

    [ObservableProperty]
    private bool _isGreater = true;

    [ObservableProperty]
    private int _auraTargetIndex = 0;

    public string AuraTarget => AuraTargetIndex == 1 ? "target" : "self";

    public AuraCheck ToAuraCheck()
    {
        return new AuraCheck
        {
            AuraName = AuraName,
            StackCount = StackCount,
            Greater = IsGreater,
            AuraTarget = AuraTarget
        };
    }

    public static AuraCheckViewModel FromAuraCheck(AuraCheck check)
    {
        return new AuraCheckViewModel
        {
            AuraName = check.AuraName,
            StackCount = check.StackCount,
            IsGreater = check.Greater,
            AuraTargetIndex = check.AuraTarget.Equals("target", StringComparison.OrdinalIgnoreCase) ? 1 : 0
        };
    }
}
