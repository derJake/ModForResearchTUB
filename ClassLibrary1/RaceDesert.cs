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
    class RaceDesert : Script, RaceInterface
    {
        private Tuple<Vector3, Vector3?>[] checkpoints;

        private int raceStartTime;

        private Vehicle raceVehicle;

        private Vector3 car_selection = new Vector3(2770.514f, 3461.25f, 55.6087f);
        private Vector3 car1_spawnpoint = new Vector3(2768.046f, 3456.5f, 55.2822f);
        private float car_spawn_heading = 245.6047f;
        private float car_spawn_player_heading = 165.074f;

        ResourceManager rm;
        Utilities ut;

        public String canonicalName { get; private set; }

        public RaceDesert(ResourceManager resman, Utilities utils, String taskKey) {
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
                new Tuple<Vector3, Vector3?>(new Vector3(2755.279f, 3407.343f, 55.81804f), new Vector3(2725.262f, 3430.425f, 55.95811f)),
                new Tuple<Vector3, Vector3?>(new Vector3(2751.237f, 3346.253f, 55.65918f), new Vector3(2697.167f, 3440.954f, 55.3436f)),
                new Tuple<Vector3, Vector3?>(new Vector3(2647.175f, 3160.671f, 50.6989f), new Vector3(2620.347f, 3371.274f, 55.73833f)),
                new Tuple<Vector3, Vector3?>(new Vector3(2528.565f, 3041.744f, 42.4942f), new Vector3(2526.048f, 3364.326f, 51.30487f)),
                new Tuple<Vector3, Vector3?>(new Vector3(2404.581f, 2977.644f, 47.01064f), new Vector3(2463.789f, 3428.886f, 49.59436f)),
                new Tuple<Vector3, Vector3?>(new Vector3(2372.07f, 2967.281f, 48.5093f), new Vector3(2458.048f, 3492.147f, 53.05719f)),
                new Tuple<Vector3, Vector3?>(new Vector3(2271.941f, 3006.352f, 45.28311f), new Vector3(2409.665f, 3493.542f, 62.36658f)),
                new Tuple<Vector3, Vector3?>(new Vector3(2184.78f, 3025.253f, 44.87322f), new Vector3(2397.047f, 3520.032f, 70.57656f)),
                new Tuple<Vector3, Vector3?>(new Vector3(2036.457f, 3086.014f, 46.40019f), new Vector3(2383.523f, 3530.394f, 70.05774f)),
                new Tuple<Vector3, Vector3?>(new Vector3(2006.884f, 3129.917f, 45.92114f), new Vector3(2347.859f, 3483.273f, 66.33865f)),
                new Tuple<Vector3, Vector3?>(new Vector3(2021.199f, 3164.91f, 45.05206f), new Vector3(2253.689f, 3433.113f, 63.33295f)),
                new Tuple<Vector3, Vector3?>(new Vector3(2075.637f, 3265.012f, 45.00402f), new Vector3(2243.594f, 3410.979f, 56.54399f)),
                new Tuple<Vector3, Vector3?>(new Vector3(2067.605f, 3295.515f, 45.18334f), new Vector3(2230.053f, 3334.511f, 45.39974f)),
                new Tuple<Vector3, Vector3?>(new Vector3(2047.698f, 3301.834f, 45.28672f), new Vector3(2180.349f, 3410.39f, 45.09612f)),
                new Tuple<Vector3, Vector3?>(new Vector3(2040.097f, 3319.735f, 45.19153f), new Vector3(2129.232f, 3432.738f, 47.37669f)),
                new Tuple<Vector3, Vector3?>(new Vector3(2058.455f, 3426.639f, 43.82508f), new Vector3(2067.3f, 3439.093f, 43.41712f)),
                //new Tuple<Vector3, Vector3?>(new Vector3(2039.139f, 3321.247f, 45.25034f), new Vector3(3212.786f, 3462.548f, 63.5235f)),
                new Tuple<Vector3, Vector3?>(new Vector3(2039.588f, 3454.929f, 43.42058f), null),
            };

            this.checkpoints = checkpointlist;

            rm = resman;
            ut = utils;
        }

        public void finishRace()
        {
            UI.ShowSubtitle(String.Format(rm.GetString("race_finished"), (Game.GameTime - raceStartTime) / 1000), 3000);
            UI.Notify(String.Format(rm.GetString("race_finished"), (Game.GameTime - raceStartTime) / 1000));

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
            World.RenderingCamera = World.CreateCamera(new Vector3(2047.247f, 3454.969f, 44.53693f), new Vector3(-1.169715f, -1.334299f, 83.22903f), 90f);
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
            var bmsg = BigMessageThread.MessageInstance;
            Logger.Log(rm.GetString("desert_initialization"));
            UI.Notify(rm.GetString("desert_initialization"));
            UI.ShowSubtitle(rm.GetString("desert_initialization"), 1250);

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
            Function.Call(Hash.SET_WEATHER_TYPE_NOW_PERSIST, "FOGGY");

            Ped player = Game.Player.Character;
            player.Task.ClearAllImmediately(); // give back control to player

            // teleport player and turn him towards cars
            Game.Player.Character.Position = car_selection;
            Game.Player.Character.Heading = car_spawn_player_heading;

            // load the vehicle model
            raceVehicle = ut.createCarAt(VehicleHash.Sanchez, car1_spawnpoint, car_spawn_heading);

            // while we're showing what's to come, we don't want the player hurt
            Game.Player.Character.IsInvincible = true;
            Game.Player.CanControlCharacter = false;

            // create a camera to look through
            Camera cam = World.CreateCamera(
                Game.Player.Character.Position, // position
                new Vector3(9f, 0f, -82.57458f), // rotation
                90f
            );

            cam.PointAt(raceVehicle);

            Game.Player.Character.Task.EnterVehicle(raceVehicle, VehicleSeat.Driver);

            // switch to this camera
            Function.Call(Hash.RENDER_SCRIPT_CAMS, 1, 0, cam, 0, 0);
            // play sound


            Audio.PlaySoundFrontend("SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET");

            cam.StopPointing();
            // show player getting onto bike
            cam.Position = new Vector3(2768.997f, 3452.713f, 55.69416f);
            cam.Rotation = new Vector3(0.6459216f, 0, 2.159997f);
            cam.FieldOfView = 48.4f;

            bmsg.ShowOldMessage(rm.GetString("desert_intro_1"), 10000);
            Wait(10000);
            Game.Player.Character.Task.LookAt(car1_spawnpoint, 2500);

            // show wide view of You Tool market and highway
            cam.Position = new Vector3(2779.671f, 3403.094f, 67.52917f);
            cam.Rotation = new Vector3(-27.8562f, 8.537737E-07f, 28.48598f);
	        cam.FieldOfView = 102.0001f;

            bmsg.ShowOldMessage(rm.GetString("desert_intro_2"), 10000);
            Wait(10000);

            // show highway and exit
            cam.Position = new Vector3(2558.172f, 3054.789f, 49.60252f);
	        cam.Rotation = new Vector3(-14.16963f, 0f, 114.9396f);
	        cam.FieldOfView = 51.6f;

            bmsg.ShowOldMessage(rm.GetString("desert_intro_3"), 10000);
            Wait(10000);

            // show desert route
            cam.Position = new Vector3(2585.001f, 3338.31f, 68.397f);
            cam.Rotation = new Vector3(-14.9001f, 0f, 53.79928f);
            cam.FieldOfView = 67.6f;

            // create red alt checkpoint markers
            int[] alt_checkpoint_markers = new int[checkpoints.Length - 1];
            for (int i = 0; i < checkpoints.Length - 1; i++) {
                if (checkpoints[i].Item2.HasValue) {
                    alt_checkpoint_markers[i] = Function.Call<int>(Hash.CREATE_CHECKPOINT,
                        2, // type
                        checkpoints[i].Item2.Value.X,
                        checkpoints[i].Item2.Value.Y,
                        checkpoints[i].Item2.Value.Z,
                        0, // facing next checkpoint?
                        0,
                        0,
                        5.0f,    // radius
                        255,    // R
                        0,     // G
                        0,        // B
                        100,    // Alpha
                        0 // number displayed in marker, if type is 42-44
                        );
                }
            }

            bmsg.ShowOldMessage(rm.GetString("desert_intro_4"), 10000);
            Wait(10000);

            cam.Position = new Vector3(2399.078f, 3552.71f, 77.98219f);
            cam.Rotation = new Vector3(-14.10008f, 0f, 151.3984f);
            cam.FieldOfView = 47.6f;

            bmsg.ShowOldMessage(rm.GetString("desert_intro_5"), 10000);
            Wait(10000);

            // delete checkpoint markers again
            foreach (int handle in alt_checkpoint_markers) {
                Function.Call(Hash.DELETE_CHECKPOINT, handle);
            }

            // switch back to main cam
            Function.Call(Hash.RENDER_SCRIPT_CAMS, 0, 1, cam, 0, 0);
            Game.Player.Character.IsInvincible = false;
            Game.Player.CanControlCharacter = true;

        }
        public List<Tuple<String, List<Tuple<String, double>>>> getCollectedData()
        {
            throw new NotImplementedException();
        }

        public void startRace()
        {
            // try and free terrain loading restriction, so car won't fall through the ground
            Function.Call(Hash.CLEAR_HD_AREA);
            Function.Call(Hash.CLEAR_FOCUS);

            UI.ShowSubtitle(rm.GetString("task_started"), 3000);

            Game.Player.Character.CurrentVehicle.NumberPlate = "RACE 2";

            raceStartTime = Game.GameTime;
        }

        public bool checkRaceStartCondition()
        {
            // check which car player is using
            return (Game.Player.Character.IsInVehicle() && Game.Player.Character.CurrentVehicle.Equals(raceVehicle));
        }

        public bool checkAlternativeBreakCondition()
        {
            return false;
        }

        public String getCanonicalName()
        {
            return canonicalName;
        }
    }
}
