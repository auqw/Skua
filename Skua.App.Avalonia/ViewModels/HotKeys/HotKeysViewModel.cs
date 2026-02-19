using CommunityToolkit.Mvvm.Messaging;
using Skua.App.Avalonia.ViewModels.Dialogs;
using Skua.Core.Interfaces;
using Skua.Core.Messaging;
using Skua.Core.Models;
using Skua.Shared.Avalonia.ViewModels;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Skua.App.Avalonia.ViewModels.HotKeys;

public partial class HotKeysViewModel : BotControlViewModelBase, IManagedWindow
{
    public HotKeysViewModel(IHotKeyService hotKeyService, ISettingsService settingsService, IDialogService dialogService)
        : base("HotKeys", 380, 0)
    {
        _hotKeyService = hotKeyService;
        _settingsService = settingsService;
        _dialogService = dialogService;
        HotKeys = _hotKeyService.GetHotKeys<HotKeyItemViewModel>();
        _hotKeyService.Reload();
    }

    private readonly IHotKeyService _hotKeyService;
    private readonly ISettingsService _settingsService;
    private readonly IDialogService _dialogService;

    protected override void OnActivated()
    {
        StrongReferenceMessenger.Default.Register<HotKeysViewModel, EditHotKeyMessage>(this, EditHotKey);
        foreach (HotKeyItemViewModel hk in HotKeys)
            StrongReferenceMessenger.Default.Register<HotKeyItemViewModel, HotKeyErrorMessage>(hk, HandleError);
    }

    private void HandleError(HotKeyItemViewModel recipient, HotKeyErrorMessage message)
    {
        if (message.Binding == recipient.Binding)
            recipient.KeyGesture = "Failed to bind";
    }

    protected override void OnDeactivated()
    {
        base.OnDeactivated();
        StrongReferenceMessenger.Default.UnregisterAll(this);
        foreach (HotKeyItemViewModel hk in HotKeys)
            StrongReferenceMessenger.Default.UnregisterAll(hk);
    }

    public List<HotKeyItemViewModel> HotKeys { get; }

    private void Save()
    {
        StringCollection hotkeys = new();
        foreach (HotKeyItemViewModel hk in HotKeys)
            hotkeys.Add($"{hk.Binding}|{hk.KeyGesture}");

        _settingsService.Set("HotKeys", hotkeys);
    }

    private void EditHotKey(HotKeysViewModel recipient, EditHotKeyMessage message)
    {
        HotKey? hotKey = recipient._hotKeyService.ParseToHotKey(message.KeyGesture);
        AssignHotKeyDialogViewModel diag = hotKey is null ? new(message.Title) : new(message.Title, hotKey);
        if (recipient._dialogService.ShowDialog(diag) == true)
        {
            HotKeyItemViewModel? hk = recipient.HotKeys.Find(hk => hk.Title == message.Title);
            if (hk != null)
            {
                hk.KeyGesture = diag.KeyGesture;
                recipient.Save();
                recipient._hotKeyService.Reload();
            }
        }
    }
}