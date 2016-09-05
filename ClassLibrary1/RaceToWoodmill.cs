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
        private Tuple<Vector3, Vector3?>[] checkpoints;

        private int raceStartTime;

        private Vehicle raceVehicle;

        private Vector3 car_selection = new Vector3(233.4025f, 5246.316f, 602.2096f);
        private Vector3 car1_spawnpoint = new Vector3(230.0952f, 5246.52f, 601.8268f);
        private float car_spawn_heading = 61f;
        private float car_spawn_player_heading = 72.78187f;

        public RaceToWoodmill() {
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
                new Tuple<Vector3, Vector3?>(new Vector3(207.0003f, 5251.836f, 598.4952f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(188.9684f, 5229.752f, 582.10f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(137.2646f, 5189.865f, 550.0045f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(134.155f, 5224.851f, 544.7256f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(115.9152f, 5188.102f, 532.106f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(113.1634f, 5106.065f, 510.8601f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(100.6905f, 5072.818f, 495.969f), new Vector3(87.26577f, 5069.991f, 492.8376f)),
                new Tuple<Vector3, Vector3?>(new Vector3(93.55081f, 5022.612f, 464.4376f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(38.55313f, 5052.62f, 458.7467f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(17.90152f, 5036.686f, 453.4427f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-9.454967f, 5008.137f, 436.741f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-72.11463f, 4945.062f, 390.1153f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-107.3159f, 4931.15f, 373.186f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-134.2825f, 4915.77f, 353.9966f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-184.3996f, 4901.407f, 332.1235f), null),
                //new Tuple<Vector3, Vector3?>(new Vector3(-55.57574f, 4999.408f, 406.6459f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-221.3511f, 4903.089f, 314.3157f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-239.7362f, 4914.677f, 301.6244f), new Vector3(-244.3771f, 4907.458f, 303.7904f)),
                new Tuple<Vector3, Vector3?>(new Vector3(-285.9736f, 4956.554f, 257.6557f), new Vector3(-301.7302f, 4947.52f, 267.1712f)),
                new Tuple<Vector3, Vector3?>(new Vector3(-328.7078f, 4994.859f, 226.7433f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-355.1001f, 4988.495f, 210.0325f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-377.4987f, 4951.176f, 197.1729f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-413.6729f, 4927.945f, 179.1848f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-475.3094f, 4969.138f, 149.8499f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-523.5085f, 5028.522f, 132.5715f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-587.2845f, 5068.314f, 134.9643f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-599.2308f, 5101.148f, 125.7583f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-581.5574f, 5157.62f, 105.0425f), new Vector3(-591.7618f, 5168.102f, 102.9612f)),
                new Tuple<Vector3, Vector3?>(new Vector3(-547.3408f, 5157.711f, 95.6012f), new Vector3(-584.4101f, 5183.821f, 94.07907f)),
                new Tuple<Vector3, Vector3?>(new Vector3(-552.1667f, 5211.077f, 81.92912f), new Vector3(-582.4078f, 5198.053f, 88.21198f)),
                new Tuple<Vector3, Vector3?>(new Vector3(-663.0966f, 5232.301f, 77.80328f), new Vector3(-582.3427f, 5216.197f, 79.25137f)),
                new Tuple<Vector3, Vector3?>(new Vector3(-578.9683f, 5248.079f, 70.07695f), null),
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

        public void handleOnTick(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        public void initRace()
        {
            Logger.Log("Downhill Initialization");
            UI.Notify("Downhill Initialization");
            UI.ShowSubtitle("Downhill Initialization", 1250);

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
                new Vector3(235.6712f, 5252.954f, 604.5853f), // position
                new Vector3(-14.1894f, -4.40320f, 125.2182f), // rotation
                90f
            );

            //cam.PointAt(raceVehicle);

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
        }

        public void startRace()
        {
            // try and free terrain loading restriction, so car won't fall through the ground
            Function.Call(Hash.CLEAR_HD_AREA);
            Function.Call(Hash.CLEAR_FOCUS);

            UI.ShowSubtitle("task started", 3000);

            Game.Player.Character.CurrentVehicle.NumberPlate = "RACE 2";

            raceStartTime = Game.GameTime;
        }

        public bool checkRaceStartCondition()
        {
            // check which car player is using
            return (Game.Player.Character.IsInVehicle() && Game.Player.Character.CurrentVehicle.Equals(raceVehicle));
        }

        public List<Tuple<String, List<Tuple<String, double>>>> getCollectedData()
        {
            throw new NotImplementedException();
        }

        public bool checkAlternativeBreakCondition()
        {
            return false;
        }

        Dictionary<string, Dictionary<string, double>> RaceInterface.getCollectedData()
        {
            throw new NotImplementedException();
        }

        public string getCanonicalName()
        {
            throw new NotImplementedException();
        }
    }
}
