# DeepGameAnalysis - JSON De-/Serialization of replay data
This package builds the basis for parsing demodata to a uniform json format. All games supported by our encounter detection
need the same file format to allow running our algorithm. The data used by our algorithm uses a JSON-file(or database type). This file needs a unique setup and layout and the game needs to fulfill certain expcetations:

  - The game is tick-based
  - The parser for this games replay data returns:
    - entities with attributes such as id,name,health,weapons,line of sight
    - events performed/caused by these entities
    - positional updates of entities OR entities at every given tick
  
# Building a JSON containing all relevant replay data
The JSON-object containing all replay information is called ReplayGamestate, although game state can be used in different contexts. (ref)


