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
    public class AOE2HDGameStateGenerator : GameStateGenerator, IGameStateGenerator
    {

        /// <summary>
        /// AOE2HD replay parser
        /// </summary>
        private static DemoParser Aoe2hdparser; //TODO

        /// <summary>
        /// Parser assembling and disassembling objects later parsed with Newtonsoft.JSON - type depends on the current game
        /// </summary>
        private static AOE2HDJSONParser Jsonparser;

        /// <summary>
        /// Constructor - Create a new CSGO Generator to generate a new Gamestate 
        /// </summary>
        /// <param name="newdemoparser"></param>
        /// <param name="parsetask"></param>
        public AOE2HDGameStateGenerator(DemoParser newdemoparser, ParseTaskSettings parsetask) : base(parsetask)
        {
            Aoe2hdparser = newdemoparser;
            //Parser to transform DemoParser events to JSON format
            Jsonparser = new AOE2HDJSONParser(parsetask.DestPath, parsetask.Settings);
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
            Aoe2hdparser.ParseHeader();
            return Aoe2hdparser.Map;
        }


        /// <summary>
        /// Assembles the gamestate object from data given by the demoparser.
        /// </summary>
        /// <param name="parser"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        override public void GenerateGamestate()
        {
            if (Aoe2hdparser == null) throw new Exception("No parser set");

        }

    }

}