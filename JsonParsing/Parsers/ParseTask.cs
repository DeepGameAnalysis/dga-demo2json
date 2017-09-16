using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace JSONParsing
{
    /// <summary>
    /// Class holding every data on how and where to parse.
    /// </summary>
    public class ParseTaskSettings
    {
        public string SrcPath { get; set; }

        public string DestPath { get; set; }

        public bool usepretty { get; set; }

        public bool ShowSteps { get; set; }

        public int PositionUpdateInterval { get; set; }

        public bool Specialevents { get; set; }

        public bool HighDetailPlayer { get; set; }

        public JsonSerializerSettings Settings { get; set; }

    }
}
