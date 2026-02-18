using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using Skua.Core.Interfaces;
using Skua.Core.Models.GitHub;
using Skua.Core.Utils;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Skua.Manager.Avalonia.ViewModels;

public class GoalsViewModel : BotControlViewModelBase
{
    public GoalsViewModel()
        : base("Goals")
    {
        OpenPaypalLink = new RelayCommand(() => Ioc.Default.GetRequiredService<IProcessService>().OpenLink("https://www.paypal.com/paypalme/sharpiiee"));
        OpenKofiLink = new RelayCommand(() => Ioc.Default.GetRequiredService<IProcessService>().OpenLink("https://ko-fi.com/sharpthenightmare"));
    }

    public IRelayCommand OpenPaypalLink { get; }
    public IRelayCommand OpenKofiLink { get; }

    protected override void OnActivated()
    {
        if (Goals.Count == 0)
            GetGoals();
    }

    private async Task GetGoals()
    {
        try
        {
            string content = await ValidatedHttpExtensions.GetStringAsync(HttpClients.GitHubRaw, "auqw/Skua/refs/heads/master/goals");
            List<GoalObject>? goals = JsonConvert.DeserializeObject<List<GoalObject>>(content);

            if (goals is null || goals.Count == 0)
            {
                Status = "Failed to parse data.";
                return;
            }

            Goals.AddRange(goals);
        }
        catch
        {
            Status = "Failed to fetch data.";
        }
    }

    private string _status = "Loading...";

    public string Status
    {
        get => _status; set => SetProperty(ref _status, value);
    }

    public RangedObservableCollection<GoalObject> Goals { get; } = new();
}