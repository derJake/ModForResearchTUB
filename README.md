# Mod for research - TU Berlin

This is a modification intended for use with the popular game GTA V.

## Current state

* teleports player to car selection
* one car has moderate acceleration and good traction
* the other one is accelerating fast, harder to handle and sounds an alarm, thereby informing the police
* when the player enters one of the cars, he/she is prompted to drive through checkpoints, composing a route
* at the last checkpoint, several variables are written to a log file

```
14.03.2016 15:36:10 : --------------------------------
14.03.2016 15:36:10 : Player is in car with good traction
14.03.2016 15:36:10 : car health: 794
14.03.2016 15:36:10 : race started: 309328ms
14.03.2016 15:36:10 : race ended: 433145ms
14.03.2016 15:36:10 : time taken: 123,82s
14.03.2016 15:36:10 : average speed: 22m/h
14.03.2016 15:36:10 : average speed: 35km/h
14.03.2016 15:36:10 : maximum speed: 47m/h
14.03.2016 15:36:10 : maximum speed: 76km/h
14.03.2016 15:36:10 : Number of times player applied brakes: 5
14.03.2016 15:36:10 : Number of times player applied handbrake: 5
14.03.2016 15:36:10 : Cumulative time spent braking: 1,47s
14.03.2016 15:36:10 : Cumulative time spent on handbrake: 0,9s
14.03.2016 15:36:10 : Vehicle collisions: 10
14.03.2016 15:36:10 : Pedestrian collisions: 0
14.03.2016 15:36:10 : Number of times player has driven against traffic: 9
14.03.2016 15:36:10 : Number of times player has driven against on pavement: 2
14.03.2016 15:36:10 : Cumulative time on pavement: 0,03
14.03.2016 15:36:10 : Cumulative time driving against traffic: 6,81
 ```

## TO DO

* Create more races / decision events
   *    use ramp for jump or take safe route
   *    go cross-country (risking collisions with foliage and rocks) or take road
   *    buy better engine/turbo or something with better traction
   *    race against opponents or drive alone
   *    Pass garbage truck, which appears on narrow road, or wait patiently
* log running red lights
* display introduction / tutorial more clearly
* show some decisive parts of the race track beforehand

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
