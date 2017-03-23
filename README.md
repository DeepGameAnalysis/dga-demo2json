# GameAnalysisUniverse - JSON (De-/Serialization of replaydata)
This package builds the basis for parsing demodata to uniform json format. All games supported by our encounter detection
need the same file format to allow running our algorithm. The data used by our algorithm uses a gamestate JSON-file. This file needs
a unique setup and layout and the game needs to fulfill certain expcetations:
  - The game is tick-based
  - The parser for this games replay data returns events as well as positional updates of entities
  

