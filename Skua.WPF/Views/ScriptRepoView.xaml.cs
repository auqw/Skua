using Skua.Core.ViewModels;
using System;
using System.Collections.Generic;
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
    private ScriptRepoViewModel? _vm;
    private ICollectionView? _view;

    private IList<ScriptInfoViewModel>? _items;

    // =========================
    // ZERO-ALLOCATION INDEX
    // token -> scripts
    // =========================
    private readonly Dictionary<string, List<ScriptInfoViewModel>> _index = new(StringComparer.OrdinalIgnoreCase);

    // active result cache (fast lookup, no allocations per filter)
    private HashSet<ScriptInfoViewModel>? _activeSet;

    private CancellationTokenSource? _searchCts;

    public ScriptRepoView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    // =========================================================
    // INIT
    // =========================================================
    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is not ScriptRepoViewModel vm)
            return;

        _vm = vm;

        // IMPORTANT: bind once, let WPF own updates
        if (ScriptsDataGrid.ItemsSource != vm.Scripts)
            ScriptsDataGrid.ItemsSource = vm.Scripts;

        _items = vm.Scripts;

        // IMPORTANT: single shared view instance
        _view = CollectionViewSource.GetDefaultView(vm.Scripts);

        // reset filter every time view is recreated
        _view.Filter = null;
    }

    // =========================================================
    // BUILD INVERTED INDEX (ONE TIME)
    // =========================================================
    private void BuildIndex(IList<ScriptInfoViewModel> scripts)
    {
        _index.Clear();

        for (int i = 0; i < scripts.Count; i++)
        {
            ScriptInfoViewModel item = scripts[i];

            AddToIndex(item.Info?.Name, item);
            AddToIndex(item.Info?.Description, item);
            AddToIndex(item.ScriptPath, item);

            if (item.InfoTags == null)
                continue;

            for (int j = 0; j < item.InfoTags.Count; j++)
                AddToIndex(item.InfoTags[j], item);
        }
    }

    private void AddToIndex(string? text, ScriptInfoViewModel item)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        ReadOnlySpan<char> span = text.AsSpan();

        int start = 0;

        for (int i = 0; i <= span.Length; i++)
        {
            if (i != span.Length &&
                span[i] != ' ' && span[i] != '_' && span[i] != '-' &&
                span[i] != '.' && span[i] != '/' && span[i] != '\\')
                continue;

            if (i > start)
            {
                string token = span.Slice(start, i - start).ToString();

                if (!_index.TryGetValue(token, out var list))
                {
                    list = new List<ScriptInfoViewModel>(4);
                    _index[token] = list;
                }

                list.Add(item);
            }

            start = i + 1;
        }
    }

    // =========================================================
    // SEARCH ENTRY (INSTANT, NO TASK.RUN)
    // =========================================================
    private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_view is null || _items is null)
            return;

        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        CancellationToken token = _searchCts.Token;

        string query = SearchBox.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(query))
        {
            _activeSet = null;

            _view.Filter = null;
            _view.Refresh();
            return;
        }

        ApplySearch(query, token);
    }

    // =========================================================
    // SEARCH ENGINE (INDEX POWERED)
    // =========================================================
    private void ApplySearch(string query, CancellationToken token)
    {
        if (_view is null || _items is null)
            return;

        string[] tokens = query.Split(
            new[] { ' ', '_', '-', '.', '/', '\\' },
            StringSplitOptions.RemoveEmptyEntries);

        if (tokens.Length == 0)
        {
            _activeSet = null;
            _view.Filter = null;
            _view.Refresh();
            return;
        }

        HashSet<ScriptInfoViewModel>? result = null;

        for (int i = 0; i < tokens.Length; i++)
        {
            if (token.IsCancellationRequested)
                return;

            string t = tokens[i];

            if (_index.TryGetValue(t, out var list))
            {
                if (result is null)
                {
                    result = new HashSet<ScriptInfoViewModel>(list);
                }
                else
                {
                    result.IntersectWith(list);
                }
            }
            else
            {
                // 🔥 fallback: partial scan instead of failing everything
                result ??= new HashSet<ScriptInfoViewModel>();

                for (int j = 0; j < _items.Count; j++)
                {
                    var item = _items[j];

                    if (ItemContains(item, t))
                        result.Add(item);
                }
            }
        }

        _activeSet = result ?? new HashSet<ScriptInfoViewModel>(_items);

        _view.Filter = FilterPredicate;
        _view.Refresh();
    }

    private static bool ItemContains(ScriptInfoViewModel item, string token)
    {
        if (item.Info?.Name?.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        if (item.Info?.Description?.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        if (item.ScriptPath?.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        if (item.InfoTags == null)
            return false;

        for (int i = 0; i < item.InfoTags.Count; i++)
        {
            if (item.InfoTags[i]?.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }

    private bool FilterPredicate(object item)
    {
        if (_activeSet is null)
            return true;

        return item is ScriptInfoViewModel s && _activeSet.Contains(s);
    }

    // =========================================================
    // RIGHT CLICK FIX
    // =========================================================
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