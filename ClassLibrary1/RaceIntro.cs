using GTA;
using GTA.Math;
using GTA.Native;
using ModForResearchTUB.Properties;
using NativeUI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

namespace ModForResearchTUB
{
    class RaceIntro : Script, RaceInterface
    {
        private Tuple<Vector3, Vector3?>[] checkpoints;

        private int raceStartTime;

        private Vehicle raceVehicle;
        private Vehicle leader;

        private Vector3 car_selection = new Vector3(1371.112f, 6495.264f, 20.00329f);
        private Vector3 car1_spawnpoint = new Vector3(1372.046f, 6510.465f, 19.66112f);
        private Vector3 leader_spawnpoint = new Vector3(1345.384f, 6503.437f, 19.87024f);
        private Vector3 leader_driver_spawnpoint = new Vector3(1343.086f, 6506.767f, 19.7821f);
        private Vector3 leader_target = new Vector3(-769.6273f, 5545.085f, 33.11019f);
        private Ped leader_driver;
        private float leader_heading = 82f;
        private float car_spawn_heading = 94.7f;
        private float car_spawn_player_heading = 71.70087f;
        private List<Tuple<String, List<Tuple<String, double>>>> collectedData = new List<Tuple<String, List<Tuple<String, double>>>>();
        private List<Tuple<String, double>> distance = new List<Tuple<String, double>>();
        private VehicleHash vehicleHash = VehicleHash.Rumpo;

        public CultureInfo CultureInfo { get; private set; }

        ResourceManager rm;

        public RaceIntro() {
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
                new Tuple<Vector3, Vector3?>(new Vector3(-763.5227f, 5506.236f, 34.75044f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-780.5091f, 5524.204f, 33.9245f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-780.6508f, 5548.24f, 33.12004f), null),
            };

            this.checkpoints = checkpointlist;

            CultureInfo = CultureInfo.CurrentCulture;
            rm = new ResourceManager(typeof(Resources));
        }

        public void finishRace()
        {
            UI.ShowSubtitle(String.Format("Race finished! - Time: {0}s", (Game.GameTime - raceStartTime) / 1000), 3000);
            UI.Notify(String.Format("Race finished! - Time: {0}s", (Game.GameTime - raceStartTime) / 1000));

            // drop wanted level
            Function.Call(Hash.SET_PLAYER_WANTED_LEVEL, Game.Player, 0, false);
            Function.Call(Hash.SET_PLAYER_WANTED_LEVEL_NOW, Game.Player, false);

            // make weather nice again
            Function.Call(Hash.SET_WEATHER_TYPE_NOW, "EXTRASUNNY");

            Game.Player.CanControlCharacter = false;
            Game.Player.Character.IsInvincible = true;

            // camera FX
            Function.Call(Hash._START_SCREEN_EFFECT, "HeistCelebPass", 0, true);
            if (Game.Player.Character.IsInVehicle())
                Game.Player.Character.CurrentVehicle.HandbrakeOn = true;
            World.DestroyAllCameras();
            World.RenderingCamera = World.CreateCamera(new Vector3(-771.7985f, 5550.436f, 33.47573f), new Vector3(12.26449f, 0, 109.785f), 90f);
            //World.RenderingCamera.PointAt(new Vector3(-550.3082f, 5291.048f, 90.11024f));

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

            this.collectedData.Add(new Tuple<string, List<Tuple<string, double>>>("distance", distance));
        }

        public Tuple<Vector3, Vector3?>[] getCheckpoints()
        {
            return checkpoints;
        }

        public void handleOnTick()
        {
            if (raceStartTime > 0)
            {
                distance.Add(new Tuple<String, double>(
                    Game.GameTime.ToString(),
                    World.GetDistance(
                        leader.Position,
                        Game.Player.Character.CurrentVehicle.Position
                    )
                ));
            }
        }

