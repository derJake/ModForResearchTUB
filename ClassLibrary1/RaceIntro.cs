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
using System.Windows.Forms;
using System.IO;
using WindowsInput;

namespace ModForResearchTUB
{
    class RaceIntro : Script, RaceInterface
    {
        private Tuple<Vector3, Vector3?>[] checkpoints;

        private int raceStartTime;

        private Vehicle raceVehicle;

        private Vector3 car_selection = new Vector3(-747.7241f, -74.62083f, 41.31352f);
        private Vector3 car1_spawnpoint = new Vector3(-748.7908f, -79.43627f, 41.31476f);
        private float car_spawn_heading = 26.37f;
        private float car_spawn_player_heading = 164.5883f;
        private List<Tuple<String, List<Tuple<String, double>>>> collectedData = new List<Tuple<String, List<Tuple<String, double>>>>();
        private VehicleHash vehicleHash = VehicleHash.Comet2;
        private int regularIntroSceneLength = 10000;

        private TimerBarPool barPool = new TimerBarPool();
        private TextTimerBar textbar;
        private int introPlayTime = 600;
        private int raceEndTime;

        private List<Vehicle> cars;
        private List<Ped> peds;
        public CultureInfo CultureInfo { get; private set; }

        private bool intro_sequence_active = false;

        ResourceManager rm;
        Utilities ut;

        public RaceIntro(ResourceManager resman, Utilities utils) {
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
                new Tuple<Vector3, Vector3?>(new Vector3(-754.8044f, -71.13426f, 41.37538f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-778.4377f, -84.18153f, 37.79903f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-811.071f, -74.18734f, 37.47362f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-862.2216f, -101.1226f, 37.57107f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-1094.416f, -221.9508f, 37.49302f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-1210.87f, -293.118f, 37.46842f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-1333.246f, -355.755f, 36.34713f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-1440.249f, -420.062f, 35.52185f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-1532.737f, -479.0613f, 35.09676f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-1637.596f, -560.9938f, 33.11208f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-1630.689f, -604.4155f, 32.7567f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-1560.134f, -660.6344f, 28.66253f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-1516.197f, -711.475f, 27.17746f), new Vector3(1854.16f, 4992.841f, 53.53355f))
            };

            this.checkpoints = checkpointlist;

            peds = new List<Ped>();
            cars = new List<Vehicle>();

            CultureInfo = CultureInfo.CurrentCulture;
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

            var player = Game.Player.Character;
            Vector3 cam_pos = player.Position + player.ForwardVector * 5 - player.RightVector * 2 + new Vector3(0, 0, 1);
            World.RenderingCamera = World.CreateCamera(cam_pos, new Vector3(), 90f);
            
            World.RenderingCamera.PointAt(player.CurrentVehicle);
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

            barPool.Remove(textbar);
        }

        public Tuple<Vector3, Vector3?>[] getCheckpoints()
        {
            return checkpoints;
        }

