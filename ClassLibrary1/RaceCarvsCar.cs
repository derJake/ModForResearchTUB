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
    class RaceCarvsCar : Script, RaceInterface
    {
        private Tuple<Vector3, Vector3?>[] checkpoints;
        private bool playerInRaceCar = false;

        private int raceStartTime;

        private String car_taken;

        private Vehicle[] vehicles = new Vehicle[2];

        private Vector3 car_selection = new Vector3(-793.8867f, -2441.894f, 14.57072f);
        private Vector3 car1_spawnpoint = new Vector3(-793.4608f, -2435.887f, 14.27493f);
        private Vector3 car2_spawnpoint = new Vector3(-790.8688f, -2438.188f, 14.27507f);
        private float car_spawn_heading = 148.0f;
        private float car_spawn_player_heading = 327.0612f;
        private Vector3 race1Start = new Vector3(-1015.348f, -2715.956f, 12.58948f);
        private Vector3 race1End = new Vector3(-45.45972f, -784.222f, 44.34782f);

        private int initCalled = 0;
        private bool flashWantedStopped = false;

        ResourceManager rm;
        Utilities ut;

        public String canonicalName { get; private set; }

        public RaceCarvsCar(ResourceManager resman, Utilities utils, String taskKey) {
            this.canonicalName = taskKey;

            // try and load this area already
            Function.Call(Hash.SET_HD_AREA,
                car1_spawnpoint.X,
                car1_spawnpoint.Y,
                car1_spawnpoint.Z,
                50f
            );

            // add some checkpoints for our race
            Tuple<Vector3, Vector3?>[] checkpointlist = {
                new Tuple<Vector3, Vector3?>(new Vector3(-807.8585f, -2466.344f, 14.45607f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-810.6682f, -2249.965f, 17.24915f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-693.1211f, -2117.945f, 13.12339f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-382.7377f, -1838.06f, 21.37794f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-246.3216f, -1826.909f, 28.96538f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-144.3558f, -1749.146f, 30.12419f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-44.43723f, -1630.049f, 28.96328f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(64.23303f, -1515.456f, 28.93484f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(137.0829f, -1376.805f, 29.34775f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(117.2407f, -1356.73f, 28.88704f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(64.65392f, -1285.516f, 29.33747f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(64.58669f, -1160.968f, 28.951f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(96.58853f, -1026.16f, 29.03582f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(82.72692f, -982.808f, 29.01929f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-11.1925f, -931.7217f, 28.90791f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(32.22876f, -773.1832f, 43.85289f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-65.03999f, -725.1259f, 43.86914f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-76.04148f, -749.7919f, 43.77972f), null),
                new Tuple<Vector3, Vector3?>(race1End, null)
            };

            this.checkpoints = checkpointlist;

            rm = resman;
            ut = utils;
        }

        public void finishRace()
        {
            // log which car was taken
            Logger.Log(car_taken);

            UI.ShowSubtitle(String.Format(rm.GetString("race_finished"), (Game.GameTime - raceStartTime) / 1000), 3000);
            UI.Notify(String.Format(rm.GetString("race_finished"), (Game.GameTime - raceStartTime) / 1000));

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

            foreach (Vehicle car in vehicles) {
                car.MarkAsNoLongerNeeded();
                car.Delete();
            }

            vehicles = new Vehicle[2];
        }

        public Tuple<Vector3, Vector3?>[] getCheckpoints()
        {
            return checkpoints;
        }

        public void handleOnTick(object sender, EventArgs e)
        {
            if (!flashWantedStopped && Game.GameTime > raceStartTime + 10000) {
                Function.Call(Hash.FLASH_WANTED_DISPLAY, false);
                flashWantedStopped = true;
            }
        }

        public void initRace()
        {
            var bmsg = BigMessageThread.MessageInstance;
            initCalled++;
            UI.ShowSubtitle(rm.GetString("carvscar_initialization"), 1250);
            UI.Notify(rm.GetString("carvscar_initialization"));
            Logger.Log(rm.GetString("carvscar_initialization"));

            // try to clear parking lot where cars are spawned
            // TODO: check, if the boolean parameters have been documented
            Function.Call(Hash.CLEAR_ANGLED_AREA_OF_VEHICLES,
                car1_spawnpoint.X,
                car1_spawnpoint.Y,
                car1_spawnpoint.Z,
                car2_spawnpoint.X,
                car2_spawnpoint.Y,
                car2_spawnpoint.Z,
                false,
                false,
                false,
                false,
                false
            );

            // maybe this will do?
            Function.Call(Hash.CLEAR_AREA_OF_VEHICLES, 0, 0, 0, 10000, false, false, false, false, false);

            // try to load detailed terrain
            Function.Call(Hash._SET_FOCUS_AREA,
                car1_spawnpoint.X,
                car1_spawnpoint.Y,
                car1_spawnpoint.Z,
                car2_spawnpoint.X,
                car2_spawnpoint.Y,
                car2_spawnpoint.Z
             );

            // set time of day
            World.CurrentDayTime = new TimeSpan(8, 30, 0);

            // set weather to rain
            Function.Call(Hash.SET_WEATHER_TYPE_NOW_PERSIST, "RAIN");

            Ped player = Game.Player.Character;
            player.Task.ClearAllImmediately(); // give back control to player

            // teleport player and turn him towards cars
            Game.Player.Character.Position = car_selection;
            Game.Player.Character.Heading = car_spawn_player_heading;

            // create the two cars
            Vehicle vehicle1 = ut.createCarAt(VehicleHash.Buffalo, car2_spawnpoint, car_spawn_heading),
                vehicle2 = ut.createCarAt(VehicleHash.RapidGT, car1_spawnpoint, car_spawn_heading);
            vehicles[0] = vehicle1;
            vehicles[1] = vehicle2;

            // open driver side door for player
            Function.Call(Hash.SET_VEHICLE_DOOR_OPEN, vehicle1, 0, true, false);
            Function.Call(Hash.SET_VEHICLE_ENGINE_ON, vehicle1, true, false, false);

            // make fast vehicle locked, but able to break and enter
            Function.Call(Hash.SET_VEHICLE_DOORS_LOCKED, vehicle2, 4);
            Function.Call(Hash.SET_VEHICLE_NEEDS_TO_BE_HOTWIRED, vehicle2, true);
            vehicle2.HasAlarm = true;

            // make the fast one colorful, the other one white
            Function.Call(Hash.SET_VEHICLE_CUSTOM_PRIMARY_COLOUR, vehicle1, 255, 255, 255);
            Function.Call(Hash.SET_VEHICLE_CUSTOM_SECONDARY_COLOUR, vehicle1, 255, 255, 255);

            Function.Call(Hash.SET_VEHICLE_CUSTOM_PRIMARY_COLOUR, vehicle2, 255, 0, 0);
            Function.Call(Hash.SET_VEHICLE_CUSTOM_SECONDARY_COLOUR, vehicle2, 255, 50, 0);

            Function.Call(Hash.SET_VEHICLE_INTERIORLIGHT, vehicle1, true);

            // intro time
            Game.Player.CanControlCharacter = false;
            Game.Player.Character.IsInvincible = true;

            // create a camera to look through
            Camera cam = World.CreateCamera(
                new Vector3(-800.2387f, -2441.272f, 14.24846f),
                new Vector3(7.068823f, -2.134434E-07f, -71.44397f),
                44.40001f
            );

            // switch to this camera
            Function.Call(Hash.RENDER_SCRIPT_CAMS, 1, 0, cam, 0, 0);

            // play sound
            Audio.PlaySoundFrontend("SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET");
            // show instruction
            bmsg.ShowOldMessage(rm.GetString("carvscar_intro_1"), 10000);
            Wait(10000);

            // play sound
            Audio.PlaySoundFrontend("SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET");
            // change perspective
            cam.Position = new Vector3(-793.6436f, -2441.077f, 14.8099f);
            cam.Rotation = new Vector3(-7.331206f, -2.027712E-06f, 4.556251f);
            cam.FieldOfView = 37.20001f;
            // show instruction
            bmsg.ShowOldMessage(rm.GetString("carvscar_intro_2"), 10000);
            Wait(10000);

            // play sound
            Audio.PlaySoundFrontend("SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET");
            // change perspective
            cam.Position = new Vector3(-794.2858f, -2433.135f, 14.49894f);
            cam.Rotation = new Vector3(2.26882f, 2.192797E-06f, -179.4448f);
            cam.FieldOfView = 14.80003f;
            // show instruction
            bmsg.ShowOldMessage(rm.GetString("carvscar_intro_3"), 10000);
            Wait(10000);

            // play sound
            Audio.PlaySoundFrontend("SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET");
            // change perspective
            cam.Position = new Vector3(-797.787f, -2436.968f, 14.07312f);
            cam.Rotation = new Vector3(2.26882f, 9.604953E-07f, -94.64352f);
            cam.FieldOfView = 37.20001f;
            // show instruction
            bmsg.ShowOldMessage(rm.GetString("carvscar_intro_4"), 10000);
            Wait(10000);

            // play sound
            Audio.PlaySoundFrontend("SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET");
            // change perspective
            cam.Position = new Vector3(-788.4284f, -2438.03f, 14.66808f);
            cam.Rotation = new Vector3(-9.091204f, 0f, 108.9772f);
            cam.FieldOfView = 42.80001f;
            // show instruction
            bmsg.ShowOldMessage(rm.GetString("carvscar_intro_5"), 10000);
            Wait(10000);

            // play sound
            Audio.PlaySoundFrontend("SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET");
            // change perspective
            cam.Position = new Vector3(-795.6731f, -2442.41f, 13.71518f);
            cam.Rotation = new Vector3(9.468822f, 4.268868E-07f, -61.84291f);
            cam.FieldOfView = 34.00002f;
            // show instruction
            bmsg.ShowOldMessage(rm.GetString("carvscar_intro_6"), 10000);
            Wait(10000);

            // switch back to main cam
            Function.Call(Hash.RENDER_SCRIPT_CAMS, 0, 1, cam, 0, 0);

            Game.Player.CanControlCharacter = true;
            Game.Player.Character.IsInvincible = false;
        }

        public List<Tuple<String, List<Tuple<String, double>>>> getCollectedData()
        {
            throw new NotImplementedException();
        }

        public void startRace()
        {
            Logger.Log("RaceCarvsCar.startRace()");
            // try and free terrain loading restriction, so car won't fall through the ground
            Function.Call(Hash.CLEAR_HD_AREA);
            Function.Call(Hash.CLEAR_FOCUS);

            UI.ShowSubtitle(rm.GetString("task_started"), 3000);

            Game.Player.Character.CurrentVehicle.NumberPlate = "RACE 1";

            raceStartTime = Game.GameTime;
        }

        public bool checkRaceStartCondition()
        {
            if (Game.Player.Character.IsInVehicle() &&
                vehicles.Length == 2) {
                // check which car player is using
                if (vehicles[1] != null && Game.Player.Character.CurrentVehicle.Equals(vehicles[1]))
                {
                    Function.Call(Hash.SET_PLAYER_WANTED_LEVEL, Game.Player, 3, false);
                    Function.Call(Hash.SET_PLAYER_WANTED_LEVEL_NOW, Game.Player, false);
                    vehicles[1].StartAlarm();
                    car_taken = "Player is in fast car";
                    playerInRaceCar = true;
                    Function.Call(Hash.FLASH_WANTED_DISPLAY, true);
                }
                if (vehicles[0] != null && Game.Player.Character.CurrentVehicle.Equals(vehicles[0]))
                {
                    playerInRaceCar = true;
                    car_taken = "Player is in car with good traction";
                }
            }
            return playerInRaceCar;
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
