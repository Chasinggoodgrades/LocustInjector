//globals from CommandsManager:
constant boolean LIBRARY_CommandsManager=true
        // Command Tiers
integer COMMAND_TIER_ALL= 0
integer COMMAND_TIER_RED= 1
integer COMMAND_TIER_VIP= 2
integer COMMAND_TIER_ADMIN= 3
integer COMMAND_TIER_DEVELOPER= 4
        
// These Values Are Manually Put in For Locust Injector
integer TEMP_INT= 0
real TEMP_REAL= 0.0
boolean TEMP_BOOL = false
player TEMP_PLAYER = null

integer TotalCommands= 0

        // Command Storage
integer array CommandsManager___CmdNameHash
integer array CommandsManager___CmdTier
string array CommandsManager___CmdName
string array CommandsManager___CmdAlias
string array CommandsManager___CmdArgDesc
string array CommandsManager___CmdDesc
integer array CommandsManager___CmdActionIndex
trigger array CommandsManager___CmdTrigger

string array CommandsManager___CmdArgs
integer CommandsManager___CmdArgCount= 0

hashtable CommandsManager___CmdHash= InitHashtable()

        // Temp player resolution
player array CommandsManager___TempPlayers
integer CommandsManager___TempCount= 0

        // Role lists
string array CommandsManager___Developers
string array CommandsManager___Admins
string array CommandsManager___Vips
integer CommandsManager___DevCount= 0
integer CommandsManager___AdminCount= 0
integer CommandsManager___VipCount= 0

force CommandsManager___ResolvedForce= CreateForce()

// Hero Finder (populates Escapers[] for human, non-computer players)
unit array Escapers
timer HeroFinderTimer = CreateTimer()
group HeroFindGroup = CreateGroup()
integer TotalHumanPlayers = 0
integer FoundHeroCount = 0

//endglobals from CommandsManager