        public void handleOnTick()
        {
            if (raceStartTime > 0 && Game.GameTime < raceEndTime)
            {
                TimeSpan timeLeft = TimeSpan.FromMilliseconds(raceEndTime - Game.GameTime);
                textbar.Text = timeLeft.ToString(@"mm\:ss");
                barPool.Draw();
            }
            else if (raceEndTime > 0 && Game.GameTime >= raceEndTime) {
                Game.Player.Character.CurrentVehicle.Position = checkpoints[checkpoints.Length - 1].Item1;
            }

            // setting this once doesn't seem to alway do the trick
            if (intro_sequence_active) {
                Game.Player.Character.IsInvincible = true;
                Game.Player.Character.Health = 100;
                Game.Player.Character.Armor = 100;
                Game.Player.CanControlCharacter = false;
                Game.Player.Character.CanBeDraggedOutOfVehicle = false;
                Game.Player.Character.CanFlyThroughWindscreen = false;
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

            // set weather
            Function.Call(Hash.SET_WEATHER_TYPE_NOW_PERSIST, "EXTRASUNNY");

            Ped player = Game.Player.Character;
            player.Task.ClearAllImmediately(); // give back control to player

            // teleport player and turn him towards cars
            Game.Player.Character.Position = car_selection;
            Game.Player.Character.Heading = car_spawn_player_heading;

            raceVehicle = ut.createCarAt(vehicleHash, car1_spawnpoint, car_spawn_heading);

            // make player enter vehicle
            Game.Player.Character.Task.EnterVehicle(raceVehicle, VehicleSeat.Driver, 10000, 2.0f, 16);
            Game.Player.Character.SetIntoVehicle(raceVehicle, VehicleSeat.Driver);

            // set time of day
            World.CurrentDayTime = new TimeSpan(19, 15, 0);
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

        public List<Tuple<string, List<Tuple<string, double>>>> getCollectedData()
        {
            return this.collectedData;
        }

        private void doIntroSequence() {
            // set state for onTick operations
            intro_sequence_active = true;

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

            player.Position = new Vector3(-341.388f, 1147.779f, 325.7267f);
            player.Heading = 7.05f;

            List<Ped> characters = spawnCharacters();
            showVector(
                new Vector3(-343f, 1151, 327f),
                new Vector3(-8.71f, 0, -179.58f)
            );
            World.RenderingCamera.FieldOfView = 70;
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
                new Vector3(-392f, 1185.584f, 326.1006f),
                new Vector3(-1.56f, 0, -205.8116f)
            );
            World.RenderingCamera.FieldOfView = 55;
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
                new Vector3(-273.8922f, 1220.785f, 316.6845f),
                new Vector3(-196.1113f, 1304.639f, 303.8253f)
            };

            Tuple<Vector3,Vector3>[] cameraPerspectives = {
                new Tuple<Vector3, Vector3>(new Vector3(-352, 1179, 326), new Vector3(-12, 0, -75.62788f)),
                new Tuple<Vector3, Vector3>(new Vector3(-324, 1200, 327), new Vector3(0, 0, 128.4084f)),
                new Tuple<Vector3, Vector3>(new Vector3(-263, 1276, 314), new Vector3(-6.111776f, 0, -145.4782f))
            };

            float radius = 5,
                radiustolerance = 2,
                speed = 30;

            World.RenderingCamera.PointAt(car);

            // have player drive through waypoints
            for (int i = 0; i < waypoints.Length; i++) {
                player.Task.DriveTo(car, waypoints[i], radius, speed);
                Function.Call(Hash.SET_DRIVE_TASK_DRIVING_STYLE, player, 1 << 9);
                World.RenderingCamera.Position = cameraPerspectives[i].Item1;
                World.RenderingCamera.Rotation = cameraPerspectives[i].Item2;

                // wait for player to drive to waypoint
                while (!player.IsInRangeOf(waypoints[i], radius + radiustolerance)) {
                    Wait(50);
                }
            }
            World.RenderingCamera.StopPointing();
            // point camera
            showVector(
                new Vector3(-199, 1308, 306),
                new Vector3(-7.977294f, 0, -35)
            );

            Vector3 checkpoint_position = new Vector3(-172.7784f, 1362.75f, 296.5129f);

            Function.Call<int>(Hash.CREATE_CHECKPOINT,
                2, // type
                checkpoint_position.X,
                checkpoint_position.Y,
                checkpoint_position.Z,
                0, // facing next checkpoint?
                0,
                0,
                5,    // radius
                255,    // R
                155,     // G
                0,        // B
                100,    // Alpha
                0 // number displayed in marker, if type is 42-44
                );
            World.RenderingCamera.PointAt(checkpoint_position);

            bmsg.ShowOldMessage(rm.GetString("intro6"), regularIntroSceneLength);
            Wait(regularIntroSceneLength);

            //show regular camera
            World.DestroyAllCameras();
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
            Wait(5000);
            blip.Remove();

            // show traffic light

            World.CurrentDayTime = new TimeSpan(7, 0, 0);

            player.Position = new Vector3(-130f, -1709.683f, 29.85f);

            // point camera
            showVector(
                new Vector3(-120.2002f, -1728f, 32f),
                new Vector3(-4.43f, 0, -45f)
            );
            World.RenderingCamera.FieldOfView = 75;

            bmsg.ShowOldMessage(rm.GetString("intro8"), regularIntroSceneLength);

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
                Wait(1000);
            }
            
            Wait(regularIntroSceneLength);

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
            Vector3 car_stop_position = new Vector3(-54f, -1669f, 28.84788f);

            var poor_ped = ut.createPedAt(PedHash.Abigail, poor_ped_position);
            poor_ped.Heading = poor_ped_heading;
            poor_ped.CanRagdoll = false;

            //Function.Call(Hash._0xE8A25867FBA3B05E, 0, 9, 1);
            //player.Task.ClearAll();

            // wait for player to have entered car, otherwise the task won't start
            while (!player.CurrentVehicle.Equals(aggro_car)) {
                Wait(100);
            }

            player.Task.DriveTo(aggro_car, poor_ped_position, 3, 80, (int)DrivingStyle.Rushed);

            // wait for player to drive through ped's area
            while (!aggro_car.IsInRangeOf(poor_ped_position, 3))
            {
                Wait(50);
            }

            var dict = "avoids";
            Function.Call(Hash.REQUEST_ANIM_DICT, dict);
            poor_ped.Task.PlayAnimation(dict, "frfront_toback", 2, 500000, false, 0);

            bmsg.ShowOldMessage(rm.GetString("intro10"), regularIntroSceneLength);

            Wait(5000);

            // show police

            bmsg.ShowOldMessage(rm.GetString("intro11"), regularIntroSceneLength);

            Game.Player.WantedLevel = 2;

            int playerRGroup = player.RelationshipGroup;

