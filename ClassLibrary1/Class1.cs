#region Using references

using System;
using GTA.Native;
using GTA;
using GTA.Math;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;
using NativeUI;
#endregion

namespace ModForResearchTUB
{
    public class Main : Script
    {
        // Variables
        int timer_1s = 0;
        List<Model> models = new List<Model>();
        List<Vehicle> vehicles = new List<Vehicle>();
        Blip currentBlip;
        Vector3[] checkpoints;
        int currentMarker;
        bool car_config_done = false;
        bool race_started = false;
        bool playerInRaceCar = false;
        bool copsCalled = false;
        int currentCheckpoint = -1;

        int lastMaxTimeSinceHitVehicle;
        int lastMaxTimeSinceHitPed;
        int lastMaxTimeSincePavement;
        int lastMaxTimeSinceAgainstTraffic;

        int numOfHitVehicles;
        int numOfHitPeds;
        int numOfTimesDrivingOnPavement;
        int numOfTimesDrivingAgaingstTraffic;

        int startedDrivingOnPavement;
        int startedDrivingAgainstTraffic;

        int cumulativeTimeOnPavement;
        int cumulativeTimeDrivingAgainstTraffic;
        int raceStartTime;
        int raceEndTime;

        Vector3 car_selection = new Vector3(-786.5052f, -2429.885f, 14.57072f);
        Vector3 car1_spawnpoint = new Vector3(-789.7347f, -2428.485f, 14.57072f);
        Vector3 car2_spawnpoint = new Vector3(-795.5708f, -2425.815f, 14.57072f);
        float car_spawn_heading = 147.0f;
        float car_spawn_player_heading = 48.0f;
        Vector3 race1Start = new Vector3(-1015.348f, -2715.956f, 12.58948f);
        Vector3 race1End = new Vector3(-45.45972f, -784.222f, 44.34782f);

        // Main Script
        public Main()
        {
            // Tick Interval
            //Interval = 10;

            // Initialize Events
            Tick += this.OnTickEvent;
            KeyDown += this.KeyDownEvent;
            KeyUp += this.KeyUpEvent;

            UI.ShowSubtitle("Press [F10] to start first race", 1250);
        }

        #region Events

