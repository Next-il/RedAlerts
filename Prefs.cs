using CounterStrikeSharp.API.Core;
using PlayerSettings;

namespace RedAlerts;

public class Preferences
{
	private static ISettingsApi? settings;
	public static void SetSettingApi(ISettingsApi _settings)
	{
		settings = _settings;
	}

	public static bool GetPlayerPreference(CCSPlayerController player)
	{
		if (settings == null)
			return true;

		bool GetPlayerPreference = settings.GetPlayerSettingsValue(player, "show-alerts", "true") == "true";
		return GetPlayerPreference;
	}

	public static void SetPlayerPreference(CCSPlayerController player, bool value)
	{
		if (settings == null)
			return;

		settings.SetPlayerSettingsValue(player, "show-alerts", value ? "true" : "false");
	}
}
