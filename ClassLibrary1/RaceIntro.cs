﻿using GTA;
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
using System.Windows.Forms;
using System.IO;

namespace ModForResearchTUB
{
    class RaceIntro : Script, RaceInterface
    {
        private Tuple<Vector3, Vector3?>[] checkpoints;

        private int raceStartTime;

        private Vehicle raceVehicle;

        private Vector3 car_selection = new Vector3(-41.13648f, 214.2873f, 105.5534f);
        private Vector3 car1_spawnpoint = new Vector3(-42.8537f, 212.8025f, 105.8433f);
        private float car_spawn_heading = 341.7267f;
        private float car_spawn_player_heading = 164.5883f;
        private Dictionary<String, Dictionary<String, double>> collectedData;
        private Dictionary<string, float> singularValues;
        private VehicleHash vehicleHash = VehicleHash.Comet2;
        private int regularIntroSceneLength = 10000,
            charSelectionStartedAt = 0;

        private TimerBarPool barPool = new TimerBarPool();
        private TextTimerBar textbar;
        private int introPlayTime = 600,
            selectedCharacter = 0,
            lastCharSelectInput = 0;
        private bool introActive = true,
            charSelectionActive = false,
            charSelectionConfirmed = false,
            charSelected = false,
            hintShown = false,
            showingMichael = false,
            showingFranklin = false,
            showingTrevor = false;
        private int raceEndTime;

        private List<Vehicle> cars;
        private List<Ped> peds;
        public CultureInfo CultureInfo { get; private set; }

        ResourceManager rm;
        Utilities ut;
        BigMessageHandler bmsg;

        public String canonicalName { get; private set; }

        private int[][] franklinProperties,
            trevorProperties,
            michaelProperties;

