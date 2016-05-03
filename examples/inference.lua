local function getName(person)
	return person.name
end

return function(person)
	print(getName(person))
	person.sayHello()
end
