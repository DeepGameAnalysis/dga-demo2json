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
    class AOE2HDJSONParser : JSONParser, IJSONParsing
    {

        public AOE2HDJSONParser(string path, JsonSerializerSettings settings) : base(path, settings)
        {
        }

        
    }
}