
//===========================================================================
// Trigger: LocustInit (Intention is to make older maps feel a smidge more modern)
//===========================================================================
function Trig_LocustInit_Actions takes nothing returns nothing
	local group g = CreateGroup()
	local unit u
	local player owner
	call GroupEnumUnitsInRect(g, GetPlayableMapRect(), null)
	loop
		set u = FirstOfGroup(g)
		exitwhen u == null
		set owner = GetOwningPlayer(u)

		if GetUnitAbilityLevel(u, 'Aloc') == 0 then
			// Check if unit is owned by a user-controlled player
				// Apply half-locust for player units
				call UnitAddAbility(u, 'Aloc')
				call ShowUnit(u, false)
				call UnitRemoveAbility(u, 'Aloc')
				call ShowUnit(u, true)

				// Add Bear Form, cast it, then remove it
				call UnitAddAbility(u, 'Abrf')
				call IssueImmediateOrder(u, "bearform")
				call UnitRemoveAbility(u, 'Abrf')
		endif
		call BlzSetUnitBooleanField(u, UNIT_BF_HERO_HIDE_HERO_DEATH_MESSAGE, true)
		if not IsUnitType(u, UNIT_TYPE_HERO) then
			call BlzSetUnitRealField(u, UNIT_RF_SELECTION_SCALE, -10.0)
		endif
		call GroupRemoveUnit(g, u)
	endloop
	call DestroyGroup(g)
	set g = null
endfunction

function InitTrig_LocustInit takes nothing returns nothing
	set gg_trg_LocustInit = CreateTrigger()
	call TriggerAddAction(gg_trg_LocustInit, function Trig_LocustInit_Actions)
endfunction

//===========================================================================
// Trigger: LocustEnter (Intention is to make older maps feel a smidge more modern)
//===========================================================================
function Trig_LocustEnter_Conditions takes nothing returns boolean
	return GetUnitAbilityLevel(GetTriggerUnit(), 'Aloc') == 0
endfunction

function Trig_LocustEnter_Actions takes nothing returns nothing
	local unit u = GetTriggerUnit()
	local player owner = GetOwningPlayer(u)

	// Check if unit is owned by a user-controlled player
		// Apply half-locust for player units
		call UnitAddAbility(u, 'Aloc')
		call ShowUnit(u, false)
		call UnitRemoveAbility(u, 'Aloc')
		call ShowUnit(u, true)

		// Add Bear Form, cast it, then remove it
		call UnitAddAbility(u, 'Abrf')
		call IssueImmediateOrder(u, "bearform")
		call UnitRemoveAbility(u, 'Abrf')
		call BlzSetUnitBooleanField(u, UNIT_BF_HERO_HIDE_HERO_DEATH_MESSAGE, true)
		if not IsUnitType(u, UNIT_TYPE_HERO) then
			call BlzSetUnitRealField(u, UNIT_RF_SELECTION_SCALE, -10.0)
		endif
endfunction

function InitTrig_LocustEnter takes nothing returns nothing
	set gg_trg_LocustEnter = CreateTrigger()
	call TriggerRegisterEnterRectSimple(gg_trg_LocustEnter, GetPlayableMapRect())
	call TriggerAddCondition(gg_trg_LocustEnter, Condition(function Trig_LocustEnter_Conditions))
	call TriggerAddAction(gg_trg_LocustEnter, function Trig_LocustEnter_Actions)
endfunction
