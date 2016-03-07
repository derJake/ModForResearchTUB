# Mod for research - TU Berlin

This is a modification intended for use with the popular game GTA V.

## Current state

* teleports player to car selection
* one car has moderate acceleration and good traction
* the other one is accelerating fast, harder to handle and sounds an alarm, thereby informing the police
* when the player enters one of the cars, he/she is prompted to drive through checkpoints, composing a route
* at the last checkpoint, several variables are written to a log file
## Dependencies:
* [ScriptHook V] or .NET, if you compile from source
* [NativeUI]

## Installation:
Copy the file
```
ClassLibrary1.dll
```
to a folder called
```
scripts/
```
inside of your GTA V game folder.

   [scripthook v]: <http://www.dev-c.com/gtav/scripthookv/>
   [nativeui]: <https://github.com/Guad/NativeUI>
