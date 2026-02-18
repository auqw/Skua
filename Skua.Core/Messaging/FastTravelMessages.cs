

using Skua.Core.Interfaces.ViewModels;
using Skua.Core.Models;

namespace Skua.Core.Messaging;

public sealed record RemoveFastTravelMessage(IFastTravelItemViewModel FastTravel);
public sealed record EditFastTravelMessage(IFastTravelItemViewModel FastTravel);