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
    class RaceDesert : Script, RaceInterface
    {
        private Tuple<Vector3, Vector3?>[] checkpoints;

        private int raceStartTime;

        private Vehicle raceVehicle;

        private Vector3 car_selection = new Vector3(2770.514f, 3461.25f, 55.6087f);
        private Vector3 car1_spawnpoint = new Vector3(2768.046f, 3456.5f, 55.2822f);
        private float car_spawn_heading = 245.6047f;
        private float car_spawn_player_heading = 165.074f;

        public RaceDesert() {
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
                new Tuple<Vector3, Vector3?>(new Vector3(2755.279f, 3407.343f, 55.81804f), new Vector3(2725.262f, 3430.425f, 55.95811f)),
                new Tuple<Vector3, Vector3?>(new Vector3(2039.139f, 3321.247f, 45.25034f), new Vector3(3212.786f, 3462.548f, 63.5235f)),
                new Tuple<Vector3, Vector3?>(new Vector3(2039.588f, 3454.929f, 43.42058f), null),
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
            Function.Call(Hash._START_SCREEN_EFFECT, "HeistCelebPass", 0, true);
            if (Game.Player.Character.IsInVehicle())
                Game.Player.Character.CurrentVehicle.HandbrakeOn = true;
            World.DestroyAllCameras();
            World.RenderingCamera = World.CreateCamera(new Vector3(-584.9957f, 5245.587f, 70.46933f), GameplayCamera.Rotation, 90f);
            World.RenderingCamera.PointAt(new Vector3(-550.3082f, 5291.048f, 90.11024f));
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

            raceVehicle.MarkAsNoLongerNeeded();
            raceVehicle.Delete();
        }

        public Tuple<Vector3, Vector3?>[] getCheckpoints()
        {
            return checkpoints;
        }

        public void handleOnTick()
        {
            throw new NotImplementedException();
        }

        public void initRace()
        {
            Logger.Log("RaceToWoodmill.initRace()");
            UI.Notify("RaceToWoodmill.initRace()");
            UI.ShowSubtitle("initializing woodmill track", 1250);

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
            World.CurrentDayTime = new TimeSpan(19, 15, 0);

            // set weather to rain
            Function.Call(Hash.SET_WEATHER_TYPE_NOW_PERSIST, "CLEARING");

            Ped player = Game.Player.Character;
            player.Task.ClearAllImmediately(); // give back control to player

            // teleport player and turn him towards cars
            Game.Player.Character.Position = car_selection;
            Game.Player.Character.Heading = car_spawn_player_heading;

            // load the two models
            var vehicle1Model = new Model(VehicleHash.TriBike3);
            vehicle1Model.Request(500);

            if (vehicle1Model.IsInCdImage &&
                vehicle1Model.IsValid
                )
            {
                // If the model isn't loaded, wait until it is
                while (!vehicle1Model.IsLoaded)
                    Script.Wait(100);

                // create the slower, reliable car
                raceVehicle = World.CreateVehicle(VehicleHash.TriBike3, car1_spawnpoint, car_spawn_heading);
                // create the racecar

                // make the fast one colorful, the other one white
                Function.Call(Hash.SET_VEHICLE_CUSTOM_PRIMARY_COLOUR, raceVehicle, 255, 255, 255);
                Function.Call(Hash.SET_VEHICLE_CUSTOM_SECONDARY_COLOUR, raceVehicle, 255, 255, 255);

                Function.Call(Hash.SET_VEHICLE_INTERIORLIGHT, raceVehicle, true);
            }

            vehicle1Model.MarkAsNoLongerNeeded();

            // while we're showing what's to come, we don't want the player hurt
            Game.Player.Character.IsInvincible = true;

            // make player look at cars
            Game.Player.Character.Task.StandStill(5000);

            // create a camera to look through
            Camera cam = World.CreateCamera(
                Game.Player.Character.Position, // position
                new Vector3(9f, 0f, -82.57458f), // rotation
                90f
            );

            cam.PointAt(raceVehicle);

            // TO DO: move camera around

            // switch to this camera
            Function.Call(Hash.RENDER_SCRIPT_CAMS, 1, 0, cam, 0, 0);
            // play sound


            Audio.PlaySoundFrontend("SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET");

            UI.ShowSubtitle("~bla~ Drive downhill to the woodmill!", 2500);
            Game.Player.Character.Task.LookAt(car1_spawnpoint, 2500);
            Wait(2500);

            // switch back to main cam
            Function.Call(Hash.RENDER_SCRIPT_CAMS, 0, 1, cam, 0, 0);
            Game.Player.Character.IsInvincible = false;

            UI.ShowSubtitle("Choose one to start the race!", 2500);
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

            Game.Player.Character.CurrentVehicle.NumberPlate = "RACE 2";

            raceStartTime = Game.GameTime;
        }

        public bool checkRaceStartCondition()
        {
            // check which car player is using
            return (Game.Player.Character.IsInVehicle() && Game.Player.Character.CurrentVehicle.Equals(raceVehicle));
        }
    }
}
