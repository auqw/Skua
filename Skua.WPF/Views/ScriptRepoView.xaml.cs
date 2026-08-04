using Skua.Core.ViewModels;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Skua.WPF.Views;

/// <summary>
/// Interaction logic for ScriptRepoView.xaml
/// </summary>
public partial class ScriptRepoView : UserControl
{
    private ICollectionView? _collectionView;

    public ScriptRepoView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is ScriptRepoViewModel vm)
        {
            _collectionView = CollectionViewSource.GetDefaultView(vm.Scripts);
        }
    }

    private bool Search(object obj)
    {
        string searchText = SearchBox.Text?.Trim() ?? string.Empty;
        if (searchText.Length == 0)
            return true;

        if (obj is not ScriptInfoViewModel script)
            return false;

        if (script.Info is not { } info)
            return false;

        if (info.Name?.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        if (info.Tags is null)
            return false;

        foreach (string? tag in info.Tags)
        {
            if (tag?.IndexOf(searchText,StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }

    private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_collectionView is null)
            return;

        _collectionView.Filter = Search;
        _collectionView.Refresh();
    }
}