        // OnTick Event
        public void OnTickEvent(object sender, EventArgs e)
        {
            var res = UIMenu.GetScreenResolutionMantainRatio();
            var safe = UIMenu.GetSafezoneBounds();

            /*
            *   SET_PED_CAN_BE_SHOT_IN_VEHICLE
            *   make it so that AI can not be shot
            */
            
            if (race_started &&
                playerInRaceCar) {
                if (currentCheckpoint >= 0) {
                    new UIResText(string.Format("currentCheckpoint is {0}/{1}", currentCheckpoint, checkpoints.Length), new Point(Convert.ToInt32(res.Width) - safe.X - 180, Convert.ToInt32(res.Height) - safe.Y - 275), 0.3f, Color.White).Draw();
                }

                // logging some variables
                int currentTimeSinceHitVehicle = Function.Call<int>(Hash.GET_TIME_SINCE_PLAYER_HIT_VEHICLE, Game.Player);
                int currentTimeSinceHitPed = Function.Call<int>(Hash.GET_TIME_SINCE_PLAYER_HIT_PED, Game.Player);
                int currentTimeSinceDrivingOnPavement = Function.Call<int>(Hash.GET_TIME_SINCE_PLAYER_DROVE_ON_PAVEMENT, Game.Player);
                int currentTimeSinceDrivingAgainstTraffic = Function.Call<int>(Hash.GET_TIME_SINCE_PLAYER_DROVE_AGAINST_TRAFFIC, Game.Player);

                // if the timer was reset, there was a collision
                if (currentTimeSinceHitVehicle < lastMaxTimeSinceHitVehicle) {
                    numOfHitVehicles++;
                }
                // either way, save new timer
                lastMaxTimeSinceHitVehicle = currentTimeSinceHitVehicle;

                // if the timer was reset, there was a collision
                if (currentTimeSinceHitPed < lastMaxTimeSinceHitPed)
                {
                    numOfHitPeds++;
                }
                // either way, save new timer
                lastMaxTimeSinceHitPed = currentTimeSinceHitPed;

                // player is currently driving on pavement
                if (currentTimeSinceDrivingOnPavement == 0)
                {
                    // start counter
                    if (startedDrivingOnPavement == 0)
                    {
                        startedDrivingOnPavement = Game.GameTime;
                    }
                    // show status
                    new UIResText(
                        String.Format(
                            "on pavement for {0}s",
                            (Game.GameTime - startedDrivingOnPavement) / 1000),
                        new Point(Convert.ToInt32(res.Width) - safe.X - 180,
                        Convert.ToInt32(res.Height) - safe.Y - 350),
                        0.3f,
                        Color.OrangeRed
                        ).Draw();
                } else if (currentTimeSinceDrivingOnPavement > 0) { // player drove on pavement, but isn't any longer
                    if (startedDrivingOnPavement > 0) {
                        // add the time interval
                        cumulativeTimeOnPavement += Game.GameTime - startedDrivingOnPavement;
                        // reset counter
                        startedDrivingOnPavement = 0;
                    }
                }

                // if the timer was reset, player drove on pavement
                if (currentTimeSinceDrivingOnPavement < lastMaxTimeSincePavement)
                {
                    numOfTimesDrivingOnPavement++;
                }
                // either way, save new timer
                lastMaxTimeSincePavement = currentTimeSinceDrivingOnPavement;

                // player is currently driving against traffic
                if (currentTimeSinceDrivingAgainstTraffic == 0) {
                    new UIResText("against traffic", new Point(Convert.ToInt32(res.Width) - safe.X - 180, Convert.ToInt32(res.Height) - safe.Y - 350), 0.3f, Color.OrangeRed).Draw();
                }

                // if the timer was reset, player drove against traffic
                if (currentTimeSinceDrivingAgainstTraffic < lastMaxTimeSinceAgainstTraffic)
                {
                    numOfTimesDrivingAgaingstTraffic++;
                }
                // either way, save new timer
                lastMaxTimeSinceAgainstTraffic = currentTimeSinceDrivingAgainstTraffic;
            }

            if (Game.Player.Character.IsInVehicle()) {
                new UIResText("player is driving", new Point(Convert.ToInt32(res.Width) - safe.X - 180, Convert.ToInt32(res.Height) - safe.Y - 300), 0.3f, Color.White).Draw();

                if (race_started && 
                    Game.Player.Character.IsInRangeOf(checkpoints[currentCheckpoint], 5f))
                {
                    // finish race, if last checkpoint is reached
                    if ((currentCheckpoint + 1) == checkpoints.Length) {
                        UI.ShowSubtitle(String.Format("Race finished! - Time: {0}s", (Game.GameTime - raceStartTime) / 1000), 3000);
                        Function.Call(Hash.SET_PLAYER_WANTED_LEVEL, Game.Player, 0, false);
                        Function.Call(Hash.SET_PLAYER_WANTED_LEVEL_NOW, Game.Player, false);

                        raceEndTime = Game.GameTime;

                        writeRaceDataToLog();
                        clearStuffUp();
                        return;
                    }

                    UI.ShowSubtitle(string.Format("checkpoint {0}/{1} reached", currentCheckpoint + 1, checkpoints.Length), 3000);

                    // set next checkpoint
                    Function.Call(Hash.DELETE_CHECKPOINT, currentMarker);
                    currentCheckpoint++;
                    Vector3 coords = checkpoints[currentCheckpoint];
                    Vector3 nextCoords;
                    int type;
                    if (currentCheckpoint < (checkpoints.Length - 1)) {
                        // if there are checkpoints left, get the next one's coordinates
                        nextCoords = checkpoints[currentCheckpoint + 1];
                        type = 2;
                    } else {
                        type = 14;
                        nextCoords = new Vector3(0,0,0);
                        coords.Z = coords.Z + 3f;
                    }
                    
                    currentMarker = Function.Call<int>(Hash.CREATE_CHECKPOINT, 
                        type, // type
                        coords.X,
                        coords.Y,
                        coords.Z - 1,
                        nextCoords.X, // facing next checkpoint?
                        nextCoords.Y,
                        nextCoords.Z,
                        5.0f,    // radius
                        255,    // R
                        155,     // G
                        0,        // B
                        100,    // Alpha
                        0 // number displayed in marker, if type is 42-44
                        );

                    currentBlip.Remove();
                    currentBlip = World.CreateBlip(checkpoints[currentCheckpoint]);
                    Function.Call(Hash.SET_BLIP_ROUTE, currentBlip, true);
                }

                // check which car player is using
                if (Game.Player.Character.CurrentVehicle.Equals(vehicles[1]))
                {
                    new UIResText("fast car", new Point(Convert.ToInt32(res.Width) - safe.X - 180, Convert.ToInt32(res.Height) - safe.Y - 250), 0.3f, Color.White).Draw();
                    if (!copsCalled) {
                        Function.Call(Hash.SET_PLAYER_WANTED_LEVEL, Game.Player, 3, false);
                        Function.Call(Hash.SET_PLAYER_WANTED_LEVEL_NOW, Game.Player, false);
                        Game.Player.Character.CurrentVehicle.StartAlarm();
                        copsCalled = true;
                    }
                    playerInRaceCar = true;
                }
                if (Game.Player.Character.CurrentVehicle.Equals(vehicles[0]))
                {
                    new UIResText("Slow, reliable car", new Point(Convert.ToInt32(res.Width) - safe.X - 180, Convert.ToInt32(res.Height) - safe.Y - 200), 0.3f, Color.White).Draw();
                    playerInRaceCar = true;
                }

                // start the race and set first marker + blip
                if (playerInRaceCar &&
                    !race_started) {
                    race_started = true;
                    UI.ShowSubtitle("Race started!", 1250);

                    // don't let player exit his racecar by conventional means
                    Game.DisableControl(0, GTA.Control.VehicleExit);

                    // disable shooting from car?
                    Game.DisableControl(0, GTA.Control.VehiclePassengerAim);

                    // select the first checkpoint
                    Vector3 coords = checkpoints[currentCheckpoint];
                    Vector3 nextCoords;
                    if (currentCheckpoint < (checkpoints.Length - 1))
                    {
                        // if there are checkpoints left, get the next one's coordinates
                        nextCoords = checkpoints[currentCheckpoint + 1];
                    }
                    else {
                        nextCoords = new Vector3(0, 0, 0);
                    }
                    currentMarker = Function.Call<int>(Hash.CREATE_CHECKPOINT,
                        2, // type
                        coords.X,
                        coords.Y,
                        coords.Z - 1,
                        nextCoords.X, // facing next checkpoint?
                        nextCoords.Y,
                        nextCoords.Z,
                        5.0f,    // radius
                        255,    // R
                        155,     // G
                        0,        // B
                        100,    // Alpha
                        0
                        );
                    currentBlip = World.CreateBlip(checkpoints[currentCheckpoint]);
                    Function.Call(Hash.SET_BLIP_ROUTE, currentBlip, true);

                    raceStartTime = Game.GameTime;
                }
                
            }
        }

