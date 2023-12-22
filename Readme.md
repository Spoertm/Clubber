# Clubber

Clubber is a bot for the [DD Pals Discord server](https://discord.gg/jMRumVerj2) that primarily keeps your roles updated per your [Devil Daggers](https://store.steampowered.com/app/422970/Devil_Daggers/) score.

### Features
* Role handling: Keeps your score roles up to date. This process can be manually triggered via the `pb` command and is done automatically once a day for all registered users.
* DD News: Notifies whenever a new player gets a personal best above 1000s.
* Personal stats: The `stats` command shows more detailed information about the player, including score, leaderboard ID, rank, number of kills, and more.

### Framework
- .NET 8.0

### Language
- C# 12.0

### Architecture
There was an attempt at following [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html), where:

* [Clubber.Domain](Clubber.Domain) represents the core logic of the application, this includes communication with Discord, Sorath servers, [ddinfo API](http://devildaggers.info/), and the database.


* [Clubber.Web.Client](Clubber.Web.Client) represents the Web/UI part that the user interacts with. It is a Blazor WebAssembly project built with [tailwindcss](https://tailwindcss.com/).


* [Clubber.Web.Server](Clubber.Web.Server) is where the API is defined and can be regarded as the application's entry point.
