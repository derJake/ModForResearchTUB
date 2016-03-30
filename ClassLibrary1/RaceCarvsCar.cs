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
    class RaceCarvsCar : Script, RaceInterface
    {
        private Tuple<Vector3, Nullable<Vector3>>[] checkpoints;
        private int currentCheckpoint;
        private bool playerInRaceCar = false;

        private int raceStartTime;

        private String car_taken;

        private List<Vehicle> vehicles = new List<Vehicle>();

        private Vector3 car_selection = new Vector3(-786.5052f, -2429.885f, 14.57072f);
        private Vector3 car1_spawnpoint = new Vector3(-789.7347f, -2428.485f, 14.57072f);
        private Vector3 car2_spawnpoint = new Vector3(-795.5708f, -2425.815f, 14.57072f);
        private float car_spawn_heading = 147.0f;
        private float car_spawn_player_heading = 48.0f;
        private Vector3 race1Start = new Vector3(-1015.348f, -2715.956f, 12.58948f);
        private Vector3 race1End = new Vector3(-45.45972f, -784.222f, 44.34782f);

        private int initCalled = 0;

        public RaceCarvsCar() {
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
            // log which car was taken
            Logger.Log(car_taken);

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

            vehicles = new List<Vehicle>();
        }

        public Tuple<Vector3, Nullable<Vector3>>[] getCheckpoints()
        {
            return checkpoints;
        }

        public void handleOnTick()
        {
            throw new NotImplementedException();
        }

        public void initRace()
        {
            initCalled++;
            UI.ShowSubtitle("initializing first race", 1250);
            UI.Notify(String.Format("Car vs Car {0}", initCalled));

            // try to clear parking lot where cars are spawned
            // TO DO: check, if the boolean parameters have been documented
            // TO DO: spawn cars on the other side of the curb, to avoid false positives
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

            // add some checkpoints for our race
            Tuple<Vector3, Nullable<Vector3>>[] checkpointlist = {
                new Tuple<Vector3, Nullable<Vector3>>(new Vector3(-807.8585f, -2466.344f, 14.45607f), null),
                new Tuple<Vector3, Nullable<Vector3>>(new Vector3(-810.6682f, -2249.965f, 17.24915f), null),
                new Tuple<Vector3, Nullable<Vector3>>(new Vector3(-693.1211f, -2117.945f, 13.12339f), null),
                new Tuple<Vector3, Nullable<Vector3>>(new Vector3(-382.7377f, -1838.06f, 21.37794f), null),
                new Tuple<Vector3, Nullable<Vector3>>(new Vector3(-246.3216f, -1826.909f, 28.96538f), null),
                new Tuple<Vector3, Nullable<Vector3>>(new Vector3(-144.3558f, -1749.146f, 30.12419f), null),
                new Tuple<Vector3, Nullable<Vector3>>(new Vector3(-44.43723f, -1630.049f, 28.96328f), null),
                new Tuple<Vector3, Nullable<Vector3>>(new Vector3(64.23303f, -1515.456f, 28.93484f), null),
                new Tuple<Vector3, Nullable<Vector3>>(new Vector3(138.5995f, -1370.135f, 28.83117f), null),
                new Tuple<Vector3, Nullable<Vector3>>(new Vector3(117.2407f, -1356.73f, 28.88704f), null),
                new Tuple<Vector3, Nullable<Vector3>>(new Vector3(64.65392f, -1285.516f, 29.33747f), null),
                new Tuple<Vector3, Nullable<Vector3>>(new Vector3(64.58669f, -1160.968f, 28.951f), null),
                new Tuple<Vector3, Nullable<Vector3>>(new Vector3(96.58853f, -1026.16f, 29.03582f), null),
                new Tuple<Vector3, Nullable<Vector3>>(new Vector3(82.72692f, -982.808f, 29.01929f), null),
                new Tuple<Vector3, Nullable<Vector3>>(new Vector3(-11.1925f, -931.7217f, 28.90791f), null),
                new Tuple<Vector3, Nullable<Vector3>>(new Vector3(32.22876f, -773.1832f, 43.85289f), null),
                new Tuple<Vector3, Nullable<Vector3>>(new Vector3(-65.03999f, -725.1259f, 43.86914f), null),
                new Tuple<Vector3, Nullable<Vector3>>(new Vector3(-76.04148f, -749.7919f, 43.77972f), null),
                new Tuple<Vector3, Nullable<Vector3>>(race1End, null)
            };
            currentCheckpoint = 0;

            this.checkpoints = checkpointlist;

            // load the two models
            var vehicle1Model = new Model(VehicleHash.Buffalo);
            var vehicle2Model = new Model(VehicleHash.RapidGT);
            vehicle1Model.Request(500);
            vehicle2Model.Request(500);

            if (vehicle1Model.IsInCdImage &&
                vehicle1Model.IsValid &&
                vehicle2Model.IsInCdImage &&
                vehicle2Model.IsValid
                )
            {
                // If the model isn't loaded, wait until it is
                while (!vehicle1Model.IsLoaded ||
                        !vehicle2Model.IsLoaded)
                    Script.Wait(100);

                // create the slower, reliable car
                var vehicle1 = World.CreateVehicle(VehicleHash.Buffalo, car1_spawnpoint, car_spawn_heading);
                // create the racecar
                var vehicle2 = World.CreateVehicle(VehicleHash.RapidGT, car2_spawnpoint, car_spawn_heading);
                vehicles.Add(vehicle1);
                vehicles.Add(vehicle2);

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

                // needs to be done onTick, me thinks
                /*
                Function.Call(
                    Hash.DRAW_SPOT_LIGHT,
                    car1_spawnpoint.X, // x
                    car1_spawnpoint.Y, // y
                    car1_spawnpoint.Z + 10f, // z
                    0f, // direction x
                    0f, // direction y
                    -10f, // direction z, make it point downwards
                    255, // R
                    255, // G
                    255, // B
                    100f, // distance
                    200f, // brightness
                    0.0f, // roundness
                    13f, // radius
                    1f // fadeout
                );*/
            }

            vehicle1Model.MarkAsNoLongerNeeded();
            vehicle2Model.MarkAsNoLongerNeeded();

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

            // switch to this camera
            Function.Call(Hash.RENDER_SCRIPT_CAMS, 1, 0, cam, 0, 0);
            // play sound
            Audio.PlaySoundFrontend("SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET");

            UI.ShowSubtitle("Normal car with good traction", 2500);
            Game.Player.Character.Task.LookAt(car2_spawnpoint, 2500);
            Wait(2500);

            // switch back to main cam
            Function.Call(Hash.RENDER_SCRIPT_CAMS, 0, 1, cam, 0, 0);

            UI.ShowSubtitle("Choose one to start the race!", 2500);
        }

        public void setCurrentCheckpoint(int index)
        {
            this.currentCheckpoint = index;
        }

        public void startRace()
        {
            // try and free terrain loading restriction, so car won't fall through the ground
            Function.Call(Hash.CLEAR_HD_AREA);
            Function.Call(Hash.CLEAR_FOCUS);

            UI.ShowSubtitle("Race started!", 1250);

            Game.Player.Character.CurrentVehicle.NumberPlate = "RACE 1";

            raceStartTime = Game.GameTime;
        }

        public bool checkRaceStartCondition()
        {
            if (Game.Player.Character.IsInVehicle() &&
                vehicles.Count == 2) {
                // check which car player is using
                if (Game.Player.Character.CurrentVehicle.Equals(vehicles[1]))
                {
                    Function.Call(Hash.SET_PLAYER_WANTED_LEVEL, Game.Player, 3, false);
                    Function.Call(Hash.SET_PLAYER_WANTED_LEVEL_NOW, Game.Player, false);
                    vehicles[1].StartAlarm();
                    car_taken = "Player is in fast car";
                    playerInRaceCar = true;
                }
                if (Game.Player.Character.CurrentVehicle.Equals(vehicles[0]))
                {
                    playerInRaceCar = true;
                    car_taken = "Player is in car with good traction";
                }
            }
            return playerInRaceCar;
        }
    }
}
