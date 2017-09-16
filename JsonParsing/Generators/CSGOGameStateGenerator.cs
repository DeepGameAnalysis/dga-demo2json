using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using DemoInfo;
using Newtonsoft.Json;
using Data.Gameevents;
using System.Diagnostics;
using System.Threading;
using JSONParsing;
using Data.Gamestate;

namespace GameStateGenerators
{
    public class CSGOGameStateGenerator : GameStateGenerator, IGameStateGenerator
    {

        /// <summary>
        /// CSGO replay parser
        /// </summary>
        private static DemoParser Csgoparser;

        /// <summary>
        /// Parser assembling and disassembling objects later parsed with Newtonsoft.JSON - type depends on the current game
        /// </summary>
        private static CSGOJSONParser Jsonparser;

        /// <summary>
        /// Constructor - Create a new CSGO Generator to generate a new Gamestate 
        /// </summary>
        /// <param name="newdemoparser"></param>
        /// <param name="parsetask"></param>
        public CSGOGameStateGenerator(DemoParser newdemoparser, ParseTaskSettings parsetask) : base(parsetask)
        {
            Csgoparser = newdemoparser;
            //Parser to transform DemoParser events to JSON format
            Jsonparser = new CSGOJSONParser(parsetask.DestPath, parsetask.Settings);
        }

        override public JSONParser GetJSONParser()
        {
            return Jsonparser;
        }

        /// <summary>
        /// Peaks the map name of a csgo demo file
        /// </summary>
        /// <returns></returns>
        override public string PeakMapname()
        {
            Csgoparser.ParseHeader();
            return Csgoparser.Map;
        }


