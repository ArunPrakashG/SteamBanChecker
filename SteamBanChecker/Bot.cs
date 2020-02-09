using SteamKit2;
using System;

namespace SteamBanChecker
{
    public class Bot
    {
        private SteamClient? SteamClient;
        private CallbackManager? Manager;
        private SteamUser? SteamUser;
        public ulong Steam64 = 0;
        public string? VanityUrl;

        public bool IsRunning { get; private set; }
        public bool IsCompleted { get; private set; }
        private (string steamID, string steamPass) Account;

        public Bot(string? steamId, string? steamPass)
        {
            if (string.IsNullOrEmpty(steamId) || string.IsNullOrEmpty(steamPass))
            {
                Program.Log("Steam id or pass is invalid or null.");
                return;
            }

            Account = (steamId, steamPass);
        }

        public void InitClient()
        {
            SteamClient = new SteamClient();
            Manager = new CallbackManager(SteamClient);
            SteamUser = SteamClient.GetHandler<SteamUser>();
            Manager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
            Manager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);
            Manager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);
            Manager.Subscribe<SteamUser.LoggedOffCallback>(OnLoggedOff);
            IsRunning = true;

            Program.Log("Connecting to Steam...");
            SteamClient.Connect();

            while (IsRunning)
            {
                Manager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
            }
        }

        private void OnConnected(SteamClient.ConnectedCallback callback)
        {
            Program.Log($"Connected to Steam! Logging in as {Account.steamID}");

            SteamUser?.LogOn(new SteamUser.LogOnDetails
            {
                Username = Account.steamID,
                Password = Account.steamPass
            });
        }

        private void OnDisconnected(SteamClient.DisconnectedCallback callback)
        {
            Program.Log("Disconnected from Steam");
            IsRunning = false;
        }

        private void OnLoggedOn(SteamUser.LoggedOnCallback callback)
        {
            switch (callback.Result)
            {
                case EResult.AccountDisabled:
                    Program.Log($"{Account.steamID} account is suspended by steam.");
                    Steam64 = 11;
                    VanityUrl = "SUSPENDED!";
                    Disconnect();
                    break;
                case EResult.InvalidPassword:
                    Program.Log($"{Account.steamID} password is incorrect.");
                    Steam64 = 11;
                    VanityUrl = "INVALID PASSWORD!";
                    Disconnect();
                    return;
                case EResult.AccountLogonDenied:
                    Program.Log($"{Account.steamID} has SteamGuard enabled.");
                    Steam64 = 11;
                    VanityUrl = "STEAM GUARD!";
                    Disconnect();
                    break;
                case EResult.AccountLoginDeniedNeedTwoFactor:
                    Program.Log($"{Account.steamID} has mobile authenticator enabled.");
                    Steam64 = 11;
                    VanityUrl = "MOBILE AUTHENTICATOR!";
                    Disconnect();
                    break;
                case EResult.OK:
                    Program.Log($"{Account.steamID} logged on successfully!");

                    if (SteamUser != null)
                    {
                        Steam64 = SteamUser.SteamID.ConvertToUInt64();
                        Program.Log($"{Account.steamID} -> {Steam64}");
                        VanityUrl = $"https://www.steamcommunity.com/profiles/{Steam64}";                        
                        Program.Log($"{Account.steamID} account has been saved successfully! Logging off...");                       
                        Disconnect();
                    }

                    break;
                case EResult.RateLimitExceeded:
                    Program.Log("Rate limited!; dont login another account for 30 mins to get unblocked by steam.");
                    break;
            }

            if (callback.Result != EResult.OK)
            {
                Program.Log($"Unable to logon to Steam: {callback.Result} / {callback.ExtendedResult}");
                IsRunning = false;
                Disconnect();
                return;
            }
        }

        private void OnLoggedOff(SteamUser.LoggedOffCallback callback)
        {
            Program.Log("Logged off of Steam: " + callback.Result);
        }

        private void Disconnect()
        {
            SteamUser?.LogOff();
            SteamClient?.Disconnect();
            IsCompleted = true;
        }
    }
}
