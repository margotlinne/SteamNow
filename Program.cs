using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;

class Program
{
    static async Task Main()
    {
        #region 1. Steam
        string steamApiKey = Environment.GetEnvironmentVariable("STEAM_API_KEY");
        string steamId = Environment.GetEnvironmentVariable("STEAM_ID");

        string steamUrl =
            $"https://api.steampowered.com/IPlayerService/GetOwnedGames/v0001/?key={steamApiKey}&steamid={steamId}&format=json&include_appinfo=true&include_played_free_games=true";

        using HttpClient client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("SteamNow/1.0");

        // Get owned games on Steam 
        var steamResponse = await client.GetStringAsync(steamUrl);
        using JsonDocument steamDoc = JsonDocument.Parse(steamResponse);
        var steamRoot = steamDoc.RootElement.GetProperty("response");

        string userGameInfo = "No recently played games.";

        if (steamRoot.TryGetProperty("games", out JsonElement games) && games.GetArrayLength() > 0)
        {
            var recentGame = games.EnumerateArray()
                .Where(g => g.TryGetProperty("rtime_last_played", out _))
                .OrderByDescending(g => g.GetProperty("rtime_last_played").GetInt64())
                .FirstOrDefault();

            var mostPlayedGame = games.EnumerateArray()
                .OrderByDescending(g => g.GetProperty("playtime_forever").GetInt32())
                .FirstOrDefault();

            string GetGameInfo(JsonElement game)
            {
                if (game.ValueKind == JsonValueKind.Undefined) return "No Game";

                string name = game.GetProperty("name").GetString();
                int playtime = game.GetProperty("playtime_forever").GetInt32();
                int hours = playtime / 60;
                int minutes = playtime % 60;

                long lastPlayedUnix = 0;
                string lastPlayedStr = "Unknown";
                if (game.TryGetProperty("rtime_last_played", out JsonElement lastPlayedElem))
                {
                    lastPlayedUnix = lastPlayedElem.GetInt64();
                    DateTime lastPlayed = DateTimeOffset.FromUnixTimeSeconds(lastPlayedUnix).DateTime;
                    lastPlayedStr = lastPlayed.ToString("yyyy-MM-dd");
                }

                string appId = game.GetProperty("appid").GetRawText();
                string imageUrl = $"https://cdn.akamai.steamstatic.com/steam/apps/{appId}/header.jpg";

                int completed = 0, total = 0;
                try
                {
                    string achUrl = $"https://api.steampowered.com/ISteamUserStats/GetPlayerAchievements/v1/?appid={appId}&key={steamApiKey}&steamid={steamId}";
                    var achResponse = client.GetStringAsync(achUrl).Result;
                    using JsonDocument achDoc = JsonDocument.Parse(achResponse);
                    var playerStats = achDoc.RootElement.GetProperty("playerstats");
                    if (playerStats.TryGetProperty("achievements", out JsonElement achievements))
                    {
                        total = achievements.GetArrayLength();
                        foreach (var ach in achievements.EnumerateArray())
                            if (ach.GetProperty("achieved").GetInt32() == 1) completed++;
                    }
                }
                catch { completed = 0; total = 0; }

                string progressBar = "";
                if (total > 0)
                {
                    int filled = (int)((completed / (double)total) * 10);
                    int empty = 10 - filled;
                    progressBar = new string('▓', filled) + new string('░', empty);
                }

                // Return HTML with image, title, playtime, last played date, and achievement progress
                return $@"
<img align=""left"" width=""400"" src=""{imageUrl}"" style=""margin-right:1em;"">
<b>『{name}』</b><br><br>
<b>Playtime</b>: {hours}h {minutes}m<br>
<b>Last Played</b>: {lastPlayedStr}<br>
<b>Achievements</b>: {progressBar} {completed}/{total}<br><br>
<br style=""clear: both;""><br>";
            }


            string bannerPath = $"https://raw.githubusercontent.com/margotlinne/SteamNow/main/banner.png";
            string bannerHtml = $@"
<a href=""https://github.com/margotlinne/SteamNow"">
  <img src=""{bannerPath}"" alt=""Banner"" width=""50%"">
</a>";

            // Compare the recent game and most played game
            if (recentGame.GetProperty("appid").GetRawText() == mostPlayedGame.GetProperty("appid").GetRawText())
            {
                // If both are the same game, show only one with label "Most and Recently Played"
                userGameInfo = bannerHtml +
                                "\n<br>\n\n## MOST AND RECENTLY PLAYED\n\n" +
                                GetGameInfo(recentGame);
            }
            else
            {
                var sb = new StringBuilder();
                sb.AppendLine(bannerHtml);
                sb.AppendLine(""); 

                sb.AppendLine("## RECENTLY PLAYED");
                sb.AppendLine(GetGameInfo(recentGame));
                sb.AppendLine("");
                sb.AppendLine("");

                sb.AppendLine("## MOST PLAYED");
                sb.AppendLine(GetGameInfo(mostPlayedGame));

                userGameInfo = sb.ToString();
            }

            // Console.WriteLine(userGameInfo);
            #endregion
        }
        #region 2. GitHub
        string gitToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
        string owner = Environment.GetEnvironmentVariable("GITHUB_OWNER");
        string repo = Environment.GetEnvironmentVariable("GITHUB_STEAMNOW_REPO");

        string readmeUrl = $"https://api.github.com/repos/{owner}/{repo}/contents/README.md";

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", gitToken);

        string readmeText = "";
        string sha = null;

        try
        {
            // Try to get the existing README
            var readmeResponse = await client.GetStringAsync(readmeUrl);
            using JsonDocument readmeDoc = JsonDocument.Parse(readmeResponse);
            var root = readmeDoc.RootElement;
            sha = root.GetProperty("sha").GetString();
            string contentBase64 = root.GetProperty("content").GetString();
            readmeText = Encoding.UTF8.GetString(Convert.FromBase64String(contentBase64));
        }
        catch (HttpRequestException e) when (e.Message.Contains("404"))
        {
            // If README does not exist, prepare to create a new one
            readmeText = "";
            sha = null; // No SHA for new file
        }

        // Prepare the new README content
        string newReadmeText = $"{userGameInfo}";

        string newContentBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(newReadmeText));

        // Send a PUT request to GitHub to update or create the README
        var putPayload = new
        {
            message = "Auto update README with recent Steam game",
            content = newContentBase64,
            sha = sha // If null, GitHub will create a new file
        };

        var jsonPayload = JsonSerializer.Serialize(putPayload);
        var putContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        var putResponse = await client.PutAsync(readmeUrl, putContent);

        if (putResponse.IsSuccessStatusCode)
            Console.WriteLine("README Update Succeeded!");
        else
            Console.WriteLine($"Update failed: {putResponse.StatusCode}");

        #endregion

    }
}