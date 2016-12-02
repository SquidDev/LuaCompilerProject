return {
	byte    = string.byte,
	char    = string.char,
	concat  = table.concat,
	format  = string.format,
	lower   = string.lower,
	reverse = string.reverse,
	rep     = string.rep,
	replace = string.gsub,
	split   = function(pattern, str)
		local out, n = { tag = "list" }, 0
		for chr in str:gmatch(pattern) do
			n = n + 1
			out[n] = chr
		end

		out.n = n
		return out
	end,
	sub     = string.sub,
	upper   = string.upper,
}
