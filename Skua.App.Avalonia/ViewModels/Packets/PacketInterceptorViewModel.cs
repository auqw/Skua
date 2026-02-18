using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Skua.Core.Interfaces;
using Skua.Core.Models;
using Skua.Core.Models.Servers;
using Skua.Core.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;

namespace Skua.App.Avalonia.ViewModels.Packets;

public partial class PacketInterceptorViewModel : BotControlViewModelBase
{

    public PacketInterceptorViewModel(IEnumerable<PacketLogFilterViewModel> filters, ICaptureProxy gameProxy, IScriptServers server)
        : base("Packet Interceptor")
    {
        _gameProxy = gameProxy;
        _server = server;
        _packetFilters = filters.ToList();
        ClearPacketsCommand = new RelayCommand(Packets.Clear);
        SynchronizationContext? context = SynchronizationContext.Current;
        void addFunc(InterceptedPacketViewModel st) => context?.Send(obj => Packets.Add((InterceptedPacketViewModel)obj!), st);
        _logger = new InterceptorLogger(addFunc);
        IsLogging = true;
    }

    protected override void OnActivated()
    {
        Messenger.Register<PacketInterceptorViewModel, PropertyChangedMessage<bool>>(this, RunningChanged);
        OnPropertyChanged(nameof(Running));
    }

    private readonly ICaptureProxy _gameProxy;
    private readonly IScriptServers _server;
    private readonly InterceptorLogger _logger;

    [ObservableProperty]
    private Server? _selectedServer;

    [ObservableProperty]
    private RangedObservableCollection<InterceptedPacketViewModel> _packets = new();

    [ObservableProperty]
    private List<PacketLogFilterViewModel> _packetFilters;

    private bool _isLogging;

    public bool IsLogging
    {
        get => _isLogging;
        set
        {
            if (SetProperty(ref _isLogging, value))
            {
                if (value)
                    _gameProxy.Interceptors.Add(_logger);
                else
                    _gameProxy.Interceptors.Remove(_logger);
            }
        }
    }

    ~PacketInterceptorViewModel()
    {
        _logger?.Dispose();
    }

    public bool Running => _gameProxy.Running;
    public List<Server> ServerList => _server.CachedServers;
    public IRelayCommand ClearPacketsCommand { get; }

    [RelayCommand]
    private void ClearFilters()
    {
        _packetFilters.ForEach(f => f.IsChecked = false);
    }

    [RelayCommand]
    private void ConnectInterceptor()
    {
        if (_gameProxy.Running)
        {
            _gameProxy.Stop();
            OnPropertyChanged(nameof(Running));
            return;
        }

        if (SelectedServer is null)
            return;

        IScriptOption options = Ioc.Default.GetRequiredService<IScriptOption>();
        bool relogin = options.AutoRelogin;
        options.AutoRelogin = false;
        IPAddress ip = IPAddress.TryParse((string?)SelectedServer.IP, out IPAddress? addr) ? addr : Dns.GetHostEntry((string)SelectedServer.IP).AddressList[0];
        int port = SelectedServer.Port != 0 ? SelectedServer.Port : 5588;
        _gameProxy.Destination = new IPEndPoint(ip, port);
        _gameProxy.Start();
        _server.Logout();
        _server.Login();
        _server.ConnectIP("127.0.0.1", port);
        OnPropertyChanged(nameof(Running));
        options.AutoRelogin = relogin;
    }

    private void RunningChanged(PacketInterceptorViewModel recipient, PropertyChangedMessage<bool> message)
    {
        if (message.PropertyName == nameof(ICaptureProxy.Running))
            recipient.OnPropertyChanged(nameof(recipient.Running));
    }
}

public class InterceptorLogger : IInterceptor, IDisposable
{
    public const bool LogToFile = false;

    private readonly Action<InterceptedPacketViewModel> _addFunc;
    private readonly StreamWriter _logWriter;
    private readonly object _logLock = new();

    public int Priority => int.MaxValue;

    public InterceptorLogger(Action<InterceptedPacketViewModel> addFunc)
    {
        _addFunc = addFunc;
        if (LogToFile)
        {
            string logPath = Path.Combine(ClientFileSources.SkuaDIR, "InterceptedLogs.txt");
            _logWriter = new StreamWriter(logPath, append: true) { AutoFlush = true };
        }
    }

    public void Intercept(MessageInfo message, bool outbound)
    {
        _addFunc(new(message.Content, message.Send ? outbound : null));

        if (LogToFile)
        {
            lock (_logLock)
            {
                string direction = message.Send ? (outbound ? "OUT" : "IN") : "UNKNOWN";
                _logWriter.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{direction}] {message.Content}");
            }
        }
    }

    public void Dispose()
    {
        if (_logWriter != null)
        {
            _logWriter?.Dispose();
        }
    }
}