        public void initRace()
        {
            Logger.Log("Intro Track Initialization");
            UI.Notify("Intro Track Initialization");
            UI.ShowSubtitle("Intro Track Initialization", 1250);

            // while we're showing what's to come, we don't want the player hurt
            Game.Player.Character.IsInvincible = true;

            doIntroSequence();

            Game.Player.Character.IsInvincible = false;

            // set weather to rain
            Function.Call(Hash.SET_WEATHER_TYPE_NOW_PERSIST, "CLEAR");

            Ped player = Game.Player.Character;
            player.Task.ClearAllImmediately(); // give back control to player

            // teleport player and turn him towards cars
            Game.Player.Character.Position = car_selection;
            Game.Player.Character.Heading = car_spawn_player_heading;

            // load the vehicle model
            var vehicle1Model = new Model(vehicleHash);
            vehicle1Model.Request(500);

            if (vehicle1Model.IsInCdImage &&
                vehicle1Model.IsValid
                )
            {
                // If the model isn't loaded, wait until it is
                while (!vehicle1Model.IsLoaded)
                    Script.Wait(100);

                // create the vehicle
                raceVehicle = World.CreateVehicle(vehicleHash, car1_spawnpoint, car_spawn_heading);
            }

            vehicle1Model.MarkAsNoLongerNeeded();
        }

        public void startRace()
        {
            // try and free terrain loading restriction, so car won't fall through the ground
            Function.Call(Hash.CLEAR_HD_AREA);
            Function.Call(Hash.CLEAR_FOCUS);

            UI.ShowSubtitle("Race started!", 1250);

            Game.Player.Character.CurrentVehicle.NumberPlate = "INTRO";

            raceStartTime = Game.GameTime;
        }

        public bool checkRaceStartCondition()
        {
            // check which car player is using
            return (Game.Player.Character.IsInVehicle() && Game.Player.Character.CurrentVehicle.Equals(raceVehicle));
        }

        public List<Tuple<string, List<Tuple<string, double>>>> getCollectedData()
        {
            return this.collectedData;
        }

        private void doIntroSequence() {
            var bmsg = BigMessageThread.MessageInstance;
            // set time of day
            World.CurrentDayTime = new TimeSpan(6, 35, 0);

            // set weather
            Function.Call(Hash.SET_WEATHER_TYPE_NOW_PERSIST, "CLEAR");

            Ped player = Game.Player.Character;
            player.Task.ClearAllImmediately(); // give back control to player

            // while we're showing what's to come, we don't want the player hurt
            Game.Player.Character.IsInvincible = true;
            Game.Player.CanControlCharacter = false;
            Game.Player.Character.Heading = 242.65f;
            showVector(
                new Vector3(76.05127f, -1012.558f, 79.81089f), // character
                new Vector3(72.1081f, -1011.055f, 81.04148f), // camPos
                new Vector3(-8.138871f, 0, -116.3f) // camRot
            );

            bmsg.ShowOldMessage(rm.GetString("intro1"), 5000);
            Wait(5000);

            // set time of day
            World.CurrentDayTime = new TimeSpan(18, 35, 0);

            // set weather
            Function.Call(Hash.SET_WEATHER_TYPE_NOW_PERSIST, "EXTRASUNNY");

            showVector(
                new Vector3(106.7525f, -502.1675f, 43.36741f), // character
                new Vector3(111.2453f, -502.276f, 53.76136f), // camPos
                new Vector3(2.76f, 2.67f, 83.51f) // camRot
            );

            bmsg.ShowOldMessage(rm.GetString("intro2"), 5000);
            Wait(5000);

            World.CurrentDayTime = new TimeSpan(10, 35, 0);

            showVector(
                new Vector3(53.65551f, -177.0567f, 54.96252f), // character
                new Vector3(60.103f, -163.81f, 60.98f), // camPos
                new Vector3(-15.33f, 0, 68.15559f) // camRot
            );

            bmsg.ShowOldMessage(rm.GetString("intro3"), 5000);
            Wait(5000);

            World.CurrentDayTime = new TimeSpan(11, 45, 0);

            List<Ped> characters = spawnCharacters();
            showVector(
                new Vector3(-341.388f, 1147.779f, 325.7267f),
                new Vector3(-343f, 1151, 327f),
                new Vector3(-8.71f, 0, -179.58f)
            );
            World.RenderingCamera.FieldOfView = 70;
            bmsg.ShowOldMessage(rm.GetString("intro3"), 5000);
            Wait(5000);

            // delete the additional character peds
            foreach (Ped charped in characters) {
                charped.Delete();
            }

            // show getting into car
            showVector(
                new Vector3(-341.388f, 1147.779f, 325.7267f),
                new Vector3(-343f, 1151, 327f),
                new Vector3(-8.71f, 0, -179.58f)
            );

            Vehicle car = createCarAt(VehicleHash.Adder, player.Position + 5*player.ForwardVector, player.Heading - 90);

            Game.Player.Character.Task.EnterVehicle(car, VehicleSeat.Driver, 10000, 1.0f, 1);
            //Game.Player.Character.SetIntoVehicle(car, VehicleSeat.Driver);

            //World.RenderingCamera.FieldOfView = 70;
            bmsg.ShowOldMessage(rm.GetString("intro4"), 5000);
            Wait(5000);

            // give control back and use regular camera
            World.RenderingCamera = null;
            World.DestroyAllCameras();
            Game.Player.Character.IsInvincible = false;
            Game.Player.CanControlCharacter = true;
        }

