using Skua.Core.Interfaces;
using Skua.Core.Interfaces.ViewModels;

namespace Skua.Core.Messaging;

public sealed record PluginLoadedMessage(IPluginContainer Container);
public sealed record PluginUnloadedMessage(IPluginContainer Container);

public sealed record AddPluginMenuItemMessage(IMainMenuItemViewModel ViewModel);
public sealed record RemovePluginMenuItemMessage(IMainMenuItemViewModel ViewModel);