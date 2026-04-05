using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Skua.Core.Interfaces;
using Skua.Shared.Avalonia.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Skua.App.Avalonia.ViewModels.Packets;

public partial class PacketLoggerViewModel : BotControlViewModelBase
{
    public PacketLoggerViewModel(IEnumerable<PacketLogFilterViewModel> filters, IFlashUtil flash, IFileDialogService fileDialog)
        : base("Packet Logger")
    {
        _flash = flash;
        _fileDialog = fileDialog;
        _allPacketFilters = filters.ToList();
        _visiblePacketFilters = _allPacketFilters.ToList();
    }

    private readonly IFlashUtil _flash;
    private readonly IFileDialogService _fileDialog;
    private readonly List<string> _allPacketLogs = [];
    private readonly List<PacketLogFilterViewModel> _allPacketFilters;

    [ObservableProperty]
    private ObservableCollection<string> _filteredPacketLogs = new();

    [ObservableProperty]
    private List<PacketLogFilterViewModel> _visiblePacketFilters;

    [ObservableProperty]
    private string _packetSearchText = string.Empty;

    [ObservableProperty]
    private string _filterSearchText = string.Empty;

    private bool _isReceivingPackets;

    public bool IsReceivingPackets
    {
        get => _isReceivingPackets;
        set
        {
            if (SetProperty(ref _isReceivingPackets, value))
                ToggleLogger();
        }
    }

    [RelayCommand]
    private void SavePacketLogs()
    {
        _fileDialog.SaveText(_allPacketLogs);
    }

    [RelayCommand]
    private void ClearFilters()
    {
        _allPacketFilters.ForEach(f => f.IsChecked = false);
    }

    [RelayCommand]
    private void ClearPacketLogs()
    {
        _allPacketLogs.Clear();
        FilteredPacketLogs.Clear();
    }

    private void ToggleLogger()
    {
        if (_isReceivingPackets)
            _flash.FlashCall += LogPackets;
        else
            _flash.FlashCall -= LogPackets;
    }

    private bool _filterEnabled
    {
        get
        {
            foreach (PacketLogFilterViewModel filter in _allPacketFilters)
            {
                if (!filter.IsChecked)
                    return true;
            }
            return false;
        }
    }

    private void LogPackets(string function, object[] args)
    {
        if (function != "packet")
            return;

        string packetText = args[0].ToString()!;

        if (!_filterEnabled)
        {
            AddPacket(packetText);
            return;
        }

        string[] packet = packetText.Split(new[] { '%' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (PacketLogFilterViewModel filterVM in _allPacketFilters)
        {
            if (!filterVM.IsChecked && filterVM.Filter.Invoke(packet))
                return;
        }

        AddPacket(packetText);
    }

    partial void OnPacketSearchTextChanged(string value)
    {
        ApplyPacketSearch();
    }

    partial void OnFilterSearchTextChanged(string value)
    {
        ApplyFilterSearch();
    }

    private void AddPacket(string packetText)
    {
        _allPacketLogs.Add(packetText);
        if (MatchesPacketSearch(packetText))
            FilteredPacketLogs.Add(packetText);
    }

    private void ApplyPacketSearch()
    {
        IEnumerable<string> filtered = _allPacketLogs.Where(MatchesPacketSearch);
        FilteredPacketLogs = new ObservableCollection<string>(filtered);
    }

    private void ApplyFilterSearch()
    {
        VisiblePacketFilters = _allPacketFilters.Where(MatchesFilterSearch).ToList();
    }

    private bool MatchesPacketSearch(string packet)
    {
        return string.IsNullOrWhiteSpace(PacketSearchText)
            || packet.Contains(PacketSearchText, StringComparison.OrdinalIgnoreCase);
    }

    private bool MatchesFilterSearch(PacketLogFilterViewModel filter)
    {
        return string.IsNullOrWhiteSpace(FilterSearchText)
            || filter.Content.Contains(FilterSearchText, StringComparison.OrdinalIgnoreCase);
    }
}
