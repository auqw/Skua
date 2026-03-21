using CommunityToolkit.Mvvm.Input;
using Avalonia.Threading;
using Skua.Core.Interfaces;
using Skua.Core.Utils;

namespace Skua.Shared.Avalonia.ViewModels;

public class ChangeLogsViewModel : BotControlViewModelBase
{
    private string _markDownContent = "Loading content...";
    private readonly IProcessService _processService;

    public ChangeLogsViewModel(IProcessService processService) : base("Change Logs", 460, 500)
    {
        _processService = processService;
        _markDownContent = string.Empty;

        Task.Run(async () => await GetChangeLogsContent());

        OpenDonationLink = new RelayCommand(OpenDonation);
        NavigateCommand = new RelayCommand<string>(NavigateToUrl);
    }

    public IRelayCommand OpenDonationLink { get; }
    public IRelayCommand NavigateCommand { get; }

    public string MarkdownDoc
    {
        get => _markDownContent; set => SetProperty(ref _markDownContent, value);
    }

    private async Task GetChangeLogsContent()
    {
        string markdown;
        try
        {
            markdown = await ValidatedHttpExtensions.GetStringAsync(HttpClients.GitHubRaw, "auqw/Skua/refs/heads/master/changelogs.md").ConfigureAwait(false);
        }
        catch
        {
            markdown = "### No content found. Please check your internet connection.";
        }

        Dispatcher.UIThread.Post(() => MarkdownDoc = markdown);
    }

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

    private void OpenDonation()
    {
        try
        {
            _processService.OpenLink("https://ko-fi.com/sharpthenightmare");
        }
        catch
        {
            /* ignored */
        }
    }
}
