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
using global::GameStateGenerators;

namespace GameStateGenerators
{
    public class GameStateGenerator : IGameStateGenerator
    {

        /// <summary>
        /// Parser assembling and disassembling objects later parsed with Newtonsoft.JSON
        /// </summary>
        private static JSONParser jsonparser;

        /// <summary>
        /// CSGO replay parser TODO: make generator and jsonparser generic to demo parser
        /// </summary>
        private static DemoParser parser;

        /// <summary>
        /// Current task containg parsing information and data
        /// </summary>
        private static ParseTask ptask;


        //
        //  Objects for JSON-Serialization
        //
        public Match Match;
        public Round CurrentRound;
        public Tick CurrentTick;

        //JSON holding the whole gamestate - delete this with GC to prevent unnecessary RAM usage!!
        public ReplayGamestate GameState; 


        //
        // Helping variables
        //
        #region Variables
        public List<Player> alreadytracked;

        public bool hasMatchStarted = false;
        public bool hasRoundStarted = false;
        public bool hasFreeezEnded = false;

        public int positioninterval; // in ms

        public int tick_id = 0;
        public int round_id = 0;

        public int tickcount = 0;
#endregion

        //
        //
        //
        //
        //
        // TODO:    1) use de-/serialization and streams for less GC and memory consumption? - most likely not useful cause string parsing is shitty
        //          6) Improve code around jsonparser - too many functions for similar tasks(see player/playerdetailed/withitems, bomb, nades) -> maybe use anonymous types
        //          9) gui communication - parsing is currently blocking UI update AND error handling missing(feedback again)
        //          11) implement threads?
        //          12) finish new events
        //

        /// <summary>
        /// Watch to measure generation process
        /// </summary>
        public Stopwatch Watch;

        /// <summary>
        /// Initializes the generator or resets it if a demo was parser before
        /// </summary>
        public void InitializeGenerator()
        {
            Match = new Match();
            CurrentRound = new Round();
            CurrentTick = new Tick();
            GameState = new ReplayGamestate();

            alreadytracked = new List<Player>();

            hasMatchStarted = false;
            hasRoundStarted = false;
            hasFreeezEnded = false;

            positioninterval = ptask.positioninterval;

            tick_id = 0;
            round_id = 0;

            tickcount = 0;

            InitWatch();

            //Parser to transform DemoParser events to JSON format //TODO choose correct parser type
            jsonparser = new CSGOJSONParser(ptask.destpath, ptask.settings);

            //Init lists
            Match.rounds = new List<Round>();
            CurrentRound.ticks = new List<Tick>();
            CurrentTick.tickevents = new List<Event>();
        }

        /// <summary>
        /// Writes the gamestate to a JSON file at the same path
        /// </summary>
        /// <param name="parser"></param>
        /// <param name="path"></param>
        public void GenerateJSONFile(DemoParser newdemoparser, ParseTask newtask)
        {
            ptask = newtask;
            parser = newdemoparser;

            InitializeGenerator();

            GenerateGamestate();

            /*for (int i = 0; i < parser.TickRate; i++) //Threadamount depending on tickrate
            {
                ThreadPool.QueueUserWorkItem(GenerateGamestate, i);

            }*/

            //Dump the complete gamestate object into JSON-file and do not pretty print(memory expensive)
            jsonparser.DumpJSONFile(GameState, ptask.usepretty);

            PrintWatch();

            //Work is done.
            CleanUp();

        }

        /// <summary>
        /// Returns a string of the serialized gamestate object
        /// </summary>
        public string GenerateJSONString(DemoParser newdemoparser, ParseTask newtask)
        {
            ptask = newtask;
            parser = newdemoparser;

            InitializeGenerator();

            GenerateGamestate(); // Fills variable gs with gamestateobject
            string gsstr = "";
            try
            {
                gsstr = jsonparser.DumpJSONString(GameState, ptask.usepretty);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                Console.ReadLine();
            }

            PrintWatch();

            CleanUp();

            return gsstr;
        }

        /// <summary>
        /// Override this function!
        /// </summary>
        public virtual void GenerateGamestate() { }

        //
        //
        // HELPING FUNCTIONS
        //
        //


        /// <summary>
        /// Measure time to roughly check performance
        /// </summary>
        private void InitWatch()
        {
            if (Watch == null)
            {
                Watch = System.Diagnostics.Stopwatch.StartNew();
            }
            else
            {
                Watch.Restart();
            }
        }

        private void PrintWatch()
        {
            //Fancy calculations and feedback for 10/10 user reviews.
            Watch.Stop();
            var elapsedMs = Watch.ElapsedMilliseconds;
            var sec = elapsedMs / 1000.0f;

            Console.WriteLine("Time to parse: " + ptask.srcpath + ": " + sec + "sec. \n");
            Console.WriteLine("You can find the corresponding JSON at the same path. \n");
        }

        public void CleanUp()
        {
            jsonparser.StopParser();
            ptask = null;
            GameState = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

    }

}

