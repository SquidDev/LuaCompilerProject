--- Object that prints nicely laid out columns.
--
-- Usage:
--  pp = pretty_printer()
--  pp.line()
--    pp.col() pp.write('a', 42)
--    pp.col() pp.write(' |')
--  pp.line()
--    pp.col() pp.write("something") pp.write(" longer")
--    pp.col() pp.write(' |')
--    pp.col() pp.write(' another column')
--   pp.print()
--
-- result:
--  a42              |
--	something longer | another column
-- @module parser.print

local function pretty_printer()
	local self = {}

	function self.write(...)
		local args = {...}
		for _, v in ipairs(args) do
			local l = self[#self] -- current line
			l[#l] = l[#l]..tostring(v)
		end
	end

	function self.col()
		local l = self[#self] -- current line
		l[#l+1] = ""
	end

	function self.line()
		self[#self+1] = {}
	end

	local function max(f)
		local m = 0
		for _, v in ipairs(self) do
			local x = f(v)
			if x > m then m = x end
		end
		return m
	end

	local function len(i)
		return function(v)
			if v[i] == nil
			then return 0
			else return v[i]:len()
			end
		end
	end

	local function nb_col(line) return #line end

	function self.print(indent)
		if indent == nil then indent = 0 end
		for i = 1, max(nb_col) do
			local max_len = max(len(i))
			for _, line in ipairs(self) do
				if line[i] == nil then line[i] = "" end
				line[i] = line[i]..string.rep(" ", max_len - line[i]:len())
			end
		end
		for _, line in ipairs(self) do
			io.write(string.rep(" ", indent))
			for _, col in ipairs(line) do
				io.write(col)
			end
			io.write('\n')
		end
	end

	return self
end

local indent = 4

--- Prints all the Earley items.
return function(S, grammar)
	for i, set in ipairs(S) do
		print((' '):rep(indent) .. '━┫ ' .. i - 1 .. ' ┣━')
		pp = pretty_printer()
		for j, st in ipairs(set) do
			pp.line()
			pp.col() pp.write(st.rule.name)
			pp.col() pp.write(' →')
			for k, symbol in ipairs(st.rule) do
				if k == st.next                   then pp.write(' •') end
				if type(symbol) == "string"       then pp.write(' ', symbol)
				elseif type(symbol) == "function" then pp.write(' ', symbol())
				else                                   error("Impossible symbol")
				end
			end

			if st.next > #st.rule then pp.write(' •') end
			pp.col() pp.write(' (', st.start-1, ')')
		end
		pp.print(indent)
		io.write('\n')
	end
end