        private void showVector(Vector3 characterPosition, Vector3 cameraPosition, Vector3 cameraRotation) {
            Game.Player.Character.Position = characterPosition;
            // create a camera to look through
            Camera cam = World.CreateCamera(
                cameraPosition, // position
                cameraRotation, // rotation
                90f // field of view
            );
            // switch to this camera
            Function.Call(Hash.RENDER_SCRIPT_CAMS, 1, 0, cam, 0, 0);
        }

        private void showEntity(Vector3 characterPosition, Vector3 cameraPosition, Entity entityOfInterest) {
            Game.Player.Character.Position = characterPosition;
            // create a camera to look through
            Camera cam = World.CreateCamera(
                cameraPosition, // position
                new Vector3(0,0,0), // rotation
                90f // field of view
            );
            cam.PointAt(entityOfInterest);
            // switch to this camera
            Function.Call(Hash.RENDER_SCRIPT_CAMS, 1, 0, cam, 0, 0);
        }

        private List<Ped> spawnCharacters() {

            List<Ped> characters = new List<Ped>(2);
            
            var franklin = new Model(PedHash.Franklin);
            franklin.Request();

            if (franklin.IsInCdImage &&
                franklin.IsValid
                )
            {
                // If the model isn't loaded, wait until it is
                while (!franklin.IsLoaded)
                    Script.Wait(100);

                // create the actual driver ped
                var franklin_ped = World.CreatePed(franklin, new Vector3(-343.1626f, 1147.788f, 325.7267f));
                franklin_ped.Heading = 7.05f;
                characters.Add(franklin_ped);
            }

            var trevor = new Model(PedHash.Trevor);
            trevor.Request();

            if (trevor.IsInCdImage &&
                trevor.IsValid
                )
            {
                // If the model isn't loaded, wait until it is
                while (!trevor.IsLoaded)
                    Script.Wait(100);

                // create the actual driver ped
                var trevor_ped = World.CreatePed(trevor, new Vector3(-345.1988f, 1147.625f, 325.7263f));
                trevor_ped.Heading = 7.05f;
                characters.Add(trevor_ped);
            }

            return characters;
        }

        private Vehicle createCarAt(VehicleHash carmodelhash, Vector3 coordinates, float heading) {
            // load the vehicle model
            var vehicle1Model = new Model(carmodelhash);
            vehicle1Model.Request(500);

            if (vehicle1Model.IsInCdImage &&
                vehicle1Model.IsValid
                )
            {
                // If the model isn't loaded, wait until it is
                while (!vehicle1Model.IsLoaded)
                    Script.Wait(100);

                // create the vehicle
                Vehicle vehicle = World.CreateVehicle(carmodelhash, coordinates, heading);
            }

            vehicle1Model.MarkAsNoLongerNeeded();

            return vehicle;
        }
    }
}
