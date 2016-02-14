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
        List<Blip> blips = new List<Blip>();
        bool car_config_done = false;
        bool race_started = false;
        bool playerInRaceCar = false;
        bool firstCheckpointReached = false;
        bool copsCalled = false;

        Entity checkpoint;

        Vector3 car_selection = new Vector3(-789.2762f, -2417.304f, 14.57072f);
        Vector3 car1_spawnpoint = new Vector3(-789.7347f, -2428.485f, 14.57072f);
        Vector3 car2_spawnpoint = new Vector3(-795.5708f, -2425.815f, 14.57072f);
        float car_spawn_heading = 147.0f;
        Vector3 car_spawn_player_heading = new Vector3(-793.0905f, -2427.258f, 14.57072f);
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
            const int interval = 45;

            // 1 Second Timer
            if (timer_1s <= Game.GameTime)
            {
                // Reset Timer
                timer_1s = Game.GameTime + 1000;

                // NATIVE EXEMPLE
                // NativeDB http://www.dev-c.com/nativedb/

                // Call a Native without a return value
                //GTA.Native.Function.Call(Hash.SET_ALL_RANDOM_PEDS_FLEE, Game.Player, true);
                //Ped[] closePeds = World.GetNearbyPeds(Game.Player.Character, 12f);
                //foreach (Ped ped in closePeds)
                //{
                //    // Kill the peds!
                //    if (!ped.IsPlayer)
                //        ped.Health = -100;
                //}

                // Call a Native function with a return value
                Boolean nativeIsPlayerPlaying = GTA.Native.Function.Call<Boolean>(Hash.IS_PLAYER_PLAYING, Game.Player);
                Boolean IsPlayerPlaying = Game.Player.IsPlaying; // Shortcut with ScriptHookDotNet Game class
            }

            /*
            *   SET_PED_CAN_BE_SHOT_IN_VEHICLE
            *   make it so that AI can not be shot
            */

            /*if (!race_started &&
                Game.Player.Character.IsInVehicle() &&
                Game.Player.Character.CurrentVehicle == vehicles[0] ||
                Game.Player.Character.CurrentVehicle == vehicles[1])
            {
                race_started = true;
                Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, (int)GTA.Control.VehicleExit);
                UI.ShowSubtitle("Player is driving", 1250);
            }*/

            // check if player has reached the first marker and remove it
            /*if (race_started &&
                Game.Player.Character.IsInRangeOf(race1Start, 5f) &&
                Game.Player.Character.IsInVehicle()) {
                UI.ShowSubtitle("Checkpoint reached", 1250);
                Function.Call(Hash.DELETE_CHECKPOINT, checkpoint);
            }*/
            
            if (!firstCheckpointReached) {
                World.DrawMarker(MarkerType.VerticalCylinder, race1Start, new Vector3(0, 0, 0), new Vector3(0, 0, 0), new Vector3(5f, 5f, 15f), Color.FromArgb(150, 255, 200, 0));
            }

            if (Game.Player.Character.IsInVehicle()) {
                new UIResText("player is driving", new Point(Convert.ToInt32(res.Width) - safe.X - 180, Convert.ToInt32(res.Height) - safe.Y - 300), 0.3f, Color.White).Draw();

                if (!firstCheckpointReached &&
                Game.Player.Character.IsInRangeOf(race1Start, 5f))
                {
                    UI.ShowSubtitle("first checkpoint reached", 1250);
                    Function.Call(Hash.CLEAR_PLAYER_WANTED_LEVEL, Game.Player);
                    firstCheckpointReached = true;
                    race_started = true;
                }

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
                    World.CreateBlip(race1Start);
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

            // clear vehicles
            /*
            foreach (Vehicle car in vehicles)
            {
                car.MarkAsNoLongerNeeded();
                Function.Call(Hash.SET_VEHICLE_AS_NO_LONGER_NEEDED, car);
            }
            */
            //Function.Call(Hash.CLEAR_AREA_OF_VEHICLES);
            //Function.Call(Hash.SET_MISSION_FLAG);
            // create a blip on the map

            Game.Player.Character.Position = car_selection;

            /*
          // 5 meters in front of the player
          var position = player.GetOffsetInWorldCoords(new Vector3(0, 5, 0));

          // At 90 degrees to the players heading
          var heading = player.Heading - 90;
          */

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
            Ped player = Game.Player.Character;
            player.Task.ClearAllImmediately(); // give back control to player

            Function.Call(Hash.DELETE_CHECKPOINT, checkpoint);

            // clear vehicles
            foreach (Vehicle car in vehicles)
            {
                car.MarkAsNoLongerNeeded();
                Function.Call(Hash.SET_VEHICLE_AS_NO_LONGER_NEEDED, car);
            }

            // clear blips
            foreach (Blip blip in blips) {
                Function.Call(Hash.SET_ENTITY_AS_NO_LONGER_NEEDED, blip);
            }
        }

        #endregion
    }
}