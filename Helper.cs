using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API;

namespace RedAlerts;

public class Helper
{
	public static List<CCSPlayerController> GetAllController()
	{
		List<CCSPlayerController> playerList = Utilities.FindAllEntitiesByDesignerName<CCSPlayerController>("cs_player_controller").Where(p => p != null && p.IsValid && !p.IsBot && !p.IsHLTV && p.Connected == PlayerConnectedState.PlayerConnected).ToList();
		return playerList;
	}
}
