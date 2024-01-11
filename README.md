# MabiGuildHelper

## Description
MabiGuildHelper is a Discord bot that helps guild management in Mabinogi which written with .Net 8

## Features
- [x] Daily effect notification
- [x] Daily dungeon information notification
- [x] Display in game Erinn time
- [X] Instance reset reminder
- [x] Data scraping from Mabinogi website
- [X] Funny reponse in discord chat
- [ ] In game chat history synchronization

## Usage
Setup the channel using `/setting` command.
### Slash Commands
```
/setting <setting name> <value>
```
### Message commands:
```
Right click message
```

## Requirements
- Visual Studio 2022
- Discord Bot Token

## Visual Studio 2022
Download Visual Studio 2022 from [here](https://visualstudio.microsoft.com/downloads/).
## Discord bot setup
### How to get Discord Bot Token
To get your Discord Bot Token, go to [Discord Developer Portal](https://discord.com/developers/applications) and create a new application. Then, go to the Bot tab and create a new bot. You can get your token in the Bot tab.
### Invite Bot to your server
To invite your bot to your server, go to the OAuth2 tab and select bot in the Scopes section. Then, select the permissions you want your bot to have in the Bot Permissions section. Finally, copy the link and paste it in your browser. You can invite your bot to your server from there.

## Config
Copy ``appsettings.template.json`` to ``appsettings.json`` and fill in with you token/api key.

Setup `DOTNET_ENVIRONMENT` (Local/Development/Production) `DiscordBot.Token` is required in `Production` environment. `DiscordBot.BetaToken` is required in `Local`/`Development` environment.

## Database
This project uses SQLite as database. You can change the connection string in `appsettings.json`.

## EF Core Entity Migration
### After edit entity:
Commnet out all ``.AddSingleton<>()``, ``AddScoped<>`` in Program.cs and run the following command in Package Manager Console.
After edit entity:
```PowerShell
Add-Migration <MigrationName>
```
To remove migration:
```PowerShell
Remove-Migration
```