        /// <summary>
        /// Assembles the gamestate object from data given by the demoparser.
        /// </summary>
        /// <param name="parser"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        override public void GenerateGamestate()
        {
            if (Csgoparser == null) throw new Exception("No parser set");

            if(Csgoparser.Header == null) // Watch out for mapname peaks
                Csgoparser.ParseHeader(); 

            #region Main Gameevents
            //Start writing the gamestate object
            Csgoparser.MatchStarted += (sender, e) =>
            {
                hasMatchStarted = true;
                //Assign Gamemetadata
                GameState.Meta = Jsonparser.AssembleGamemeta(Csgoparser.Map, Csgoparser.TickRate, Csgoparser.PlayingParticipants);
            };

            //Assign match object
            Csgoparser.WinPanelMatch += (sender, e) =>
            {
                if (hasMatchStarted)
                    GameState.Match = Match;
                hasMatchStarted = false;

            };

            //Start writing a round object
            Csgoparser.RoundStart += (sender, e) =>
            {
                if (hasMatchStarted)
                {
                    hasRoundStarted = true;
                    round_id++;
                    CurrentRound.round_id = round_id;
                }

            };

            //Add round object to match object
            Csgoparser.RoundEnd += (sender, e) =>
            {
                if (hasMatchStarted)
                {
                    if (hasRoundStarted) //TODO: maybe round fires false -> do in tickdone event (see github issues of DemoInfo)
                    {
                        CurrentRound.winner_team = e.Winner.ToString();
                        Match.Rounds.Add(CurrentRound);
                        CurrentRound = new Round();
                        CurrentRound.ticks = new List<Tick>();
                    }

                    hasRoundStarted = false;

                }

            };

            Csgoparser.FreezetimeEnded += (object sender, FreezetimeEndedEventArgs e) =>
            {
                if (hasMatchStarted)
                    hasFreeezEnded = true; //Just capture movement after freezetime has ended
            };


            #endregion

            #region Playerevents

            Csgoparser.WeaponFired += (object sender, WeaponFiredEventArgs we) =>
            {
                if (hasMatchStarted)
                    CurrentTick.tickevents.Add(Jsonparser.AssembleWeaponFire(we));
            };

            Csgoparser.PlayerSpotted += (sender, e) =>
            {
                if (hasMatchStarted)
                    CurrentTick.tickevents.Add(Jsonparser.AssemblePlayerSpotted(e));
            };

            Csgoparser.WeaponReload += (object sender, WeaponReloadEventArgs we) =>
            {
                if (hasMatchStarted)
                {
                    //tick.tickevents.Add(jsonparser.assembleWeaponReload(we));
                }
            };

            Csgoparser.WeaponFiredEmpty += (object sender, WeaponFiredEmptyEventArgs we) =>
            {
                if (hasMatchStarted)
                {
                    //tick.tickevents.Add(jsonparser.assembleWeaponFireEmpty(we));
                }
            };

            Csgoparser.PlayerJumped += (sender, e) =>
            {
                if (hasMatchStarted)
                {
                    if (e.Jumper != null)
                    {
                        //tick.tickevents.Add(jsonparser.assemblePlayerJumped(e));
                        //steppers.Add(e.Jumper);
                    }
                }

            };

            Csgoparser.PlayerFallen += (sender, e) =>
            {
                if (hasMatchStarted)
                    if (e.Fallen != null)
                    {
                        //tick.tickevents.Add(jsonparser.assemblePlayerFallen(e));
                    }
            };

            Csgoparser.PlayerStepped += (sender, e) =>
            {
                if (hasMatchStarted)
                {
                    if (e.Stepper != null && Csgoparser.PlayingParticipants.Contains(e.Stepper))
                    { //Prevent spectating players from producing steps 
                        CurrentTick.tickevents.Add(Jsonparser.AssemblePlayerStepped(e));
                        alreadytracked.Add(e.Stepper);
                    }
                }

            };

            Csgoparser.PlayerKilled += (object sender, PlayerKilledEventArgs e) =>
            {
                if (hasMatchStarted)
                {
                    //the killer is null if victim is killed by the world - eg. by falling
                    if (e.Killer != null)
                    {
                        CurrentTick.tickevents.Add(Jsonparser.AssemblePlayerKilled(e));
                        alreadytracked.Add(e.Killer);
                        alreadytracked.Add(e.Victim);
                    }
                }

            };

            Csgoparser.PlayerHurt += (object sender, PlayerHurtEventArgs e) =>
            {
                if (hasMatchStarted)
                {
                    //the attacker is null if victim is damaged by the world - eg. by falling
                    if (e.Attacker != null)
                    {
                        CurrentTick.tickevents.Add(Jsonparser.AssemblePlayerHurt(e));
                        alreadytracked.Add(e.Attacker);
                        alreadytracked.Add(e.Victim);
                    }
                }

            };
            #endregion

            #region Nadeevents
            //Nade (Smoke Fire Decoy Flashbang and HE) events
            Csgoparser.ExplosiveNadeExploded += (object sender, GrenadeEventArgs e) =>
                {
                    if (e.ThrownBy != null && hasMatchStarted)
                        CurrentTick.tickevents.Add(Jsonparser.assembleNade(e, "hegrenade_exploded"));
                };

            Csgoparser.FireNadeStarted += (object sender, FireEventArgs e) =>
                    {
                        if (e.ThrownBy != null && hasMatchStarted)
                            CurrentTick.tickevents.Add(Jsonparser.assembleNade(e, "firenade_exploded"));
                    };

            Csgoparser.FireNadeEnded += (object sender, FireEventArgs e) =>
                        {
                            if (e.ThrownBy != null && hasMatchStarted)
                                CurrentTick.tickevents.Add(Jsonparser.assembleNade(e, "firenade_ended"));
                        };

            Csgoparser.SmokeNadeStarted += (object sender, SmokeEventArgs e) =>
            {
                if (e.ThrownBy != null && hasMatchStarted)
                    CurrentTick.tickevents.Add(Jsonparser.assembleNade(e, "smoke_exploded"));
            };


            Csgoparser.SmokeNadeEnded += (object sender, SmokeEventArgs e) =>
            {
                if (e.ThrownBy != null && hasMatchStarted)
                    CurrentTick.tickevents.Add(Jsonparser.assembleNade(e, "smoke_ended"));
            };

            Csgoparser.DecoyNadeStarted += (object sender, DecoyEventArgs e) =>
            {
                if (e.ThrownBy != null && hasMatchStarted)
                    CurrentTick.tickevents.Add(Jsonparser.assembleNade(e, "decoy_exploded"));
            };

            Csgoparser.DecoyNadeEnded += (object sender, DecoyEventArgs e) =>
            {
                if (e.ThrownBy != null && hasMatchStarted)
                    CurrentTick.tickevents.Add(Jsonparser.assembleNade(e, "decoy_ended"));
            };

            Csgoparser.FlashNadeExploded += (object sender, FlashEventArgs e) =>
            {
                if (e.ThrownBy != null && hasMatchStarted)
                    CurrentTick.tickevents.Add(Jsonparser.assembleNade(e, "flash_exploded"));
            };
            /*
            // Seems to be redundant with all exploded events
            parser.NadeReachedTarget += (object sender, NadeEventArgs e) =>
            {
                if (e.ThrownBy != null && hasMatchStarted)
                    tick.tickevents.Add(jsonparser.assembleNade(e, "nade_reachedtarget"));
            }; */

            #endregion

            #region Bombevents
            Csgoparser.BombAbortPlant += (object sender, BombEventArgs e) =>
            {
                //tick.tickevents.Add(jsonparser.assembleBomb(e, "bomb_abort_plant"));
            };

            Csgoparser.BombAbortDefuse += (object sender, BombDefuseEventArgs e) =>
            {
                //tick.tickevents.Add(jsonparser.assembleBombDefuse(e, "bomb_abort_defuse"));
            };

            Csgoparser.BombBeginPlant += (object sender, BombEventArgs e) =>
            {
                //tick.tickevents.Add(jsonparser.assembleBomb(e, "bomb_begin_plant"));
            };

            Csgoparser.BombBeginDefuse += (object sender, BombDefuseEventArgs e) =>
            {
                //tick.tickevents.Add(jsonparser.assembleBombDefuse(e, "bomb_begin_defuse"));
            };

            Csgoparser.BombPlanted += (object sender, BombEventArgs e) =>
            {
                //tick.tickevents.Add(jsonparser.assembleBomb(e, "bomb_planted"));
            };

            Csgoparser.BombDefused += (object sender, BombEventArgs e) =>
            {
                //tick.tickevents.Add(jsonparser.assembleBomb(e, "bomb_defused"));
            };

            Csgoparser.BombExploded += (object sender, BombEventArgs e) =>
            {
                //tick.tickevents.Add(jsonparser.assembleBomb(e, "bomb_exploded"));
            };


            Csgoparser.BombDropped += (object sender, BombDropEventArgs e) =>
            {
                //tick.tickevents.Add(jsonparser.assembleBombState(e, "bomb_dropped"));
            };

            Csgoparser.BombPicked += (object sender, BombPickUpEventArgs e) =>
            {
                //tick.tickevents.Add(jsonparser.assembleBombState(e, "bomb_picked"));
            };
            #endregion

            #region Serverevents

            Csgoparser.PlayerBind += (sender, e) =>
            {
                if (hasMatchStarted && Csgoparser.PlayingParticipants.Contains(e.Player))
                {
                    Console.WriteLine("Tickid: " + tick_id);
                    CurrentTick.tickevents.Add(Jsonparser.assemblePlayerBind(e.Player));
                }
            };

            Csgoparser.PlayerDisconnect += (sender, e) =>
            {
                if (hasMatchStarted && Csgoparser.PlayingParticipants.Contains(e.Player))
                {
                    Console.WriteLine("Tickid: " + tick_id);
                    CurrentTick.tickevents.Add(Jsonparser.assemblePlayerDisconnected(e.Player));
                }
            };

            Csgoparser.BotTakeOver += (sender, e) =>
            {
                if (hasMatchStarted && Csgoparser.PlayingParticipants.Contains(e.Taker))
                {
                    Console.WriteLine("Tickid: " + tick_id);
                    CurrentTick.tickevents.Add(Jsonparser.assemblePlayerTakeOver(e));
                }
            };

            /*parser.PlayerTeam += (sender, e) =>
            {
                if (e.Swapped != null)
                    Console.WriteLine("Player swapped: " + e.Swapped.Name + " " + e.Swapped.SteamID + " Oldteam: "+ e.OldTeam + " Newteam: " + e.NewTeam +  "IsBot: " + e.IsBot + "Silent: " + e.Silent);
            };*/

            #endregion

            #region Futureevents
            /*
            //Extraevents maybe useful
            parser.RoundFinal += (object sender, RoundFinalEventArgs e) => {

            };
            parser.RoundMVP += (object sender, RoundMVPEventArgs e) => {

            };
            parser.RoundOfficiallyEnd += (object sender, RoundOfficiallyEndedEventArgs e) => {

            };
            parser.LastRoundHalf += (object sender, LastRoundHalfEventArgs e) => {

            };
            */
            #endregion

            // TickDone at last!! Otherwise events following this region are not tracked
            #region Tickevent / Ticklogic
            //Assemble a tick object with the above gameevents
            Csgoparser.TickDone += (sender, e) =>
            {
                if (!hasMatchStarted) //Dont count ticks if the game has not started already (dismissing warmup and knife-phase for official matches)
                    return;

                // 8 = 250ms, 16 = 500ms usw
                var updaterate = 8 * (int)(Math.Ceiling(Csgoparser.TickRate / 32));

                if (updaterate == 0)
                   throw new Exception("Updaterate cannot be Zero");
                // Dump playerpositions every at a given updaterate according to the tickrate
                if ((tick_id % updaterate == 0) && hasFreeezEnded)
                    foreach (var player in Csgoparser.PlayingParticipants.Where(player => !alreadytracked.Contains(player)))
                        CurrentTick.tickevents.Add(Jsonparser.AssemblePlayerPosition(player));
                    
                

                tick_id++;
                alreadytracked.Clear();
            };

            //
            // MAIN PARSING LOOP
            //
            try
            {
                //Parse tickwise and add the resulting tick to the round object
                while (Csgoparser.ParseNextTick())
                {
                    if (hasMatchStarted)
                    {
                        CurrentTick.tick_id = tick_id;
                        //Tickevents were registered
                        if (CurrentTick.tickevents.Count != 0)
                        {
                            CurrentRound.ticks.Add(CurrentTick);
                            tickcount++;
                            CurrentTick = new Tick();
                            CurrentTick.tickevents = new List<Event>();
                        }

                    }

                }
                Console.WriteLine("Parsed ticks: " + tick_id + "\n");
                Console.WriteLine("NOT empty ticks: " + tickcount + "\n");

            }
            catch (System.IO.EndOfStreamException e)
            {
                Console.WriteLine("Problem with tick-parsing. Is your .dem valid? See this projects github page for more info.\n");
                Console.WriteLine("Stacktrace: " + e.StackTrace + "\n");
                Jsonparser.StopParser();
                Watch.Stop();
            }
            #endregion

        }

    }

}