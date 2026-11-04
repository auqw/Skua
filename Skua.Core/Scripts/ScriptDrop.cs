using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Skua.Core.Interfaces;
using Skua.Core.Messaging;
using Skua.Core.Models.Items;
using Skua.Core.Utils;

namespace Skua.Core.Scripts;

public partial class ScriptDrop : ObservableRecipient, IScriptDrop, IAsyncDisposable
{
    public ScriptDrop(
        Lazy<IFlashUtil> flash,
        Lazy<IScriptSend> send,
        Lazy<IScriptMap> map,
        Lazy<IScriptWait> wait,
        Lazy<IScriptOption> options,
        Lazy<IScriptPlayer> player,
        Lazy<IScriptInventory> inventory)
    {
        _lazySend = send;
        _lazyWait = wait;
        _lazyMap = map;
        _lazyOptions = options;
        _lazyPlayer = player;
        _lazyFlash = flash;
        _lazyInventory = inventory;

        StrongReferenceMessenger.Default.Register<ScriptDrop, ItemDroppedMessage, int>(this, (int)MessageChannels.GameEvents, AddDrop);
        StrongReferenceMessenger.Default.Register<ScriptDrop, ItemPickedUpMessage, int>(this, (int)MessageChannels.GameEvents, DropPickedUp);
        StrongReferenceMessenger.Default.Register<ScriptDrop, LogoutMessage, int>(this, (int)MessageChannels.GameEvents, ClearDrops);
        StrongReferenceMessenger.Default.Register<ScriptDrop, ScriptStoppedMessage, int>(this, (int)MessageChannels.ScriptStatus, ScriptStopped);
        Messenger.Register<ScriptDrop, PropertyChangedMessage<bool>>(this, OptionsChanged);

        _timerDrops = new(TimeSpan.FromMilliseconds(1000));
    }

    private readonly Lazy<IScriptSend> _lazySend;
    private readonly Lazy<IScriptWait> _lazyWait;
    private readonly Lazy<IScriptMap> _lazyMap;
    private readonly Lazy<IScriptOption> _lazyOptions;
    private readonly Lazy<IScriptPlayer> _lazyPlayer;
    private readonly Lazy<IFlashUtil> _lazyFlash;
    private IScriptSend Send => _lazySend.Value;
    private IScriptWait Wait => _lazyWait.Value;
    private IScriptMap Map => _lazyMap.Value;
    private IScriptOption Options => _lazyOptions.Value;
    private IScriptPlayer Player => _lazyPlayer.Value;
    private IFlashUtil Flash => _lazyFlash.Value;
    private readonly Lazy<IScriptInventory> _lazyInventory;
    private IScriptInventory Inventory => _lazyInventory.Value;

    private readonly PeriodicTimer _timerDrops;
    private Task? _taskDrops;
    private CancellationTokenSource? _ctsDrops;

    [ObservableProperty]
    private int _interval;

    [ObservableProperty]
    private bool _rejectElse;

    public bool Enabled => _taskDrops is { IsCompleted: false } && _ctsDrops is { IsCancellationRequested: false };
    private readonly SynchronizedList<string> _toPickup = new();
    public IEnumerable<string> ToPickup => _toPickup.Items;
    private readonly SynchronizedList<int> _toPickupIDs = new();
    public IEnumerable<int> ToPickupIDs => _toPickupIDs.Items;
    private readonly SynchronizedList<ItemBase> _currentDropInfos = new();
    public IEnumerable<ItemBase> CurrentDropInfos => _currentDropInfos.Items;
    public IEnumerable<string> CurrentDrops => CurrentDropInfos.Select(x => x.Name.Trim()).ToList();

    public void Pickup(string name)
    {
        if (!CurrentDrops.Contains(name, StringComparer.OrdinalIgnoreCase))
            return;

        ItemBase drop = _currentDropInfos.Find(d => d.Name.ToLowerInvariant() == name.ToLowerInvariant())!;
        if (!drop.Temp && !Inventory.HasSpaceFor(drop))
            return;
        Send.Packet($"%xt%zm%getDrop%{Map.RoomID}%{drop.ID}%");
    }