            // spawn cop car and cops
            ut.createCarAt(VehicleHash.Police, new Vector3(-48.13033f, -1672.859f, 28.9749f), 61.59479f);
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
                cop.Task.ShootAt(player);
            }

            World.SetRelationshipBetweenGroups(Relationship.Hate, playerRGroup, copHash);

            World.RenderingCamera.Position = new Vector3(-42, -1679, 30);
            World.RenderingCamera.Rotation = new Vector3(-0.8f, 0, 47);
            World.RenderingCamera.FieldOfView = 25;

            Wait(regularIntroSceneLength);

            showVector(
                new Vector3(-163, -1660, 37),
                new Vector3(-36.11f, -2.14f, -90f)
            );

            bmsg.ShowOldMessage(rm.GetString("intro12"), regularIntroSceneLength);

            Function.Call(Hash.FLASH_WANTED_DISPLAY, true);

            Wait(regularIntroSceneLength / 2);

            player.IsInvincible = true;
            Game.Player.CanControlCharacter = false;

            bmsg.ShowOldMessage(rm.GetString("intro13"), regularIntroSceneLength);
            Wait(3000);

            Wait(regularIntroSceneLength / 2);

            Function.Call(Hash.FLASH_WANTED_DISPLAY, false);

            // remove wanted level and aggressive cop peds
            World.SetRelationshipBetweenGroups(Relationship.Neutral, playerRGroup, copHash);
            Game.Player.WantedLevel = 0;

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
            Function.Call(Hash.SET_PED_TO_RAGDOLL, player, regularIntroSceneLength - 2000);
            Wait(regularIntroSceneLength);

            // driving sequence 2
            player.Position = new Vector3(1792, 3325, 41.5f);
            World.CurrentDayTime = new TimeSpan(8, 5, 0);
            Vehicle desert_car = ut.createCarAt(VehicleHash.Surge, new Vector3(1791, 3324, 41), 180);

            bmsg.ShowOldMessage(rm.GetString("intro17"), regularIntroSceneLength);

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
                    Wait(50);
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
                new Vector3(-1057.758f, 393.5327f, 68.75936f),
                new Vector3(-1033.359f, 395.3765f, 69.95447f),
                new Vector3(-989.835f, 367.2408f, 72.13671f)
            };

            Tuple<Vector3, Vector3>[] camera_perspectives_urban = {
                new Tuple<Vector3, Vector3>(new Vector3(-1085f, 444f, 75.63884f), new Vector3(-2.2232f, 0, -85f)),
                new Tuple<Vector3, Vector3>(new Vector3(-1078.817f, 437.9136f, 75.37682f), new Vector3(-18.2789f, 0, -178.5177f)),
                new Tuple<Vector3, Vector3>(new Vector3(-1076, 403, 68.75f), new Vector3(2f, 0, -18.6f)),
                new Tuple<Vector3, Vector3>(new Vector3(-1062.851f, 394.5331f, 69.79096f), new Vector3(-1.448f, 0, -99.76324f)),
                new Tuple<Vector3, Vector3>(new Vector3(-1027, 393, 71), new Vector3(-2f, 0, 75)),
                new Tuple<Vector3, Vector3>(new Vector3(-985, 366, 73), new Vector3(-6.3f, 0, 76.7f))
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

            bmsg.ShowOldMessage(rm.GetString("intro19"), regularIntroSceneLength);

            Wait(3000);
            UI.ShowSubtitle("reverse parking");
            Vector3 destination = new Vector3(-1007, 368.8f, 71.8f);
            //player.Task.ParkVehicle(car, new Vector3(-1007, 368.8f, 71.8f), 311);
            //Function.Call(Hash.TASK_VEHICLE_PARK, desert_car, -1007, 368.8f, 71.8f, 311, 2, 0, true);

            Function.Call(Hash.TASK_VEHICLE_GOTO_NAVMESH,
                player,
                desert_car,
                destination.X,
                destination.Y,
                destination.Z,
                10,
                1 << 10 | 1 << 8 | 1 << 16 | 1 << 32, // <==== use the bar to separate flags. This is a bitwise OR
                3
            );

            Wait(regularIntroSceneLength);

            // show flipped car

            bmsg.ShowOldMessage(rm.GetString("intro20"), regularIntroSceneLength);

            World.RenderingCamera.Position = new Vector3(-998, 370, 73);
            World.RenderingCamera.Rotation = new Vector3(-6.3f, 0, 90f);

            desert_car.Rotation = new Vector3(0,180,311);

            player.Task.DriveTo(desert_car, new Vector3(), 21, 25);

            //while (Math.Abs(desert_car.Rotation.Y) > 30) {
            //    Wait(300);
            //}

            Wait(regularIntroSceneLength);

            // give control back and use regular camera
            World.RenderingCamera = null;
            World.DestroyAllCameras();
            Game.Player.Character.IsInvincible = false;
            Game.Player.CanControlCharacter = true;
            intro_sequence_active = false;
        }

        private void showVector(Vector3 cameraPosition, Vector3 cameraRotation) {
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

            List<Ped> characters = new List<Ped>(2);
            Ped franklin = ut.createPedAt(PedHash.Franklin, new Vector3(-343.1626f, 1147.788f, 325.7267f));
            Ped trevor = ut.createPedAt(PedHash.Trevor, new Vector3(-345.1988f, 1147.625f, 325.7263f));
            franklin.Heading = 7.05f;
            trevor.Heading = 7.05f;
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
    }
}
