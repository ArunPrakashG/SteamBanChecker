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
		public static string API_KEY = ""; // fill this for getting ban status and stuff

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

			List<ulong> steamIds = new List<ulong>();
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
				steamIds.Add(bot.Steam64);
				Program.Log($"{pair.Key} bot process completed!");
			}

			List<BanStatusStructure.Player>? banStatus = await BanStatusGet.GetStatus(steamIds).ConfigureAwait(false);

			if (banStatus != null && banStatus.Count > 0)
			{
				foreach (var stat in banStatus)
				{
					await Parser.WriteBanStatus(stat).ConfigureAwait(false);
					Program.Log($"Ban status saved for {stat.SteamId}");
				}
			}

			Program.Log("All process completed!");
			Program.Log("Press any key to exit...");
			Console.ReadKey();
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