        public RaceIntro(ResourceManager resman, Utilities utils, String taskKey) {
            this.canonicalName = taskKey;

            // try and load this area already
            Function.Call(Hash.SET_HD_AREA,
                car1_spawnpoint.X,
                car1_spawnpoint.Y,
                car1_spawnpoint.Z,
                50f
            );

            // West Vinewood 
            Tuple<Vector3, Vector3?>[] checkpointlist = {
                new Tuple<Vector3, Vector3?>(new Vector3(-31.20458f, 215.13f, 105.5534f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(4.941189f, 203.2218f, 103.9356f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-3.047211f, 172.0848f, 96.66257f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-34.95789f, 85.41812f, 74.13844f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-57.20152f, -1.112961f, 70.13404f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-68.41341f, -47.59233f, 61.19105f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-97.84596f, -93.5451f, 56.68484f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-169.5122f, -68.69582f, 52.06757f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-303.1286f, -22.01921f, 47.65413f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-351.4349f, -5.117895f, 46.36396f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-428.3386f, 8.799047f, 45.27877f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-520.395f, 12.03447f, 43.53487f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-600.9536f, 5.027071f, 42.0069f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-680.8534f, -6.934255f, 37.51486f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-738.6771f, -33.88528f, 36.88163f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-815.42f, -73.8839f, 36.86637f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-860.4484f, -105.7367f, 36.95527f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-888.5425f, -140.534f, 37.32124f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-942.6324f, -168.0043f, 40.88167f), null),
            };
            // Rockford Hills

            this.checkpoints = checkpointlist;

            peds = new List<Ped>();
            cars = new List<Vehicle>();

            CultureInfo = CultureInfo.CurrentCulture;
            rm = resman;
            ut = utils;

            singularValues = new Dictionary<string, float>();

            initFranklinProperties();
            initTrevorProperties();
            initMichaelProperties();
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

            var player = Game.Player.Character;
            World.RenderingCamera = World.CreateCamera(
                new Vector3(-926.2764f, -164.5017f, 45.91834f),
                new Vector3(-5.055873f, -1.173939E-06f, 106.6668f),
                47.6f
            );

            World.RenderingCamera.InterpTo(
                World.CreateCamera(
                    new Vector3(-926.2764f, -164.5017f, 48.31835f),
                    new Vector3(-7.455873f, -1.067217E-06f, 106.6668f),
                    47.6f
                ),
                2750,
                true,
                true
                );

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

            barPool.Remove(textbar);
        }

        public Tuple<Vector3, Vector3?>[] getCheckpoints()
        {
            return checkpoints;
        }

        public void handleOnTick(object sender, EventArgs e)
        {
            if (raceStartTime > 0 && Game.GameTime < raceEndTime)
            {
                TimeSpan timeLeft = TimeSpan.FromMilliseconds(raceEndTime - Game.GameTime);
                textbar.Text = timeLeft.ToString(@"mm\:ss");
                barPool.Draw();
            }

            foreach (Vehicle otherCar in World.GetNearbyVehicles(checkpoints[checkpoints.Length - 1].Item1, 10)) {
                if (otherCar != raceVehicle) {
                    otherCar.Delete();
                }
            }

            if (!hintShown && Game.GameTime > raceStartTime + 10000) {
                hintShown = true;

                BigMessageHandler bmsg = BigMessageThread.MessageInstance;
                bmsg.ShowOldMessage(rm.GetString("intro25"), regularIntroSceneLength);
            }
        }

        public void initRace()
        {
            Logger.Log("Intro Track Initialization");
            UI.Notify("Intro Track Initialization");
            UI.ShowSubtitle("Intro Track Initialization", 1250);

            // while we're showing what's to come, we don't want the player hurt
            Game.Player.Character.IsInvincible = true;

            if (introActive)
            {
                doIntroSequence();
            }

            Game.Player.Character.IsInvincible = false;

            // set weather
            Function.Call(Hash.SET_WEATHER_TYPE_NOW_PERSIST, "EXTRASUNNY");

            // set time of day
            World.CurrentDayTime = new TimeSpan(19, 5, 0);

            Ped player = Game.Player.Character;
            player.Task.ClearAllImmediately(); // give back control to player

            foreach (Ped ped in World.GetNearbyPeds(car_selection, 10)) {
                ped.Delete();
            }

            // teleport player and turn him towards cars
            Game.Player.Character.Position = car_selection;
            Game.Player.Character.Heading = car_spawn_player_heading;

            foreach (Vehicle car in World.GetNearbyVehicles(car1_spawnpoint, 10)) {
                car.Delete();
            }

            raceVehicle = ut.createCarAt(vehicleHash, car1_spawnpoint, car_spawn_heading);

            // make player enter vehicle
            Game.Player.Character.SetIntoVehicle(raceVehicle, VehicleSeat.Driver);
            bmsg = BigMessageThread.MessageInstance;

            Game.Player.CanControlCharacter = false;
            player.IsInvincible = true;
            raceVehicle.IsInvincible = true;
            raceVehicle.HandbrakeOn = true;

            Camera cam = showVector(
                new Vector3(-36.50168f, 206.7234f, 107.3275f),
                new Vector3(-2.045077f, -2.668042E-07f, 103.3765f)
            );
            World.RenderingCamera.FieldOfView = 50;

            cam.InterpTo(
                World.CreateCamera(
                    new Vector3(-37.82938f, 214.4279f, 107.3489f),
                    new Vector3(-4.445077f, -2.134434E-07f, 104.1765f),
                    50f
                ),
                regularIntroSceneLength / 2,
                true,
                true
            );

            bmsg.ShowOldMessage(rm.GetString("intro21"), regularIntroSceneLength);
            Wait(regularIntroSceneLength);

            Camera cam2 = World.CreateCamera(
                new Vector3(-18.56182f, 241.6352f, 124.6819f),
                new Vector3(-19.64479f, 8.537736E-07f, 64.97605f),
                61.19999f
            );

            cam2.IsActive = true;
            World.RenderingCamera = cam2;

            cam2.InterpTo(
                World.CreateCamera(
                    new Vector3(-12.38881f, 257.8497f, 116.882f),
                    new Vector3(-10.04478f, -4.802477E-07f, 87.37595f),
                    61.19999f
                ),
                regularIntroSceneLength,
                true,
                true
            );

            bmsg.ShowOldMessage(rm.GetString("intro22"), regularIntroSceneLength);
            Wait(regularIntroSceneLength);

            Vector3 checkpoint_position = new Vector3(7.46516f, 202.9962f, 104.0948f);

            int mock_checkpoint = Function.Call<int>(Hash.CREATE_CHECKPOINT,
                2, // type
                checkpoint_position.X,
                checkpoint_position.Y,
                checkpoint_position.Z,
                0, // facing next checkpoint?
                0,
                0,
                5.0f,    // radius
                255,    // R
                155,     // G
                0,        // B
                100,    // Alpha
                0 // number displayed in marker, if type is 42-44
                );

            Camera cam3 = World.CreateCamera(
                new Vector3(18.69672f, 214.1314f, 114.4572f),
                new Vector3(-17.24473f, -8.537736E-07f, 133.775f),
                61.19999f
            );

            cam3.IsActive = true;
            World.RenderingCamera = cam3;

            cam3.InterpTo(
                World.CreateCamera(
                    new Vector3(21.32537f, 210.6287f, 110.8572f),
                    new Vector3(-10.04471f, -1.707547E-06f, 121.7748f),
                    61.19999f
                ),
                regularIntroSceneLength,
                true,
                true
            );

            bmsg.ShowOldMessage(rm.GetString("intro23"), regularIntroSceneLength);
            Wait(regularIntroSceneLength);

            // remove checkpoint graphic
            Function.Call(Hash.DELETE_CHECKPOINT, mock_checkpoint);

            Camera cam4 = World.CreateCamera(
                new Vector3(-33.90136f, 217.1371f, 107.0533f),
                new Vector3(3.019554f, 9.071346E-07f, 106.801f),
                78.80004f
            );

            bmsg.ShowOldMessage(rm.GetString("intro23_0"), regularIntroSceneLength);
            Wait(regularIntroSceneLength);

            charSelection();

            Game.Player.CanControlCharacter = true;
            player.IsInvincible = false;
            raceVehicle.HandbrakeOn = false;
            //raceVehicle.IsInvincible = false;
        }

        public void startRace()
        {
            // try and free terrain loading restriction, so car won't fall through the ground
            Function.Call(Hash.CLEAR_HD_AREA);
            Function.Call(Hash.CLEAR_FOCUS);

            UI.ShowSubtitle("Race started!", 1250);

            Game.Player.Character.CurrentVehicle.NumberPlate = "INTRO";

            raceStartTime = Game.GameTime;

            raceEndTime = raceStartTime + (introPlayTime * 1000);

            textbar = new TextTimerBar("TIME LEFT", "10:00");
            barPool.Add(textbar);
        }

        public bool checkRaceStartCondition()
        {
            // check which car player is using
            return (Game.Player.Character.IsInVehicle() && Game.Player.Character.CurrentVehicle.Equals(raceVehicle));
        }

        public Dictionary<String, Dictionary<String, double>> getCollectedData()
        {
            throw new NotImplementedException();
        }

        private void doIntroSequence() {
            var bmsg = BigMessageThread.MessageInstance;
            // set time of day
            World.CurrentDayTime = new TimeSpan(6, 35, 0);

            // set weather
            Function.Call(Hash.SET_WEATHER_TYPE_NOW_PERSIST, "CLEAR");

            Ped player = Game.Player.Character;
            player.Health += 100;
            player.Armor += 100;
            player.Task.ClearAllImmediately(); // give back control to player

            // while we're showing what's to come, we don't want the player hurt
            Game.Player.Character.IsInvincible = true;
            Game.Player.CanControlCharacter = false;
            player.Heading = 242.65f;

            player.Position = new Vector3(76.05127f, -1012.558f, 79.81089f);
            showVector(
                new Vector3(73.1081f, -1011.055f, 82.04148f), // camPos
                new Vector3(-15.138871f, 0, -106.3f) // camRot
            );

            bmsg.ShowOldMessage(rm.GetString("intro1"), regularIntroSceneLength);
            Wait(regularIntroSceneLength);

            // set time of day
            World.CurrentDayTime = new TimeSpan(18, 35, 0);

            // set weather
            Function.Call(Hash.SET_WEATHER_TYPE_NOW_PERSIST, "EXTRASUNNY");

            player.Position = new Vector3(106.7525f, -502.1675f, 43.36741f);
            showVector(
                new Vector3(111.2453f, -502.276f, 53.76136f), // camPos
                new Vector3(2.76f, 2.67f, 83.51f) // camRot
            );

            bmsg.ShowOldMessage(rm.GetString("intro2"), regularIntroSceneLength);
            Wait(regularIntroSceneLength);

            // show player peds

            World.CurrentDayTime = new TimeSpan(11, 45, 0);

            player.Position = new Vector3(-370.7453f, 1164.046f, 325.699f);
            player.Heading = 7.05f;

            showVector(
                new Vector3(-343f, 1151, 327f),
                new Vector3(-8.71f, 0, -179.58f)
            );

            World.RenderingCamera.FieldOfView = 70;
            List<Ped> characters = spawnCharacters();
            bmsg.ShowOldMessage(rm.GetString("intro3"), regularIntroSceneLength);
            Wait(regularIntroSceneLength);

            // hopefully show cars

            World.CurrentDayTime = new TimeSpan(10, 35, 0);

            player.Position = new Vector3(-378.4301f, 1226.474f, 325.7583f);
            showVector(
                new Vector3(-381, 1218, 326),
                new Vector3(0, 0, 140) // camRot
            );
            World.RenderingCamera.FieldOfView = 75;

            Vector3 potential_car = new Vector3(-383.5511f, 1215.216f, 325.2293f);

            // check if there is a parked car
            if (World.GetNearbyVehicles(potential_car, 3).Length == 0) {
                ut.createCarAt(VehicleHash.Adder, potential_car, 270.2285f);
            }

            bmsg.ShowOldMessage(rm.GetString("intro4"), regularIntroSceneLength);
            Wait(regularIntroSceneLength);

            // delete the additional character peds
            foreach (Ped charped in characters) {
                charped.Delete();
            }

            World.RenderingCamera.StopPointing();

            // show getting into car
            showVector(
                new Vector3(-398f, 1178f, 326.1006f),
                new Vector3(-1.56f, 0, -205.8116f)
            );
            //World.RenderingCamera.FieldOfView = 55;
            player.Heading = 155.9079f;

            Vehicle car = ut.createCarAt(
                VehicleHash.Adder,
                new Vector3(-403, 1174.109f, 325.23f),
                274
            );
            car.CustomPrimaryColor = Color.White;
            car.CustomSecondaryColor = Color.Black;

            //Game.Player.Character.Task.EnterVehicle(car, VehicleSeat.Driver, 10000, 1.0f, 1);
            Game.Player.Character.SetIntoVehicle(car, VehicleSeat.Driver);
            //World.RenderingCamera.PointAt(car);
            bmsg.ShowOldMessage(rm.GetString("intro5"), regularIntroSceneLength);

            // show driving a bit

            Vector3[] waypoints = {
                new Vector3(-348.0623f, 1168.467f, 324.8051f),
                new Vector3(-273.8922f, 1225.785f, 316.6845f),
                new Vector3(-199.8456f, 1307.512f, 304.0928f),
                new Vector3(-179.732f, 1385.757f, 293.7937f),
                new Vector3(-192.6232f, 1427.798f, 289.517f),
                new Vector3(-168.6804f, 1496.93f, 288.4995f)
            };

            Tuple<Vector3,Vector3>[] cameraPerspectives = {
                new Tuple<Vector3, Vector3>(new Vector3(-352, 1179, 326), new Vector3(-12, 0, -75.62788f)),
                new Tuple<Vector3, Vector3>(new Vector3(-324, 1200, 327), new Vector3(0, 0, 128.4084f)),
                new Tuple<Vector3, Vector3>(new Vector3(-263, 1276, 314), new Vector3(-6.111776f, 0, -145.4782f)),
                new Tuple<Vector3, Vector3>(new Vector3(-168.7183f, 1346.6f, 298.9369f), new Vector3(3.901602f, -0.003050533f, 129.2239f)),
                new Tuple<Vector3, Vector3>(new Vector3(-185.9702f, 1377.245f, 303.4766f), new Vector3(-28.59324f, 2.134434E-07f, -8.393929f)),
                new Tuple<Vector3, Vector3>(new Vector3(-199.6416f, 1477.424f, 290.3156f), new Vector3(-1.770226f, 8.004129E-08f, -173.8964f))
            };

            float radius = 5,
                radiustolerance = 2,
                speed = 30;

            World.RenderingCamera.PointAt(car);

            // have player drive through waypoints
            for (int i = 0; i < waypoints.Length; i++) {
                if (i == 3)
                {
                    bmsg.ShowOldMessage(rm.GetString("intro_5_1"), regularIntroSceneLength);
                }
                player.Task.DriveTo(car, waypoints[i], radius, speed);
                Function.Call(Hash.SET_DRIVE_TASK_DRIVING_STYLE, player, 1 << 9);
                World.RenderingCamera.Position = cameraPerspectives[i].Item1;
                World.RenderingCamera.Rotation = cameraPerspectives[i].Item2;

                // wait for player to drive to waypoint
                while (!player.IsInRangeOf(waypoints[i], radius + radiustolerance)) {
                    Wait(50);
                    // delete cars close to the last waypoint
                    foreach (Vehicle otherCar in World.GetNearbyVehicles(waypoints[waypoints.Length - 1], 10)) {
                        if (otherCar != car)
                        {
                            otherCar.Delete();
                        }
                    }
                }
            }

            car.HandbrakeOn = true;

            World.RenderingCamera.StopPointing();
            // point camera
            World.RenderingCamera.Position = new Vector3(-195.3602f, 1486.22f, 289.9316f);
            World.RenderingCamera.Rotation = new Vector3(1.860572f, 1.067217E-07f, -74.11179f);
            World.RenderingCamera.FieldOfView = 19.60003f;

            Vector3 checkpoint_position = new Vector3(-162.1843f, 1501.855f, 288.5133f);

            int mock_checkpoint = Function.Call<int>(Hash.CREATE_CHECKPOINT,
                2, // type
                checkpoint_position.X,
                checkpoint_position.Y,
                checkpoint_position.Z,
                0, // facing next checkpoint?
                0,
                0,
                5.0f,    // radius
                255,    // R
                155,     // G
                0,        // B
                100,    // Alpha
                0 // number displayed in marker, if type is 42-44
                );

            bmsg.ShowOldMessage(rm.GetString("intro6"), regularIntroSceneLength);
            Wait(regularIntroSceneLength);

            bmsg.ShowOldMessage(rm.GetString("intro7"), regularIntroSceneLength);
            Blip blip = World.CreateBlip(checkpoint_position);
            Function.Call(Hash._SET_RADAR_BIGMAP_ENABLED, 1, 0);
            Function.Call(Hash.SET_BLIP_ROUTE, blip, true);
            Function.Call(Hash.FLASH_MINIMAP_DISPLAY);
            Function.Call(Hash.PULSE_BLIP, blip);
            Wait(1000);
            Function.Call(Hash.FLASH_MINIMAP_DISPLAY);
            Wait(1000);
            Function.Call(Hash._SET_RADAR_BIGMAP_ENABLED, 0, 0);
            Function.Call(Hash.SET_BLIP_ROUTE, blip, true);
            Function.Call(Hash.PULSE_BLIP, blip);
            Wait(1000);
            Function.Call(Hash.FLASH_MINIMAP_DISPLAY);
            Wait(1000);
            Function.Call(Hash.FLASH_MINIMAP_DISPLAY);
            Wait(4000);
            blip.Remove();

            // remove checkpoint graphic
            Function.Call(Hash.DELETE_CHECKPOINT, mock_checkpoint);

            // deactivate scriptcam
            World.RenderingCamera = null;
            car.HandbrakeOn = false;

            // some more driving
            //Vector3 target = new Vector3(217.9269f, 1327.979f, 238.8758f); // next intersection

            Function.Call(Hash.SET_CINEMATIC_MODE_ACTIVE, true);

            Vector3 target = new Vector3(743.7599f, 1199.881f, 325.914f); // Hollywood Sign
            player.Task.DriveTo(car, target, radius, speed);
            Function.Call(Hash.SET_DRIVE_TASK_DRIVING_STYLE, player, 1 << 9);

            bmsg.ShowOldMessage(rm.GetString("intro7_1"), regularIntroSceneLength);
            Wait(regularIntroSceneLength);

            bmsg.ShowOldMessage(rm.GetString("intro7_2"), regularIntroSceneLength);
            Wait(regularIntroSceneLength);

            bmsg.ShowOldMessage(rm.GetString("intro7_3"), regularIntroSceneLength);
            Wait(regularIntroSceneLength);

            Function.Call(Hash.SET_CINEMATIC_MODE_ACTIVE, false);

            // show traffic light

            World.CurrentDayTime = new TimeSpan(7, 0, 0);

            player.Position = new Vector3(-130f, -1709.683f, 29.85f);

            // point camera
            var trafficLightCam = showVector(
                new Vector3(-120.2002f, -1728f, 32f),
                new Vector3(-4.43f, 0, -45f)
            );
            World.RenderingCamera.FieldOfView = 75;

            bmsg.ShowOldMessage(rm.GetString("intro8"), regularIntroSceneLength);

            trafficLightCam.InterpTo(
                World.CreateCamera(
                    new Vector3(-116.5719f, -1723.378f, 35.87222f),
                    new Vector3(-10.53937f, 3.415095E-06f, -38.5164f),
                    50.20222f
                ),
                Convert.ToInt32(regularIntroSceneLength * 0.75),
                true,
                true
            );

            // circle through traffic light states
            for (int i = 0; i < 7; i++)
            {
                foreach (Entity ent in World.GetNearbyEntities(new Vector3(-111, -1723, 29), 5))
                {
                    if (ent.Model.Hash == 862871082)
                    {
                        Function.Call(Hash.SET_ENTITY_TRAFFICLIGHT_OVERRIDE, ent, i%3);
                    }
                }
                Wait(1500);
            }

            //Wait(regularIntroSceneLength);

            // run over pedestrian

            //player.Position = new Vector3(-193.707f, -1673.128f, 33.59856f);

            foreach (Vehicle vehicle in World.GetNearbyVehicles(player, 50)) {
                vehicle.Delete();
            }

            Vehicle aggro_car = ut.createCarAt(VehicleHash.Buffalo, new Vector3(-65.35116f, -1671.119f, 28.92488f), 318.4243f);

            Game.Player.Character.SetIntoVehicle(aggro_car, VehicleSeat.Driver);

            bmsg.ShowOldMessage(rm.GetString("intro9"), regularIntroSceneLength);

            // point camera
            showVector(
                new Vector3(-56, -1675, 29f),
                new Vector3(6, 0, -10)
            );
            World.RenderingCamera.FieldOfView = 55;

            Vector3 poor_ped_position = new Vector3(-53.90526f, -1670.566f, 29.28915f);
            float poor_ped_heading = 72.20514f;
            Vector3 car_stop_position = new Vector3(-54f, -1669f, 28.84788f),
                poor_ped_flee_position = new Vector3(-18.6755f, -1637.57f, 29.3045f);

            var poor_ped = ut.createPedAt(PedHash.Abigail, poor_ped_position);
            poor_ped.Heading = poor_ped_heading;
            poor_ped.CanRagdoll = false;

            //Function.Call(Hash._0xE8A25867FBA3B05E, 0, 9, 1);
            //player.Task.ClearAll();

            player.Task.DriveTo(aggro_car, poor_ped_position, 2, 15, (int)DrivingStyle.Rushed);

            // wait for player to drive through ped's area
            //while (!aggro_car.IsInRangeOf(poor_ped_position, 3.5f))
            //{
            //    Wait(50);
            //}
            Wait(3000);

            aggro_car.HandbrakeOn = true;

            // have ped do an avoiding animation towards car
            var dict = "avoids";
            Function.Call(Hash.REQUEST_ANIM_DICT, dict);
            poor_ped.Task.PlayAnimation(dict, "frfront_toback", 2, 500000, false, 0);
            Wait(3000);
            poor_ped.Task.ClearAllImmediately();

            bmsg.ShowOldMessage(rm.GetString("intro10"), regularIntroSceneLength);
            poor_ped.Task.GoTo(poor_ped_flee_position);
            Wait(5000);

            // show police

            bmsg.ShowOldMessage(rm.GetString("intro11"), regularIntroSceneLength);

            Game.Player.WantedLevel = 2;

            int playerRGroup = player.RelationshipGroup;

            // spawn cop car and cops
            Vehicle cop_car = ut.createCarAt(VehicleHash.Police, new Vector3(-48.13033f, -1672.859f, 28.9749f), 61.59479f);
            List<Ped> police = new List<Ped>(2);
            Ped policeman_1 = ut.createPedAt(PedHash.Cop01SFY, new Vector3(-49.15336f, -1674.606f, 29.335f));
            Ped policeman_2 = ut.createPedAt(PedHash.Cop01SMY, new Vector3(-47.04166f, -1671.634f, 29.33792f));
            policeman_1.Heading = 44.67088f;
            policeman_2.Heading = 50.33441f;
            police.Add(policeman_1);
            police.Add(policeman_2);
            int copHash = Function.Call<int>(Hash.GET_HASH_KEY, "COP");

            // make police aggressive and shoot at player
            foreach (Ped cop in police) {
                cop.RelationshipGroup = copHash;
                cop.Weapons.Give(WeaponHash.CombatPistol, 2000, true, true);
                cop.Task.AimAt(player.Position, regularIntroSceneLength);
            }

            World.SetRelationshipBetweenGroups(Relationship.Hate, playerRGroup, copHash);

            player.IsInvincible = true;

            World.RenderingCamera.Position = new Vector3(-42, -1679, 30);
            World.RenderingCamera.Rotation = new Vector3(-0.8f, 0, 47);
            World.RenderingCamera.FieldOfView = 25;

            Wait(regularIntroSceneLength);

            var cam = showVector(
                new Vector3(-63, -1653, 30),
                new Vector3(-0.8f, 0, -149)
            );
            cam.FieldOfView = 25;

            bmsg.ShowOldMessage(rm.GetString("intro12"), regularIntroSceneLength);

            Function.Call(Hash.FLASH_WANTED_DISPLAY, true);

            Wait(regularIntroSceneLength / 2);

            bmsg.ShowOldMessage(rm.GetString("intro13"), regularIntroSceneLength);
            Wait(3000);

            Wait(regularIntroSceneLength / 2);

            Function.Call(Hash.FLASH_WANTED_DISPLAY, false);

            // remove wanted level and aggressive cop peds
            World.SetRelationshipBetweenGroups(Relationship.Neutral, playerRGroup, copHash);
            Game.Player.WantedLevel = 0;

            // remove cops
            foreach (Ped cop in police) {
                cop.Delete();
            }

            // remove cop car
            cop_car.Delete();

            cleanUpIntro();

            // remove vehicles in next area
            foreach (Vehicle random_car in World.GetNearbyVehicles(new Vector3(-80, -1579, 30), 50)) {
                random_car.Delete();
            }

            player.Position = new Vector3(-95.64503f, -1564.393f, 32.65067f);
            Vehicle crash_car = ut.createCarAt(VehicleHash.Premier, new Vector3(-73.7f, -1585.2f, 29.82f), 232f);
            // make player enter vehicle
            Game.Player.Character.SetIntoVehicle(crash_car, VehicleSeat.Driver);

            Vehicle car_rearended = ut.createCarAt(VehicleHash.Tailgater, new Vector3(-65.16207f, -1592.397f, 29.12429f), 234.1621f);

            Vector3 target_pos = new Vector3(-52.3836f, -1603.602f, 28.6389f);

            while (player.CurrentVehicle != crash_car) {
                Wait(100);
            }

            showVector(
                new Vector3(-63, -1585.5f, 31),
                new Vector3(1f, 0, 140)
            );
            World.RenderingCamera.FieldOfView = 65;

            player.CanBeDraggedOutOfVehicle = false;
            player.CanFlyThroughWindscreen = false;
            player.IsInvincible = true;

            Wait(4000);
            crash_car.HandbrakeOn = false;
            crash_car.ApplyForce(crash_car.ForwardVector * 250);

            bmsg.ShowOldMessage(rm.GetString("intro14"), regularIntroSceneLength);

            // wait for player to crash into parked car
            while (!car_rearended.HasBeenDamagedBy(crash_car)) {
                Wait(50);
                foreach (Vehicle random_car in World.GetNearbyVehicles(new Vector3(-80, -1579, 30), 50))
                {
                    if (random_car != car_rearended && random_car != crash_car)
                    {
                        random_car.Delete();
                    }
                }
            }

            Vector3 cam_pos = player.Position + player.ForwardVector * 5 - player.RightVector * 2 + new Vector3(0, 0, 1);
            World.RenderingCamera.Position = cam_pos;
            World.RenderingCamera.PointAt(player.CurrentVehicle);
            World.RenderingCamera.FieldOfView = 45;

            Wait(5000);

            bmsg.ShowOldMessage(rm.GetString("intro15"), regularIntroSceneLength);

            cam_pos = crash_car.Position + crash_car.ForwardVector * 5 + new Vector3(0, 0, 2);
            World.RenderingCamera.Position = cam_pos;
            World.RenderingCamera.PointAt(player.CurrentVehicle);

            Wait(3000);

            //player.CurrentVehicle.Explode();

            //Wait(regularIntroSceneLength);

            // player health scene
            World.RenderingCamera.StopPointing();
            player.Position = new Vector3(-68.60161f, -1690.505f, 29.20564f);
            player.Heading = 195.2348f;
            World.RenderingCamera.Position = new Vector3(-69.5f, -1695, 29);
            World.RenderingCamera.Rotation = new Vector3(-1, 0, -18);
            bmsg.ShowOldMessage(rm.GetString("intro16"), regularIntroSceneLength);
            Wait(2000);

            player.IsInvincible = true;

            Function.Call(Hash.SET_PED_TO_RAGDOLL, player, regularIntroSceneLength - 2000);
            Wait(regularIntroSceneLength);

            // driving sequence 2
            player.Position = new Vector3(1792, 3325, 41.5f);
            World.CurrentDayTime = new TimeSpan(8, 5, 0);

            Function.Call(Hash.CLEAR_AREA_OF_VEHICLES, 0, 0, 0, 1000, false, false, false, false, false);

            Vehicle desert_car = ut.createCarAt(VehicleHash.Surge, new Vector3(1791, 3324, 41), 180);

            bmsg.ShowOldMessage(rm.GetString("intro17"), regularIntroSceneLength);
            var timestamp = Game.GameTime;

            // make player enter vehicle
            //Game.Player.Character.Task.EnterVehicle(desert_car, VehicleSeat.Driver, 10000, 2.0f, 16);
            Game.Player.Character.SetIntoVehicle(desert_car, VehicleSeat.Driver);

            // show driving a bit
            Vector3[] waypoints_desert = {
                new Vector3(1844.38f, 3299.551f, 43.05259f),
                new Vector3(1930.464f, 3313.151f, 44.89776f),
                new Vector3(1986.8f, 3291.7f, 45.24f)
            };

            Tuple<Vector3, Vector3>[] camera_perspectives_desert = {
                new Tuple<Vector3, Vector3>(new Vector3(1796.121f, 3309.683f, 42.8127f), new Vector3(0, 0, -111.41f)),
                new Tuple<Vector3, Vector3>(new Vector3(1838.032f, 3299.171f, 43.84303f), new Vector3(0.6644f, 0, -89.36685f)),
                new Tuple<Vector3, Vector3>(new Vector3(1925.446f, 3316.135f, 46.16137f), new Vector3(-3.91f, 0, -120.3298f))
            };

            float desert_radius = 5,
                desert_speed = 30;

            World.RenderingCamera.FieldOfView = 50;
            World.RenderingCamera.PointAt(desert_car);

            while (player.CurrentVehicle != desert_car)
            {
                Wait(100);
            }

            // have player drive through waypoints
            for (int i = 0; i < waypoints_desert.Length; i++)
            {
                player.Task.DriveTo(desert_car, waypoints_desert[i], desert_radius, desert_speed, (int)DrivingStyle.Rushed);
                World.RenderingCamera.Position = camera_perspectives_desert[i].Item1;
                World.RenderingCamera.Rotation = camera_perspectives_desert[i].Item2;

                // wait for player to drive to waypoint
                while (!player.IsInRangeOf(waypoints_desert[i], desert_radius + radiustolerance))
                {
                    // remove vehicles to avoid waiting times at crossings
                    foreach (Vehicle vehicle in World.GetNearbyVehicles(desert_car.Position, 1000))
                    {
                        if (vehicle != desert_car)
                        {
                            vehicle.Delete();
                        }
                    }
                    Wait(50);

                    if (Game.GameTime > timestamp + regularIntroSceneLength) {
                        bmsg.ShowOldMessage(rm.GetString("intro17_1"), regularIntroSceneLength);
                    }
                }
            }

            // show no blinker

            bmsg.ShowOldMessage(rm.GetString("intro18"), regularIntroSceneLength);

            player.CurrentVehicle.Position = new Vector3(-1075, 444, 74);
            player.CurrentVehicle.Heading = 120;
            World.CurrentDayTime = new TimeSpan(7, 30, 0);
            World.RenderingCamera.Position = new Vector3(-1085f, 444f, 75.63884f);
            World.RenderingCamera.Rotation = new Vector3(-2.2232f, 0, -85f);

            // show driving a bit
            Vector3[] waypoints_urban = {
                new Vector3(-1076.348f, 446.7233f, 74.5505f),
                new Vector3(-1078.721f, 432.4045f, 72.68067f),
                new Vector3(-1075.836f, 414.0519f, 69.21885f),
                new Vector3(-1057.758f, 393.5327f, 68.75936f)
            };

            Tuple<Vector3, Vector3>[] camera_perspectives_urban = {
                new Tuple<Vector3, Vector3>(new Vector3(-1085f, 444f, 75.63884f), new Vector3(-2.2232f, 0, -85f)),
                new Tuple<Vector3, Vector3>(new Vector3(-1078.817f, 437.9136f, 75.37682f), new Vector3(-18.2789f, 0, -178.5177f)),
                new Tuple<Vector3, Vector3>(new Vector3(-1076, 403, 68.75f), new Vector3(2f, 0, -18.6f)),
                new Tuple<Vector3, Vector3>(new Vector3(-1062.851f, 394.5331f, 69.79096f), new Vector3(-1.448f, 0, -99.76324f))
            };

            float urban_radius = 5,
                urban_speed = 10f;

            World.RenderingCamera.FieldOfView = 50;

            // have player drive through waypoints
            for (int i = 0; i < waypoints_urban.Length; i++)
            {
                player.Task.DriveTo(desert_car, waypoints_urban[i], urban_radius, urban_speed);
                player.DrivingStyle = DrivingStyle.Normal;
                World.RenderingCamera.Position = camera_perspectives_urban[i].Item1;
                World.RenderingCamera.Rotation = camera_perspectives_urban[i].Item2;

                // wait for player to drive to waypoint
                while (!player.IsInRangeOf(waypoints_urban[i], urban_radius + radiustolerance))
                {
                    Wait(50);
                }
            }

            // show reversing

            // put player's car into another driveway

            World.RenderingCamera.Position = new Vector3(-985, 366, 73);
            World.RenderingCamera.Rotation = new Vector3(-6.3f, 0, 76.7f);

            desert_car.Heading = 199.2738f;
            desert_car.Position = new Vector3(-1043.221f, 385.1127f, 69.23925f);

            Ped reverse_driver = ut.createPedAt(PedHash.AfriAmer01AMM, new Vector3(-998.9263f, 373.3408f, 71.93969f));
            Vehicle reverse_car = ut.createCarAt(VehicleHash.Surge, new Vector3(-990.1054f, 366.7356f, 72.04808f), 237.6931f);
            reverse_driver.SetIntoVehicle(reverse_car, VehicleSeat.Driver);

            bmsg.ShowOldMessage(rm.GetString("intro19"), regularIntroSceneLength);

            Wait(3000);
            Vector3 destination = new Vector3(-1007, 368.8f, 71.8f);
            //player.Task.ParkVehicle(car, new Vector3(-1007, 368.8f, 71.8f), 311);
            Function.Call(Hash.TASK_VEHICLE_PARK, reverse_driver, reverse_car, -1007, 368.8f, 71.8f, 311, 2, 0, true);
            World.RenderingCamera.PointAt(reverse_car);

            //Function.Call(Hash.TASK_VEHICLE_GOTO_NAVMESH,
            //    reverse_driver,
            //    reverse_car,
            //    destination.X,
            //    destination.Y,
            //    destination.Z,
            //    10,
            //    1 << 11 | 1 << 10 | 1 << 8 | 1 << 16 | 1 << 32, // <==== use the bar to separate flags. This is a bitwise OR
            //    3
            //);

            Wait(regularIntroSceneLength);

            ut.cleanUp();

            // give control back and use regular camera
            World.RenderingCamera = null;
            World.DestroyAllCameras();
            Game.Player.Character.IsInvincible = false;
            Game.Player.CanControlCharacter = true;
        }

        private Camera showVector(Vector3 cameraPosition, Vector3 cameraRotation) {
            World.RenderingCamera = null;
            World.DestroyAllCameras();
            // create a camera to look through
            Camera cam = World.CreateCamera(
                cameraPosition, // position
                cameraRotation, // rotation
                90f // field of view
            );
            // switch to this camera
            Function.Call(Hash.RENDER_SCRIPT_CAMS, 1, 0, cam, 0, 0);
            return cam;
        }

        private void showEntity(Vector3 cameraPosition, Entity entityOfInterest) {
            World.RenderingCamera = null;
            World.DestroyAllCameras();
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

            List<Ped> characters = new List<Ped>(3);
            Ped michael = ut.createPedAt(PedHash.Michael, new Vector3(-341.388f, 1147.779f, 325.7267f));
            loadCharacterProperties(michael, michaelProperties);
            Ped franklin = ut.createPedAt(PedHash.Franklin, new Vector3(-343.1626f, 1147.788f, 325.7267f));
            loadCharacterProperties(franklin, franklinProperties);
            Ped trevor = ut.createPedAt(PedHash.Trevor, new Vector3(-345.1988f, 1147.625f, 325.7263f));
            loadCharacterProperties(trevor, trevorProperties);

            michael.Heading = 7.05f;
            franklin.Heading = 7.05f;
            trevor.Heading = 7.05f;

            characters.Add(michael);
            characters.Add(franklin);
            characters.Add(trevor);

            return characters;
        }

        private void cleanUpIntro()
        {
            foreach (Ped ped in peds)
            {
                ped.Delete();
            }

            foreach (Vehicle car in cars)
            {
                car.Delete();
            }
        }

        public bool checkAlternativeBreakCondition()
        {
            return raceEndTime > 0 && Game.GameTime > raceEndTime;
        }

        public String getCanonicalName() {
            return canonicalName;
        }

        public Dictionary<string, float> getSingularDataValues()
        {
            return singularValues;
        }

        public bool IntroActive
        {
            get
            {
                return introActive;
            }

            set
            {
                introActive = value;
            }
        }

        #region characterSelection

        private void charSelection() {
            peds = new List<Ped>(3);
            var heading = 105.6581f;

            Game.Player.Character.Position = new Vector3(-42.44312f, 208.1661f, 102.1461f);

            Ped michael = ut.createPedAt(PedHash.Michael, new Vector3(-38.70818f, 217.8385f, 106.5534f));
            Ped franklin = ut.createPedAt(PedHash.Franklin, new Vector3(-37.49139f, 215.4538f, 106.5535f));
            Ped trevor = ut.createPedAt(PedHash.Trevor, new Vector3(-36.45705f, 213.0283f, 106.5534f));

            loadCharacterProperties(michael, michaelProperties);
            loadCharacterProperties(franklin, franklinProperties);
            loadCharacterProperties(trevor, trevorProperties);

            peds.Add(michael);
            peds.Add(franklin);
            peds.Add(trevor);

            foreach (Ped ped in peds) {
                ped.IsInvincible = true;
                //ped.FreezePosition = true;
                ped.Heading = heading;
            }

            // show the peds
            int interpolationTime = 1500,
                showCharacterFor = 5000;

            Camera cam1 = World.CreateCamera(
                new Vector3(-42.87101f, 216.0611f, 106.7447f),
                new Vector3(-5.745561f, 1.28066E-06f, -68.76078f),
                31.60002f
            );

            Camera cam2 = World.CreateCamera(
                new Vector3(-41.86174f, 213.4643f, 106.9447f),
                new Vector3(-5.745561f, 1.28066E-06f, -68.76078f),
                31.60002f
            );

            Camera cam3 = World.CreateCamera(
                new Vector3(-41.06875f, 211.424f, 106.9447f),
                new Vector3(-5.745561f, 1.28066E-06f, -68.76078f),
                31.60002f
            );

            cam1.IsActive = true;
            World.RenderingCamera = cam1;

            //bmsg.ShowOldMessage("Michael", showCharacterFor);
            showingMichael = true;
            Wait(showCharacterFor);
            showingMichael = false;

            cam1.InterpTo(
                cam2,
                interpolationTime,
                true,
                true
            );

            Wait(interpolationTime);
            

            cam2.IsActive = true;
            World.RenderingCamera = cam2;

            showingFranklin = true;
            //bmsg.ShowOldMessage("Franklin", showCharacterFor);
            Wait(showCharacterFor);
            showingFranklin = false;

            cam2.InterpTo(
                cam3,
                interpolationTime,
                true,
                true
            );
            Wait(interpolationTime);

            cam3.IsActive = true;
            World.RenderingCamera = cam3;

            showingTrevor = true;
            //bmsg.ShowOldMessage("Trevor", showCharacterFor);
            Wait(showCharacterFor);
            showingTrevor = false;

            Camera cam = World.CreateCamera(
                new Vector3(-50.30263f, 210.1201f, 108.0545f),
                new Vector3(-4.945566f, 1.280661E-06f, -66.3608f),
                18.80003f
            );

            cam3.InterpTo(
                cam,
                interpolationTime,
                true,
                true
            );
            Wait(interpolationTime);

            cam.IsActive = true;
            World.RenderingCamera = cam;

            charSelectionActive = true;
            charSelectionStartedAt = Game.GameTime;
        }

        public void handleCharSelection(object sender, EventArgs e) {
            SizeF res = UIMenu.GetScreenResolutionMantainRatio();
            Point safe = UIMenu.GetSafezoneBounds();

            showCharacterName(res, safe);

            if (charSelectionActive)
            {
                Game.Player.CanControlCharacter = true;
                Game.Player.Character.FreezePosition = true;
                Game.Player.IsInvincible = true;
                var pos = peds[selectedCharacter].Position;

                World.DrawMarker(
                    MarkerType.VerticalCylinder,
                    pos - new Vector3(0, 0, 1),
                    new Vector3(),
                    new Vector3(),
                    new Vector3(2, 2, 1),
                    Color.Yellow
                );

                if (charSelectionConfirmed) {
                    World.DrawMarker(
                        MarkerType.VerticalCylinder,
                        pos - new Vector3(0, 0, 1),
                        new Vector3(),
                        new Vector3(),
                        new Vector3(1.5f, 1.5f, 1.5f),
                        Color.Aqua
                    );
                }

                new UIResText(
                    rm.GetString("intro23_2"),
                    new Point((Convert.ToInt32(res.Width) / 2 - 200), 75),
                    0.5f,
                    Color.White
                ).Draw();

                new UIResText(
                    rm.GetString("intro23_1"),
                    new Point((Convert.ToInt32(res.Width) / 2 - 200), 125),
                    0.5f,
                    Color.White
                ).Draw();

                // provide some delay, so player can react
                if (lastCharSelectInput + 1000 < Game.GameTime)
                {
                    // cycle left
                    if (Game.IsKeyPressed(Keys.A)
                        || Function.Call<int>(Hash.GET_CONTROL_VALUE, 0, 9) < 100
                        || Game.IsControlPressed(0, GTA.Control.MoveLeft))
                    {
                        selectedCharacter = mod(selectedCharacter - 1, peds.Count);
                        lastCharSelectInput = Game.GameTime;
                    }

                    // cycle right
                    if (Game.IsKeyPressed(Keys.D)
                        || Function.Call<int>(Hash.GET_CONTROL_VALUE, 0, 9) > 155
                        || Game.IsControlPressed(0, GTA.Control.MoveRight))
                    {
                        selectedCharacter = (selectedCharacter + 1) % peds.Count;
                        lastCharSelectInput = Game.GameTime;
                    }

                    // pressing confirm button
                    if (Game.IsKeyPressed(Keys.Enter)
                        || Game.IsControlPressed(0, GTA.Control.Enter))
                    {
                        lastCharSelectInput = Game.GameTime;

                        if (!charSelectionConfirmed)
                        {
                            charSelectionConfirmed = true;
                        }
                        else
                        {
                            charSelected = true;
                            charSelectionActive = false;

                            // log the time it took the player
                            singularValues.Add("intro_char_selection_time", Game.GameTime - charSelectionStartedAt);
                            singularValues.Add("intro_char_selected", selectedCharacter);

                            // revert to regular camera
                            World.RenderingCamera = null;

                            changePedToSelectedSkin();
                        }
                    }
                }
            }
            else if ((charSelected && raceEndTime == 0)
                && (Game.Player.Character.CurrentVehicle == null
                    || Game.Player.Character.CurrentVehicle != raceVehicle)) {

                var player = Game.Player.Character;
                float radius = 13,
                    halfradiussquared = 68;

                Game.Player.CanControlCharacter = true;
                Game.Player.IsInvincible = false;
                player.FreezePosition = false;

                new UIResText(
                    rm.GetString("intro24"),
                    new Point((Convert.ToInt32(res.Width) / 2 - 400), 125),
                    0.5f,
                    Color.White
                ).Draw();

                World.DrawMarker(
                    MarkerType.VerticalCylinder,
                    car_selection,
                    new Vector3(),
                    new Vector3(),
                    new Vector3(radius, radius, 0.3f),
                    Color.Aqua
                );

                if (player.Position.DistanceToSquared(car_selection) > halfradiussquared)
                {
                    player.Position = car_selection;
                }
            }
        }

        private void changePedToSelectedSkin() {
            String modelSuffix = "zero",
                modelName = "player_";

            switch (selectedCharacter) {
                case 1:
                    modelSuffix = "one";
                    break;
                case 2:
                    modelSuffix = "two";
                    break;
            }

            loadAndSetPlayerModel(modelName + modelSuffix);
            removeOtherPeds();
            Game.Player.Character.Position = car_selection;
        }

        private void loadAndSetPlayerModel(String pedModelName) {
            var pedmodel = new Model(pedModelName);
            pedmodel.Request();

            if (pedmodel.IsInCdImage &&
                pedmodel.IsValid
                )
            {
                // If the model isn't loaded, wait until it is
                while (!pedmodel.IsLoaded)
                    Script.Wait(100);

                Game.Player.ChangeModel(pedmodel);

                switch (selectedCharacter) {
                    case 0:
                        loadCharacterProperties(Game.Player.Character, michaelProperties);
                        break;
                    case 1:
                        loadCharacterProperties(Game.Player.Character, franklinProperties);
                        break;
                    case 2:
                        loadCharacterProperties(Game.Player.Character, trevorProperties);
                        break;
                }

                pedmodel.MarkAsNoLongerNeeded();
            }
        }

        private void removeOtherPeds() {
            foreach (Ped ped in peds) {
                if (ped != Game.Player.Character) {
                    ped.Delete();
                }
            }
        }

        private int mod(int x, int m)
        {
            int r = x % m;
            return r < 0 ? r + m : r;
        }

        private void initFranklinProperties() {
            franklinProperties = new int[][] {
                new int[] {0, 8},
                new int[] {1, 0},
                new int[] {0, 2},
                new int[] {13, 6},
                new int[] {15, 4},
                new int[] {1, 0},
                new int[] {12, 3},
                new int[] {0, 0},
                new int[] {0, 0},
                new int[] {0, 0},
                new int[] {0, 0},
                new int[] {0, 0}
            };
        }

        private void initTrevorProperties() {
            trevorProperties = new int[][] {
                new int[] {0, 1},
                new int[] {5, 0},
                new int[] {4, 0},
                new int[] {24, 1},
                new int[] {19, 3},
                new int[] {0, 0},
                new int[] {1, 0},
                new int[] {0, 0},
                new int[] {14, 0},
                new int[] {0, 0},
                new int[] {0, 0},
                new int[] {0, 0}
            };
        }

        private void initMichaelProperties() {
            michaelProperties = new int[][] {
                new int[] {0, 0},
                new int[] {0, 0},
                new int[] {0, 0},
                new int[] {0, 12},
                new int[] {0, 12},
                new int[] {0, 0},
                new int[] {0, 0},
                new int[] {0, 0},
                new int[] {0, 0},
                new int[] {0, 0},
                new int[] {0, 0},
                new int[] {0, 0}
            };
        }

        private void loadCharacterProperties(Ped ped, int[][] properties) {
            for (int i = 0; i < properties.Length; i++) {
                if (Function.Call<bool>(Hash.IS_PED_COMPONENT_VARIATION_VALID, ped, i, properties[i][0], properties[i][1])) {
                    Function.Call(Hash.SET_PED_COMPONENT_VARIATION, ped, i, properties[i][0], properties[i][1], 0);
                }
            }
        }

        private void showCharacterName(SizeF res, Point safe) {
            String name = "";

            if (showingMichael) {
                name = "Michael";
            }
            if (showingFranklin) {
                name = "Franklin";
            }
            if (showingTrevor) {
                name = "Trevor";
            }

            if (!String.IsNullOrEmpty(name)) {
                UIResText charName = new UIResText(
                    name, 
                    new Point(900, 900),
                    1,
                    Color.Yellow,
                    GTA.Font.Pricedown,
                    UIResText.Alignment.Centered
                );
                charName.Outline = true;
                charName.Draw();
            }
        }

        #endregion characterSelection
    }
}
