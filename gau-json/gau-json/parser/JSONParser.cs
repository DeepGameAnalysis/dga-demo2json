﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DemoInfo;
using Newtonsoft.Json;
using Data.Gamestate;
using Data.Gameevents;
using Data.Gameobjects;
using MathNet.Spatial.Euclidean;

namespace JSONParsing
{
    class JSONParser
    {

        private static StreamWriter outputStream;

        private JsonSerializerSettings settings;

        public JSONParser(string path, JsonSerializerSettings settings)
        {
            string outputpath = path.Replace(".dem", "") + ".json";
            outputStream = new StreamWriter(outputpath);

            this.settings = settings;

        }

        /// <summary>
        /// Dumps the Gamestate in prettyjson or as one-liner(default)
        /// </summary>
        /// <param name="gs"></param>
        /// <param name="prettyjson"></param>
        public void dumpJSONFile(ReplayGamestate gs, bool prettyjson)
        {
            Formatting f = Formatting.None;
            if (prettyjson)
                f = Formatting.Indented;

            outputStream.Write(JsonConvert.SerializeObject(gs, settings));
        }


        public ReplayGamestate deserializeGamestateString(string gamestatestring)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<ReplayGamestate>(gamestatestring, settings);
        }


        /// <summary>
        /// Dumps gamestate in a string
        /// </summary>
        /// <param name="gs"></param>
        /// <param name="prettyjson"></param>
        public string dumpJSONString(ReplayGamestate gs, bool prettyjson)
        {
            Formatting f = Formatting.None;
            if (prettyjson)
                f = Formatting.Indented;
            return JsonConvert.SerializeObject(gs, f);
        }

        public void stopParser()
        {
            outputStream.Close();
        }

        

        public ReplayGamstateMeta assembleGamemeta(string mapname, float tickrate, IEnumerable<DemoInfo.Player> players)
        {
            return new ReplayGamstateMeta
            {
                gamestate_id = 0,
                mapname = mapname,
                tickrate = tickrate,
                players = assemblePlayers(players.ToArray())
            };
        }

        #region Playerevents

        internal PlayerKilled assemblePlayerKilled(PlayerKilledEventArgs pke)
        {
            return new PlayerKilled
            {
                gameeventtype = "player_killed",
                actor = assemblePlayerDetailed(pke.Killer),
                victim = assemblePlayerDetailed(pke.Victim),
                assister = assemblePlayerDetailed(pke.Assister),
                headshot = pke.Headshot,
                penetrated = pke.PenetratedObjects,
                weapon = assembleWeapon(pke.Weapon)
            };
        }

        internal PlayerSpotted assemblePlayerSpotted(PlayerSpottedEventArgs e)
        {
            return new PlayerSpotted
            {
                gameeventtype = "player_spotted",
                actor = assemblePlayerDetailed(e.player)
            };
        }

        internal PlayerHurt assemblePlayerHurt(PlayerHurtEventArgs phe)
        {
            return new PlayerHurt
            {
                gameeventtype = "player_hurt",
                actor = assemblePlayerDetailed(phe.Attacker),
                victim = assemblePlayerDetailed(phe.Victim),
                armor = phe.Armor,
                armor_damage = phe.ArmorDamage,
                HP = phe.Health,
                HP_damage = phe.HealthDamage,
                hitgroup = (int)phe.Hitgroup,
                weapon = assembleWeapon(phe.Weapon)
            };
        }

        internal MovementEvents assemblePlayerPosition(DemoInfo.Player p)
        {
            return new MovementEvents
            {
                gameeventtype = "player_position",
                actor = assemblePlayerDetailed(p)
            };
        }
        #endregion

        #region Weaponevents
        internal WeaponFire assembleWeaponFire(WeaponFiredEventArgs we)
        {
            return new WeaponFire
            {
                gameeventtype = "weapon_fire",
                actor = assemblePlayerDetailed(we.Shooter),
                weapon = assembleWeapon(we.Weapon)
            };
        }

        internal WeaponFire assembleWeaponFireEmpty(WeaponFiredEmptyEventArgs we)
        {
            return new WeaponFire
            {
                gameeventtype = "weapon_fire_empty",
                actor = assemblePlayerDetailed(we.Shooter),
                weapon = assembleWeapon(we.Weapon)
            };
        }

        #endregion

        #region Nades

