#!/usr/bin/env sh 
mono packages/FantomasCLI/lib/Fantomas.exe --recurse --pageWidth 120 src > /dev/null
xbuild && mono src/bin/Debug/test/LuaCP.Test.exe
