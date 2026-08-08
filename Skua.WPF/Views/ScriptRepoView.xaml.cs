using Skua.Core.ViewModels;
using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace Skua.WPF.Views;

public partial class ScriptRepoView : UserControl
{
    private ListCollectionView? _listView;
    private System.Threading.Timer? _debounceTimer;
    private readonly object _syncLock = new();
    private SearchScope _currentScope = SearchScope.All;
    private string _searchText = string.Empty;

    private enum SearchScope
    {
        All,
        Name,
        Tag,
        Desc
    }

    private sealed class SearchComparer : IComparer
    {
        private readonly string _query;
        private readonly SearchScope _scope;

        public SearchComparer(string query, SearchScope scope)
        {
            _query = query;
            _scope = scope;
        }

        public int Compare(object? x, object? y)
        {
            if (ReferenceEquals(x, y))
                return 0;
            if (x is not ScriptInfoViewModel a || y is not ScriptInfoViewModel b)
                return 0;

            int rankA = Rank(a);
            int rankB = Rank(b);

            if (rankA != rankB)
                return rankA.CompareTo(rankB);

            return string.Compare(a.FileName, b.FileName, StringComparison.OrdinalIgnoreCase);
        }

        private int Rank(ScriptInfoViewModel item)
        {
            if (string.IsNullOrWhiteSpace(_query))
                return 0;

            if (_scope is SearchScope.Tag)
            {
                foreach (string tag in item.InfoTags)
                {
                    if (string.Equals(tag, _query, StringComparison.OrdinalIgnoreCase))
                        return -1;
                    if (tag.StartsWith(_query, StringComparison.OrdinalIgnoreCase))
                        return 0;
                    if (tag.Contains(_query, StringComparison.OrdinalIgnoreCase))
                        return 1;
                }
                return 2;
            }

            if (item.Info.Name?.Contains(_query, StringComparison.OrdinalIgnoreCase) == true)
                return -1;
            if (item.Info.Description?.Contains(_query, StringComparison.OrdinalIgnoreCase) == true)
                return 0;
            return 1;
        }
    }

        public ScriptRepoView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Unloaded += ScriptRepoView_Unloaded;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is ScriptRepoViewModel vm)
        {
            BindingOperations.EnableCollectionSynchronization(vm.Scripts, _syncLock);
            ScriptsDataGrid.ItemsSource = vm.Scripts;
            _listView = CollectionViewSource.GetDefaultView(vm.Scripts) as ListCollectionView;
            ApplySearchView();
        }
    }

    private void ScriptRepoView_Unloaded(object sender, RoutedEventArgs e)
    {
        _debounceTimer?.Dispose();
        _debounceTimer = null;
    }

    private void SearchScopeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _currentScope = SearchScopeCombo.SelectedIndex switch
        {
            1 => SearchScope.Name,
            2 => SearchScope.Tag,
            3 => SearchScope.Desc,
            _ => SearchScope.All
        };

        ApplySearchView();
    }

    private bool Search(object obj)
    {
        if (string.IsNullOrWhiteSpace(_searchText))
            return true;

        if (obj is not ScriptInfoViewModel script)
            return false;

        if (_currentScope is SearchScope.All or SearchScope.Name)
        {
            if (script.Info.Name?.Contains(_searchText, StringComparison.OrdinalIgnoreCase) == true)
                return true;
        }

        if (_currentScope is SearchScope.All or SearchScope.Desc)
        {
            if (script.Info.Description?.Contains(_searchText, StringComparison.OrdinalIgnoreCase) == true)
                return true;
        }

        if (_currentScope is SearchScope.All or SearchScope.Tag)
        {
            return script.InfoTags.Any(tag => tag.Contains(_searchText, StringComparison.OrdinalIgnoreCase));
        }

        return false;
    }

    private void ApplySearchView()
    {
        if (_listView is null)
            return;

        _listView.Filter = Search;
        _listView.CustomSort = string.IsNullOrWhiteSpace(_searchText)
            ? null
            : new SearchComparer(_searchText, _currentScope);
        _listView.Refresh();
    }

    private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_listView is null)
            return;

        _debounceTimer?.Change(System.Threading.Timeout.Infinite, 0);
        _debounceTimer = new System.Threading.Timer(_ =>
        {
            Dispatcher.Invoke(() =>
            {
                _searchText = SearchBox.Text ?? string.Empty;
                ApplySearchView();
            });
        }, null, 250, System.Threading.Timeout.Infinite);
    }

    private void ScriptsDataGrid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject dep)
            return;

        DependencyObject? current = dep;

        while (current != null && current is not DataGridRow)
            current = VisualTreeHelper.GetParent(current);

        if (current is not DataGridRow row)
            return;

        if (!row.IsSelected)
            row.IsSelected = true;

        e.Handled = false;
    }
}
