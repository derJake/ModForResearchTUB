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
                    new UIResText(string.Format("currentCheckpoint is {0}", currentCheckpoint), new Point(Convert.ToInt32(res.Width) - safe.X - 180, Convert.ToInt32(res.Height) - safe.Y - 275), 0.3f, Color.White).Draw();
                }
            }

            if (Game.Player.Character.IsInVehicle()) {
                new UIResText("player is driving", new Point(Convert.ToInt32(res.Width) - safe.X - 180, Convert.ToInt32(res.Height) - safe.Y - 300), 0.3f, Color.White).Draw();

                if (race_started && 
                    Game.Player.Character.IsInRangeOf(checkpoints[currentCheckpoint], 5f))
                {
                    UI.ShowSubtitle(string.Format("checkpoint {0} reached", currentCheckpoint + 1), 3000);
                    // finish race, if last checkpoint is reached
                    if (currentCheckpoint == checkpoints.Length) {
                        Function.Call(Hash.CLEAR_PLAYER_WANTED_LEVEL, Game.Player);
                        clearStuffUp();
                    }

                    // set next checkpoint
                    Function.Call(Hash.DELETE_CHECKPOINT, currentMarker);
                    currentCheckpoint++;
                    Vector3 coords = checkpoints[currentCheckpoint];
                    Vector3 nextCoords;
                    if (currentCheckpoint < (checkpoints.Length - 1)) {
                        // if there are checkpoints left, get the next one's coordinates
                        nextCoords = checkpoints[currentCheckpoint + 1];
                    } else {
                        nextCoords = new Vector3(0,0,0);
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
                        copsCalled = true;
                    }
                    playerInRaceCar = true;
                }
                if (Game.Player.Character.CurrentVehicle.Equals(vehicles[0]))
                {
                    new UIResText("Slow, reliable car", new Point(Convert.ToInt32(res.Width) - safe.X - 180, Convert.ToInt32(res.Height) - safe.Y - 200), 0.3f, Color.White).Draw();
                    playerInRaceCar = true;
                }

                if (playerInRaceCar &&
                    !race_started) {
                    race_started = true;
                    UI.ShowSubtitle("Race started!", 1250);
                    Game.DisableControl(0, GTA.Control.VehicleExit);
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
                }
                
            }
           
            // Do something every tick here
            //GTA.Native.Function.Call(Hash._SHOW_CURSOR_THIS_FRAME);
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

            Game.Player.Character.Position = car_selection;
            Game.Player.Character.Heading = car_spawn_player_heading;

            checkpoints = new Vector3[5];
            checkpoints[0] = race1Start;
            checkpoints[1] = new Vector3(-810.6682f, -2249.965f, 17.24915f);
            checkpoints[2] = new Vector3(-144.3558f, -1749.146f, 30.12419f);
            checkpoints[3] = new Vector3(64.65392f, -1285.516f, 29.33747f);
            checkpoints[4] = race1End;
            currentCheckpoint = 0;

            //Function.Call(Hash.REQUEST_MODEL, VehicleHash.Buffalo);
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
                    2f, // brightness
                    0.0f, // roundness
                    13f, // radius
                    1f // fadeout
                );
            }

            vehicle1Model.MarkAsNoLongerNeeded();
            vehicle2Model.MarkAsNoLongerNeeded();


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
            UI.ShowSubtitle("clearing stuff up", 1250);
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

            race_started = false;
            currentCheckpoint = -1;
        }

        #endregion
    }
}