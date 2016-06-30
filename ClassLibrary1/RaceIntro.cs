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
                new Vector3(72.1081f, -1011.055f, 81.04148f), // camPos
                new Vector3(-8.138871f, 0, -116.3f) // camRot
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

            World.CurrentDayTime = new TimeSpan(11, 45, 0);

            player.Position = new Vector3(-341.388f, 1147.779f, 325.7267f);

            List<Ped> characters = spawnCharacters();
            showVector(
                new Vector3(-343f, 1151, 327f),
                new Vector3(-8.71f, 0, -179.58f)
            );
            World.RenderingCamera.FieldOfView = 70;
            bmsg.ShowOldMessage(rm.GetString("intro3"), regularIntroSceneLength);
            Wait(regularIntroSceneLength);

            World.CurrentDayTime = new TimeSpan(10, 35, 0);

            player.Position = new Vector3(53.65551f, -177.0567f, 54.96252f);
            showVector(
                new Vector3(60.103f, -163.81f, 60.98f), // camPos
                new Vector3(-15.33f, 0, 68.15559f) // camRot
            );

            bmsg.ShowOldMessage(rm.GetString("intro4"), regularIntroSceneLength);
            Wait(regularIntroSceneLength);

            // delete the additional character peds
            foreach (Ped charped in characters) {
                charped.Delete();
            }

            World.CurrentDayTime = new TimeSpan(16, 45, 0);

            player.Position = new Vector3(-759.9341f, 5537.745f, 33.48476f);

            // show getting into car
            showVector(
                new Vector3(-754.44f, 5536.25f, 33.46589f),
                new Vector3(6.91f, -2.15f, 100.23f)
            );
            World.RenderingCamera.FieldOfView = 45;
            player.Heading = 155.9079f;

            Vehicle car = createCarAt(
                VehicleHash.Adder,
                new Vector3(-761.6691f, 5533.743f, 33.50467f),
                267.3927f
            );

            Game.Player.Character.Task.EnterVehicle(car, VehicleSeat.Driver, 10000, 1.0f, 1);

            bmsg.ShowOldMessage(rm.GetString("intro5"), regularIntroSceneLength);
            Wait(regularIntroSceneLength);

            //player.Position = new Vector3(-759.9341f, 5537.745f, 33.48476f);
            // show driving a bit

            Vector3[] waypoints = {
                new Vector3(-779.4083f, 5550.534f, 33.0866f),
                new Vector3(-783.6174f, 5506.958f, 34.1205f),
                new Vector3(-862.26f, 5447.984f, 34.16988f)
            };

            Tuple<Vector3,Vector3>[] cameraPerspectives = {
                new Tuple<Vector3, Vector3>(new Vector3(-758, 5532, 34), new Vector3(-12.5f, 0, 32)),
                new Tuple<Vector3, Vector3>(new Vector3(-793, 5539.46f, 35.21597f), new Vector3(-11.17726f, 2.175f, -169.5087f)),
                new Tuple<Vector3, Vector3>(new Vector3(-784.9023f, 5496.826f, 35.44334f), new Vector3(-8.351498f, 0, 135.9224f))
            };

            float radius = 5,
                speed = 15;

            // have player drive through waypoints
            for (int i = 0; i < waypoints.Length; i++) {
                player.Task.DriveTo(car, waypoints[i], radius, speed);
                showVector(cameraPerspectives[i].Item1, cameraPerspectives[i].Item2);

                // wait for player to drive to waypoint
                while (!player.IsInRangeOf(waypoints[i], radius)) {
                    Wait(50);
                }
            }

            World.CurrentDayTime = new TimeSpan(10, 30, 0);

            player.Position = new Vector3(1186, -3215, 5.79f);

            // point camera
            showVector(
                new Vector3(1175, -3221, 5.284f),
                new Vector3(14.55f, 2.2f, 77.322f)
            );
            World.RenderingCamera.FieldOfView = 75;

            Function.Call<int>(Hash.CREATE_CHECKPOINT,
                2, // type
                1166.71f,
                -3218.176f,
                5.799773f,
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

            bmsg.ShowOldMessage(rm.GetString("intro6"), regularIntroSceneLength);
            Wait(regularIntroSceneLength);

            //show regular camera
            World.DestroyAllCameras();
            bmsg.ShowOldMessage(rm.GetString("intro7"), regularIntroSceneLength);
            Blip blip = World.CreateBlip(player.Position + 25*player.ForwardVector);
            Function.Call(Hash.SET_BLIP_ROUTE, blip, true);

            Function.Call(Hash._SET_RADAR_BIGMAP_ENABLED, 1, 0);
            Function.Call(Hash.FLASH_MINIMAP_DISPLAY);
            Wait(1000);
            Function.Call(Hash.FLASH_MINIMAP_DISPLAY);
            Wait(1000);
            Function.Call(Hash._SET_RADAR_BIGMAP_ENABLED, 0, 0);
            Wait(1000);
            Function.Call(Hash.FLASH_MINIMAP_DISPLAY);
            blip.Remove();
            Wait(1000);

            World.CurrentDayTime = new TimeSpan(7, 0, 0);

            player.Position = new Vector3(-130f, -1709.683f, 29.85f);

            // point camera
            showVector(
                new Vector3(-120.2002f, -1728f, 32f),
                new Vector3(-4.43f, 0, -45f)
            );
            World.RenderingCamera.FieldOfView = 75;

            bmsg.ShowOldMessage(rm.GetString("intro8"), regularIntroSceneLength);
            Wait(regularIntroSceneLength);

            // run over pedestrian

            player.Position = new Vector3(-171, -1699.85f, 32.084f);
            Vehicle aggro_car = createCarAt(VehicleHash.Buffalo, new Vector3(-169.1496f, -1698.676f, 31.59141f), 16.60919f);

            Game.Player.Character.Task.EnterVehicle(aggro_car, VehicleSeat.Driver, 10000, 2.0f, 16);
            Game.Player.Character.SetIntoVehicle(aggro_car, VehicleSeat.Driver);

            bmsg.ShowOldMessage(rm.GetString("intro9"), regularIntroSceneLength);

            // point camera
            showVector(
                new Vector3(-174, -1686f, 33.289f),
                new Vector3(15.46f, 4.52f, -27.52f)
            );
            World.RenderingCamera.FieldOfView = 75;

            Vector3 poor_ped_position = new Vector3(-172.2675f, -1679.185f, 33.0725f);

            createPedAt(PedHash.Abigail, poor_ped_position);
            createPedAt(PedHash.Genstreet01AFO, new Vector3(-169.7445f, -1671.922f, 33.26389f));
            createPedAt(PedHash.Genstreet01AMY, new Vector3(-170.4802f, -1667.074f, 33.23298f));
            createPedAt(PedHash.Latino01AMY, new Vector3(-175.6795f, -1671.036f, 33.23465f));

            player.Task.DriveTo(car, new Vector3(-172.8149f, -1650.993f, 33.05474f), 5, 80);

            // wait for player to drive through ped's area
            while (!player.IsInRangeOf(poor_ped_position, 3))
            {
                Wait(50);
            }

            // point camera
            showVector(
                new Vector3(-178.6524f, -1668.159f, 33.37955f),
                new Vector3(4.23f, 0, -99)
            );

            bmsg.ShowOldMessage(rm.GetString("intro9"), regularIntroSceneLength);
            Wait(regularIntroSceneLength);

            // give control back and use regular camera
            World.RenderingCamera = null;
            World.DestroyAllCameras();
            Game.Player.Character.IsInvincible = false;
            Game.Player.CanControlCharacter = true;
        }

        private void showVector(Vector3 cameraPosition, Vector3 cameraRotation) {
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
                return World.CreatePed(pedmodel, pos);
                
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

                return vehicle;
            }

            vehicle1Model.MarkAsNoLongerNeeded();

            throw new Exception("vehicle model could not be loaded");
        }
    }
}
