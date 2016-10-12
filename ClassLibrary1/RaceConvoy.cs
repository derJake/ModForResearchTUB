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
        private Dictionary<string, Dictionary<string, double>> collectedData = new Dictionary<string, Dictionary<string, double>>();
        private Dictionary<string, float> singularValues = new Dictionary<string, float>();
        private Dictionary<string, double> distance = new Dictionary<string, double>();
        private VehicleHash vehicleHash = VehicleHash.Rumpo;

        private int regularIntroSceneLength = 10000;

        private TimerBarPool barPool = new TimerBarPool();
        private BarTimerBar distanceBar;

        private int last_take_over,
            num_take_overs = 0,
            time_player_leads = 0;

        ResourceManager rm;
        Utilities ut;

        public String canonicalName { get; private set; }

        public RaceConvoy(ResourceManager resman, Utilities utils, String taskKey) {
            this.canonicalName = taskKey;

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
            UI.ShowSubtitle(String.Format(rm.GetString("race_finished"), (Game.GameTime - raceStartTime) / 1000), 3000);
            UI.Notify(String.Format(rm.GetString("race_finished"), (Game.GameTime - raceStartTime) / 1000));

            Logger.Log(String.Format("number of times player passed leader: {0}", num_take_overs));
            Logger.Log(String.Format("time player was in front of leader: {0}", time_player_leads));
            Logger.Log(String.Format("leading vehicle distance to target: {0}", World.GetDistance(leader.Position, leader_target)));
            singularValues.Add("times_player_leads", num_take_overs);
            singularValues.Add("duration_player_leads", time_player_leads);
            singularValues.Add("distance_leader_target", World.GetDistance(leader.Position, leader_target));

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

            this.collectedData.Add("distance", distance);
        }

        public Tuple<Vector3, Vector3?>[] getCheckpoints()
        {
            return checkpoints;
        }

        public void handleOnTick(object sender, EventArgs e)
        {
            if (World.GetDistance(raceVehicle.Position, leader.Position) > 150f) {
                UI.ShowSubtitle(rm.GetString("convoy_warning"), 1250);
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
                distance.Add(
                    Game.GameTime.ToString(),
                    currentDistance
                );

                float leader_to_target = World.GetDistance(leader.Position, checkpoints[checkpoints.Length - 1].Item1),
                    player_to_target = World.GetDistance(Game.Player.Character.CurrentVehicle.Position, checkpoints[checkpoints.Length - 1].Item1);
                if (leader_to_target > player_to_target) // player took over leader driver
                {
                    if (last_take_over == 0) // log time
                    {
                        last_take_over = Game.GameTime;
                    }
                }
                else if (last_take_over > 0) { // player fell back behind leader
                    time_player_leads += Game.GameTime - last_take_over;
                    last_take_over = 0;
                    num_take_overs++;
                }

                if (currentDistance > 150)
                {
                    hintAtLeader();
                }
            }
        }

        public void initRace()
        {
            var bmsg = BigMessageThread.MessageInstance;
            Logger.Log(rm.GetString("convoy_initialization"));
            UI.Notify(rm.GetString("convoy_initialization"));
            UI.ShowSubtitle(rm.GetString("convoy_initialization"), 1250);

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

            // switch to this camera
            Function.Call(Hash.RENDER_SCRIPT_CAMS, 1, 0, cam, 0, 0);
            // play sound
            Audio.PlaySoundFrontend("SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET");

            Game.Player.CanControlCharacter = false;
            player.IsInvincible = true;
            raceVehicle.IsInvincible = true;

            bmsg.ShowOldMessage(rm.GetString("convoy_intro_1"), 10000);
            Wait(10000);

            // show different perspective and instruction
            cam.Position = new Vector3(1330, 6505, 20f);
            cam.Rotation = new Vector3(2, 0, -95f);

            bmsg.ShowOldMessage(rm.GetString("convoy_intro_2"), 10000);
            Wait(10000);

            bmsg.ShowOldMessage(rm.GetString("convoy_intro_3"), 10000);
            Wait(1000);
            Function.Call(Hash.FLASH_MINIMAP_DISPLAY);
            Wait(1000);
            Function.Call(Hash.FLASH_MINIMAP_DISPLAY);
            Wait(8000);

            World.RenderingCamera = null;
            World.DestroyAllCameras();

            player.IsInvincible = false;
            Game.Player.CanControlCharacter = true;
        }

        public void startRace()
        {
            // try and free terrain loading restriction, so car won't fall through the ground
            Function.Call(Hash.CLEAR_HD_AREA);
            Function.Call(Hash.CLEAR_FOCUS);

            UI.ShowSubtitle(rm.GetString("task_started"), 3000);

            Game.Player.Character.CurrentVehicle.NumberPlate = this.getCanonicalName();

            Wait(6000);

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

        public Dictionary<String, Dictionary<String, double>> getCollectedData()
        {
            return this.collectedData;
        }

        public bool checkAlternativeBreakCondition()
        {
            return false;
        }

        public String getCanonicalName()
        {
            return canonicalName;
        }

        public Dictionary<string, float> getSingularDataValues()
        {
            return singularValues;
        }

        private void hintAtLeader() {
            World.DrawMarker(
                MarkerType.ThickChevronUp,
                leader.Position + new Vector3(0,0,10),
                new Vector3(),
                new Vector3(180, 0, 0),
                new Vector3(10,10,10),
                Color.Blue
            );
        }
    }
}
