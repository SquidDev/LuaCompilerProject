local str
str = {
	byte    = string.byte,
	char    = string.char,
	concat  = table.concat,
	find    = function(text, pattern, offset, plaintext)
		local start, finish = string.find(text, pattern, offset, plaintext)
		if start then
			return { tag = "list", n = 2, start, finish }
		else
			return nil
		end
	end,
	format  = string.format,
	lower   = string.lower,
	reverse = string.reverse,
	rep     = string.rep,
	replace = string.gsub,
	sub     = string.sub,
	upper   = string.upper,

	['#s']   = string.len,
  ['->string'] = function(x)
    if type(x) == 'table' and x.contents then
      return str['->string'](x.contents)
    else
      return tostring(x)
    end
  end
}

return str
