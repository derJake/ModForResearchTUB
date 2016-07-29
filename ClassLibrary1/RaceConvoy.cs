using GTA;
using GTA.Math;
using GTA.Native;
using NativeUI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

namespace ModForResearchTUB
{
    class RaceConvoy : Script, RaceInterface
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

        private int regularIntroSceneLength = 10000;

        private TimerBarPool barPool = new TimerBarPool();
        private BarTimerBar distanceBar;

        ResourceManager rm;
        Utilities ut;

        public RaceConvoy(ResourceManager resman, Utilities utils) {
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
            rm = resman;
            ut = utils;
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

        public void handleOnTick(object sender, EventArgs e)
        {
            if (World.GetDistance(raceVehicle.Position, leader.Position) > 150f) {
                UI.ShowSubtitle("~r~Don't lose the other truck!", 1250);
            }

            if (raceStartTime > 0)
            {
                // calculate distance to leading vehicle
                var currentDistance = World.GetDistance(
                        leader.Position,
                        Game.Player.Character.CurrentVehicle.Position
                    );

                distanceBar.Percentage = currentDistance / 200f > 1f ? 1f : currentDistance / 200f;
                barPool.Draw();

                // log it for later diagram drawing
                distance.Add(new Tuple<String, double>(
                    Game.GameTime.ToString(),
                    currentDistance
                ));
            }
        }

        public void initRace()
        {
            var bmsg = BigMessageThread.MessageInstance;
            Logger.Log("Convoy Track Initialization");
            UI.Notify("Convoy Track Initialization");
            UI.ShowSubtitle("Convoy Track Initialization", 1250);

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
            World.CurrentDayTime = new TimeSpan(18, 35, 0);

            // set weather to rain
            Function.Call(Hash.SET_WEATHER_TYPE_NOW_PERSIST, "CLEAR");

            Ped player = Game.Player.Character;
            player.Task.ClearAllImmediately(); // give back control to player

            // teleport player and turn him towards cars
            player.Position = car_selection;
            player.Heading = car_spawn_player_heading;

            // create the vehicles
            raceVehicle = ut.createCarAt(vehicleHash, car1_spawnpoint, car_spawn_heading);
            leader = ut.createCarAt(vehicleHash, leader_spawnpoint, leader_heading);
            leader.IsInvincible = true;

            // create the ped driving the leading vehicle
            leader_driver = ut.createPedAt(PedHash.RampMex, leader_driver_spawnpoint);
            // set it into the vehicle
            leader_driver.SetIntoVehicle(leader, VehicleSeat.Driver);
            // make the ped invincible
            leader_driver.IsInvincible = true;

            Function.Call(Hash.SET_VEHICLE_DOORS_LOCKED, leader, 2);

            // while we're showing what's to come, we don't want the player hurt
            player.IsInvincible = true;

            // make player enter vehicle
            player.SetIntoVehicle(raceVehicle, VehicleSeat.Driver);

            // create a camera to look through
            Camera cam = World.CreateCamera(
                new Vector3(1386, 6509, 20f),
                new Vector3(2, 0, 90f), // rotation
                25f// field of view
            );

            //cam.PointAt(raceVehicle);

            // TO DO: move camera around

            // switch to this camera
            Function.Call(Hash.RENDER_SCRIPT_CAMS, 1, 0, cam, 0, 0);
            // play sound
            Audio.PlaySoundFrontend("SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET");

            bmsg.ShowOldMessage(rm.GetString("convoy_intro_1"), 10000);
            Wait(10000);

            // show different perspective and instruction
            cam.Position = new Vector3(1330, 6505, 20f);
            cam.Rotation = new Vector3(2, 0, -95f);

            bmsg.ShowOldMessage(rm.GetString("convoy_intro_2"), 10000);
            Wait(10000);

            // switch back to main cam
            Function.Call(Hash.RENDER_SCRIPT_CAMS, 0, 1, cam, 0, 0);

            Function.Call(Hash.SET_CINEMATIC_MODE_ACTIVE, true);
            raceVehicle.IsInvincible = true;
            leader_driver.Task.DriveTo(leader, leader_target, 5, 20, Convert.ToInt32("110111111", 2));
            //player.Task.DriveTo(raceVehicle, checkpoints[checkpoints.Length - 1].Item1, 5, 20, Convert.ToInt32("110111111", 2));
            player.Task.VehicleChase(leader_driver);
            player.DrivingStyle = DrivingStyle.AvoidTraffic;

            Wait(regularIntroSceneLength);

            // put vehicles back to start
            player.Task.ClearAll();
            leader_driver.Task.ClearAll();
            raceVehicle.Position = car1_spawnpoint;
            raceVehicle.Heading = car_spawn_heading;
            raceVehicle.IsInvincible = false;
            leader.Position = leader_spawnpoint;
            leader.Heading = leader_heading;

            Function.Call(Hash.SET_CINEMATIC_MODE_ACTIVE, false);

            player.IsInvincible = false;

            // set time of day
            World.CurrentDayTime = new TimeSpan(18, 35, 0);
        }

        public void startRace()
        {
            // try and free terrain loading restriction, so car won't fall through the ground
            Function.Call(Hash.CLEAR_HD_AREA);
            Function.Call(Hash.CLEAR_FOCUS);

            UI.ShowSubtitle("Race started!", 1250);

            Game.Player.Character.CurrentVehicle.NumberPlate = "RACE 2";

            raceStartTime = Game.GameTime;

            leader_driver.Task.DriveTo(leader, leader_target, 5, 20, Convert.ToInt32("110111111", 2));
            Blip leaderblip = leader.AddBlip();
            leaderblip.Color = BlipColor.Blue;

            distanceBar = new BarTimerBar("DISTANCE");
            barPool.Add(distanceBar);
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
    }
}
