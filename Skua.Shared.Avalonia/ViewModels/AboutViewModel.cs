using CommunityToolkit.Mvvm.Input;
using Avalonia.Threading;
using Skua.Core.Interfaces;
using Skua.Core.Utils;

namespace Skua.Shared.Avalonia.ViewModels;

public class AboutViewModel : BotControlViewModelBase
{
    private string _markDownContent = "Loading content...";

    public AboutViewModel(IProcessService processService) : base("About")
    {
        _processService = processService;
        _markDownContent = string.Empty;

        Task.Run(async () => await GetAboutContent());

        NavigateCommand = new RelayCommand<string>(NavigateToUrl);
    }

    private readonly IProcessService _processService;

    public string MarkdownDoc
    {
        get => _markDownContent; set => SetProperty(ref _markDownContent, value);
    }

    public IRelayCommand NavigateCommand { get; }

    private void NavigateToUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return;

        try
        {
            if (url.StartsWith("./"))
            {
                _processService.OpenLink($"https://github.com/auqw/Skua/blob/master/{url.Substring(2)}");
            }
            else
            {
                _processService.OpenLink(url);
            }
        }
        catch
        {
            /* ignored */
        }
    }

    private async Task GetAboutContent()
    {
        string markdown;
        try
        {
            markdown = await ValidatedHttpExtensions.GetStringAsync(HttpClients.GitHubRaw, "auqw/Skua/refs/heads/master/readme.md").ConfigureAwait(false);
        }
        catch
        {
            markdown = "### No content found. Please check your internet connection.";
        }

        Dispatcher.UIThread.Post(() => MarkdownDoc = markdown);
    }
}
