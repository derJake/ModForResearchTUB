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
                new Tuple<Vector3, Vector3?>(new Vector3(1164.767f, -3218.366f, 5.799805f), new Vector3(1854.16f, 4992.841f, 53.53355f))
            };

            this.checkpoints = checkpointlist;

            peds = new List<Ped>();
            cars = new List<Vehicle>();

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

            var player = Game.Player.Character;
            Vector3 cam_pos = player.Position + player.ForwardVector * 5 - player.RightVector * 2 + new Vector3(0, 0, 1);
            World.RenderingCamera = World.CreateCamera(cam_pos, new Vector3(12.26449f, 0, 109.785f), 90f);
            
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
                textbar.Text = timeLeft.Minutes.ToString() + ':' + timeLeft.Seconds.ToString();
                barPool.Draw();
            }
            else if (raceEndTime > 0 && Game.GameTime >= raceEndTime) {
                Game.Player.Character.CurrentVehicle.Position = checkpoints[checkpoints.Length - 1].Item1;
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

            raceVehicle = createCarAt(vehicleHash, car1_spawnpoint, car_spawn_heading);

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
                createCarAt(VehicleHash.Adder, potential_car, 270.2285f);
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
                new Vector3(-1.56f, 0, -105.8116f)
            );
            World.RenderingCamera.FieldOfView = 55;
            player.Heading = 155.9079f;

            Vehicle car = createCarAt(
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
            Wait(regularIntroSceneLength);

            //player.Position = new Vector3(-759.9341f, 5537.745f, 33.48476f);
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

            // point camera
            showVector(
                new Vector3(-199, 1308, 306),
                new Vector3(14.55f, 2.2f, 77.322f)
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
            World.RenderingCamera.StopPointing();
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

            player.Position = new Vector3(-193.707f, -1673.128f, 33.59856f);

            foreach (Vehicle vehicle in World.GetNearbyVehicles(player, 50)) {
                vehicle.Delete();
            }

            Vehicle aggro_car = createCarAt(VehicleHash.Buffalo, new Vector3(-192.55f, -1674.355f, 33.092825f), 281.5312f);

            Game.Player.Character.SetIntoVehicle(aggro_car, VehicleSeat.Driver);

            bmsg.ShowOldMessage(rm.GetString("intro9"), regularIntroSceneLength);

            // point camera
            showVector(
                new Vector3(-174, -1686f, 33.289f),
                new Vector3(15.46f, 4.52f, -27.52f)
            );
            World.RenderingCamera.FieldOfView = 75;

            List<Ped> bystanders = new List<Ped>(3);

            Vector3 poor_ped_position = new Vector3(-174.8878f, -1669.339f, 32.88508f);
            Vector3 car_stop_position = new Vector3(-153.6f, -1659.196f, 32.47199f);
            //World.DrawMarker(MarkerType.VerticalCylinder, new Vector3(-172.2675f, -1679.185f, 33.0725f), new Vector3(), new Vector3(), new Vector3(2,0,1), Color.AliceBlue);

            createPedAt(PedHash.Abigail, poor_ped_position);
            bystanders.Add(createPedAt(PedHash.Genstreet01AFO, new Vector3(-169.7445f, -1671.922f, 33.26389f)));
            bystanders.Add(createPedAt(PedHash.Genstreet01AMY, new Vector3(-170.4802f, -1667.074f, 33.23298f)));
            bystanders.Add(createPedAt(PedHash.Latino01AMY, new Vector3(-175.6795f, -1671.036f, 33.23465f)));

            Random rnd = new Random();

            foreach (Ped ped in bystanders) {
                ped.Heading = rnd.Next(0, 360);
            }

            // wait for player to have entered car, otherwise the task won't start
            while (!player.CurrentVehicle.Equals(aggro_car)) {
                Wait(100);
            }

            player.Task.DriveTo(aggro_car, car_stop_position, 5, 80, (int)DrivingStyle.Rushed);

            // wait for player to drive through ped's area
            while (!aggro_car.IsInRangeOf(poor_ped_position, 10))
            {
                Wait(50);
            }

            // point camera
            showVector(
                new Vector3(-165f, -1670f, 35f),
                new Vector3(-26f, 0, 70f)
            );
            World.RenderingCamera.FieldOfView = 75;

            bmsg.ShowOldMessage(rm.GetString("intro10"), regularIntroSceneLength);

            // wait for player to drive through ped's area
            while (!aggro_car.IsInRangeOf(car_stop_position, 10))
            {
                Wait(50);
            }

            player.IsInvincible = true;
            Game.Player.CanControlCharacter = false;
            //Function.Call(Hash.SET_VEHICLE_DOORS_LOCKED, aggro_car, 2);

            // point camera
            showVector(
                new Vector3(-153, -1658, 34),
                new Vector3(-8.7f, 2.13f, 116)
            );
            World.RenderingCamera.FieldOfView = 45;

            player.CanBeDraggedOutOfVehicle = false;
            player.CanFlyThroughWindscreen = false;

            foreach (Ped bystander in bystanders) {
                bystander.Task.FightAgainst(player);
            }

            Wait(5000);

            // show police

            bmsg.ShowOldMessage(rm.GetString("intro11"), regularIntroSceneLength);

            Game.Player.WantedLevel = 2;

            int playerRGroup = player.RelationshipGroup;

            // spawn cop car and cops
            createCarAt(VehicleHash.Police, new Vector3(-147.3967f, -1646.757f, 32.05892f), 143.9459f);
            List<Ped> police = new List<Ped>(2);
            Ped policeman_1 = createPedAt(PedHash.Cop01SFY, new Vector3(-145.7362f, -1646.869f, 32.61502f));
            Ped policeman_2 = createPedAt(PedHash.Cop01SMY, new Vector3(-148.2104f, -1644.999f, 32.72406f));
            policeman_1.Heading = 143.2996f;
            policeman_2.Heading = 136.2911f;
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

            World.RenderingCamera.Position = new Vector3(-164, -1667, 34);
            World.RenderingCamera.Rotation = new Vector3(-6.11f, -2.14f, -39.55f);
            World.RenderingCamera.FieldOfView = 25;

            Wait(regularIntroSceneLength);

            showVector(
                new Vector3(-163, -1660, 37),
                new Vector3(-36.11f, -2.14f, -90f)
            );

            bmsg.ShowOldMessage(rm.GetString("intro12"), regularIntroSceneLength);

            Function.Call(Hash.FLASH_WANTED_DISPLAY, true);

            Wait(regularIntroSceneLength);

            Game.Player.WantedLevel = 4;

            VehicleHash police_car_model = VehicleHash.Police2;

            createCarAt(police_car_model, new Vector3(-162.3957f, -1666.599f, 32.63364f), 310);
            Vehicle middle_police_car = createCarAt(police_car_model, new Vector3(-146.7052f, -1641.89f, 32.41359f), 120);
            createCarAt(police_car_model, new Vector3(-143.4058f, -1645.468f, 32.21568f), 158.9681f);

            List<Ped> additional_police = new List<Ped>(6);
            Ped policeman_3 = createPedAt(PedHash.Cop01SMY, new Vector3(-142.0587f, -1645.612f, 32.63714f));
            Ped policeman_4 = createPedAt(PedHash.Cop01SMY, new Vector3(-144.813f, -1645.273f, 32.58187f));
            Ped policeman_5 = createPedAt(PedHash.Cop01SMY, new Vector3(-163.5185f, -1665.744f, 33.08033f));
            Ped policeman_6 = createPedAt(PedHash.Cop01SMY, new Vector3(-161.4851f, -1667.609f, 33.08136f));
            Ped policeman_7 = createPedAt(PedHash.Cop01SMY, new Vector3(-145.5382f, -1643.036f, 32.66076f));
            Ped policeman_8 = createPedAt(PedHash.Cop01SMY, new Vector3(-147.2389f, -1640.95f, 32.9472f));
            additional_police.Add(policeman_3);
            additional_police.Add(policeman_4);
            additional_police.Add(policeman_5);
            additional_police.Add(policeman_6);
            additional_police.Add(policeman_7);
            additional_police.Add(policeman_8);

            // make additional police aggressive and shoot at player
            foreach (Ped cop in additional_police)
            {
                cop.RelationshipGroup = copHash;
                cop.Weapons.Give(WeaponHash.CombatPistol, 2000, true, true);
                cop.Task.ShootAt(player);
            }

            player.IsInvincible = true;
            Game.Player.CanControlCharacter = false;

            bmsg.ShowOldMessage(rm.GetString("intro13"), regularIntroSceneLength);
            Wait(3000);

            World.RenderingCamera.Position = new Vector3(-149.37f, -1655.196f, 33.9734f);
            World.RenderingCamera.Rotation = new Vector3(-9.968f, 0, 120.2608f);
            World.RenderingCamera.FieldOfView = 25;
            World.RenderingCamera.PointAt(player.CurrentVehicle);

            Wait(regularIntroSceneLength / 2);

            World.RenderingCamera.PointAt(middle_police_car);

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
            Vehicle crash_car = createCarAt(VehicleHash.Premier, new Vector3(-73.7f, -1585.2f, 29.82f), 232f);
            // make player enter vehicle
            Game.Player.Character.Task.EnterVehicle(crash_car, VehicleSeat.Driver, 10000, 2.0f, 16);
            Game.Player.Character.SetIntoVehicle(crash_car, VehicleSeat.Driver);

            Vehicle car_rearended = createCarAt(VehicleHash.Tailgater, new Vector3(-65.16207f, -1592.397f, 29.12429f), 234.1621f);

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

            player.CurrentVehicle.Explode();

            Wait(regularIntroSceneLength);

            // player health scene
            World.RenderingCamera.StopPointing();
            player.Position = new Vector3(-68.60161f, -1690.505f, 29.20564f);
            player.Heading = 195.2348f;
            World.RenderingCamera.Position = new Vector3(-69.5f, -1695, 29);
            World.RenderingCamera.Rotation = new Vector3(-1, 0, -18);
            Wait(2000);
            Function.Call(Hash.SET_PED_TO_RAGDOLL, player, regularIntroSceneLength - 2000);
            bmsg.ShowOldMessage(rm.GetString("intro16"), regularIntroSceneLength);
            Wait(regularIntroSceneLength);

            Game.Player.CanControlCharacter = false;
            player.IsInvincible = true;

            // driving sequence 2
            player.Position = new Vector3(1792, 3325, 41.5f);
            World.CurrentDayTime = new TimeSpan(8, 5, 0);
            Vehicle desert_car = createCarAt(VehicleHash.Surge, new Vector3(1791, 3324, 41), 180);

            bmsg.ShowOldMessage(rm.GetString("intro17"), regularIntroSceneLength);

            // make player enter vehicle
            //Game.Player.Character.Task.EnterVehicle(desert_car, VehicleSeat.Driver, 10000, 2.0f, 16);
            Game.Player.Character.SetIntoVehicle(desert_car, VehicleSeat.Driver);

            // show driving a bit
            Vector3[] waypoints_desert = {
                new Vector3(1844.38f, 3299.551f, 43.05259f),
                new Vector3(1930.464f, 3313.151f, 44.89776f),
                new Vector3(1986.8f, 3291.7f, 45.24f),
                //new Vector3(1983.673f, 3286.297f, 45.25464f),
                //new Vector3(2036.384f, 3313.048f, 45.57026f),
                //new Vector3(2060.816f, 3394.411f, 44.94481f),
                //new Vector3(2060.663f, 3431.443f, 43.85197f)
            };

            Tuple<Vector3, Vector3>[] camera_perspectives_desert = {
                new Tuple<Vector3, Vector3>(new Vector3(1796.121f, 3309.683f, 42.8127f), new Vector3(0, 0, -111.41f)),
                new Tuple<Vector3, Vector3>(new Vector3(1838.032f, 3299.171f, 43.84303f), new Vector3(0.6644f, 0, -89.36685f)),
                new Tuple<Vector3, Vector3>(new Vector3(1925.446f, 3316.135f, 46.16137f), new Vector3(-3.91f, 0, -120.3298f)),
                //new Tuple<Vector3, Vector3>(new Vector3(1990f, 3290f, 45.821f), new Vector3(2.29f, 0, 95)),
                //new Tuple<Vector3, Vector3>(new Vector3(1978.3f, 3284.077f, 46.73193f), new Vector3(-6, 0, -67.33727f)),
                //new Tuple<Vector3, Vector3>(new Vector3(2036.246f, 3307.218f, 46.85886f), new Vector3(-4.2114f, 0, -1.58f)),
                //new Tuple<Vector3, Vector3>(new Vector3(2075, 3395, 45.5f), new Vector3(-2.55f, 0, 55)),
                //new Tuple<Vector3, Vector3>(new Vector3(2063, 3438, 44.5f), new Vector3(0.9f, 0, 159))
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
                while (!player.IsInRangeOf(waypoints_urban[i], urban_radius))
                {
                    Wait(50);
                }
            }

            // show reversing

            bmsg.ShowOldMessage(rm.GetString("intro19"), regularIntroSceneLength);

            Wait(3000);
            UI.ShowSubtitle("reverse parking");
            Function.Call(Hash.TASK_VEHICLE_PARK, desert_car, -1007, 368.8f, 71.8f, 311, 2, 50, true);

            Wait(regularIntroSceneLength);

            // show flipped car

            bmsg.ShowOldMessage(rm.GetString("intro20"), regularIntroSceneLength);

            World.RenderingCamera.Position = new Vector3(-998, 370, 73);
            World.RenderingCamera.Rotation = new Vector3(-6.3f, 0, 90f);

            car.Rotation = new Vector3(0,180,311);

            while (Math.Abs(car.Rotation.Y) > 30) {
                SendKeys.Send("{Left}");
                Wait(50);
            }

            Wait(regularIntroSceneLength);

            // give control back and use regular camera
            World.RenderingCamera = null;
            World.DestroyAllCameras();
            Game.Player.Character.IsInvincible = false;
            Game.Player.CanControlCharacter = true;
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
            Ped franklin = createPedAt(PedHash.Franklin, new Vector3(-343.1626f, 1147.788f, 325.7267f));
            Ped trevor = createPedAt(PedHash.Trevor, new Vector3(-345.1988f, 1147.625f, 325.7263f));
            franklin.Heading = 7.05f;
            trevor.Heading = 7.05f;
            characters.Add(franklin);
            characters.Add(trevor);

            return characters;
        }

        private Ped createPedAt(PedHash hash, Vector3 pos) {
            var pedmodel = new Model(hash);
            pedmodel.Request();

            if (pedmodel.IsInCdImage &&
                pedmodel.IsValid
                )
            {
                // If the model isn't loaded, wait until it is
                while (!pedmodel.IsLoaded)
                    Script.Wait(100);

                // create the actual driver ped
                Ped ped = World.CreatePed(pedmodel, pos);
                peds.Add(ped);
                return ped;
                
            }

            return null;
        }

        private Vehicle createCarAt(VehicleHash carmodelhash, Vector3 coordinates, float heading) {
            Vehicle vehicle;

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
                vehicle = World.CreateVehicle(carmodelhash, coordinates, heading);
                cars.Add(vehicle);
                return vehicle;
            }

            vehicle1Model.MarkAsNoLongerNeeded();

            throw new Exception("vehicle model could not be loaded");
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
