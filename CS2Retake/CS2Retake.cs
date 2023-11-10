﻿using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using CS2Retake.Entity;
using CS2Retake.Manager;
using CS2Retake.Utils;

namespace CS2Retake
{
    public class CS2Retake : BasePlugin  
    {
        public override string ModuleName => "CS2Retake";
        public override string ModuleVersion => "0.0.1";
        public override string ModuleAuthor => "LordFetznschaedl";
        public override string ModuleDescription => "Retake Plugin implementation for CS2";
       

        public override void Load(bool hotReload)
        {
            this.Log(PluginInfo());
            this.Log(this.ModuleDescription);

            RetakeManager.GetInstance().ModuleName= this.ModuleName;
            MapManager.GetInstance().ModuleName= this.ModuleName;

            this.RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
            this.RegisterEventHandler<EventRoundStart>(OnRoundStart);
            this.RegisterEventHandler<EventRoundEnd>(OnRoundEnd);

            if (MapManager.GetInstance().CurrentMap == null)
            {
                MapManager.GetInstance().CurrentMap = new MapEntity(Server.MapName, this.ModuleDirectory, this.ModuleName);
            }

            this.RegisterListener<Listeners.OnMapStart>((mapName) =>
            {
                this.Log($"Map changed to {mapName}");
                MapManager.GetInstance().CurrentMap = new MapEntity(Server.MapName, this.ModuleDirectory, this.ModuleName);
            });
        }



        [ConsoleCommand("css_retakeinfo", "This command prints the plugin information")]
        public void OnCommandInfo(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null)
            {
                this.Log("Command has been called by the server.");
                return;
            }

            command.ReplyToCommand(PluginInfo());
        }

        [ConsoleCommand("css_retakespawn", "This command teleports the player to a spawn with the given index in the args")]
        public void OnCommandSpawn(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null)
            {
                this.Log("Command has been called by the server.");
                return;
            }
            if(!player.PlayerPawn.IsValid)
            {
                this.Log("PlayerPawn not valid");
                return;
            }

            if (command.ArgCount != 2)
            {
                this.Log($"ArgCount: {command.ArgCount} - ArgString: {command.ArgString}");
                command.ReplyToCommand($"One argument with a valid spawn index is needed! Example: !retakespawn 0");
                return;
            }

            if(!int.TryParse(command.ArgByIndex(1), out int spawnIndex))
            {
                this.Log("Argument index not a valid integer!");
                return;
            }

            MapManager.GetInstance().CurrentMap.TeleportPlayerToSpawn(player, BombSiteEnum.Undefined ,spawnIndex);
        }

        [ConsoleCommand("css_retakewrite", "This command writes the spawns for the current map")]
        public void OnCommandWrite(CCSPlayerController? player, CommandInfo command)
        {
            MapManager.GetInstance().CurrentMap.WriteSpawns();
        }

        [ConsoleCommand("css_retakeread", "This command reads the spawns for the current map")]
        public void OnCommandRead(CCSPlayerController? player, CommandInfo command)
        {
            MapManager.GetInstance().CurrentMap.ReadSpawns();
            this.Log($"{MapManager.GetInstance().CurrentMap.SpawnPoints.Count} spawnpoints read");
        }


        [ConsoleCommand("css_retakescramble", "This command reads the spawns for the current map")]
        public void OnCommandScramble(CCSPlayerController? player, CommandInfo command)
        {
            RetakeManager.GetInstance().ScrambleTeams();
        }

        [GameEventHandler]
        public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
        {
            if(MapManager.GetInstance().BombSite == BombSiteEnum.Undefined) 
            { 
                MapManager.GetInstance().RandomBombSite();
            }

            if (@event == null)
            {
                return HookResult.Continue;
            }
            if(!@event.Userid.IsValid)
            {
                return HookResult.Continue;
            }

            MapManager.GetInstance().CurrentMap.TeleportPlayerToSpawn(@event.Userid, MapManager.GetInstance().BombSite);

            return HookResult.Continue;
        }

        [GameEventHandler]
        private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
        {
            RetakeManager.GetInstance().PlantBomb();

            return HookResult.Continue;
        }

        [GameEventHandler]
        private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
        {
            if (@event.Winner == (int)CsTeam.Terrorist)
            {
                MapManager.GetInstance().TerroristRoundWinStreak++;
                Server.PrintToChatAll($"[{ChatColors.Gold}CS2Retake{ChatColors.White}] The Terrorists have won {ChatColors.Red}{MapManager.GetInstance().TerroristRoundWinStreak}{ChatColors.White} rounds subsequently.");
            }
            else
            {
                MapManager.GetInstance().TerroristRoundWinStreak = 0;
            }

            if(MapManager.GetInstance().TerroristRoundWinStreak == 5)
            {
                Server.PrintToChatAll($"[{ChatColors.Gold}CS2Retake{ChatColors.White}] Teams will be scrambled now!");
                MapManager.GetInstance().TerroristRoundWinStreak = 0;
                RetakeManager.GetInstance().ScrambleTeams();
            }

            MapManager.GetInstance().RandomBombSite();
            MapManager.GetInstance().CurrentMap.ResetSpawnInUse();

            return HookResult.Continue;
        }

        private string PluginInfo()
        {
            return $"Plugin: {this.ModuleName} - Version: {this.ModuleVersion} by {this.ModuleAuthor}";
        }

        private void Log(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[{this.ModuleName}] {message}");
            Console.ResetColor();
        }
    }
}