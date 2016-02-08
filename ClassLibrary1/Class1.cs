#region Using references

using System;
using GTA.Native;
using GTA;
using GTA.Math;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;
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

            if (Game.Player.Character.CurrentVehicle == vehicles[0]) {
                UI.ShowSubtitle("Slow car", 1250);
            }

            if (Game.Player.Character.CurrentVehicle == vehicles[1])
            {
                UI.ShowSubtitle("Fast car", 1250);
            }

            // Do something every tick here
            //GTA.Native.Function.Call(Hash._SHOW_CURSOR_THIS_FRAME);
        }

        // KeyDown Event
        public void KeyDownEvent(object sender, KeyEventArgs e)
        {
            /*if (Game.IsKeyPressed(Keys.F11))
            {
                UI.ShowSubtitle("[F11] KeyDown", 1250);
                Ped player = Game.Player.Character;
                Vector3 spawn_pos = new Vector3(
                    player.Position.X + (player.ForwardVector.X * 5),
                    player.Position.Y + (player.ForwardVector.Y * 5),
                    player.Position.Z + (player.ForwardVector.Z * 5)
                );
                GTA.World.CreateBlip(spawn_pos);
                World.CreateBlip(player.GetOffsetInWorldCoords(new Vector3(0, 15, 0)));
                // Blip blip = UI.ADD_BLIP_FOR_COORD(Game.Player.Character.GetOffsetInWorldCoords(new Vector3(0, 15, 0));
            }*/

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

            Function.Call(Hash.DELETE_CHECKPOINT, checkpoint);

            // clear vehicles
            foreach (Vehicle car in vehicles)
            {
                car.MarkAsNoLongerNeeded();
                Function.Call(Hash.SET_VEHICLE_AS_NO_LONGER_NEEDED, car);
            }
            //Function.Call(Hash.CLEAR_AREA_OF_VEHICLES);
            //Function.Call(Hash.SET_MISSION_FLAG);
            // create a blip on the map
            /*Blip blip = World.CreateBlip(
                player.GetOffsetInWorldCoords(new Vector3(0, 10, 0)),
                5.0f
            );
            // seems to make the blip white
            Function.Call(Hash.SET_ENTITY_AS_MISSION_ENTITY, blip);*/

            Function.Call(
                Hash.SET_PED_COORDS_KEEP_VEHICLE,
                Game.Player.Character,
                car_selection.X,
                car_selection.Y,
                car_selection.Z
            );

            //Game.Player.Character.Rotation = car_spawn_player_heading; seems to CRASH the game

            // create an offset in front right of the player
            //Vector3 checkpoint_coords = player.GetOffsetInWorldCoords(new Vector3(10f, 10f, 0));
            Blip blip = World.CreateBlip(race1Start);
            blips.Add(blip);
            blip.Sprite = BlipSprite.Race;
            // create a race checkpoint with direction arrow
            checkpoint = Function.Call<Entity>(
                    Hash.CREATE_CHECKPOINT,
                    0,
                    race1Start.X,
                    race1Start.Y,
                    race1Start.Z - 1,
                    race1End.X,
                    race1End.Y,
                    race1End.Z,
                    5.0f,
                    255,
                    200,
                    0,
                    127,
                    0
                );

            Function.Call(Hash.SET_CHECKPOINT_CYLINDER_HEIGHT, checkpoint, 5.0f, 15.0f, 5.0f);

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