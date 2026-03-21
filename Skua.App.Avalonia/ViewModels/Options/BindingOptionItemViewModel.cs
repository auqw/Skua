using CommunityToolkit.Mvvm.Input;
using Skua.Core.Interfaces;
using Skua.Shared.Avalonia.ViewModels.Options;
using System;
using System.ComponentModel;

namespace Skua.App.Avalonia.ViewModels.Options;

public class BindingOptionItemViewModel<TDisplay, TOptionBindingTarget> : CommandOptionItemViewModel, IDisposable
    where TOptionBindingTarget : class, IOptionDictionary, INotifyPropertyChanged
{
    private readonly string _binding;
    private readonly TOptionBindingTarget _options;
    private bool _disposed;

    public BindingOptionItemViewModel(string content, string binding, TOptionBindingTarget options, IRelayCommand command) : base(content, command, typeof(TDisplay))
    {
        _binding = binding;
        _options = options;
        Value = _options.OptionDictionary[_binding].Invoke();
        _options.PropertyChanged += Option_PropertyChanged;
    }

    public BindingOptionItemViewModel(string content, string tag, string binding, TOptionBindingTarget options, IRelayCommand command) : base(content, tag, command, typeof(TDisplay))
    {
        _binding = binding;
        _options = options;
        Value = _options.OptionDictionary[_binding].Invoke();
        _options.PropertyChanged += Option_PropertyChanged;
    }

    public BindingOptionItemViewModel(string content, string description, string tag, string binding, TOptionBindingTarget options, IRelayCommand command) : base(content, description, tag, command, typeof(TDisplay))
    {
        _binding = binding;
        _options = options;
        Value = _options.OptionDictionary[_binding].Invoke();
        _options.PropertyChanged += Option_PropertyChanged;
    }

    private void Option_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == _binding)
            Value = _options?.OptionDictionary[_binding].Invoke();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _options.PropertyChanged -= Option_PropertyChanged;
        GC.SuppressFinalize(this);
    }

    ~BindingOptionItemViewModel()
    {
        Dispose();
    }
}