        // KeyDown Event
        public void KeyDownEvent(object sender, KeyEventArgs e)
        {
            // Check KeyDown KeyCode
            switch (e.KeyCode)
            {
                case Keys.E:
                    UI.ShowSubtitle("[E] KeyDown", 1250);
                    break;
                default:
                    break;
            }
        }

        // KeyUp Event
        public void KeyUpEvent(object sender, KeyEventArgs e)
        {
            // Check KeyUp KeyCode
            switch (e.KeyCode)
            {
                case Keys.E:
                    UI.ShowSubtitle("[E] KeyUp", 1250);
                    break;
                case Keys.F10:
                    UI.ShowSubtitle("trying to call race", 1250);
                    initFirstRace();
                    break;
                case Keys.F11:
                    UI.ShowSubtitle("Teleport Player to customization", 1250);
                    teleportPlayerToCarCustomization();
                    break;
                default:
                    break;
            }
        }

        // Dispose Event
        protected override void Dispose(bool A_0)
        {
            if (A_0)
            {
                //remove any ped,vehucle,Blip,prop,.... that you create
                clearStuffUp();
            }
        }

        protected void initFirstRace() {
            UI.ShowSubtitle("initializing first race", 1250);
            Ped player = Game.Player.Character;
            player.Task.ClearAllImmediately(); // give back control to player

            // teleport player and turn him towards cars
            Game.Player.Character.Position = car_selection;
            Game.Player.Character.Heading = car_spawn_player_heading;

            // set up everything for logging
            resetLoggingVariables();

            // add some checkpoints for our race
            checkpoints = new Vector3[5];
            checkpoints[0] = race1Start;
            checkpoints[1] = new Vector3(-810.6682f, -2249.965f, 17.24915f);
            checkpoints[2] = new Vector3(-144.3558f, -1749.146f, 30.12419f);
            checkpoints[3] = new Vector3(64.65392f, -1285.516f, 29.33747f);
            checkpoints[4] = race1End;
            currentCheckpoint = 0;

            // load the two models
            var vehicle1Model = new Model(VehicleHash.Buffalo);
            var vehicle2Model = new Model(VehicleHash.RapidGT);
            vehicle1Model.Request(500);
            vehicle2Model.Request(500);

            if (vehicle1Model.IsInCdImage &&
                vehicle1Model.IsValid &&
                vehicle2Model.IsInCdImage &&
                vehicle2Model.IsValid
                )
            {
                // If the model isn't loaded, wait until it is
                while (!vehicle1Model.IsLoaded ||
                        !vehicle2Model.IsLoaded)
                    Script.Wait(100);

                // create the slower, reliable car
                var vehicle1 = World.CreateVehicle(VehicleHash.Buffalo, car1_spawnpoint, car_spawn_heading);
                // create the racecar
                var vehicle2 = World.CreateVehicle(VehicleHash.RapidGT, car2_spawnpoint, car_spawn_heading);
                vehicles.Add(vehicle1);
                vehicles.Add(vehicle2);

                // open driver side door for player
                Function.Call(Hash.SET_VEHICLE_DOOR_OPEN, vehicle1, 0, true, false);
                Function.Call(Hash.SET_VEHICLE_ENGINE_ON, vehicle1, true, false, false);

                // make fast vehicle locked, but able to break and enter
                Function.Call(Hash.SET_VEHICLE_DOORS_LOCKED, vehicle2, 4);
                Function.Call(Hash.SET_VEHICLE_NEEDS_TO_BE_HOTWIRED, vehicle2, true);

                // make the fast one colorful, the other one white
                Function.Call(Hash.SET_VEHICLE_CUSTOM_PRIMARY_COLOUR, vehicle1, 255, 255, 255);
                Function.Call(Hash.SET_VEHICLE_CUSTOM_SECONDARY_COLOUR, vehicle1, 255, 255, 255);

                Function.Call(Hash.SET_VEHICLE_CUSTOM_PRIMARY_COLOUR, vehicle2, 255,0,0);
                Function.Call(Hash.SET_VEHICLE_CUSTOM_SECONDARY_COLOUR, vehicle2, 255, 50, 0);

                Function.Call(Hash.SET_VEHICLE_DOORS_LOCKED, vehicle2, 4);

                Function.Call(Hash.SET_VEHICLE_INTERIORLIGHT, vehicle1, true);

                Function.Call(
                    Hash.DRAW_SPOT_LIGHT,
                    car1_spawnpoint.X, // x
                    car1_spawnpoint.Y, // y
                    car1_spawnpoint.Z + 10f, // z
                    0f, // direction x
                    0f, // direction y
                    -10f, // direction z, make it point downwards
                    255, // R
                    255, // G
                    255, // B
                    100f, // distance
                    200f, // brightness
                    0.0f, // roundness
                    13f, // radius
                    1f // fadeout
                );
            }

            vehicle1Model.MarkAsNoLongerNeeded();
            vehicle2Model.MarkAsNoLongerNeeded();

            // make player look at cars
            Game.Player.Character.Task.StandStill(5000);

            Game.DisableControl(0, GTA.Control.CursorX);
            Game.DisableControl(0, GTA.Control.CursorY);

            UI.ShowSubtitle("slow car, good traction", 2500);
            Game.Player.Character.Task.LookAt(car1_spawnpoint, 2500);
            Wait(2500);
            UI.ShowSubtitle("racecar (STEAL!)", 2500);
            Game.Player.Character.Task.LookAt(car2_spawnpoint, 2500);
            Wait(2500);

            Game.EnableControl(0, GTA.Control.CursorX);
            Game.EnableControl(0, GTA.Control.CursorY);
        }