        internal NadeEvents assembleNade(NadeEventArgs e, string eventname)
        {

            if (e.GetType() == typeof(FlashEventArgs)) //Exception for FlashEvents -> we need flashed players
            {
                FlashEventArgs f = e as FlashEventArgs;
                return new FlashNade
                {
                    gameeventtype = eventname,
                    actor = assemblePlayerDetailed(e.ThrownBy),
                    nadetype = e.NadeType.ToString(),
                    position = new Point3D(e.Position.X, e.Position.Y, e.Position.Z),
                    flashedplayers = assembleFlashedPlayers(f.FlashedPlayers)
                };
            }

            return new NadeEvents
            {
                gameeventtype = eventname,
                actor = assemblePlayerDetailed(e.ThrownBy),
                nadetype = e.NadeType.ToString(),
                position = new Point3D(e.Position.X, e.Position.Y, e.Position.Z),
            };
        }


        #endregion

        #region Bombevents

        internal BombEvents assembleBomb(BombEventArgs be, string gameevent)
        {
            return new BombEvents
            {
                gameeventtype = gameevent,
                site = be.Site,
                actor = assemblePlayerDetailed(be.Player)
            };
        }

        internal BombState assembleBombState(BombDropEventArgs be, string gameevent)
        {
            return new BombState
            {
                gameeventtype = gameevent,
                actor = assemblePlayerDetailed(be.Player)
            };
        }

        internal BombState assembleBombState(BombPickUpEventArgs be, string gameevent)
        {
            return new BombState
            {
                gameeventtype = gameevent,
                actor = assemblePlayerDetailed(be.Player)
            };
        }

        internal BombEvents assembleBombDefuse(BombDefuseEventArgs bde, string gameevent)
        {
            return new BombEvents
            {
                gameeventtype = gameevent,
                site = bde.Site,
                actor = assemblePlayerDetailed(bde.Player),
                haskit = bde.HasKit
            };
        }
        #endregion

        #region Serverevents
        internal ServerEvents assemblePlayerBind(DemoInfo.Player player)
        {
            Console.WriteLine("Bind: " + player.Name + " ID: " + player.SteamID);
            return new ServerEvents
            {
                gameeventtype = "player_bind",
                actor = assemblePlayer(player)
            };
        }

        internal ServerEvents assemblePlayerDisconnected(DemoInfo.Player player)
        {
            Console.WriteLine("Disconnect: " +player.Name + " ID: "+player.SteamID);
            return new ServerEvents
            {
                gameeventtype = "player_disconnected",
                actor = assemblePlayer(player)
            };
        }

        internal TakeOverEvent assemblePlayerTakeOver(BotTakeOverEventArgs e)
        {
            if(e.Index != null)
                Console.WriteLine("Takeover: Taker:" + e.Taker.Name + " ID: " + e.Taker.SteamID+ " Taken:" + e.Taken.Name + " ID: " + e.Taken.SteamID + " Index:" + e.Index.Name + " ID: " + e.Index.SteamID);
            else
                Console.WriteLine("Takeover: Taker:" + e.Taker.Name + " ID: " + e.Taker.SteamID + " Taken:" + e.Taken.Name + " ID: " + e.Taken.SteamID );

            return new TakeOverEvent
            {
                gameeventtype = "player_takeover",
                actor = assemblePlayer(e.Taker),
                taken = assemblePlayer(e.Taken)
            };
        }

        #endregion

        #region Subevents

        internal List<PlayerDetailed> assemblePlayers(DemoInfo.Player[] ps)
        {
            if (ps == null)
                return null;
            List<PlayerDetailed> players = new List<PlayerDetailed>();
            foreach (var player in ps)
                players.Add(assemblePlayerDetailed(player));

            return players;
        }

        internal Data.Gameobjects.Player assemblePlayer(DemoInfo.Player p)
        {
            return new Data.Gameobjects.Player
            {
                playername = p.Name,
                player_id = p.SteamID,
                position = new Point3D(p.Position.X, p.Position.Y, p.Position.Z),
                facing = new Facing { Yaw = p.ViewDirectionX, Pitch = p.ViewDirectionY },
                velocity = new Velocity { VX = p.Velocity.X, VY = p.Velocity.Y, VZ = p.Velocity.Z },
                team = p.Team.ToString(),
                isSpotted = p.IsSpotted,
                HP = p.HP
            };
        }

        internal List<PlayerFlashed> assembleFlashedPlayers(DemoInfo.Player[] ps)
        {
            if (ps == null)
                return null;
            List<PlayerFlashed> players = new List<PlayerFlashed>();
            foreach (var player in ps)
                players.Add(assembleFlashPlayer(player));

            return players;
        }