    public void Pickup(int id)
    {
        ItemBase? drop = CurrentDropInfos.FirstOrDefault(i => i.ID == id);
        if (drop is null || (!drop.Temp && !Inventory.HasSpaceFor(drop)))
            return;

        Send.Packet($"%xt%zm%getDrop%{Map.RoomID}%{id}%");
    }

    public void Pickup(params string[] names)
    {
        Pickup(names, CancellationToken.None);
    }

    public void Pickup(params int[] ids)
    {
        Pickup(ids, CancellationToken.None);
    }

    public void PickupACItems()
    {
        PickupACItems(CancellationToken.None);
    }

    public void PickupAll(bool skipWait = false)
    {
        PickupAll(skipWait, CancellationToken.None);
    }

    // The token lets the drop loop stop between pickups and stop waiting for one, instead of finishing the whole pass.
    private void Pickup(string[] names, CancellationToken token)
    {
        foreach (string name in names)
        {
            token.ThrowIfCancellationRequested();
            Pickup(name);
            WaitForPickup(() => !((IScriptDrop)this).Exists(name), token);
        }
    }

    private void Pickup(int[] ids, CancellationToken token)
    {
        foreach (int id in ids)
        {
            token.ThrowIfCancellationRequested();
            Pickup(id);
            WaitForPickup(() => !((IScriptDrop)this).Exists(id), token);
        }
    }

    private void PickupACItems(CancellationToken token)
    {
        Pickup(CurrentDropInfos.Where(d => d.Coins).Select(d => d.Name).ToArray(), token);
    }

    private void PickupAll(bool skipWait, CancellationToken token)
    {
        foreach (ItemBase drop in _currentDropInfos.Items)
        {
            token.ThrowIfCancellationRequested();
            Pickup(drop.Name);
        }
        OnPropertyChanged(nameof(CurrentDropInfos));
        OnPropertyChanged(nameof(CurrentDrops));
        if (!skipWait)
            WaitForPickup(() => !((IScriptDrop)this).Exists("*"), token);
    }

    // Same wait as IScriptWait.ForPickup, but it also ends when the token is cancelled.
    private void WaitForPickup(Func<bool> pickedUp, CancellationToken token)
    {
        Wait.ForTrue(() => token.IsCancellationRequested || !Player.Playing || pickedUp(), Wait.OverrideTimeout ? Wait.DropActionTimeout : 10);
        token.ThrowIfCancellationRequested();
    }

    public void RejectExcept(params string[] names)
    {
        if (Options.AcceptACDrops)
            PickupACItems();
        RejectExceptCore(names);
    }

    public void RejectExcept(params int[] ids)
    {
        if (Options.AcceptACDrops)
            PickupACItems();
        IEnumerable<string> items = CurrentDropInfos.Where(d => ids.Contains(d.ID)).Select(d => d.Name);
        RejectExceptCore(items);
    }

    public void RejectAll(bool skipWait = false)
    {
        if (Options.AcceptACDrops)
            PickupACItems();
        RejectExceptCore(Array.Empty<string>());
    }

    private void RejectExceptCore(IEnumerable<string> names)
    {
        HashSet<string> keep = new(names, StringComparer.OrdinalIgnoreCase);
        if (Options.AcceptACDrops)
            keep.UnionWith(CurrentDropInfos.Where(d => d.Coins).Select(d => d.Name));
        if (!Flash.Call<bool>("rejectExcept", keep.Join(',').ToLowerInvariant()))
            return;
        OnPropertyChanged(nameof(CurrentDropInfos));
        OnPropertyChanged(nameof(CurrentDrops));
    }

