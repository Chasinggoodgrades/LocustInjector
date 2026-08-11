//library CommandsManager:

    // ================================================================
    // Role Service
    // ================================================================
    function CommandsManager___InitRoles takes nothing returns nothing
        // Developers
        set CommandsManager___Developers[CommandsManager___DevCount]="Aches#1817"
        set CommandsManager___DevCount=CommandsManager___DevCount + 1
       
        set CommandsManager___Developers[CommandsManager___DevCount]="WorldEdit"
        set CommandsManager___DevCount=CommandsManager___DevCount + 1
       
        set CommandsManager___Developers[CommandsManager___DevCount]="JamesFranco#11719"
        set CommandsManager___DevCount=CommandsManager___DevCount + 1
        set CommandsManager___Developers[CommandsManager___DevCount]="hoff#11404"
        set CommandsManager___DevCount=CommandsManager___DevCount + 1
        // Admins
        set CommandsManager___Admins[CommandsManager___AdminCount]="SomeRandomClown#12805"
        set CommandsManager___AdminCount=CommandsManager___AdminCount + 1
       
        // VIPs
        set CommandsManager___Vips[CommandsManager___VipCount]="AnotherRandomClown#23315"
        set CommandsManager___VipCount=CommandsManager___VipCount + 1
    endfunction

    function GetPlayerCommandTier takes player p returns integer
        local string name= GetPlayerName(p)
        local integer i= 0
        // if RuntimeTier[GetPlayerId(p)] != -1 then
        // return RuntimeTier[GetPlayerId(p)]
        // endif
        loop
            exitwhen i >= CommandsManager___DevCount
            if name == CommandsManager___Developers[i] then
                return COMMAND_TIER_DEVELOPER
            endif
            set i=i + 1
        endloop
        set i=0
        loop
            exitwhen i >= CommandsManager___AdminCount
            if name == CommandsManager___Admins[i] then
                return COMMAND_TIER_ADMIN
            endif
            set i=i + 1
        endloop
        set i=0
        loop
            exitwhen i >= CommandsManager___VipCount
            if name == CommandsManager___Vips[i] then
                return COMMAND_TIER_VIP
            endif
            set i=i + 1
        endloop
        if GetPlayerId(p) == 0 then
            return COMMAND_TIER_RED
        endif
        return COMMAND_TIER_ALL
    endfunction

    // ================================================================
    // Helper Functions
    // ================================================================
    function CommandsManager___StringContains takes string source,string find returns boolean
        local integer srcLen= StringLength(source)
        local integer findLen= StringLength(find)
        local integer i= 0
        if findLen <= 0 or findLen > srcLen then
            return false
        endif
        loop
            exitwhen i > ( srcLen - findLen )
            if SubString(source, i, i + findLen) == find then
                return true
            endif
            set i=i + 1
        endloop
       
        return false
    endfunction

    function CommandsManager___ClearArgs takes nothing returns nothing
        set CommandsManager___CmdArgCount=0
    endfunction

    function ParseArgs takes string msg returns nothing
        local integer i= 0
        local integer start= 0
        local string token= ""
        set CommandsManager___CmdArgCount=0 // INLINED!!
        loop
            exitwhen i >= StringLength(msg)
            if SubString(msg, i, i + 1) == " " then
                set token=SubString(msg, start, i)
                if token != "" then
                    set CommandsManager___CmdArgs[CommandsManager___CmdArgCount]=token
                    set CommandsManager___CmdArgCount=CommandsManager___CmdArgCount + 1
                endif
                set start=i + 1
            endif
            set i=i + 1
        endloop
        if start < StringLength(msg) then
            set token=SubString(msg, start, StringLength(msg))
            if token != "" then
                set CommandsManager___CmdArgs[CommandsManager___CmdArgCount]=token
                set CommandsManager___CmdArgCount=CommandsManager___CmdArgCount + 1
            endif
        endif
    endfunction

    // ================================================================
    // Player Resolution
    // ================================================================
    function CommandsManager___ClearResolvedGroup takes nothing returns nothing
        call ForceClear(CommandsManager___ResolvedForce)
    endfunction

    function ResolvePlayerIdArray takes string arg returns nothing
        local string larg= StringCase(arg, false)
        local integer i= 0
        local player p
        local boolean matched= false
        call ForceClear(CommandsManager___ResolvedForce) // INLINED!!
        // 1. Empty arg = Self
        if arg == "" or arg == null then
            call ForceAddPlayer(CommandsManager___ResolvedForce, GetTriggerPlayer())
            return
        endif
        // 2. "All" or "a" = Everyone playing
        if larg == "all" or larg == "a" then
            set i=0
            loop
                exitwhen i >= 24
                set p=Player(i)
                if GetPlayerSlotState(p) == PLAYER_SLOT_STATE_PLAYING then
                    call ForceAddPlayer(CommandsManager___ResolvedForce, p)
                endif
                set i=i + 1
            endloop
            return
        endif
        // 3. Number ID
        if S2I(arg) > 0 then
            set i=S2I(arg) - 1
            if i >= 0 and i < 24 then
                call ForceAddPlayer(CommandsManager___ResolvedForce, Player(i))
            endif
            return
        endif
        // 4. Name match
        set i=0
        loop
            exitwhen i >= 24
            set p=Player(i)
            if GetPlayerSlotState(p) == PLAYER_SLOT_STATE_PLAYING then
                if CommandsManager___StringContains(StringCase(GetPlayerName(p), false) , larg) then
                    call ForceAddPlayer(CommandsManager___ResolvedForce, p)
                    set matched=true
                    exitwhen true
                endif
            endif
            set i=i + 1
        endloop
        if not matched then
            call DisplayTimedTextToPlayer(GetTriggerPlayer(), 0, 0, 5, "|cffffcc00Invalid player: |r" + arg)
        endif
    endfunction

    function ForEachResolvedPlayer takes code actionFunc returns nothing
        call ForForce(CommandsManager___ResolvedForce, actionFunc)
    endfunction

    function GetResolvedForce takes nothing returns force
        return CommandsManager___ResolvedForce
    endfunction

    // ================================================================
    // Command Registration
    // ================================================================
    function RegisterCommand takes string cmdName,string cmdAlias,integer tier,string argDesc,string description,code func returns nothing
        local integer id= TotalCommands
        local integer pos= 0
        local integer comma= 0
        local string al= ""
        set CommandsManager___CmdName[id]=cmdName
        set CommandsManager___CmdAlias[id]=cmdAlias
        set CommandsManager___CmdTier[id]=tier
        set CommandsManager___CmdArgDesc[id]=argDesc
        set CommandsManager___CmdDesc[id]=description
       
        set CommandsManager___CmdTrigger[id]=CreateTrigger()
        call TriggerAddAction(CommandsManager___CmdTrigger[id], func)
        // Main command
        call SaveInteger(CommandsManager___CmdHash, 0, StringHash(StringCase(cmdName, false)), id)
        if cmdAlias != "" then
            set pos=0
            loop
                set comma=pos
                loop
                    exitwhen comma >= StringLength(cmdAlias) or SubString(cmdAlias, comma, comma + 1) == ","
                    set comma=comma + 1
                endloop
                set al=SubString(cmdAlias, pos, comma)
                if al != "" then
                    call SaveInteger(CommandsManager___CmdHash, 0, StringHash(StringCase(al, false)), id)
                endif
                set pos=comma + 1
                exitwhen pos >= StringLength(cmdAlias)
            endloop
        endif
        set TotalCommands=TotalCommands + 1
    endfunction

    function RegisterCommandTrigger takes string cmdName,string cmdAlias,integer tier,string argDesc,string description,trigger trig returns nothing
        local integer id= TotalCommands
        local integer pos= 0
        local integer comma= 0
        local string al= ""
        set CommandsManager___CmdName[id]=cmdName
        set CommandsManager___CmdAlias[id]=cmdAlias
        set CommandsManager___CmdTier[id]=tier
        set CommandsManager___CmdArgDesc[id]=argDesc
        set CommandsManager___CmdDesc[id]=description
       
        // Primarily to support and store GUI triggers, gross but whatever
        set CommandsManager___CmdTrigger[id]=trig
        call SaveInteger(CommandsManager___CmdHash, 0, StringHash(StringCase(cmdName, false)), id)
        if cmdAlias != "" then
            set pos=0
            loop
                set comma=pos
                loop
                    exitwhen comma >= StringLength(cmdAlias) or SubString(cmdAlias, comma, comma + 1) == ","
                    set comma=comma + 1
                endloop
                set al=SubString(cmdAlias, pos, comma)
                if al != "" then
                    call SaveInteger(CommandsManager___CmdHash, 0, StringHash(StringCase(al, false)), id)
                endif
                set pos=comma + 1
                exitwhen pos >= StringLength(cmdAlias)
            endloop
        endif
        set TotalCommands=TotalCommands + 1
    endfunction

    function CommandsManager___GetCommandIndex takes string name returns integer
        local integer hash= StringHash(StringCase(name, false))
        if HaveSavedInteger(CommandsManager___CmdHash, 0, hash) then
            return LoadInteger(CommandsManager___CmdHash, 0, hash)
        endif
        return - 1
    endfunction

    function CommandsManager___OnPlayerChat takes nothing returns nothing
        local string msg= GetEventPlayerChatString()
        local string commandStr
        local integer spacePos
        local integer cmdIndex
        local player p= GetTriggerPlayer()
        if SubString(msg, 0, 1) != "-" then
            return
        endif
        set msg=SubString(msg, 1, StringLength(msg))
        set spacePos=0
        loop
            exitwhen spacePos >= StringLength(msg) or SubString(msg, spacePos, spacePos + 1) == " "
            set spacePos=spacePos + 1
        endloop
        set commandStr=SubString(msg, 0, spacePos)
       
        if spacePos < StringLength(msg) then
            call ParseArgs(SubString(msg, spacePos + 1, StringLength(msg)))
        else
            call ParseArgs("")
        endif
        set cmdIndex=CommandsManager___GetCommandIndex(commandStr)
        if cmdIndex == - 1 then
            call DisplayTimedTextToPlayer(p, 0, 0, 5, "|cffff0000Unknown command: -|r" + commandStr)
            return
        endif
        if GetPlayerCommandTier(p) < CommandsManager___CmdTier[cmdIndex] then
            call DisplayTimedTextToPlayer(p, 0, 0, 5, "|cffffcc00Insufficient permission.|r")
            return
        endif
        call TriggerExecute(CommandsManager___CmdTrigger[cmdIndex])
    endfunction

    // Get argument by index (0 = first arg after command name)
    function GetArg takes integer index returns string
        if index < 0 or index >= CommandsManager___CmdArgCount then
            return ""
        endif
        return CommandsManager___CmdArgs[index]
    endfunction

    function GetArgCount takes nothing returns integer
        return CommandsManager___CmdArgCount
    endfunction

    function GetArgInt takes integer index returns integer
        return S2I(GetArg(index))
    endfunction

    function GetArgReal takes integer index returns real
        return S2R(GetArg(index))
    endfunction

    function GetArgBool takes integer index returns boolean
        local string s= StringCase(GetArg(index), false)
        return s == "on" or s == "true" or s == "1" or s == "yes"
    endfunction

    // ================================================================
    // Hero Finder -- Populates vAches_Escapers[] for human, non-computer players
    // Doesn't assume heroes exist at map init; polls every second until
    // every human playing slot has a hero recorded, then stops itself.
    // Won't overwrite anything already set (e.g. via Locust Injector).
    // ================================================================
    function CommandsManager___CountHumanPlayers takes nothing returns nothing
        local integer i = 0
        set TotalHumanPlayers = 0
        loop
            exitwhen i >= 24
            if GetPlayerSlotState(Player(i)) == PLAYER_SLOT_STATE_PLAYING and GetPlayerController(Player(i)) == MAP_CONTROL_USER then
                set TotalHumanPlayers = TotalHumanPlayers + 1
            endif
            set i = i + 1
        endloop
    endfunction

    // Added check for MoG3 style hero selectors
    function CommandsManager___HeroHasChanged takes nothing returns boolean
        local integer i = 0
        local player p
        loop
            exitwhen i >= 24
            set p = Player(i)
            if GetPlayerSlotState(p) == PLAYER_SLOT_STATE_PLAYING and GetPlayerController(Player(i)) == MAP_CONTROL_USER then
                if vAches_Escapers[GetConvertedPlayerId(p)] == null or GetUnitTypeId(vAches_Escapers[GetConvertedPlayerId(p)]) == 0 then
                    return true
                endif
            endif
            set i = i + 1
        endloop
        return false
    endfunction

    function CommandsManager___TryFindHeroes takes nothing returns nothing
        local integer i = 0
        local player p
        local unit u
        local unit found
        if not CommandsManager___HeroHasChanged() then
            if FoundHeroCount >= TotalHumanPlayers then
                call DisplayTimedTextToForce(GetPlayersAll(), 6.00, "|cFFFFFF00All maze heroes found.|r |cFF40E0D0-vAches|r")
                call PauseTimer(HeroFinderTimer)
                call DestroyTimer(HeroFinderTimer)
                set HeroFinderTimer = null
                return
            endif
        endif
        loop
            exitwhen i >= 24
            set p = Player(i)
            if GetPlayerSlotState(p) == PLAYER_SLOT_STATE_PLAYING then
                if vAches_Escapers[GetConvertedPlayerId(p)] == null or GetUnitTypeId(vAches_Escapers[GetConvertedPlayerId(p)]) == 0 then
                    set found = null
                    call GroupEnumUnitsOfPlayer(HeroFindGroup, p, null)
                    loop
                        set u = FirstOfGroup(HeroFindGroup)
                        exitwhen u == null
                        call GroupRemoveUnit(HeroFindGroup, u)
                        if found == null and IsUnitType(u, UNIT_TYPE_HERO) then
                            set found = u
                        endif
                    endloop
                    if found != null then
                        set vAches_Escapers[GetConvertedPlayerId(p)] = found
                        set FoundHeroCount = FoundHeroCount + 1
                    endif
                endif
            endif
            set i = i + 1
        endloop
    endfunction

    function CommandsManager___InitHeroFinder takes nothing returns nothing
        call CommandsManager___CountHumanPlayers()
        call CommandsManager___TryFindHeroes()
        // Timer increased to handle mazes like MoG3 that allow hero changes for the first 4 seconds with ESC key
        // The issue with MoG3 is that it will find a valid hero at Init, but the hero can then be cycled for first 4 seconds
        call TimerStart(HeroFinderTimer, 5.00, true, function CommandsManager___TryFindHeroes)
    endfunction

    // ================================================================
    // Help Command -- Keeping this built in.. No reason to have to put this in GUI lol
    // ================================================================
    function CommandsManager___Commands_Help takes nothing returns nothing
        local player p= GetTriggerPlayer()
        local string filter= GetArg(0)
        local integer tier= GetPlayerCommandTier(p)
        local string msg= "|cff00CED1Available Commands:|r\n"
        local integer i= 0
        local string aliasText= ""
        if filter != "" then
            set filter=StringCase(filter, false)
        endif
        loop
            exitwhen i >= TotalCommands
            if tier >= CommandsManager___CmdTier[i] then
               
            if filter == "" or CommandsManager___GetCommandIndex(filter) == i or CommandsManager___StringContains(StringCase(CommandsManager___CmdDesc[i], false) , filter) then
                    if CommandsManager___CmdAlias[i] != "" then
                        set aliasText=" (|cffbebebe" + CommandsManager___CmdAlias[i] + "|r)"
                    else
                        set aliasText=""
                    endif
                    set msg=msg + "|cffffff00" + CommandsManager___CmdName[i] + "|r" + aliasText + " |cffff0000[" + CommandsManager___CmdArgDesc[i] + "]|r - |cffFFD700" + CommandsManager___CmdDesc[i] + "|r\n"
                endif
            endif
            set i=i + 1
        endloop
        call DisplayTimedTextToPlayer(p, 0, 0, 15, msg)
    endfunction

    // ================================================================
    // KC Command -- Keeping this built in.. SINCE I HATE QUITTING GAMES MANUALLY
    // ================================================================
    function CommandsManager___Commands_KC takes nothing returns nothing
        local player p= GetTriggerPlayer()
        call CustomDefeatBJ(p, "|cffff0000You have been kicked from the game.|r")
    endfunction

    // ================================================================
    // Clear Command -- BUILT IN BABY
    // ================================================================
    function CommandsManager___Commands_Clear takes nothing returns nothing
        local player p= GetTriggerPlayer()
        if ( GetLocalPlayer() == p ) then
            call ClearTextMessages()
        endif
    endfunction

    // ================================================================
    // OHC Command -- BUILT IN BABY
    // ================================================================
    function CommandsManager___Commands_OHC takes nothing returns nothing
        local player p= GetTriggerPlayer()
        if ( GetLocalPlayer() == p ) then
            call SetCameraField(CAMERA_FIELD_ANGLE_OF_ATTACK, 280.00, 0)
        endif
    endfunction

    // ================================================================
    // Zoom Command -- BUILT IN BABY
    // ================================================================
    function CommandsManager___Commands_Zoom takes nothing returns nothing
        local player p= GetTriggerPlayer()
        local real zoom= (S2R(GetArg((0)))) // INLINED!!
        if ( GetLocalPlayer() == p ) then
            call SetCameraField(CAMERA_FIELD_TARGET_DISTANCE, zoom, 1.0)
        endif
    endfunction

    // ================================================================
    // Reset Command -- BUILT IN BABY
    // ================================================================
    function CommandsManager___Commands_Reset takes nothing returns nothing
        local player p= GetTriggerPlayer()
        if ( GetLocalPlayer() == p ) then
            call ResetToGameCamera(0)
            call SetCameraField(CAMERA_FIELD_TARGET_DISTANCE, 2400.0, 0.0)
        endif
    endfunction

    // ================================================================
    // Level Command Actions
    // ================================================================
    function CommandsManager___SetHeroLevelActions takes nothing returns nothing
        local integer p = GetConvertedPlayerId(GetEnumPlayer())
        local unit u = vAches_Escapers[p]

        if (u != null) then 
            call SetHeroLevelBJ(u, vAches_INT, true)
        endif
    endfunction

    // ================================================================
    // Level Command Setup For ResolvePlayerIdArray and ForEachResolvedPlayer
    // ================================================================
    function CommandsManager___Commands_SetHeroLevel takes nothing returns nothing
        set vAches_INT = GetArgInt(0)
        call ResolvePlayerIdArray(GetArg(1))
        call ForEachResolvedPlayer(function CommandsManager___SetHeroLevelActions)
    endfunction

    // ================================================================
    // LocustMe Command -- BUILT IN BABY
    // ================================================================
    function CommandsManager___Commands_LocustMe takes nothing returns nothing
        local player p = GetTriggerPlayer()
        local unit u = vAches_Escapers[GetConvertedPlayerId(p)]
        if u != null then
            call UnitAddAbility(u, 'Aloc')
            call ShowUnit(u, false)
            call UnitRemoveAbility(u, 'Aloc')
            call ShowUnit(u, true)
            call BlzSetUnitBooleanField(u, UNIT_BF_HERO_HIDE_HERO_DEATH_MESSAGE, true)
        endif
    endfunction

    // ================================================================
    // HeroeFix Command -- BUILT IN BABY
    // ================================================================
    function CommandsManager___Commands_HeroeFix takes nothing returns nothing
        // basically calling CommandsManager___TryFindHeroes() again and resetting count
        set FoundHeroCount = 0
        call CommandsManager___CountHumanPlayers()
        call CommandsManager___TryFindHeroes()
    endfunction

    // ================================================================
    // RTR Command -- BUILT IN BABY
    // ================================================================
    function CommandsManager___Commands_RTR takes nothing returns nothing
        local integer i = 0
        local integer j = 0
        local player p
        local unit u
        loop
            exitwhen i >= 24
            set p = Player(i)
            if GetPlayerSlotState(p) == PLAYER_SLOT_STATE_PLAYING then
                set u = vAches_Escapers[GetConvertedPlayerId(p)]
                call SetHeroLevelBJ(u, 10, false)
                // Max Unholy Aura, Endurance Aura, Wind Walk
                set j = 0
                loop
                    exitwhen j >= 3
                    call SelectHeroSkill(u, 'AOae')
                    call SelectHeroSkill(u, 'AUau')
                    call SelectHeroSkill(u, 'AOwk')
                    set j = j + 1
                endloop
                // Line below doesn't work. Cannot get around the object editor default speed limits
                // Perhaps have Wind Walk constantly activated? :)
                call SetUnitMoveSpeed(u, 522.00)
            endif
            set i = i + 1
        endloop
        call DisplayTimedTextToForce(GetPlayersAll(), 6.00, "|cFFFFFF00RTR Enabled.|r |cFF40E0D0-vAches&Hoff|r")
    endfunction

    // ===============================================================
    // Shareforce Command Actions
    // ================================================================
    function CommandsManager___ShareforceActions takes nothing returns nothing
        local player p = GetEnumPlayer()
        call SetPlayerAlliance(p, vAches_PLAYER, ALLIANCE_SHARED_CONTROL, vAches_BOOL)
    endfunction

    // ===============================================================
    // Shareforce Command Setup For ResolvePlayerIdArray and ForEachResolvedPlayer
    // ================================================================
    function CommandsManager___Commands_Shareforce takes nothing returns nothing
        set vAches_PLAYER = GetTriggerPlayer()
        set vAches_BOOL = GetArgBool(1)
        call ResolvePlayerIdArray(GetArg(0))
        call ForEachResolvedPlayer(function CommandsManager___ShareforceActions)
    endfunction

    // ===============================================================
    // Share Command Actions
    // ================================================================
    function CommandsManager___ShareActions takes nothing returns nothing
        local player p = GetEnumPlayer()
        call SetPlayerAlliance(vAches_PLAYER, p, ALLIANCE_SHARED_CONTROL, vAches_BOOL)
    endfunction

    // ===============================================================
    // Share Command Setup For ResolvePlayerIdArray and ForEachResolvedPlayer
    // ================================================================
    function CommandsManager___Commands_Share takes nothing returns nothing
        set vAches_PLAYER = GetTriggerPlayer()
        set vAches_BOOL = GetArgBool(1)
        call ResolvePlayerIdArray(GetArg(0))
        call ForEachResolvedPlayer(function CommandsManager___ShareActions)
    endfunction

    // ================================================================
    // Initialization
    // ================================================================
    function CommandsManager___InitCommands takes nothing returns nothing
        local trigger t= CreateTrigger()
        local integer i= 0
        call CommandsManager___InitRoles()
        call RegisterCommand("help" , "commands,?" , COMMAND_TIER_ALL , "[topic]" , "Displays commands" , function CommandsManager___Commands_Help)
        call RegisterCommand("kc" , "" , COMMAND_TIER_ALL , "" , "Kicks you from the game" , function CommandsManager___Commands_KC)
        call RegisterCommand("clear" , "c" , COMMAND_TIER_ALL , "" , "Clears messages from your screen" , function CommandsManager___Commands_Clear)
        call RegisterCommand("zoom" , "cam" , COMMAND_TIER_ALL , "[zoom]" , "Zooms your camera out to the passed value" , function CommandsManager___Commands_Zoom)
        call RegisterCommand("reset" , "r" , COMMAND_TIER_ALL , "" , "Resets your camera to 2400 zoom" , function CommandsManager___Commands_Reset)
        call RegisterCommand("overheadcam" , "ohc" , COMMAND_TIER_ALL , "" , "Sets your camera to an overhead view" , function CommandsManager___Commands_OHC)
        call RegisterCommand("level" , "lvl" , COMMAND_TIER_DEVELOPER , "[level]" , "Sets your hero's level to the passed value" , function CommandsManager___Commands_SetHeroLevel) // This is more or less an example rather than a useable command for anyone.
        call RegisterCommand("locustme" , "locust" , COMMAND_TIER_ALL , "" , "Applies the locust effect to your hero if it didn't on init or spawn" , function CommandsManager___Commands_LocustMe)
        call RegisterCommand("rtr" , "" , COMMAND_TIER_VIP , "" , "Runs the RTR Logic" , function CommandsManager___Commands_RTR)
        call RegisterCommand("herofix" , "" , COMMAND_TIER_ALL , "" , "Updates the vAches_Escapers[] array, only use if locust command isn't working." , function CommandsManager___Commands_HeroeFix)
        call RegisterCommand("shareforce" , "sf" , COMMAND_TIER_RED , "[player] [on/off]" , "Forces shared control of the passed players to yourself" , function CommandsManager___Commands_Shareforce)
        call RegisterCommand("share" , "s" , COMMAND_TIER_ALL , "[player] [on/off]" , "Shares control with the passed players" , function CommandsManager___Commands_Share)
        
        loop
            exitwhen i >= 24
            call TriggerRegisterPlayerChatEvent(t, Player(i), "-", false)
            set i=i + 1
        endloop
        call TriggerAddAction(t, function CommandsManager___OnPlayerChat)
        call CommandsManager___InitHeroFinder()
    endfunction

//library CommandsManager ends