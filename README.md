# Steam Ban Checker
Checks 
* Ban status (VAC, Game ban, Economy ban, vac count, game ban count)
* Steam profile link, steam 64 id of the profile.

of the specified account(s) and writes the data into CSV files for easy import into google spreadsheets.

To check ban status, you will have to provide a valid Steam Web Api Key which you can get from here -> https://steamcommunity.com/dev/apikey
(You will have to login with an unrestricted account to get the api key)
There are 2 ways to set the api key:
* In Source: If you have the means to recompile the application from source, Assign the Api key to the API_KEY static variable in Program.cs
* During Runtime: after all accounts have been processed and once all account related data (Steam 64 ID and profile link) has been saved into CSV files, You will be prompted to enter a valid Api Key. Enter the Api key and press Enter to run the ban status checker.

## NOTE 
I DO NOT provide any warrenty or any such thing for this program. You are running this at your own risk. 
Although it has few delays to limit requests send to steam servers, there is always some people to abuse the system. Its in your hands if you are one of them.
