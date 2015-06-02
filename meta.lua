#!/usr/bin/env lua
package.path = './src/?.lua;./src/?/?.lua;./src/?/init.lua;' .. package.path
loadfile('src/metalua/bin/metalua.lua')(...)
