using Skua.Core.ViewModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace Skua.WPF.Views;

public partial class ScriptRepoView : UserControl
{
    private ListCollectionView? _listView;
    private readonly DispatcherTimer _searchDebounceTimer;
    private readonly object _syncLock = new();
    private SearchScope _currentScope = SearchScope.All;
    private string _searchText = string.Empty;

    private enum SearchScope
    {
        All,
        Name,
        Tag,
        Desc,
        File
    }

    private sealed class SearchComparer : IComparer
    {
        private readonly string _query;
        private readonly SearchScope _scope;
        private readonly Dictionary<ScriptInfoViewModel, int> _rankCache = new();

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

            int rankA = GetRank(a);
            int rankB = GetRank(b);

            if (rankA != rankB)
                return rankA.CompareTo(rankB);

            return string.Compare(a.FileName, b.FileName, StringComparison.OrdinalIgnoreCase);
        }

        private int GetRank(ScriptInfoViewModel item)
        {
            if (_rankCache.TryGetValue(item, out int rank))
                return rank;

            rank = Rank(item);
            _rankCache[item] = rank;
            return rank;
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

            if (_scope is SearchScope.File)
            {
                string file = item.ScriptPathFromScriptsDir ?? string.Empty;

                if (string.Equals(file, _query, StringComparison.OrdinalIgnoreCase))
                    return -1;

                if (file.StartsWith(_query, StringComparison.OrdinalIgnoreCase))
                    return 0;

                if (file.Contains(_query, StringComparison.OrdinalIgnoreCase))
                    return 1;

                return 2;
            }

            if (item.Info.Name?.Contains(_query, StringComparison.OrdinalIgnoreCase) == true)
                return -1;

            if (item.Info.Description?.Contains(_query, StringComparison.OrdinalIgnoreCase) == true)
                return 0;

            if (_scope is SearchScope.All && item.ScriptPathFromScriptsDir?.Contains(_query, StringComparison.OrdinalIgnoreCase) == true)
                return 1;

            return 2;
        }
    }
    public ScriptRepoView()
    {
        InitializeComponent();

        _searchDebounceTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };

        Loaded += ScriptRepoView_Loaded;
        DataContextChanged += OnDataContextChanged;
        Unloaded += ScriptRepoView_Unloaded;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is ScriptRepoViewModel vm)
        {
            BindingOperations.EnableCollectionSynchronization(vm.Scripts, _syncLock);
            ScriptsList.ItemsSource = vm.Scripts;
            _listView = CollectionViewSource.GetDefaultView(vm.Scripts) as ListCollectionView;
            ApplySearchView();
        }
    }

    private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchText = SearchBox.Text ?? string.Empty;

        if (_listView is null)
            return;

        _searchDebounceTimer.Stop();
        _searchDebounceTimer.Start();
    }

    private void SearchDebounceTimer_Tick(object? sender, EventArgs e)
    {
        _searchDebounceTimer.Stop();
        ApplySearchView();
    }

    private void ScriptRepoView_Loaded(object sender, RoutedEventArgs e)
    {
        _searchDebounceTimer.Tick -= SearchDebounceTimer_Tick;
        _searchDebounceTimer.Tick += SearchDebounceTimer_Tick;
    }

    private void ScriptRepoView_Unloaded(object sender, RoutedEventArgs e)
    {
        _searchDebounceTimer.Stop();
        _searchDebounceTimer.Tick -= SearchDebounceTimer_Tick;
    }

    private void SearchScopeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _currentScope = SearchScopeCombo.SelectedIndex switch
        {
            1 => SearchScope.Name,
            2 => SearchScope.Tag,
            3 => SearchScope.Desc,
            4 => SearchScope.File,
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
            foreach (string tag in script.InfoTags)
            {
                if (tag.Contains(_searchText, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        if (_currentScope is SearchScope.All or SearchScope.File)
        {
            if (script.ScriptPathFromScriptsDir?.Contains(_searchText, StringComparison.OrdinalIgnoreCase) == true)
                return true;
        }

        return false;
    }

    private void ApplySearchView()
    {
        if (_listView is null)
            return;

        using (_listView.DeferRefresh())
        {
            if (string.IsNullOrWhiteSpace(_searchText))
            {
                _listView.Filter = null;
                _listView.CustomSort = null;
                return;
            }

            _listView.Filter = Search;
            _listView.CustomSort = new SearchComparer(_searchText, _currentScope);
        }
    }

    private void ScriptsList_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject dep)
            return;

        DependencyObject? current = dep;

        while (current != null && current is not ListBoxItem)
            current = VisualTreeHelper.GetParent(current);

        if (current is not ListBoxItem item)
            return;

        if (!item.IsSelected)
            item.IsSelected = true;

        e.Handled = false;
    }
}
