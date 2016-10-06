# Mod for research - TU Berlin

This is a modification intended for use with the popular game GTA V.

## Current state

* defines several tracks or tasks each one containing at least one decision event
  * Intro: explaining the tests and providing 10min. of free play time
  * Convoy: following another vehicle (while logging the distance)
  * Suburban: placing an obstacle in the player's path, logging passing or braking
  * Desert: alternative waypoints with more exciting route through hills and landscape
  * Terminal: another alternative route, through tight backalleys
  * Car vs. Car: make the player choose between two cars, a fast one with a wanted level and a regular one with better maneuverability
  * Jesiah: a mountain road with alt. waypoints leading to a ramp jump
* logs variables like gas pedal, brake pedal and steering input, velocity of the vehicle and collisions
* each task might introduce additional logging variables, tied to its specific decision events
* logs are written to a SQL Server DB and a textual log file
* continuous variables get exported as diagrams
* provides tools for
  * generating routes in free fly mode via checkpoints and generating code for those
  * defining camera perspectives and generating code for camera creation
* Localization support via Resource files

### Example log file
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
   *    buy better engine/turbo or something with better traction
   *    race against opponents or drive alone
* finish traffic signal logging tools
  * complete state machine for selecting a traffic light and defining the zones in which to look for halted cars and cars running the red light
  * write to / read from DB
  * edit mode
* finish route designer tool
  * create task for each route
  * load from DB
  * edit mode
  * generic class for testing the route

## Dependencies:
* [ScriptHook V]
* [Community ScriptHook V .NET]
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

## Usage
Press the [F10] key while inside the game. The mod's menu should show up.

   [scripthook v]: <http://www.dev-c.com/gtav/scripthookv/>
   [nativeui]: <https://github.com/Guad/NativeUI>
   [Community ScriptHook V .NET]: <https://github.com/crosire/scripthookvdotnet/releases>
