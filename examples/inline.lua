local function range(start, finish)
	return function(_, i)
		if i == nil then
			return start
		elseif i >= finish then
			return nil
		else
			return i + 1
		end
	end
end

for i in range(0, 10) do
	print(i)
end
