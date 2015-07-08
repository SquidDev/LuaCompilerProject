#!/usr/bin/env lua5.1
package.path = './src/?.lua;./src/?/?.lua;./src/?/init.lua;' .. package.path
loadfile('src/metalua/bin/metalua.lua')(...)