        protected void teleportPlayerToCarCustomization() {
            if (Game.Player.Character.IsSittingInVehicle() && !car_config_done)
            {
                Game.Player.LastVehicle.Position = new Vector3(-1140.731f, -1985.894f, 12.78301f);
                Game.Player.LastVehicle.Rotation = new Vector3(0.0f, 0.0f, 135.0f);
                Logger.Log("Player is entering garage");
                Game.Player.Character.Task.DriveTo(
                    Game.Player.LastVehicle,
                    new GTA.Math.Vector3(-1147.906f, -1993.416f, 12.7937f),
                    1.0f,
                    5.0f
                );
                
                //Game.Player.LastVehicle.Position = new Vector3(-1156.724f, -2007.222f, 12.79617f);
            }
        }

        protected void clearStuffUp() {
            Ped player = Game.Player.Character;
            player.Task.ClearAllImmediately(); // give back control to player

            // clear vehicles
            foreach (Vehicle car in vehicles)
            {
                car.MarkAsNoLongerNeeded();
                car.Delete();
            }

            // clear map blip
            currentBlip.Remove();

            // delete 3D marker
            Function.Call(Hash.DELETE_CHECKPOINT, currentMarker);

            race_started = false;
            currentCheckpoint = -1;

            resetLoggingVariables();
            Wait(3000);
            UI.ShowSubtitle("Everything reset", 3000);
        }

