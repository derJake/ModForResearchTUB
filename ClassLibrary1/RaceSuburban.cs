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
    class RaceSuburban : Script, RaceInterface
    {
        private Tuple<Vector3, Vector3?>[] checkpoints;

        private int raceStartTime;

        private Vehicle raceVehicle;

        private Vector3 car1_spawnpoint = new Vector3(-478.9794f, 654.7506f, 143.7375f);
        private Vector3 player_spawnpoint = new Vector3(-482.3359f, 654.1779f, 144.0759f);
        private float car_spawn_heading = 61f;
        private Vector3 obstacle_spawnpoint = new Vector3(-813.8827f, 710.9035f, 146.8423f);
        private Vector3 obstacle_trigger = new Vector3(-752.3201f, 659.8228f, 142.8785f);
        private Vehicle obstacle;
        private Ped obstacle_driver;
        private Vector3 obstacle_driver_spawnpoint = new Vector3(-817.1896f, 709.8116f, 147.2454f);
        private Vector3 obstacle_target = new Vector3(-1042.243f, 775.3391f, 167.1406f);
        private Vector3 standstill_area_corner_1 = new Vector3(-800.9453f, 716.5583f, 145.9721f);
        private Vector3 standstill_area_corner_2 = new Vector3(-775.9354f, 703.6378f, 144.5459f);
        private float obstacle_spawn_heading = 19.92268f;
        private bool obstacle_started = false;
        private int obstacle_visible;
        private bool player_passed_obstacle = false;
        private int standstill_brake_start;
        private int standstill_brake_end;
        private int obstacleStartTime = 0;
        private Dictionary<Vector3, float> exclusionZones;
        private List<Vehicle> knownCars;
        private Vector3 sendCarsTo = new Vector3(-814, 832, 201);

        private Dictionary<string, float> singularValues = new Dictionary<string, float>();

        ResourceManager rm;
        Utilities ut;

        public String canonicalName { get; private set; }

        public RaceSuburban(ResourceManager resman, Utilities utils, String taskKey) {
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
                new Tuple<Vector3, Vector3?>(new Vector3(-499.3143f, 662.8717f, 140.9828f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-539.3671f, 669.1696f, 143.0276f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-632.4818f, 692.2712f, 150.6278f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-667.5978f, 701.4254f, 153.4151f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-681.6805f, 690.8635f, 153.9171f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-686.3596f, 613.5186f, 144.1656f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-743.2444f, 622.3666f, 141.9783f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-779.8662f, 706.8424f, 144.8662f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-871.2201f, 709.1328f, 149.1696f), null),
                //new Tuple<Vector3, Vector3?>(new Vector3(-920.3366f, 701.915f, 151.5278f), null),
                //new Tuple<Vector3, Vector3?>(new Vector3(-976.8248f, 699.5857f, 156.134f), null),
                //new Tuple<Vector3, Vector3?>(new Vector3(-1016.307f, 707.3165f, 162.142f), null),
                //new Tuple<Vector3, Vector3?>(new Vector3(-1038.259f, 740.6125f, 166.4818f), null),
                //new Tuple<Vector3, Vector3?>(new Vector3(-1041.269f, 780.5007f, 167.2972f), null),
                //new Tuple<Vector3, Vector3?>(new Vector3(-1022.013f, 789.4909f, 169.56f), null),
                //new Tuple<Vector3, Vector3?>(new Vector3(-1018.94f, 806.6804f, 171.3554f), null),
            };

            this.checkpoints = checkpointlist;

            rm = resman;
            ut = utils;

            exclusionZones = new Dictionary<Vector3, float> {
                { obstacle_spawnpoint, 200f },
                { new Vector3(-690, 599, 143), 180 },
                { new Vector3(-1005, 695, 160), 170 }
            };

            knownCars = new List<Vehicle>();
        }

        public void finishRace()
        {
            UI.ShowSubtitle(String.Format(rm.GetString("race_finished"), (Game.GameTime - raceStartTime) / 1000), 3000);
            UI.Notify(String.Format(rm.GetString("race_finished"), (Game.GameTime - raceStartTime) / 1000));

            Logger.Log(String.Format("obstacle visible: {0}", obstacle_visible));

            singularValues.Add("obstacle_visible", obstacle_visible);
            singularValues.Add("player_passed_garbage_truck", 0);
            singularValues.Add("brake_in_front_of_garbage_truck", 0);

            if (player_passed_obstacle)
            {
                singularValues["player_passed_garbage_truck"] = 1;
                Logger.Log("player passed garbage truck");
            }
            else {
                Logger.Log("player was behind garbage truck");
            }

            if (standstill_brake_end > 0) {
                float standstill_time = (standstill_brake_start - standstill_brake_end / 1000);
                Logger.Log(String.Format("player braked in front of obstacle for {0}s", standstill_time));
                singularValues["brake_in_front_of_garbage_truck"] = standstill_time;
            }

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

        public void handleOnTick(object sender, EventArgs e)
        {
            if (!obstacle_started)
            {
                sendAwayOtherCars();

                // trigger first
                if (Game.Player.Character.IsInRangeOf(obstacle_trigger, 7.0f))
                {
                    float dist = Game.Player.Character.Position.DistanceTo(obstacle_spawnpoint),
                        speed = Game.Player.Character.CurrentVehicle.Speed;
                    int timeToStart = Convert.ToInt32(Math.Round(dist / speed))*1000;

                    UI.Notify(String.Format("timeToStart: {0}", timeToStart));
                    obstacleStartTime = Game.GameTime + timeToStart;
                }

                // start obstacle driving
                if (obstacleStartTime > 0) {

                    new UIResText(String.Format("obstacle in {0}", obstacleStartTime - Game.GameTime), new Point(400, 20), 0.5f, Color.OrangeRed).Draw();

                    if (Game.GameTime > obstacleStartTime)
                    {
                        obstacle_driver.Task.DriveTo(obstacle, obstacle_target, 3.0f, 10.0f, (int)DrivingStyle.Normal);
                        obstacle_started = true;
                        Logger.Log(String.Format("garbage truck started driving at {0}", Game.GameTime));
                    }
                }
            }
            else {
                if (obstacle.IsVisible && obstacle_visible == 0) {
                    obstacle_visible = Game.GameTime;
                    Logger.Log(String.Format("garbage truck visible at {0}", obstacle_visible));
                }
                // compare distance of garbage truck to finish with player's car to finish
                if (World.GetDistance(obstacle.Position, checkpoints[checkpoints.Length - 1].Item1) >
                    World.GetDistance(raceVehicle.Position, checkpoints[checkpoints.Length - 1].Item1))
                {
                    player_passed_obstacle = true;
                }
                else {
                    player_passed_obstacle = false;
                }
            }

            if (obstacle_visible > 0) {
                //new UIResText(String.Format("obstacle visible: {0}", obstacle_visible), new Point(950, 75), 0.3f, Color.White).Draw();

                // brakeing?
                if (Function.Call<int>(Hash.GET_CONTROL_VALUE, 0, 8) >= 254 &&
                    obstacle_visible + 2000 < Game.GameTime &&
                    raceVehicle.IsInArea(standstill_area_corner_1, standstill_area_corner_2)) {
                    standstill_brake_start = Game.GameTime;
                }
                if (standstill_brake_start > 0 && standstill_brake_end == 0 && Function.Call<int>(Hash.GET_CONTROL_VALUE, 0, 8) < 254) {
                    standstill_brake_end = Game.GameTime;
                }
                if (standstill_brake_end > 0) {
                    new UIResText(String.Format("brake in front of obstacle: {0}", (standstill_brake_end - standstill_brake_start / 1000)), new Point(950, 125), 0.3f, Color.White).Draw();
                }
            }
        }

        public void initRace()
        {
            var bmsg = BigMessageThread.MessageInstance;
            Logger.Log(rm.GetString("suburban_initialization"));
            UI.Notify(rm.GetString("suburban_initialization"));
            UI.ShowSubtitle(rm.GetString("suburban_initialization"), 1250);

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
            raceVehicle = ut.createCarAt(VehicleHash.Surge, car1_spawnpoint, car_spawn_heading);
            // set colors for vehicle
            Function.Call(Hash.SET_VEHICLE_CUSTOM_PRIMARY_COLOUR, raceVehicle, 100, 0, 0);
            Function.Call(Hash.SET_VEHICLE_CUSTOM_SECONDARY_COLOUR, raceVehicle, 100, 0, 0);

            raceVehicle.PearlescentColor = VehicleColor.Chrome;

            Function.Call(Hash.SET_VEHICLE_INTERIORLIGHT, raceVehicle, true);

            // load the car model
            obstacle = ut.createCarAt(VehicleHash.Trash, obstacle_spawnpoint, obstacle_spawn_heading);

            // load the driver model
            obstacle_driver = ut.createPedAt(PedHash.GarbageSMY, obstacle_driver_spawnpoint);
            obstacle_driver.SetIntoVehicle(obstacle, VehicleSeat.Driver);

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

            // switch to this camera
            Function.Call(Hash.RENDER_SCRIPT_CAMS, 1, 0, cam, 0, 0);
            // play sound
            Audio.PlaySoundFrontend("SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET");
            bmsg.ShowOldMessage(rm.GetString("suburban_intro_1"), 10000);
            Wait(10000);

            // play sound
            Audio.PlaySoundFrontend("SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET");
            // change perspective
            cam.Position = new Vector3(-539.8182f, 665.8551f, 145.2555f);
            cam.Rotation = new Vector3(-10.15267f, 0f, -69.7598f);
            cam.FieldOfView = 104.4001f;
            // show instruction
            bmsg.ShowOldMessage(rm.GetString("suburban_intro_2"), 10000);
            Wait(10000);

            // play sound
            Audio.PlaySoundFrontend("SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET");
            // change perspective
            cam.Position = new Vector3(-478.3805f, 654.7722f, 144.4642f);
            cam.Rotation = new Vector3(-12.35267f, -1.280661E-06f, 83.03968f);
            cam.FieldOfView = 61.19999f;
            // show instruction
            bmsg.ShowOldMessage(rm.GetString("suburban_intro_3"), 10000);
            Wait(10000);

            // switch back to main cam
            Function.Call(Hash.RENDER_SCRIPT_CAMS, 0, 1, cam, 0, 0);
            Game.Player.Character.IsInvincible = false;
            Game.Player.CanControlCharacter = true;
        }

        public void startRace()
        {
            // try and free terrain loading restriction, so car won't fall through the ground
            Function.Call(Hash.CLEAR_HD_AREA);
            Function.Call(Hash.CLEAR_FOCUS);

            UI.ShowSubtitle(rm.GetString("task_started"), 3000);

            Game.Player.Character.CurrentVehicle.NumberPlate = "RACE 3";

            raceStartTime = Game.GameTime;

            Game.Player.CanControlCharacter = true;
        }

        public bool checkRaceStartCondition()
        {
            // check which car player is using
            return (Game.Player.Character.IsInVehicle() && Game.Player.Character.CurrentVehicle.Equals(raceVehicle));
        }

        public Dictionary<string, Dictionary<string, double>> getCollectedData()
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        private void sendAwayOtherCars() {
            foreach (KeyValuePair<Vector3, float> kvp in exclusionZones)
            {
                foreach (Vehicle car in World.GetNearbyVehicles(kvp.Key, kvp.Value))
                {
                    if (!car.Equals(obstacle) 
                        && !car.Equals(raceVehicle)
                        && !knownCars.Contains(car))
                    {
                        //car.Delete();
                        var driver = car.GetPedOnSeat(VehicleSeat.Driver);
                        driver.Task.DriveTo(
                            car,
                            sendCarsTo,
                            5,
                            35
                        );
                        knownCars.Add(car);
                    }
                }
            }
        }
    }
}
