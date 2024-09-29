using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Core;
using System.Text.Json;
using PlayerSettings;

namespace RedAlerts;

[MinimumApiVersion(246)]
public partial class RedAlerts : BasePlugin
{
	public override string ModuleAuthor => "ShiNxz";
	public override string ModuleName => "RedAlerts (Pakar)";
	public override string ModuleVersion => "v1.0.0";

	public static RedAlerts Instance { get; private set; } = new();

	private static ApiResponse? CurrentAlert = null;
	// Settings
	private ISettingsApi? _settings;
	private readonly PluginCapability<ISettingsApi?> _settingsCapability = new("settings:nfcore");

	// CancellationTokenSource for stopping the interval task
	private CancellationTokenSource? _cancellationTokenSource;

	public override void Load(bool hotReload)
	{
		base.Load(hotReload);
		Instance = this;
		StartApiPollingTask();
		RegisterFakeConVars(typeof(ConVars));

		RegisterListener<Listeners.OnTick>(OnTick);
	}

	public override void Unload(bool hotReload)
	{
		base.Unload(hotReload);
		StopApiPollingTask();
	}

	public override void OnAllPluginsLoaded(bool hotReload)
	{
		_settings = _settingsCapability.Get();
		if (_settings == null) Console.WriteLine("[RedAlerts] PlayerSettings core not found...");
		else Preferences.SetSettingApi(_settings);
	}

	// Commands can also be registered using the `Command` attribute.
	[ConsoleCommand("css_alerts", "Toggle Pakar alerts")]
	public void OnToggleAlerts(CCSPlayerController? player, CommandInfo commandInfo)
	{
		if (player == null)
			return;

		bool newValue = !Preferences.GetPlayerPreference(player);
		Preferences.SetPlayerPreference(player, newValue);
		player.PrintToCenterAlert(newValue ? "RedAlerts Enabled!" : "RedAlerts Disabled.");
	}

	private void StartApiPollingTask()
	{
		_cancellationTokenSource = new CancellationTokenSource();
		Task.Run(() => PollApiAsync(_cancellationTokenSource.Token));
	}

	private void StopApiPollingTask()
	{
		if (_cancellationTokenSource != null)
		{
			_cancellationTokenSource.Cancel();
			_cancellationTokenSource.Dispose();
			_cancellationTokenSource = null;
		}
	}

	private async Task PollApiAsync(CancellationToken cancellationToken)
	{
		var interval = TimeSpan.FromSeconds(ConVars.IntervalSecondsCvar.Value);
		using var httpClient = new HttpClient();

		while (!cancellationToken.IsCancellationRequested)
		{
			try
			{
				var request = new HttpRequestMessage(HttpMethod.Get, "https://www.oref.org.il/WarningMessages/alert/alerts.json");

				var response = await httpClient.SendAsync(request, cancellationToken);
				response.EnsureSuccessStatusCode();

				var content = await response.Content.ReadAsStringAsync(cancellationToken);

				ApiResponse? apiResponse;

				if (string.IsNullOrWhiteSpace(content))
				{
					// Handle empty response
					apiResponse = null;
				}
				else
				{
					apiResponse = JsonSerializer.Deserialize<ApiResponse>(content);
				}

				if (apiResponse != null && apiResponse.Data != null && apiResponse.Data.Length > 0)
				{
#if DEBUG
					Console.WriteLine("[RedAlerts] DemoFunction called with data:");
					Console.WriteLine($"ID: {apiResponse.Id}");
					Console.WriteLine($"Category: {apiResponse.Cat}");
					Console.WriteLine($"Title: {apiResponse.Title}");
					Console.WriteLine($"Description: {apiResponse.Desc}");
					Console.WriteLine("Data:");

					foreach (var item in apiResponse.Data)
					{
						Console.WriteLine(item);
					}
#endif

					// Print to chat
					CurrentAlert = apiResponse;
					ChatAll();
				}
				else
				{
					Console.WriteLine("[RedAlerts] No alerts available.");
					CurrentAlert = null;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[RedAlerts] Error polling API: {ex.Message}");
			}

			await Task.Delay(interval, cancellationToken);
		}
	}

	public void OnTick()
	{
		// Check if there is an alert to show
		if (CurrentAlert == null || CurrentAlert.Data.Length == 0)
		{
			return;
		}

		// Show an alert to all players
		List<CCSPlayerController> players = Helper.GetAllController();

		players.ForEach(player =>
		{
			if (player == null || !player.IsValid || player.IsBot)
			{
				return;
			}

			// Check if the player has enabled alerts
			if (!Preferences.GetPlayerPreference(player))
			{
				return;
			}

			string cities = string.Join(", ", CurrentAlert.Data);

			player.PrintToCenterHtml(
				$"<pre>" +
				$"<strong class='fontSize-m fontWeight-Bold' color='red'>{CurrentAlert.Title}</strong><br>" +
				$"<span class='fontSize-sm' color='white'>{cities}</p>" +
				"<br><br>" +
				$"<span class='fontSize-s' color='#ff8f78'>/alerts • לביטול התרעות</span>" +
				"</pre>"
			);
		});
	}

	public void ChatAll()
	{
		if (CurrentAlert == null)
		{
			return;
		}

		List<CCSPlayerController> players = Helper.GetAllController();

		players.ForEach(player =>
		{
			if (player == null || !player.IsValid || player.IsBot)
			{
				return;
			}

			// Check if the player has enabled alerts
			if (!Preferences.GetPlayerPreference(player))
			{
				return;
			}

			string cities = string.Join(", ", CurrentAlert.Data);
			Console.WriteLine($"[RedAlerts] Showing alert to {player.PlayerName}: {CurrentAlert.Title} - {cities}");

			player.PrintToChat(
				$" \u2029 {ChatColors.Red}➖➖➖➖➖➖➖➖➖➖➖➖➖➖➖➖➖ • Red Alert • ➖➖➖➖➖➖➖➖➖➖➖➖➖➖➖➖➖➖{ChatColors.Default} \u2029 {ChatColors.Orange}{CurrentAlert.Title}:{ChatColors.Default}\u2029{cities}\u2029{ChatColors.Grey}/alerts • לביטול התרעות{ChatColors.Red}\u2029➖➖➖➖➖➖➖➖➖➖➖➖➖➖➖➖➖➖➖➖➖➖➖➖➖➖➖➖➖➖➖➖➖➖➖➖➖➖➖➖➖{ChatColors.Default}\u2029"
			);
		});
	}

#if DEBUG
	[ConsoleCommand("css_alerttest", "Test Pakar alerts")]
	public void OnTestAlerts(CCSPlayerController? player, CommandInfo commandInfo)
	{
		ApiResponse apiResponse = new()
		{
			Id = "133719493820000000",
			Cat = "1",
			Title = "ירי רקטות וטילים",
			Data = ["צפת - עיר", "צפת - נוף כנרת", "צפת - נוף רמת הגולן"],
			Desc = "היכנסו למרחב המוגן ושהו בו 10 דקות"
		};

		CurrentAlert = apiResponse;
		ChatAll();
	}
#endif
}

public class ApiResponse
{
	public string Id { get; set; } = string.Empty;
	public string Cat { get; set; } = string.Empty;
	public string Title { get; set; } = string.Empty;
	public string[] Data { get; set; } = [];
	public string Desc { get; set; } = string.Empty;
}
