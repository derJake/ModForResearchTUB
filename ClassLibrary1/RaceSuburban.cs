﻿using GTA;
using GTA.Math;
using GTA.Native;
using NativeUI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModForResearchTUB
{
    class RaceSuburban : Script, RaceInterface
    {
        private Tuple<Vector3, Vector3?>[] checkpoints;

        private int raceStartTime;

        private Vehicle raceVehicle;

        private Vector3 car1_spawnpoint = new Vector3(-478.9794f, 654.7506f, 143.7375f);
        private Vector3 player_spawnpoint = new Vector3(-482.3359f, 654.1779f, 144.0759f);
        private float car_spawn_heading = 61f;
        private Vector3 obstacle_spawnpoint = new Vector3(-813.8827f, 710.9035f, 146.8423f);
        private Vector3 obstacle_trigger = new Vector3(-777.0529f, 704.7166f, 144.5829f);
        private Vehicle obstacle;
        private Ped obstacle_driver;
        private Vector3 obstacle_driver_spawnpoint = new Vector3(-817.1896f, 709.8116f, 147.2454f);
        private Vector3 obstacle_target = new Vector3(-1042.243f, 775.3391f, 167.1406f);
        private float obstacle_spawn_heading = 19.92268f;
        private bool obstacle_started = false;

        public RaceSuburban() {
            // try and load this area already
            Function.Call(Hash.SET_HD_AREA,
                car1_spawnpoint.X,
                car1_spawnpoint.Y,
                car1_spawnpoint.Z,
                50f
            );

            // add some checkpoints for our race
            Tuple<Vector3, Vector3?>[] checkpointlist =
            {
                new Tuple<Vector3, Vector3?>(new Vector3(-499.3143f, 662.8717f, 140.9828f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-539.3671f, 669.1696f, 143.0276f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-632.4818f, 692.2712f, 150.6278f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-667.5978f, 701.4254f, 153.4151f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-681.6805f, 690.8635f, 153.9171f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-686.3596f, 613.5186f, 144.1656f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-743.2444f, 622.3666f, 141.9783f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-779.8662f, 706.8424f, 144.8662f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-871.2201f, 709.1328f, 149.1696f), null),
            };

            this.checkpoints = checkpointlist;
        }

        public void finishRace()
        {
            UI.ShowSubtitle(String.Format("Race finished! - Time: {0}s", (Game.GameTime - raceStartTime) / 1000), 3000);
            UI.Notify(String.Format("Race finished! - Time: {0}s", (Game.GameTime - raceStartTime) / 1000));

            // drop wanted level
            Function.Call(Hash.SET_PLAYER_WANTED_LEVEL, Game.Player, 0, false);
            Function.Call(Hash.SET_PLAYER_WANTED_LEVEL_NOW, Game.Player, false);

            // make weather nice again
            Function.Call(Hash.SET_WEATHER_TYPE_NOW, "CLEARING");

            Game.Player.CanControlCharacter = false;
            Game.Player.Character.IsInvincible = true;

            // camera FX
            Function.Call(Hash._START_SCREEN_EFFECT, "HeistCelebPass", 1000, true);
            if (Game.Player.Character.IsInVehicle())
                Game.Player.Character.CurrentVehicle.HandbrakeOn = true;
            World.DestroyAllCameras();
            World.RenderingCamera = World.CreateCamera(new Vector3(-877.6011f, 708.1453f, 149f), new Vector3(7f, 0, 235f), 90f);
            World.RenderingCamera.PointAt(Game.Player.Character);
            World.RenderingCamera.DepthOfFieldStrength = 200f;

            // play sounds
            Audio.PlaySoundFrontend("RACE_PLACED", "HUD_AWARDS");
            Wait(750);
            Function.Call(Hash.PLAY_SOUND_FRONTEND, 0, "CHECKPOINT_UNDER_THE_BRIDGE", "HUD_MINI_GAME_SOUNDSET");
            Wait(2000);

            // reset camera stuff
            Function.Call(Hash._STOP_SCREEN_EFFECT, "HeistCelebPass");
            World.RenderingCamera = null;

            Game.Player.Character.IsInvincible = false;
            Game.Player.CanControlCharacter = true;

            obstacle_driver.Delete();
            obstacle.Delete();

            raceVehicle.MarkAsNoLongerNeeded();
            raceVehicle.Delete();
        }

        public Tuple<Vector3, Vector3?>[] getCheckpoints()
        {
            return checkpoints;
        }

        public void handleOnTick()
        {
            if (!obstacle_started &&
                Game.Player.Character.IsInRangeOf(obstacle_trigger, 7.0f)) {
                foreach (Vehicle car in World.GetNearbyVehicles(obstacle_spawnpoint, 7.0f)) {
                    if (!car.Equals(obstacle)) {
                        car.Delete();
                    }
                }
                obstacle_driver.Task.DriveTo(obstacle, obstacle_target, 3.0f, 10.0f, (int)DrivingStyle.Normal);
                obstacle_started = true;
                Logger.Log(String.Format("garbage truck started driving at {0}", Game.GameTime));
            }
        }

        public void initRace()
        {
            Logger.Log("RaceSuburban.initRace()");
            UI.Notify("RaceSuburban.initRace()");
            UI.ShowSubtitle("initializing suburban track", 1250);

            // try to clear parking lot where cars are spawned
            // TO DO: check, if the boolean parameters have been documented
            // TO DO: spawn cars on the other side of the curb, to avoid false positives
            Function.Call(Hash.CLEAR_AREA_OF_VEHICLES,
                car1_spawnpoint.X,
                car1_spawnpoint.Y,
                car1_spawnpoint.Z,
                50f,
                false,
                false,
                false,
                false,
                false
            );

            // set time of day
            World.CurrentDayTime = new TimeSpan(8, 15, 0);

            // set weather to rain
            Function.Call(Hash.SET_WEATHER_TYPE_NOW_PERSIST, "CLEAR");

            Ped player = Game.Player.Character;
            player.Task.ClearAllImmediately(); // give back control to player

            // teleport player and turn him towards cars
            player.Position = player_spawnpoint;
            player.Heading = 309;

            // load the car model
            var vehicle1Model = new Model(VehicleHash.Surge);
            vehicle1Model.Request(500);

            if (vehicle1Model.IsInCdImage &&
                vehicle1Model.IsValid
                )
            {
                // If the model isn't loaded, wait until it is
                while (!vehicle1Model.IsLoaded)
                    Script.Wait(100);

                // create the slower, reliable car
                raceVehicle = World.CreateVehicle(VehicleHash.Surge, car1_spawnpoint, car_spawn_heading);
                // create the racecar

                // make the fast one colorful, the other one white
                Function.Call(Hash.SET_VEHICLE_CUSTOM_PRIMARY_COLOUR, raceVehicle, 100, 0, 0);
                Function.Call(Hash.SET_VEHICLE_CUSTOM_SECONDARY_COLOUR, raceVehicle, 100, 0, 0);

                raceVehicle.PearlescentColor = VehicleColor.Chrome;

                Function.Call(Hash.SET_VEHICLE_INTERIORLIGHT, raceVehicle, true);
            }

            // load the car model
            var obstacleModel = new Model(VehicleHash.Trash);
            obstacleModel.Request(500);

            if (obstacleModel.IsInCdImage &&
                obstacleModel.IsValid
                )
            {
                // If the model isn't loaded, wait until it is
                while (!obstacleModel.IsLoaded)
                    Script.Wait(100);

                // create the slower, reliable car
                obstacle = World.CreateVehicle(VehicleHash.Trash, obstacle_spawnpoint, obstacle_spawn_heading);
            }

            // load the driver model
            var driver = new Model(PedHash.GarbageSMY);
            driver.Request(500);

            driver.MarkAsNoLongerNeeded();

            if (driver.IsInCdImage &&
                driver.IsValid
                )
            {
                // If the model isn't loaded, wait until it is
                while (!driver.IsLoaded)
                    Script.Wait(100);

                // create the slower, reliable car
                obstacle_driver = World.CreatePed(driver, obstacle_driver_spawnpoint);
                obstacle_driver.Task.EnterVehicle(obstacle, VehicleSeat.Driver);
                obstacle_driver.SetIntoVehicle(obstacle, VehicleSeat.Driver);
            }

            // while we're showing what's to come, we don't want the player hurt
            Game.Player.Character.IsInvincible = true;

            Game.Player.CanControlCharacter = false;

            // make player look at cars
            Game.Player.Character.Task.EnterVehicle(raceVehicle, VehicleSeat.Driver);

            // create a camera to look through
            Camera cam = World.CreateCamera(
                new Vector3(-481.0917f, 658.4233f, 143.9391f), // position
                new Vector3(9f, 0f, -82.57458f), // rotation
                90f
            );

            cam.PointAt(raceVehicle);

            // TO DO: move camera around

            // switch to this camera
            Function.Call(Hash.RENDER_SCRIPT_CAMS, 1, 0, cam, 0, 0);

            // play sound
            Audio.PlaySoundFrontend("SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET");

            UI.ShowSubtitle("~bla~ Drive around the neighbourhood!", 2500);
            Game.Player.Character.Task.LookAt(car1_spawnpoint, 2500);
            Wait(5000);

            // switch back to main cam
            Function.Call(Hash.RENDER_SCRIPT_CAMS, 0, 1, cam, 0, 0);
            Game.Player.Character.IsInvincible = false;
        }

        public void setCurrentCheckpoint(int index)
        {
            return;
        }

        public void startRace()
        {
            // try and free terrain loading restriction, so car won't fall through the ground
            Function.Call(Hash.CLEAR_HD_AREA);
            Function.Call(Hash.CLEAR_FOCUS);

            UI.ShowSubtitle("Race started!", 1250);

            Game.Player.Character.CurrentVehicle.NumberPlate = "RACE 3";

            raceStartTime = Game.GameTime;

            Game.Player.CanControlCharacter = true;
        }

        public bool checkRaceStartCondition()
        {
            // check which car player is using
            return (Game.Player.Character.IsInVehicle() && Game.Player.Character.CurrentVehicle.Equals(raceVehicle));
        }
    }
}
