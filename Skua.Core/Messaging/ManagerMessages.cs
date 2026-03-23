using CommunityToolkit.Mvvm.Messaging.Messages;
using Skua.Core.Interfaces.ViewModels;
using Skua.Core.Models.GitHub;

namespace Skua.Core.Messaging;

public sealed record CheckClientUpdateMessage();
public sealed record DownloadClientUpdateMessage(UpdateInfo UpdateInfo);
public sealed record UpdateScriptsMessage(bool Reset);

public sealed class UpdateStartedMessage : AsyncRequestMessage<bool>
{ }

public sealed record UpdateFinishedMessage();
public sealed record ClearPasswordBoxMessage();
public sealed record RemoveAccountMessage(IAccountItemViewModel Account);
public sealed record AccountSelectedMessage(bool Add);
public sealed record AddAccountToGroupMessage(IAccountItemViewModel Account);
public sealed record AddTagsMessage(IAccountItemViewModel Account);
public sealed record StartAccountMessage(IAccountItemViewModel Account, bool WithScript);
public sealed record RemoveGroupMessage(IGroupItemViewModel Group);
public sealed record StartGroupMessage(IGroupItemViewModel Group, bool WithScript);
public sealed record RenameGroupMessage(IGroupItemViewModel Group);
public sealed record RemoveAccountFromGroupMessage(IGroupItemViewModel Group, IAccountItemViewModel Account);
public sealed record ReplaceAccountInGroupMessage(IGroupItemViewModel Group, IAccountItemViewModel CurrentAccount, IAccountItemViewModel ReplacementAccount);
public sealed record RefreshAccountDisplayNamesMessage();
