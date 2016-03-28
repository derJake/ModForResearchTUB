using GTA;
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
    class RaceToWoodmill : Script, RaceInterface
    {
        private Vector3[] checkpoints;

        private int raceStartTime;

        private Vehicle raceVehicle;

        private Vector3 car_selection = new Vector3(-786.5052f, -2429.885f, 14.57072f);
        private Vector3 car1_spawnpoint = new Vector3(-789.7347f, -2428.485f, 14.57072f);
        private float car_spawn_heading = 147.0f;
        private float car_spawn_player_heading = 48.0f;

        public RaceToWoodmill() {
            // try and load this area already
            Function.Call(Hash.SET_HD_AREA,
                car1_spawnpoint.X,
                car1_spawnpoint.Y,
                car1_spawnpoint.Z,
                50f
            );
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
            World.RenderingCamera = World.CreateCamera(GameplayCamera.Position, GameplayCamera.Rotation, 60f);
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

        public Vector3[] getCheckpoints()
        {
            return checkpoints;
        }

        public void handleOnTick()
        {
            throw new NotImplementedException();
        }

        public void initRace()
        {
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

            // add some checkpoints for our race
            checkpoints = new Vector3[19];
            checkpoints[0] = new Vector3(-807.8585f, -2466.344f, 14.45607f);
            checkpoints[1] = new Vector3(-810.6682f, -2249.965f, 17.24915f);
            checkpoints[2] = new Vector3(-693.1211f, -2117.945f, 13.12339f);
            checkpoints[3] = new Vector3(-382.7377f, -1838.06f, 21.37794f);
            checkpoints[4] = new Vector3(-246.3216f, -1826.909f, 28.96538f);
            checkpoints[5] = new Vector3(-144.3558f, -1749.146f, 30.12419f);
            checkpoints[6] = new Vector3(-44.43723f, -1630.049f, 28.96328f);
            checkpoints[7] = new Vector3(64.23303f, -1515.456f, 28.93484f);
            checkpoints[8] = new Vector3(138.5995f, -1370.135f, 28.83117f);
            checkpoints[9] = new Vector3(117.2407f, -1356.73f, 28.88704f);
            checkpoints[10] = new Vector3(64.65392f, -1285.516f, 29.33747f);
            checkpoints[11] = new Vector3(64.58669f, -1160.968f, 28.951f);
            checkpoints[12] = new Vector3(96.58853f, -1026.16f, 29.03582f);
            checkpoints[13] = new Vector3(82.72692f, -982.808f, 29.01929f);
            checkpoints[14] = new Vector3(-11.1925f, -931.7217f, 28.90791f);
            checkpoints[15] = new Vector3(32.22876f, -773.1832f, 43.85289f);
            checkpoints[16] = new Vector3(-65.03999f, -725.1259f, 43.86914f);
            checkpoints[17] = new Vector3(-76.04148f, -749.7919f, 43.77972f);
            checkpoints[18] = new Vector3();

            // load the two models
            var vehicle1Model = new Model(VehicleHash.Buffalo);
            vehicle1Model.Request(500);

            if (vehicle1Model.IsInCdImage &&
                vehicle1Model.IsValid
                )
            {
                // If the model isn't loaded, wait until it is
                while (!vehicle1Model.IsLoaded)
                    Script.Wait(100);

                // create the slower, reliable car
                raceVehicle = World.CreateVehicle(VehicleHash.Buffalo, car1_spawnpoint, car_spawn_heading);
                // create the racecar

                // make the fast one colorful, the other one white
                Function.Call(Hash.SET_VEHICLE_CUSTOM_PRIMARY_COLOUR, raceVehicle, 255, 255, 255);
                Function.Call(Hash.SET_VEHICLE_CUSTOM_SECONDARY_COLOUR, raceVehicle, 255, 255, 255);

                Function.Call(Hash.SET_VEHICLE_INTERIORLIGHT, raceVehicle, true);
            }

            vehicle1Model.MarkAsNoLongerNeeded();

            // make player look at cars
            Game.Player.Character.Task.StandStill(5000);

            // create a camera to look through
            Camera cam = World.CreateCamera(
                new Vector3(-799.5338f, -2427f, 14.52622f), // position
                new Vector3(9f, 0f, -82.57458f), // rotation
                90f
            );

            // TO DO: move camera around

            // switch to this camera
            Function.Call(Hash.RENDER_SCRIPT_CAMS, 1, 0, cam, 0, 0);
            // play sound


            Audio.PlaySoundFrontend("SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET");

            UI.ShowSubtitle("Racecar (locked with alarm)", 2500);
            Game.Player.Character.Task.LookAt(car1_spawnpoint, 2500);
            Wait(2500);

            // create a camera to look through
            cam = World.CreateCamera(
                new Vector3(-793.5338f, -2430f, 14.52622f), // position
                new Vector3(10f, 0f, -92.57458f), // rotation
                90f
            );

            // switch back to main cam
            Function.Call(Hash.RENDER_SCRIPT_CAMS, 0, 1, cam, 0, 0);

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
            return Game.Player.Character.CurrentVehicle.Equals(raceVehicle);
        }
    }
}
