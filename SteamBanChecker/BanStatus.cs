using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using static SteamBanChecker.BanStatusStructure;

namespace SteamBanChecker
{
	[Serializable]
	public class BanStatusStructure
	{
		public Player[] players { get; set; }

		[Serializable]
		public class Player
		{
			public string? SteamId { get; set; }
			public bool CommunityBanned { get; set; }
			public bool VACBanned { get; set; }
			public int NumberOfVACBans { get; set; }
			public int DaysSinceLastBan { get; set; }
			public int NumberOfGameBans { get; set; }
			public string? EconomyBan { get; set; }

			public override string ToString()
			{
				return $"{SteamId}|COM_BAN:{CommunityBanned}|VAC:{VACBanned}|VAC_COUNT:{NumberOfVACBans}|DAYS_SINCE_BAN:{DaysSinceLastBan}|GAME_BAN_COUNT:{NumberOfGameBans}|ECO_BAN:{EconomyBan}";
		    }
		}
	}

	public class BanStatus
	{
		private List<Player> IsBannedCollection = new List<Player>();
		private static readonly SemaphoreSlim Sync = new SemaphoreSlim(1, 1);
		static HttpClient Client = new HttpClient();

		public async Task<List<Player>?> GetStatus(List<(ulong, string)> steamIds)
		{
			if (steamIds == null || steamIds.Count <= 0)
			{
				Program.Log("Specified steam ids are empty.");
				return null;
			}

			return await Get(steamIds).ConfigureAwait(false);
		}

		private async Task<List<Player>?> Get(List<(ulong, string)> steamIds)
		{
			if (string.IsNullOrEmpty(Program.API_KEY))
			{
				Program.Log("No api key specified. Please input your api key -> ");
				Program.API_KEY = Console.ReadLine().Trim();
			}

			string requestUrl = $"https://api.steampowered.com/ISteamUser/GetPlayerBans/v1/?key={Program.API_KEY}&steamids=";

			await Sync.WaitAsync().ConfigureAwait(false);

			try
			{
				foreach (var id in steamIds)
				{
					if (id.Item1 <= 0 || string.IsNullOrEmpty(id.Item2))
					{
						continue;
					}

					requestUrl = requestUrl + id.Item1.ToString() + ",";
				}
				
				Program.Log($"Requesting --> {requestUrl}");
				var response = await Client.GetAsync(requestUrl).ConfigureAwait(false);
				string? json = null;

				if (response.StatusCode == HttpStatusCode.OK)
				{
					Program.Log("Request success!");
					json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
				}
				else
				{
					Program.Log("Request failed. recheck the api key you entered!");
					return null;
				}

				BanStatusStructure structure = JsonConvert.DeserializeObject<BanStatusStructure>(json);

				if (structure != null && structure.players != null && structure.players.Length > 0)
				{
					IsBannedCollection = structure.players.ToList();
					return IsBannedCollection;
				}

				return null;
			}
			catch (Exception e)
			{
				Program.Log(e.ToString());
				return null;
			}
			finally
			{
				Sync.Release();
			}
		}
	}
}