        protected void writeRaceDataToLog() {
            Logger.Log("--------------------------------");
            Logger.Log(String.Format("race started: {0}", raceStartTime));
            Logger.Log(String.Format("race ended: {0}", raceEndTime));
            Logger.Log(String.Format("time taken: {0}", (raceEndTime - raceStartTime) / 1000));
            Logger.Log(String.Format("Vehicle collisions: {0}", numOfHitVehicles));
            Logger.Log(String.Format("Pedestrian collisions: {0}", numOfHitPeds));
            Logger.Log(String.Format("Number of times player has driven against traffic: {0}", numOfTimesDrivingAgaingstTraffic));
            Logger.Log(String.Format("Number of times player has driven against on pavement: {0}", numOfTimesDrivingOnPavement));
            Logger.Log(String.Format("Cumulative time on pavement: {0}", cumulativeTimeOnPavement));
            Logger.Log(String.Format("Cumulative time driving against traffic: {0}", cumulativeTimeDrivingAgainstTraffic));
        }

        protected void resetLoggingVariables() {
            // reset logging variables
            lastMaxTimeSinceHitVehicle = -1;
            lastMaxTimeSinceHitPed = -1;
            lastMaxTimeSincePavement = -1;
            lastMaxTimeSinceAgainstTraffic = -1;
            numOfHitVehicles = 0;
            numOfHitPeds = 0;
            numOfTimesDrivingOnPavement = 0;
            numOfTimesDrivingAgaingstTraffic = 0;

            startedDrivingOnPavement = 0;
            startedDrivingAgainstTraffic = 0;

            raceStartTime = -1;
            raceEndTime = -1;

            cumulativeTimeDrivingAgainstTraffic = 0;
            cumulativeTimeOnPavement = 0;
        }

        #endregion
    }
}