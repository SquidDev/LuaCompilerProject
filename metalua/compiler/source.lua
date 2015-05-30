local source = require 'metalua.compiler.source.compile'

return function(ast, resolver)
	return source(resolver):run(ast)
end
