using CommunityToolkit.Mvvm.Messaging.Messages;
using Skua.Core.Models.GitHub;
using Skua.Core.ViewModels;
using Skua.Core.ViewModels.Manager;

namespace Skua.Core.Messaging;

public sealed record CheckClientUpdateMessage();
public sealed record DownloadClientUpdateMessage(UpdateInfo UpdateInfo);
public sealed record UpdateScriptsMessage(bool Reset);

public sealed class UpdateStartedMessage : AsyncRequestMessage<bool>
{ }

public sealed record UpdateFinishedMessage();
public sealed record ClearPasswordBoxMessage();
public sealed record RemoveAccountMessage(AccountItemViewModel Account);
public sealed record AccountSelectedMessage(bool Add);
public sealed record AddAccountToGroupMessage(AccountItemViewModel Account);
public sealed record AddTagsMessage(AccountItemViewModel Account);
public sealed record StartAccountMessage(AccountItemViewModel Account, bool WithScript);
public sealed record RemoveGroupMessage(GroupItemViewModel Group);
public sealed record StartGroupMessage(GroupItemViewModel Group, bool WithScript);
public sealed record RenameGroupMessage(GroupItemViewModel Group);
public sealed record RemoveAccountFromGroupMessage(GroupItemViewModel Group, AccountItemViewModel Account);
public sealed record RefreshAccountDisplayNamesMessage();
