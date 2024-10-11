using CounterStrikeSharp.API.Modules.Cvars;

namespace RedAlerts;

public static class ConVars
{
	public static FakeConVar<int> IntervalSecondsCvar = new("alerts_interval", "Interval in seconds to send the request", 2);
}
