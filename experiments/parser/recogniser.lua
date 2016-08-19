-- Copyright: Consider this file public domain.  Attribution would be
-- nice, though not required.

-- Usage: just execute `lua recogniser.lua` in the command line.
-- You probably want to tinker with the grammar and the input at the
-- end of this file.

-- Prerequisites: imperative programming and basic knowledge of Earley
-- parsing. The tutorial at
--   <http://loup-vaillant.fr/tutorials/earley-parsing/>
-- is highly recomended.
-- You don't need to know Lua.  Think of it as "executable pseudocode".

--------------------
-- Test utilities --  (Don't read this yet, it's not very interesting.)
--------------------

local function hasPartialParse(S, i, grammar, start)
	local set = S[i]
	for i = 1, #set do
		local item = set[i]
		local rule  = item.rule
		if  rule.name  == start and item.next  >  #rule and item.start == 1 then
			return true
		end
	end
	return false
end

local function hasCompleteParse(S, grammar, start)
	return hasPartialParse(S, #S, grammar, start)
end

local function lastPartialParse(S, grammar, start)
	for i = #S, 1, -1 do
		if hasPartialParse(S, i, grammar, start) then
			return i
		end
	end
	return nil
end

local function diagnose(S, grammar, input, start)
	if hasCompleteParse(S, grammar, start) then
		if #S == input:len() + 1 then
			print("The input has been recognised. Congratulations!")
		else
			print("Expecting EOF at character " .. #S)
			print(input)
			print((" "):rep(#S - 1) .. "^")
		end
	else
		if #S == input:len() + 1 then
			print("The whole input made sense. Maybe it is incomplete?")
		else
			print("The input stopped making sense at character " .. #S)
			print(input)
			print((" "):rep(#S - 1) .. "^")
		end

		local lpp = lastPartialParse(S, grammar, start)
		if lpp ~= nil then
			io.write("This begining of the input has been recognised: ",
			input:sub(1, lpp - 1), '\n')
		else
			print("The begining of the input couldn't be parsed.")
		end
	end
end

---------------
-- Utilities -- (Read the function names and the comments.)
---------------
-- next element in the rule of this item
local function nextSymbol(grammar, item)
	return item.rule[item.next]
end

-- gets the name of the rule pointed by the item
local function name(grammar, item)
	return item.rule.name
end

-- compares two items for equality (needed for safe append)
local function equal(item1, item2)
	return item1.rule  == item2.rule and item1.start == item2.start and item1.next  == item2.next
end

-- Adds an item at the end of the Earley set, **unless already present**
local function append(set, item)
	local hash = item.hash
	if not hash then
		hash = tostring(item.rule) .. "-" .. item.start .. "-" .. item.next
		item.hash = hash
	end

	if not set[hash] then
		print(hash)
		set[hash] = item
		set[#set + 1] = item
	end
end

---------------------------
-- Detecting nullable rules
-- Nullable symbols sets are named "nss".
---------------------------
local function addNullableRule(rule_name, nss)
	if nss[rule_name] then return end  -- Do nothing for known nullable rules.
	nss[rule_name] = true              -- The others are added,
	nss.size = nss.size + 1            -- and the size is ajusted.
end

-- Returns true if it can say for sure the rule is nullable.
-- Returns false otherwise
local function isNullable(rule, nss)
	for i = 1, #rule do
		if not nss[rule[i]] then
			return false
		end
	end
	return true
end

-- Adds nullable rules to the nss, by examining them in one pass.
local function updateNss(nss, grammar)
	for i = 1, #grammar do                        -- For each rule,
		if isNullable(grammar[i], nss) then       -- if the rule is nullable for sure,
			addNullableRule(grammar[i].name, nss) -- add it to the nss.
		end
	end
end

local function nullableRules(grammar)
	local nss = { size = 0 }
	repeat                      -- Keep...
		local old_size = nss.size
		updateNss(nss, grammar) -- ...updating the nss,
	until old_size == nss.size  -- as long as it keeps growing.
	return nss                  -- An nss that stopped growing is complete.
end

---------------------------  ---------------------------  ----------------
-- Building Earley items --  This is the core algorithm.  Read everything.
---------------------------  ---------------------------  ----------------
local function complete(S, i, j, grammar)
	local item = S[i][j]
	for _, old_item in ipairs(S[item.start]) do
		if nextSymbol(grammar, old_item) == name(grammar, item) then
			append(S[i], {
				rule  = old_item.rule,
				next  = old_item.next + 1,
				start = old_item.start
			})
		end
	end
end

local function scan(S, i, j, symbol, input)
	local item = S[i][j]
	if symbol(input, i) then -- terminal symbols are predicates
		if not S[i+1] then S[i+1] = {} end
		append(S[i+1], {
			rule  = item.rule,
			next  = item.next + 1,
			start = item.start
		})
	end
end

local function predict(S, i, j, symbol, grammar, nss)
	local current = S[i]
	local item = current[j]

	for _, rule in ipairs(grammar[symbol]) do
		append(current, {
			rule  = rule,
			next  = 1 ,
			start = i
		})
		if nss[rule.name] then -- magical completion
   			append(current, {
				rule  = item.rule,
				next  = item.next + 1,
				start = item.start
			})
		end
	end
end

local function buildItems(grammar, input, start)
	-- Nullable rules detection
	local nss = nullableRules(grammar)
	-- Earley sets
	local S = {{}}
	-- put start item(s) in S[1]
	for _, rule in ipairs(grammar[start]) do
		append(S[1], {
			rule  = rule,
			start = 1,
			next  = 1
		})
	end
	-- populate the rest of S[i]
	local i = 1
	while i <= #S do
		local j = 1
		while j <= #S[i] do
			local symbol = nextSymbol(grammar, S[i][j])
			if     type(symbol) == "nil"      then complete(S, i, j, grammar)
			elseif type(symbol) == "function" then scan    (S, i, j, symbol, input)
			elseif type(symbol) == "string"   then predict (S, i, j, symbol, grammar, nss)
			else error("illegal rule")
			end
			j = j + 1
		end
		i = i + 1
	end
	return S
end

return {
	buildItems = buildItems,
	diagnose = diagnose,
}
