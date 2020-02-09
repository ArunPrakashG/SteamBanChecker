using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SteamBanChecker
{
	public class AccountParser
	{
		private const string ACCOUNTS_PATH = "accounts.txt";
		private const string ACCOUNT_DATA_PATH = "accountinfo.csv";
		private const string PROFILE_LINK_ONLY_PATH = "profiles.csv";
		private const string BAN_STATUS_PATH = "banstatus.csv";
		private const string BAN_STATUS_RAW_PATH = "banstatusraw.csv";
		private const char APPEND_DELIMITER = ',';
		private const char DELIMITER = ':';
		private static readonly SemaphoreSlim WriteSync = new SemaphoreSlim(1, 1);

		public Dictionary<string, string>? GetAccounts()
		{
			if (!File.Exists(ACCOUNTS_PATH))
			{
				Program.Log("Accounts file doesn't exist.");
				return null;
			}

			string[] contents = File.ReadAllLines(ACCOUNTS_PATH);
			Dictionary<string, string> accounts = new Dictionary<string, string>();

			if (contents == null || contents.Length <= 0)
			{
				Program.Log("File doesn't exist or is empty.");
				return null;
			}

			foreach (var acc in contents)
			{
				if (string.IsNullOrEmpty(acc))
				{
					continue;
				}

				if (acc.Contains(DELIMITER))
				{
					var accInfo = acc.Trim().Split(DELIMITER);

					if (accInfo.Length == 2)
					{
						try
						{
							accounts.Add(accInfo[0], accInfo[1]);
							Program.Log($"Parsed {acc} ...");
						}
						catch (ArgumentException)
						{
							Program.Log($"{acc} is repeated; Skipping...");
							continue;
						}						
					}
				}
			}

			if (contents.Length == accounts.Count)
			{
				Program.Log($"All {accounts.Count} accounts have been parsed...");
			}

			return accounts;
		}

		public async Task Write(string? steamId, string? steamPass, ulong steam64, string? profileUrl)
		{
			if(string.IsNullOrEmpty(steamId) || string.IsNullOrEmpty(steamPass) || steam64 <= 0 || string.IsNullOrEmpty(profileUrl))
			{
				return;
			}

			await WriteSync.WaitAsync().ConfigureAwait(false);

			try
			{
				await File.AppendAllTextAsync(ACCOUNT_DATA_PATH, $"{steamId}{APPEND_DELIMITER}{steamPass}{APPEND_DELIMITER}{steam64}{APPEND_DELIMITER}{profileUrl}\n").ConfigureAwait(false);
				await File.AppendAllTextAsync(PROFILE_LINK_ONLY_PATH, $"{steamId}{APPEND_DELIMITER}{steam64}\n").ConfigureAwait(false);
			}
			catch(Exception e)
			{
				Program.Log(e.ToString());
				return;
			}
			finally
			{
				WriteSync.Release();
			}
		}

		public async Task WriteBanStatus(BanStatusStructure.Player accStatus)
		{
			if(accStatus == null)
			{
				return;
			}

			await WriteSync.WaitAsync().ConfigureAwait(false);

			try
			{
				string format = $"{accStatus.SteamId}{APPEND_DELIMITER}VAC_COUNT-{accStatus.NumberOfVACBans}{APPEND_DELIMITER}GAME_BAN_COUNT-{accStatus.NumberOfGameBans}\n";
				await File.AppendAllTextAsync(BAN_STATUS_PATH, format).ConfigureAwait(false);
				await File.AppendAllTextAsync(BAN_STATUS_RAW_PATH, accStatus.ToString() + Environment.NewLine).ConfigureAwait(false);
			}
			catch(Exception e)
			{
				Program.Log(e.ToString());
				return;
			}
			finally
			{
				WriteSync.Release();
			}
		}
	}
}
