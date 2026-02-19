using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Skua.Core.Interfaces;
using Skua.Core.Messaging;
using Skua.Core.Models.Items;
using Skua.Core.Models.Skills;
using Skua.Shared.Avalonia.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Skua.App.Avalonia.ViewModels;

public partial class AutoViewModel : BotControlViewModelBase, IDisposable
{
    private CancellationTokenSource? _autoCts;

    public AutoViewModel(IScriptAuto auto, IScriptInventory inventory, IAdvancedSkillContainer advancedSkills)
        : base("Auto Attack")
    {
        StrongReferenceMessenger.Default.Register<AutoViewModel, StopAutoMessage>(this, async (r, m) => await r.StopAutoAsync());
        StrongReferenceMessenger.Default.Register<AutoViewModel, StartAutoAttackMessage>(this, async (r, m) => await r.StartAutoAttack());
        StrongReferenceMessenger.Default.Register<AutoViewModel, StartAutoHuntMessage>(this, async (r, m) => await r.StartAutoHunt());

        Auto = auto;
        _inventory = inventory;
        _advancedSkills = advancedSkills;
        StopAutoAsyncCommand = new AsyncRelayCommand(StopAutoAsync);
    }

    private readonly IScriptInventory _inventory;
    private readonly IAdvancedSkillContainer _advancedSkills;

    [ObservableProperty]
    private ClassUseMode? _selectedClassMode;

    [ObservableProperty]
    private string? _selectedClassModeString;

    [ObservableProperty]
    private string? _manualMapIDs;

    async partial void OnSelectedClassStringChanged(string? value)
    {
        await EquipSelectedClassAsync();
    }

    async partial void OnSelectedClassModeStringChanged(string? value)
    {
        await LoadSelectedClassMode();
    }

    public IScriptAuto Auto { get; }
    public List<string>? PlayerClasses
    {
        get
        {
            if (_inventory.Items is null)
                return null;

            List<string> classes = new();
            foreach (ItemBase item in _inventory.Items)
            {
                if (item.Category == ItemCategory.Class)
                    classes.Add(item.Name);
            }

            return classes
                .OrderBy(name => name, System.StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

    [ObservableProperty]
    private List<string> _currentClassModeStrings = new();

    [ObservableProperty]
    private string? _selectedClassString;

    public string? SelectedClass
    {
        get => _selectedClassString;
        set
        {
            if (SetProperty(ref _selectedClassString, value) && value is not null)
            {
                CurrentClassModes = new();
                CurrentClassModeStrings = new List<string>();
                foreach (AdvancedSkill skill in _advancedSkills.LoadedSkills)
                {
                    if (skill.ClassName == _selectedClassString)
                        CurrentClassModes.Add(skill.ClassUseMode);
                }

                Dictionary<string, List<string>> classModes = _advancedSkills.GetAvailableClassModes();
                if (classModes.TryGetValue(_selectedClassString, out List<string>? modes))
                {
                    CurrentClassModeStrings = new List<string>(modes.OrderBy(x => x));
                }

                OnPropertyChanged(nameof(CurrentClassModes));

                if (CurrentClassModes.Count > 0)
                {
                    SelectedClassMode = CurrentClassModes.First();
                }
                if (CurrentClassModeStrings.Count > 0 && SelectedClassModeString == null)
                {
                    SelectedClassModeString = Enumerable.First<string>(CurrentClassModeStrings);
                }

                OnSelectedClassStringChanged(_selectedClassString);
            }
        }
    }

    private async Task EquipSelectedClassAsync()
    {
        try
        {
            await Task.Run(() => _inventory.EquipItem(_selectedClassString)).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
        }
    }

    public List<ClassUseMode>? CurrentClassModes { get; private set; }
    public IAsyncRelayCommand StopAutoAsyncCommand { get; }

    [RelayCommand]
    private void ReloadClasses()
    {
        OnPropertyChanged(nameof(PlayerClasses));

        CurrentClassModes = null;
        CurrentClassModeStrings = new List<string>();
        SelectedClass = null;
        SelectedClassMode = null;
        SelectedClassModeString = null;
    }

    private async Task LoadSelectedClassMode()
    {
        if (string.IsNullOrEmpty(SelectedClassModeString))
            return;

        AdvancedSkill? skill = _advancedSkills.GetClassModeSkills(_selectedClassString, SelectedClassModeString);
        if (skill != null)
        {
            SelectedClassMode = skill.ClassUseMode;
        }
    }

    [RelayCommand]
    private async Task StartAutoHunt()
    {
        _autoCts?.Cancel();
        _autoCts?.Dispose();
        _autoCts = new CancellationTokenSource();

        int[]? manualMapIDs = ParseManualMapIDs();

        if (_selectedClassString is not null && _selectedClassMode is not null)
        {
            await Task.Factory.StartNew(
                () => Auto.StartAutoHunt(_selectedClassString, (ClassUseMode)_selectedClassMode, manualMapIDs),
                _autoCts.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
            return;
        }

        await Task.Factory.StartNew(
            () => Auto.StartAutoHunt(null, ClassUseMode.Base, manualMapIDs),
            _autoCts.Token,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
    }

    [RelayCommand]
    private async Task StartAutoAttack()
    {
        _autoCts?.Cancel();
        _autoCts?.Dispose();
        _autoCts = new CancellationTokenSource();

        int[]? manualMapIDs = ParseManualMapIDs();

        if (_selectedClassString is not null && _selectedClassMode is not null)
        {
            await Task.Factory.StartNew(
                () => Auto.StartAutoAttack(_selectedClassString, (ClassUseMode)_selectedClassMode, manualMapIDs),
                _autoCts.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
            return;
        }

        await Task.Factory.StartNew(
            () => Auto.StartAutoAttack(null, ClassUseMode.Base, manualMapIDs),
            _autoCts.Token,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
    }

    private int[]? ParseManualMapIDs()
    {
        if (string.IsNullOrWhiteSpace(ManualMapIDs))
            return null;

        List<int> mapIds = new();
        foreach (string part in ManualMapIDs.Split(new[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (int.TryParse(part.Trim(), out int id))
                mapIds.Add(id);
        }
        return mapIds.Count > 0 ? mapIds.ToArray() : null;
    }

    private async Task StopAutoAsync()
    {
        _autoCts?.Cancel();
        await Auto.StopAsync();
        _autoCts?.Dispose();
        _autoCts = null;
    }

    private bool _disposed = false;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _autoCts?.Cancel();
                _autoCts?.Dispose();
                StrongReferenceMessenger.Default.UnregisterAll(this);
            }

            _disposed = true;
        }
    }

    ~AutoViewModel()
    {
        Dispose(false);
    }
}