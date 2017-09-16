using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DemoInfo;
using Newtonsoft.Json;
using Data.Gamestate;
using MathNet.Spatial.Euclidean;

namespace JSONParsing
{
    public class JSONParser
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
        public void DumpJSONFile(ReplayGamestate gs, bool prettyjson)
        {
            Formatting f = Formatting.None;
            if (prettyjson)
                f = Formatting.Indented;

            outputStream.Write(JsonConvert.SerializeObject(gs, settings));
        }


        public ReplayGamestate DeserializeGamestateString(string gamestatestring)
        {
            return JsonConvert.DeserializeObject<ReplayGamestate>(gamestatestring, settings);
        }


        /// <summary>
        /// Dumps gamestate in a string
        /// </summary>
        /// <param name="gs"></param>
        /// <param name="prettyjson"></param>
        public string DumpJSONString(ReplayGamestate gs, bool prettyjson)
        {
            Formatting f = Formatting.None;
            if (prettyjson)
                f = Formatting.Indented;
            return JsonConvert.SerializeObject(gs, f);
        }

        public void StopParser()
        {
            outputStream.Close();
        }
        
    }
}