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
        /// Parser assembling and disassembling objects later parsed with Newtonsoft.JSON
        /// </summary>
        private static CSGOJSONParser jsonparser;

        /// <summary>
        /// CSGO replay parser
        /// </summary>
        private static DemoParser parser;

        /// <summary>
        /// Current task containg parsing information and data
        /// </summary>
        private static ParseTask ptask;


        public string PeakMapname(DemoParser newdemoparser, ParseTask newtask)
        {
            ptask = newtask;
            parser = newdemoparser;

            InitializeGenerator();

            parser.ParseHeader();

            return parser.Map;
        }


        /// <summary>
        /// Assembles the gamestate object from data given by the demoparser.
        /// </summary>
        /// <param name="parser"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        override public void GenerateGamestate()
        {
            parser.ParseHeader();

            #region Main Gameevents
            //Start writing the gamestate object
            parser.MatchStarted += (sender, e) =>
            {
                hasMatchStarted = true;
                //Assign Gamemetadata
                GameState.meta = jsonparser.AssembleGamemeta(parser.Map, parser.TickRate, parser.PlayingParticipants);
            };

            //Assign match object
            parser.WinPanelMatch += (sender, e) =>
            {
                if (hasMatchStarted)
                    GameState.match = Match;
                hasMatchStarted = false;

            };

            //Start writing a round object
            parser.RoundStart += (sender, e) =>
            {
                if (hasMatchStarted)
                {
                    hasRoundStarted = true;
                    round_id++;
                    CurrentRound.round_id = round_id;
                }

            };

            //Add round object to match object
            parser.RoundEnd += (sender, e) =>
            {
                if (hasMatchStarted)
                {
                    if (hasRoundStarted) //TODO: maybe round fires false -> do in tickdone event (see github issues of DemoInfo)
                    {
                        CurrentRound.winner_team = e.Winner.ToString();
                        Match.rounds.Add(CurrentRound);
                        CurrentRound = new Round();
                        CurrentRound.ticks = new List<Tick>();
                    }

                    hasRoundStarted = false;

                }

            };

            parser.FreezetimeEnded += (object sender, FreezetimeEndedEventArgs e) =>
            {
                if (hasMatchStarted)
                    hasFreeezEnded = true; //Just capture movement after freezetime has ended
            };


            #endregion

            #region Playerevents

            parser.WeaponFired += (object sender, WeaponFiredEventArgs we) =>
            {
                if (hasMatchStarted)
                    CurrentTick.tickevents.Add(jsonparser.AssembleWeaponFire(we));
            };

            parser.PlayerSpotted += (sender, e) =>
            {
                if (hasMatchStarted)
                    CurrentTick.tickevents.Add(jsonparser.AssemblePlayerSpotted(e));
            };

            parser.WeaponReload += (object sender, WeaponReloadEventArgs we) =>
            {
                if (hasMatchStarted)
                {
                    //tick.tickevents.Add(jsonparser.assembleWeaponReload(we));
                }
            };

            parser.WeaponFiredEmpty += (object sender, WeaponFiredEmptyEventArgs we) =>
            {
                if (hasMatchStarted)
                {
                    //tick.tickevents.Add(jsonparser.assembleWeaponFireEmpty(we));
                }
            };

            parser.PlayerJumped += (sender, e) =>
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

            parser.PlayerFallen += (sender, e) =>
            {
                if (hasMatchStarted)
                    if (e.Fallen != null)
                    {
                        //tick.tickevents.Add(jsonparser.assemblePlayerFallen(e));
                    }
            };

            parser.PlayerStepped += (sender, e) =>
            {
                if (hasMatchStarted)
                {
                    if (e.Stepper != null && parser.PlayingParticipants.Contains(e.Stepper))
                    { //Prevent spectating players from producing steps 
                        CurrentTick.tickevents.Add(jsonparser.AssemblePlayerStepped(e));
                        alreadytracked.Add(e.Stepper);
                    }
                }

            };

            parser.PlayerKilled += (object sender, PlayerKilledEventArgs e) =>
            {
                if (hasMatchStarted)
                {
                    //the killer is null if victim is killed by the world - eg. by falling
                    if (e.Killer != null)
                    {
                        CurrentTick.tickevents.Add(jsonparser.AssemblePlayerKilled(e));
                        alreadytracked.Add(e.Killer);
                        alreadytracked.Add(e.Victim);
                    }
                }

            };

            parser.PlayerHurt += (object sender, PlayerHurtEventArgs e) =>
            {
                if (hasMatchStarted)
                {
                    //the attacker is null if victim is damaged by the world - eg. by falling
                    if (e.Attacker != null)
                    {
                        CurrentTick.tickevents.Add(jsonparser.AssemblePlayerHurt(e));
                        alreadytracked.Add(e.Attacker);
                        alreadytracked.Add(e.Victim);
                    }
                }

            };
            #endregion

            #region Nadeevents
            //Nade (Smoke Fire Decoy Flashbang and HE) events
            parser.ExplosiveNadeExploded += (object sender, GrenadeEventArgs e) =>
                {
                    if (e.ThrownBy != null && hasMatchStarted)
                        CurrentTick.tickevents.Add(jsonparser.assembleNade(e, "hegrenade_exploded"));
                };

            parser.FireNadeStarted += (object sender, FireEventArgs e) =>
                    {
                        if (e.ThrownBy != null && hasMatchStarted)
                            CurrentTick.tickevents.Add(jsonparser.assembleNade(e, "firenade_exploded"));
                    };

            parser.FireNadeEnded += (object sender, FireEventArgs e) =>
                        {
                            if (e.ThrownBy != null && hasMatchStarted)
                                CurrentTick.tickevents.Add(jsonparser.assembleNade(e, "firenade_ended"));
                        };

            parser.SmokeNadeStarted += (object sender, SmokeEventArgs e) =>
            {
                if (e.ThrownBy != null && hasMatchStarted)
                    CurrentTick.tickevents.Add(jsonparser.assembleNade(e, "smoke_exploded"));
            };


            parser.SmokeNadeEnded += (object sender, SmokeEventArgs e) =>
            {
                if (e.ThrownBy != null && hasMatchStarted)
                    CurrentTick.tickevents.Add(jsonparser.assembleNade(e, "smoke_ended"));
            };

            parser.DecoyNadeStarted += (object sender, DecoyEventArgs e) =>
            {
                if (e.ThrownBy != null && hasMatchStarted)
                    CurrentTick.tickevents.Add(jsonparser.assembleNade(e, "decoy_exploded"));
            };

            parser.DecoyNadeEnded += (object sender, DecoyEventArgs e) =>
            {
                if (e.ThrownBy != null && hasMatchStarted)
                    CurrentTick.tickevents.Add(jsonparser.assembleNade(e, "decoy_ended"));
            };

            parser.FlashNadeExploded += (object sender, FlashEventArgs e) =>
            {
                if (e.ThrownBy != null && hasMatchStarted)
                    CurrentTick.tickevents.Add(jsonparser.assembleNade(e, "flash_exploded"));
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
            parser.BombAbortPlant += (object sender, BombEventArgs e) =>
            {
                //tick.tickevents.Add(jsonparser.assembleBomb(e, "bomb_abort_plant"));
            };

            parser.BombAbortDefuse += (object sender, BombDefuseEventArgs e) =>
            {
                //tick.tickevents.Add(jsonparser.assembleBombDefuse(e, "bomb_abort_defuse"));
            };

            parser.BombBeginPlant += (object sender, BombEventArgs e) =>
            {
                //tick.tickevents.Add(jsonparser.assembleBomb(e, "bomb_begin_plant"));
            };

            parser.BombBeginDefuse += (object sender, BombDefuseEventArgs e) =>
            {
                //tick.tickevents.Add(jsonparser.assembleBombDefuse(e, "bomb_begin_defuse"));
            };

            parser.BombPlanted += (object sender, BombEventArgs e) =>
            {
                //tick.tickevents.Add(jsonparser.assembleBomb(e, "bomb_planted"));
            };

            parser.BombDefused += (object sender, BombEventArgs e) =>
            {
                //tick.tickevents.Add(jsonparser.assembleBomb(e, "bomb_defused"));
            };

            parser.BombExploded += (object sender, BombEventArgs e) =>
            {
                //tick.tickevents.Add(jsonparser.assembleBomb(e, "bomb_exploded"));
            };


            parser.BombDropped += (object sender, BombDropEventArgs e) =>
            {
                //tick.tickevents.Add(jsonparser.assembleBombState(e, "bomb_dropped"));
            };

            parser.BombPicked += (object sender, BombPickUpEventArgs e) =>
            {
                //tick.tickevents.Add(jsonparser.assembleBombState(e, "bomb_picked"));
            };
            #endregion

            #region Serverevents

            parser.PlayerBind += (sender, e) =>
            {
                if (hasMatchStarted && parser.PlayingParticipants.Contains(e.Player))
                {
                    Console.WriteLine("Tickid: " + tick_id);
                    CurrentTick.tickevents.Add(jsonparser.assemblePlayerBind(e.Player));
                }
            };

            parser.PlayerDisconnect += (sender, e) =>
            {
                if (hasMatchStarted && parser.PlayingParticipants.Contains(e.Player))
                {
                    Console.WriteLine("Tickid: " + tick_id);
                    CurrentTick.tickevents.Add(jsonparser.assemblePlayerDisconnected(e.Player));
                }
            };

            parser.BotTakeOver += (sender, e) =>
            {
                if (hasMatchStarted && parser.PlayingParticipants.Contains(e.Taker))
                {
                    Console.WriteLine("Tickid: " + tick_id);
                    CurrentTick.tickevents.Add(jsonparser.assemblePlayerTakeOver(e));
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
            parser.TickDone += (sender, e) =>
            {
                if (!hasMatchStarted) //Dont count ticks if the game has not started already (dismissing warmup and knife-phase for official matches)
                    return;

                // 8 = 250ms, 16 = 500ms usw
                var updaterate = 8 * (int)(Math.Ceiling(parser.TickRate / 32));

                if (updaterate == 0)
                   throw new Exception("Updaterate cannot be Zero");
                // Dump playerpositions every at a given updaterate according to the tickrate
                if ((tick_id % updaterate == 0) && hasFreeezEnded)
                    foreach (var player in parser.PlayingParticipants.Where(player => !alreadytracked.Contains(player)))
                        CurrentTick.tickevents.Add(jsonparser.AssemblePlayerPosition(player));
                    
                

                tick_id++;
                alreadytracked.Clear();
            };

            //
            // MAIN PARSING LOOP
            //
            try
            {
                //Parse tickwise and add the resulting tick to the round object
                while (parser.ParseNextTick())
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
                jsonparser.StopParser();
                Watch.Stop();
            }
            #endregion

        }

    }

}