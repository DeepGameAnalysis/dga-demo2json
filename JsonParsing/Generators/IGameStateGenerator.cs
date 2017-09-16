using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameStateGenerators
{
    /// <summary>
    /// Generators are the key element to build a gamestate file for later encounter detection and game analysis.
    /// The generator decides which data is included and at which parameters the data is included.
    /// </summary>
    interface IGameStateGenerator
    {
        void GenerateGamestate();
    }
}
