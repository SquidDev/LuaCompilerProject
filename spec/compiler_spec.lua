describe("Correct output #compiler", function()
	-- Shamelessly stolen from luaj.luajc
	local files = {
		"BranchUpvalue",
		"DoBlock",
		"EdgeCases",
		"!Error", -- Error messages are wonky
		"Function",
		"LoadBytecode",
		"NForLoop",
		"Recursive",
		"!RecursiveTrace", -- Error messages are wonky
		"StringDump",
		"TailCall",
		"Upvalues",
		"WhileLoop",

		"!fragment/ArgParamUseNone", -- `arg` is broken
		"!fragment/ArgVarargsUseBoth",
		"fragment/AssignReferUpvalues",
		"fragment/BasicForLoop",
		"fragment/ControlCharStringLiterals",
		"fragment/ForLoopParamUpvalues",
		"fragment/ForLoops",
		"fragment/GenericForMultipleValues",
		"fragment/LoadBool",
		"fragment/LoadedNilUpvalue",
		"fragment/LoadNilUpvalue",
		"fragment/LocalFunctionDeclarations",
		"fragment/LoopVarNames",
		"fragment/LoopVarUpvalues",
		"fragment/MultiAssign",
		"!fragment/NeedsArgAndHasArg",
		"fragment/NestedUpvalues",
		"fragment/NilsInTableConstructor",
		"fragment/NonAsciiStringLiterals",
		"fragment/NoReturnValuesPlainCall",
		"fragment/NumericForUpvalues",
		"fragment/NumericForUpvalues2",
		"fragment/PhiVarUpvalue",
		"fragment/ReadOnlyAndReadWriteUpvalues",
		"fragment/ReturnUpvalue",
		"fragment/SelfOp",
		"fragment/SetlistVarargs",
		"fragment/SetListWithOffsetAndVarargs",
		"fragment/SetUpvalueTableInitializer",
		"fragment/SimpleRepeatUntil",
		"fragment/TestOpUpvalues",
		"fragment/TestSimpleBinops",
		"fragment/UninitializedAroundBranch",
		"fragment/UninitializedUpvalue",
		"fragment/UnreachableCode",
		"fragment/UpvalueClosure",
		"fragment/UpvalueInFirstSlot",
		"fragment/Upvalues",
		"fragment/UpvaluesInElseClauses",
		"fragment/VarargsInFirstArg",
		"fragment/VarargsInTableConstructor",
		"fragment/VarargsWithParameters",
		"!fragment/VarVarargsUseArg",
		"!fragment/VarVarargsUseBoth",
	}

	function assertMany(...)
		local args = {...}
		local len = select('#', ...) / 2
		for i = 1, len do
			assert.are.equals(args[i], args[i + len])
		end
	end

	local function doubleBack(src)
		local compiler = require 'metalua.compiler'.new()
		return loadstring(compiler:ast_to_src(compiler:srcfile_to_ast(src)))
	end

	assertEquals = assert.are.equals
	verbose = function() end

	for _, file in ipairs(files) do
		if file:sub(1, 1) == '!' then
			pending(file:sub(2), function() end)
		else
			describe(file, function()
				it("#bytecode", function()
					local compiler = require 'metalua.compiler'.new()
					local f = compiler:srcfile_to_function('spec/compiler/' .. file .. '.lua')
					setfenv(f, getfenv())()
				end)

				it("#source", function()
					setfenv(doubleBack('spec/compiler/' .. file .. '.lua'), getfenv())()
				end)
			end)

			--[[
			it('Native' .. file, function()
				local f = loadfile('spec/compiler/' .. file .. '.lua')
				setfenv(f, getfenv())()
			end)
			]]
		end
	end
end)
