using System;
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
    class CSGOJSONParser : JSONParser, IJSONParsing
    {

        public CSGOJSONParser(string path, JsonSerializerSettings settings) : base(path, settings)
        {
        }

        public ReplayGamstateMeta AssembleGamemeta(string mapname, float tickrate, IEnumerable<DemoInfo.Player> players)
        {
            return new ReplayGamstateMeta
            {
                gamestate_id = 0,
                Mapname = mapname,
                Tickrate = tickrate,
                Players = AssemblePlayerMetas(players.ToArray())
            };
        }

        #region Playerevents

        internal PlayerKilled AssemblePlayerKilled(PlayerKilledEventArgs pke)
        {
            return new PlayerKilled
            {
                GameeventType = "player_killed",
                Actor = AssemblePlayer(pke.Killer),
                Victim = AssemblePlayer(pke.Victim),
                Assister = AssemblePlayerDetailed(pke.Assister),
                Headshot = pke.Headshot,
                Penetrated = pke.PenetratedObjects,
                Weapon = AssembleWeapon(pke.Weapon)
            };
        }

        internal PlayerSpotted AssemblePlayerSpotted(PlayerSpottedEventArgs e)
        {
            return new PlayerSpotted
            {
                GameeventType = "player_spotted",
                Actor = AssemblePlayer(e.player)
            };
        }

        internal PlayerHurt AssemblePlayerHurt(PlayerHurtEventArgs phe)
        {
            return new PlayerHurt
            {
                GameeventType = "player_hurt",
                Actor = AssemblePlayer(phe.Attacker),
                Victim = AssemblePlayer(phe.Victim),
                Armor = phe.Armor,
                Armor_damage = phe.ArmorDamage,
                HP = phe.Health,
                HP_damage = phe.HealthDamage,
                Hitgroup = (int)phe.Hitgroup,
                Weapon = AssembleWeapon(phe.Weapon)
            };
        }

        internal Event AssemblePlayerPosition(DemoInfo.Player p)
        {
            return new Event
            {
                GameeventType = "player_position",
                Actor = AssemblePlayer(p)
            };
        }
        #endregion

        #region Weaponevents
        internal WeaponFire AssembleWeaponFire(WeaponFiredEventArgs we)
        {
            return new WeaponFire
            {
                GameeventType = "weapon_fire",
                Actor = AssemblePlayer(we.Shooter),
                Weapon = AssembleWeapon(we.Weapon)
            };
        }

        internal WeaponFire AssembleWeaponFireEmpty(WeaponFiredEmptyEventArgs we)
        {
            return new WeaponFire
            {
                GameeventType = "weapon_fire_empty",
                Actor = AssemblePlayer(we.Shooter),
                Weapon = AssembleWeapon(we.Weapon)
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
                    GameeventType = eventname,
                    Actor = AssemblePlayer(e.ThrownBy),
                    nadetype = e.NadeType.ToString(),
                    position = new Point3D(e.Position.X, e.Position.Y, e.Position.Z),
                    Flashedplayers = AssembleFlashedPlayers(f.FlashedPlayers)
                };
            }

            return new NadeEvents
            {
                GameeventType = eventname,
                Actor = AssemblePlayer(e.ThrownBy),
                nadetype = e.NadeType.ToString(),
                position = new Point3D(e.Position.X, e.Position.Y, e.Position.Z),
            };
        }


        #endregion

        #region Bombevents

        internal BombEvents AssembleBomb(BombEventArgs be, string gameevent)
        {
            return new BombEvents
            {
                GameeventType = gameevent,
                site = be.Site,
                Actor = AssemblePlayer(be.Player)
            };
        }

        internal BombState AssembleBombState(BombDropEventArgs be, string gameevent)
        {
            return new BombState
            {
                GameeventType = gameevent,
                Actor = AssemblePlayer(be.Player)
            };
        }

        internal BombState AssembleBombState(BombPickUpEventArgs be, string gameevent)
        {
            return new BombState
            {
                GameeventType = gameevent,
                Actor = AssemblePlayer(be.Player)
            };
        }

        internal BombEvents AssembleBombDefuse(BombDefuseEventArgs bde, string gameevent)
        {
            return new BombEvents
            {
                GameeventType = gameevent,
                site = bde.Site,
                Actor = AssemblePlayer(bde.Player),
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
                GameeventType = "player_bind",
                Actor = AssemblePlayer(player)
            };
        }

        internal ServerEvents assemblePlayerDisconnected(DemoInfo.Player player)
        {
            Console.WriteLine("Disconnect: " + player.Name + " ID: " + player.SteamID);
            return new ServerEvents
            {
                GameeventType = "player_disconnected",
                Actor = AssemblePlayer(player)
            };
        }

        internal TakeOverEvent assemblePlayerTakeOver(BotTakeOverEventArgs e)
        {
            if (e.Index != null)
                Console.WriteLine("Takeover: Taker:" + e.Taker.Name + " ID: " + e.Taker.SteamID + " Taken:" + e.Taken.Name + " ID: " + e.Taken.SteamID + " Index:" + e.Index.Name + " ID: " + e.Index.SteamID);
            else
                Console.WriteLine("Takeover: Taker:" + e.Taker.Name + " ID: " + e.Taker.SteamID + " Taken:" + e.Taken.Name + " ID: " + e.Taken.SteamID);

            return new TakeOverEvent
            {
                GameeventType = "player_takeover",
                Actor = AssemblePlayer(e.Taker),
                taken = AssemblePlayer(e.Taken)
            };
        }

        #endregion

        #region Subevents

        internal List<Data.Gameobjects.Player> AssemblePlayers(DemoInfo.Player[] ps)
        {
            if (ps == null)
                return null;
            List<Data.Gameobjects.Player> players = new List<Data.Gameobjects.Player>();
            foreach (var player in ps)
                players.Add(AssemblePlayer(player));

            return players;
        }

        internal List<PlayerMeta> AssemblePlayerMetas(DemoInfo.Player[] ps)
        {
            if (ps == null)
                return null;
            List<PlayerMeta> players = new List<PlayerMeta>();
            foreach (var player in ps)
                players.Add(AssemblePlayerMeta(player));

            return players;
        }

        internal PlayerMeta AssemblePlayerMeta(DemoInfo.Player p)
        {
            return new PlayerMeta
            {
                Playername = p.Name,
                player_id = p.SteamID,
                Team = p.Team.ToString(),
                Clanname = p.AdditionaInformations.Clantag
            };
        }

        internal Data.Gameobjects.Player AssemblePlayer(DemoInfo.Player p)
        {
            return new Data.Gameobjects.Player
            {
                Playername = p.Name,
                player_id = p.SteamID,
                Position = new Point3D(p.Position.X, p.Position.Y, p.Position.Z),
                Facing = new Facing { Yaw = p.ViewDirectionX, Pitch = p.ViewDirectionY },
                Velocity = new Velocity { VX = p.Velocity.X, VY = p.Velocity.Y, VZ = p.Velocity.Z },
                Team = p.Team.ToString(),
                IsSpotted = p.IsSpotted,
                HP = p.HP
            };
        }

        internal List<PlayerFlashed> AssembleFlashedPlayers(DemoInfo.Player[] ps)
        {
            if (ps == null)
                return null;
            List<PlayerFlashed> players = new List<PlayerFlashed>();
            foreach (var player in ps)
                players.Add(AssembleFlashPlayer(player));

            return players;
        }

        internal PlayerFlashed AssembleFlashPlayer(DemoInfo.Player p)
        {
            PlayerFlashed player = new PlayerFlashed
            {
                Playername = p.Name,
                player_id = p.SteamID,
                Position = new Point3D(p.Position.X, p.Position.Y, p.Position.Z),
                Velocity = new Velocity { VX = p.Velocity.X, VY = p.Velocity.X, VZ = p.Velocity.X },
                Facing = new Facing { Yaw = p.ViewDirectionX, Pitch = p.ViewDirectionY },
                HP = p.HP,
                IsSpotted = p.IsSpotted,
                Team = p.Team.ToString(),
                Flashedduration = p.FlashDuration
            };

            return player;
        }

        internal Event AssemblePlayerJumped(PlayerJumpedEventArgs e)
        {
            return new MovementEvents
            {
                GameeventType = "player_jumped",
                Actor = AssemblePlayer(e.Jumper)
            };
        }

        internal Event AssemblePlayerFallen(PlayerFallEventArgs e)
        {
            return new MovementEvents
            {
                GameeventType = "player_fallen",
                Actor = AssemblePlayer(e.Fallen)
            };
        }

        internal Event AssembleWeaponReload(WeaponReloadEventArgs we)
        {
            return new MovementEvents
            {
                GameeventType = "weapon_reload",
                Actor = AssemblePlayer(we.Actor)
            };
        }

        internal Event AssemblePlayerStepped(PlayerSteppedEventArgs e)
        {
            return new Event
            {
                GameeventType = "player_footstep",
                Actor = AssemblePlayer(e.Stepper)
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


        internal PlayerDetailed AssemblePlayerDetailed(DemoInfo.Player p)
        {
            if (p == null) return null;

            return new PlayerDetailed
            {
                Playername = p.Name,
                player_id = p.SteamID,
                Position = new Point3D(p.Position.X, p.Position.Y, p.Position.Z),
                Facing = new Facing { Yaw = p.ViewDirectionX, Pitch = p.ViewDirectionY },
                Team = p.Team.ToString(),
                IsDucking = p.IsDucking,
                IsSpotted = p.IsSpotted,
                IsScoped = p.IsScoped,
                IsWalking = p.IsWalking,
                HasHelmet = p.HasHelmet,
                HasDefuser = p.HasDefuseKit,
                HP = p.HP,
                Armor = p.Armor,
                Velocity = new Velocity { VX = p.Velocity.X, VY = p.Velocity.Y, VZ = p.Velocity.Z },
                Items = AssembleWeapons(p.Weapons)

            };
        }

        internal List<Item> AssembleWeapons(IEnumerable<Equipment> wps)
        {
            List<Item> jwps = new List<Item>();
            foreach (var w in wps)
                jwps.Add(AssembleWeapon(w));

            return jwps;
        }

        internal Item AssembleWeapon(Equipment wp)
        {
            if (wp == null)
            {
                Console.WriteLine("Weapon null. Bytestream not suitable for this version of DemoInfo");
                return new Item();
            }

            Item jwp = new Item
            {
                //owner = assemblePlayerDetailed(wp.Owner), //TODO: fill weaponcategorie and type
                Name = wp.Weapon.ToString(),
                ammo_in_magazine = wp.AmmoInMagazine,
            };

            return jwp;
        }



        #endregion
    }


}