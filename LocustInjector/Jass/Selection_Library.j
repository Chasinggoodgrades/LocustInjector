
//===========================================================================
// Function: GetPlayerColorString
//===========================================================================
function GetPlayerColorString takes player p returns string
	//Credits to Andrewgosu from TH for the color codes//
	local playercolor c = GetPlayerColor(p)
	if c == PLAYER_COLOR_RED then
		return "|cffFF0202"
	elseif c == PLAYER_COLOR_BLUE then
		return "|cff0041FF"
	elseif c == PLAYER_COLOR_CYAN then
		return "|cff1BE5B8"
	elseif c == PLAYER_COLOR_PURPLE then
		return "|cff530080"
	elseif c == PLAYER_COLOR_YELLOW then
		return "|cffFFFC00"
	elseif c == PLAYER_COLOR_ORANGE then
		return "|cffFE890D"
	elseif c == PLAYER_COLOR_GREEN then
		return "|cff1FBF00"
	elseif c == PLAYER_COLOR_PINK then
		return "|cffE45AAF"
	elseif c == PLAYER_COLOR_LIGHT_GRAY then
		return "|cff949596"
	elseif c == PLAYER_COLOR_LIGHT_BLUE then
		return "|cff7DBEF1"
	elseif c == PLAYER_COLOR_AQUA then
		return "|cff0F6145"
	elseif c == PLAYER_COLOR_BROWN then
		return "|cff4D2903"
	elseif c == PLAYER_COLOR_MAROON then
		return "|cffbB0000"
	elseif c == PLAYER_COLOR_NAVY then
		return "|cff0000c3"
	elseif c == PLAYER_COLOR_TURQUOISE then
		return "|cff00eaff"
	elseif c == PLAYER_COLOR_VIOLET then
		return "|cffbe00fe"
	elseif c == PLAYER_COLOR_WHEAT then
		return "|cffebcd87"
	elseif c == PLAYER_COLOR_PEACH then
		return "|cfff8a48b"
	elseif c == PLAYER_COLOR_MINT then
		return "|cffbfff80"
	elseif c == PLAYER_COLOR_LAVENDER then
		return "|cffdcb9eb"
	elseif c == PLAYER_COLOR_COAL then
		return "|cff282828"
	elseif c == PLAYER_COLOR_SNOW then
		return "|cffebf0ff"
	elseif c == PLAYER_COLOR_EMERALD then
		return "|cff00781e"
	elseif c == PLAYER_COLOR_PEANUT then
		return "|cffa46f33"
	else
		return "|cffFFFFFF"
	endif
endfunction

//===========================================================================
// Trigger: Adding Unit Selection Event To Display PlayerNames
//===========================================================================
function Trig_AddUnitSelectionEvent_Actions takes nothing returns nothing
	local unit u = GetTriggerUnit()
	local player owner = GetOwningPlayer(u)
	local player selector = GetTriggerPlayer()
	if owner == selector then
		return
	endif
	call DisplayTimedTextToPlayer(selector, 0, 0, 1.00, "|cFFFFCC00You've selected |r" + GetPlayerColorString(owner) + GetPlayerName(owner) + "|r")
endfunction

function InitTrig_AddUnitSelectionEvent takes nothing returns nothing
	local integer i = bj_MAX_PLAYERS - 1
	set gg_trg_AddUnitSelectionEvent = CreateTrigger()
	loop
		exitwhen i < 0
		call TriggerRegisterPlayerUnitEvent(gg_trg_AddUnitSelectionEvent, Player(i), EVENT_PLAYER_UNIT_SELECTED, null)
		set i = i - 1
	endloop
	call TriggerAddAction(gg_trg_AddUnitSelectionEvent, function Trig_AddUnitSelectionEvent_Actions)
endfunction
