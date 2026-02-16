<img src="https://raw.githubusercontent.com/margotlinne/SteamNow/main/banner.png" alt="Banner" width="40%">

Automatically updates your GitHub profile README with your recently played Steam game.

<br>

## How To

1. Get **Steam Web API** Key: https://steamcommunity.com/dev/apikey → **Don't forget to remember this**
2. Go to your steam profile on web, "https://steamcommunity.com/profiles/{your_steam_id}/" → remember this **"your_steam_id"** part
3. Your GitHub profile > Settings > Developer Settings > Personal access tokens > Tokens (classic) > Generate new token → **Don't forget to remember this**
4. Clone this project into your GitHub profile
5. Go to cloned repository > Settings > Security - Secrets and variables > Actions > Repository secrets

   <img width="782" height="287" alt="image" src="https://github.com/user-attachments/assets/7f11f2a6-f4ef-4df7-ad77-bf2fc5a76685" />

   Add these four secrets:
   
   **"PAT_TOKEN"** = your personal access tokens (number 3)
   
   **"STEAM_REPO"** = repository name where README you wanna edit automatically is
   
   **"STEAM_API_KEY"** = steam api key (number 1)
   
   **"STEAM_ID"** = your steam id (number 2)

6. Go to cloned respository > Actions > Update SteamNow README > Run workflow > Run workflow
7. Check if your target repository README is updated correctly
8. You're done!

<br>

## Outro

If you have any questions or troubles with this project, feel free to leave an Issue.
