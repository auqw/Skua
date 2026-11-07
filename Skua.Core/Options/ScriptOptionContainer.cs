using Skua.Core.Interfaces;
using Skua.Core.Models;

namespace Skua.Core.Options;

public class ScriptOptionContainer : OptionContainer, IScriptOptionContainer
{
    public ScriptOptionContainer(IDialogService dialogService)
        : base(dialogService) { }

    public string Storage { get; set; } = "default";

    public override string OptionsFile => Path.Combine(ClientFileSources.SkuaOptionsDIR, $"{Storage}.cfg");

    private bool _resetsReviewed;

    /// <summary>
    /// Shows the options window. After a window that showed reset choices, the next call is skipped so a script that
    /// opens its own options window at start doesn't show it a second time.
    /// </summary>
    public override void Configure()
    {
        if (_resetsReviewed)
        {
            _resetsReviewed = false;
            return;
        }

        base.Configure();
        _resetsReviewed = ResetOptions.Count > 0;
    }
}