        internal PlayerFlashed assembleFlashPlayer(DemoInfo.Player p)
        {
            PlayerFlashed player = new PlayerFlashed
            {
                playername = p.Name,
                player_id = p.SteamID,
                position = new Point3D(p.Position.X, p.Position.Y, p.Position.Z),
                velocity = new Velocity { VX = p.Velocity.X, VY = p.Velocity.X, VZ = p.Velocity.X },
                facing = new Facing { Yaw = p.ViewDirectionX, Pitch = p.ViewDirectionY },
                HP = p.HP,
                isSpotted = p.IsSpotted,
                team = p.Team.ToString(),
                flashedduration = p.FlashDuration
            };

            return player;
        }

        internal Event assemblePlayerJumped(PlayerJumpedEventArgs e)
        {
            return new MovementEvents
            {
                gameeventtype = "player_jumped",
                actor = assemblePlayerDetailed(e.Jumper)
            };
        }

        internal Event assemblePlayerFallen(PlayerFallEventArgs e)
        {
            return new MovementEvents
            {
                gameeventtype = "player_fallen",
                actor = assemblePlayerDetailed(e.Fallen)
            };
        }

        internal Event assembleWeaponReload(WeaponReloadEventArgs we)
        {
            return new MovementEvents
            {
                gameeventtype = "weapon_reload",
                actor = assemblePlayerDetailed(we.Actor)
            };
        }

        internal Event assemblePlayerStepped(PlayerSteppedEventArgs e)
        {
            return new MovementEvents
            {
                gameeventtype = "player_footstep",
                actor = assemblePlayerDetailed(e.Stepper)
            };
        }

        /*public List<PlayerMeta> assemblePlayers(IEnumerable<DemoInfoModded.Player> ps)
        {
            if (ps == null)
                return null;
            List<PlayerMeta> players = new List<PlayerMeta>();
            foreach (var player in ps)
                players.Add(assemblePlayerMeta(player));

            return players;
        }*/




        internal PlayerMeta assemblePlayerMeta(DemoInfo.Player p)
        {
            return new PlayerMeta
            {
                playername = p.Name,
                player_id = p.SteamID,
                team = p.Team.ToString(),
                clanname = p.AdditionaInformations.Clantag,
            };
        }

        internal PlayerDetailed assemblePlayerDetailed(DemoInfo.Player p)
        {
            if (p == null) return null;
 
            return new PlayerDetailed
            {
                playername = p.Name,
                player_id = p.SteamID,
                position = new Point3D(p.Position.X, p.Position.Y, p.Position.Z),
                facing = new Facing { Yaw = p.ViewDirectionX, Pitch = p.ViewDirectionY },
                team = p.Team.ToString(),
                isDucking = p.IsDucking,
                isSpotted = p.IsSpotted,
                isScoped = p.IsScoped,
                isWalking = p.IsWalking,
                hasHelmet = p.HasHelmet,
                hasDefuser = p.HasDefuseKit,
                HP = p.HP,
                armor = p.Armor,
                velocity = new Velocity { VX = p.Velocity.X, VY = p.Velocity.Y, VZ = p.Velocity.Z }  
            };
        }


        internal PlayerDetailedWithItems assemblePlayerDetailedWithItems(DemoInfo.Player p)
        {
            PlayerDetailedWithItems playerdetailed = new PlayerDetailedWithItems
            {
                playername = p.Name,
                player_id = p.SteamID,
                position = new Point3D(p.Position.X, p.Position.Y, p.Position.Z),
                facing = new Facing { Yaw = p.ViewDirectionX, Pitch = p.ViewDirectionY },
                team = p.Team.ToString(),
                isDucking = p.IsDucking,
                hasHelmet = p.HasHelmet,
                hasDefuser = p.HasDefuseKit,
                HP = p.HP,
                armor = p.Armor,
                velocity = new Velocity { VX = p.Velocity.X, VY = p.Velocity.Y, VZ = p.Velocity.Z },
                items = assembleWeapons(p.Weapons)
            };

            return playerdetailed;
        }

        internal List<Weapon> assembleWeapons(IEnumerable<Equipment> wps)
        {
            List<Weapon> jwps = new List<Weapon>();
            foreach (var w in wps)
                jwps.Add(assembleWeapon(w));

            return jwps;
        }

        internal Weapon assembleWeapon(Equipment wp)
        {
            if (wp == null)
            {
                Console.WriteLine("Weapon null. Bytestream not suitable for this version of DemoInfo");
                return new Weapon();
            }

            Weapon jwp = new Weapon
            {
                //owner = assemblePlayerDetailed(wp.Owner), //TODO: fill weaponcategorie and type
                name = wp.Weapon.ToString(),
                ammo_in_magazine = wp.AmmoInMagazine,
            };

            return jwp;
        }



        #endregion
    }


}