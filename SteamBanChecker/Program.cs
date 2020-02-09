using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SteamBanChecker
{
	internal class Program
	{
		private static List<Bot> Bots = new List<Bot>();
		private static Dictionary<string, string>? Accounts = new Dictionary<string, string>();
		private static BanStatus BanStatusGet = new BanStatus();
		internal static AccountParser Parser = new AccountParser();
		public static string API_KEY = ""; // fill this for getting ban status and stuff; It will be asked during runtime if u didnt fill and recompile from source.

		private static async Task Main(string[] args)
		{
			Accounts = Parser.GetAccounts();

			if (Accounts == null)
			{
				Program.Log("Accounts returned is empty or null.");
				Program.Log("Press any key to exit.");
				Console.ReadLine();
				Environment.Exit(-1);
			}

			List<(ulong, string)> steamIds = new List<(ulong, string)>();
			foreach (var pair in Accounts)
			{
				if (string.IsNullOrEmpty(pair.Key) || string.IsNullOrEmpty(pair.Value))
				{
					continue;
				}

				Bot bot = new Bot(pair.Key, pair.Value);
				Bots.Add(bot);
				bot.InitClient();
				await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);

				while (!bot.IsCompleted)
				{
					Program.Log("Waiting for bot to finish saving...");
					await Task.Delay(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
				}

				await Parser.Write(pair.Key, pair.Value, bot.Steam64, bot.VanityUrl).ConfigureAwait(false);
				steamIds.Add((bot.Steam64, pair.Key));
				Program.Log($"{pair.Key} bot process completed!");
			}

			List<BanStatusStructure.Player>? banStatus = await BanStatusGet.GetStatus(steamIds).ConfigureAwait(false);

			if (banStatus != null && banStatus.Count > 0)
			{
				foreach (var stat in banStatus)
				{
					await Parser.WriteBanStatus(GetSteamUsername(Convert.ToUInt64(stat.SteamId), steamIds),  stat).ConfigureAwait(false);
					Program.Log($"Ban status saved for {stat.SteamId}");
				}
			}

			Program.Log("All process completed!");
			Program.Log("Press any key to exit...");
			Console.ReadKey();
		}

		private static string GetSteamUsername(ulong targetid, List<(ulong, string)> steamIds)
		{
			if(targetid > 0 && steamIds != null && steamIds.Count > 0)
			{
				foreach(var id in steamIds)
				{
					if(id.Item1 == targetid && !string.IsNullOrEmpty(id.Item2))
					{
						return id.Item2;
					}
				}
			}

			return "Unknown";
		}

		public static void Log(string? msg)
		{
			if (!string.IsNullOrEmpty(msg))
			{
				Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}] {msg}");
			}
		}
	}
}
