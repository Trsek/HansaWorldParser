@echo off
rem --------------------------------------------------------------------------
rem Generate txt file with all global function/procedure.
rem Generate 2 seperate files global.hansa and global.patince.
rem In last step join it to global.txt file.
rem                                         Software (c) 2019, Zdeno Sekerak
rem --------------------------------------------------------------------------

HansaWorldParser.exe -s"D:\HansaWorld\Zdrojovy kod" -g
copy global.txt global.hansa

HansaWorldParser.exe -s"D:\HansaWorld\Patince-8.5\halcust" -g
copy global.txt global.patince
type global.hansa >> global.txt