    public void Start()
    {
        if (Enabled)
            return;

        // A stopped loop may still be finishing a pickup, so the new one starts after it rather than beside it: two loops
        // would pick up the same drops, and the PeriodicTimer allows only one waiter.
        Task previous = _taskDrops ?? Task.CompletedTask;
        CancellationTokenSource? previousCts = _ctsDrops;
        _ctsDrops = new();
        CancellationToken token = _ctsDrops.Token;
        // Run off the caller's context: started from the UI, the loop would resume on the UI thread and block it in Wait.ForPickup.
        _taskDrops = Task.Run(async () =>
        {
            try
            {
                await previous;
            }
            catch { }
            previousCts?.Dispose();
            await HandleDrops(_timerDrops, token);
        });
        OnPropertyChanged(nameof(Enabled));
    }

    public void Stop()
    {
        if (_taskDrops is null)
            return;

        _ctsDrops?.Cancel();
        Wait.ForTrue(() => _taskDrops?.IsCompleted == true, null, 2);
        ClearStoppedTask();
        OnPropertyChanged(nameof(Enabled));
    }

    public async ValueTask StopAsync()
    {
        if (_taskDrops is null)
            return;

        _ctsDrops?.Cancel();
        await Wait.ForTrueAsync(() => _taskDrops?.IsCompleted == true, 20);
        ClearStoppedTask();
        OnPropertyChanged(nameof(Enabled));
    }

    // A loop that hasn't finished yet stays in _taskDrops so the next Start waits for it.
    private void ClearStoppedTask()
    {
        if (_taskDrops is not { IsCompleted: true })
            return;

        _ctsDrops?.Dispose();
        _ctsDrops = null;
        _taskDrops = null;
    }

    public void Add(params string[] names)
    {
        _toPickup.AddRange(names.Except(_toPickup.Items));
        OnPropertyChanged(nameof(ToPickup));
        Broadcast(null, ToPickup, nameof(ToPickup));
    }

    public void Add(params int[] ids)
    {
        _toPickupIDs.AddRange(ids.Except(_toPickupIDs.Items));
        OnPropertyChanged(nameof(ToPickupIDs));
        Broadcast(null, ToPickupIDs, nameof(ToPickupIDs));
    }

    public void Clear()
    {
        _toPickupIDs.Clear();
        _toPickup.Clear();
        OnPropertyChanged(nameof(ToPickupIDs));
        Broadcast(null, ToPickupIDs, nameof(ToPickupIDs));
        OnPropertyChanged(nameof(ToPickup));
        Broadcast(null, ToPickup, nameof(ToPickup));
    }

    public void Remove(params string[] names)
    {
        _toPickup.Remove(names.Contains);
        OnPropertyChanged(nameof(ToPickup));
        Broadcast(null, ToPickup, nameof(ToPickup));
    }

    public void Remove(params int[] ids)
    {
        _toPickupIDs.Remove(ids.Contains);
        OnPropertyChanged(nameof(ToPickupIDs));
        Broadcast(null, ToPickupIDs, nameof(ToPickupIDs));
    }

    private async Task HandleDrops(PeriodicTimer timer, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                if (!await timer.WaitForNextTickAsync(token))
                    break;
                if (!Player.Playing)
                    continue;

                if (Interval > 0)
                    await Task.Delay(Interval, token);

                if (Options.AcceptAllDrops)
                {
                    PickupAll(false, token);
                    continue;
                }

                // AC items are picked up here, so the rejects below call RejectExceptCore rather than RejectAll/RejectExcept,
                // which would pick them up again without the token.
                if (Options.AcceptACDrops)
                    PickupACItems(token);

                if (Options.RejectAllDrops)
                {
                    RejectExceptCore(Array.Empty<string>());
                    continue;
                }

                int[] pickupIDs = _toPickupIDs.Items.ToArray();
                if (pickupIDs.Length > 0)
                    Pickup(pickupIDs, token);

                string[] pickupNames = _toPickup.Items.ToArray();
                if (pickupNames.Length > 0)
                    Pickup(pickupNames, token);

                token.ThrowIfCancellationRequested();
                if (RejectElse)
                    RejectExceptCore(BuildRejectElseWhitelist(pickupNames, pickupIDs));
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Error in drop handler: {ex.Message}");
            }
        }
    }

    private string[] BuildRejectElseWhitelist(string[] pickupNames, int[] pickupIDs)
    {
        HashSet<string> whitelist = new(pickupNames, StringComparer.OrdinalIgnoreCase);
        if (pickupIDs.Length == 0)
            return whitelist.ToArray();

        HashSet<int> idWhitelist = new(pickupIDs);
        foreach (ItemBase drop in _currentDropInfos.Items)
        {
            if (idWhitelist.Contains(drop.ID))
                whitelist.Add(drop.Name);
        }

        return whitelist.ToArray();
    }

    private void OptionsChanged(ScriptDrop recipient, PropertyChangedMessage<bool> message)
    {
        if (message.Sender.GetType() != typeof(ScriptOption))
            return;

        switch (message.PropertyName)
        {
            case nameof(IScriptOption.AcceptACDrops):
            case nameof(IScriptOption.AcceptAllDrops):
            case nameof(IScriptOption.RejectAllDrops):
                recipient.Start();
                break;
        }
    }

    private void ClearDrops(ScriptDrop recipient, LogoutMessage message)
    {
        recipient._currentDropInfos.Clear();
        recipient.OnPropertyChanged(nameof(recipient.CurrentDropInfos));
        recipient.OnPropertyChanged(nameof(recipient.CurrentDrops));
    }

    private void DropPickedUp(ScriptDrop recipient, ItemPickedUpMessage message)
    {
        if (recipient._currentDropInfos.Remove(drop => drop.ID == message.ItemID) == 0)
            return;
        recipient.OnPropertyChanged(nameof(recipient.CurrentDropInfos));
        recipient.OnPropertyChanged(nameof(recipient.CurrentDrops));
    }

    private void AddDrop(ScriptDrop recipient, ItemDroppedMessage message)
    {
        if (message.AddedToInv)
            return;

        if (!recipient.CurrentDropInfos.Contains(message.Item))
            recipient._currentDropInfos.Add(message.Item);
        else
            recipient._currentDropInfos.Find(i => i.Equals(message.Item))!.Quantity += message.Item.Quantity;
        recipient.OnPropertyChanged(nameof(recipient.CurrentDropInfos));
        recipient.OnPropertyChanged(nameof(recipient.CurrentDrops));
    }

    private async void ScriptStopped(ScriptDrop recipient, ScriptStoppedMessage message)
    {
        try
        {
            await recipient.StopAsync();
        }
        catch (Exception ex)
        {
            // Log the exception to prevent unobserved task exceptions
            System.Diagnostics.Trace.WriteLine($"Error in ScriptStopped handler: {ex.Message}");
        }
    }

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            _ctsDrops?.Cancel();
            Wait.ForTrue(() => _taskDrops?.IsCompleted == true, null, 20);
        }
        catch { }

        _ctsDrops?.Dispose();
        _ctsDrops = null;
        _taskDrops = null;

        _timerDrops?.Dispose();

        if (IsActive)
        {
            StrongReferenceMessenger.Default.UnregisterAll(this);
            Messenger.UnregisterAll(this);
            IsActive = false;
        }

        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            if (_taskDrops is not null)
            {
                _ctsDrops?.Cancel();
                await _taskDrops;
            }
        }
        catch { }

        _ctsDrops?.Dispose();
        _ctsDrops = null;
        _taskDrops = null;

        _timerDrops?.Dispose();

        if (IsActive)
        {
            StrongReferenceMessenger.Default.UnregisterAll(this);
            Messenger.UnregisterAll(this);
            IsActive = false;
        }

        GC.SuppressFinalize(this);
    }
}