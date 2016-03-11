#region Using references

using System;
using GTA.Native;
using GTA;
using GTA.Math;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;
using NativeUI;
#endregion

namespace ModForResearchTUB
{
    public class Main : Script
    {
        // Variables
        int timer_1s = 0;
        List<Model> models = new List<Model>();
        List<Vehicle> vehicles = new List<Vehicle>();
        List<int> trafficSignalHashes = new List<int>(3);
        Blip currentBlip;
        Vector3[] checkpoints;
        int currentMarker;
        bool car_config_done = false;
        bool race_started = false;
        bool playerInRaceCar = false;
        bool copsCalled = false;
        int currentCheckpoint = -1;

        int speeds;
        int numOfSpeeds;
        int maxSpeed;

        int lastTimeBrake;
        int numBrakeApplied;
        int lastTimeHandbrake;
        int numHandBrakeApplied;
        int cumulativeTimeBraking;
        int cumulativeTimeHandbraking;

        const float mTokm = 1.60934f;

        int lastMaxTimeSinceHitVehicle;
        int lastMaxTimeSinceHitPed;
        int lastMaxTimeSincePavement;
        int lastMaxTimeSinceAgainstTraffic;

        int numOfHitVehicles;
        int numOfHitPeds;
        int numOfTimesDrivingOnPavement;
        int numOfTimesDrivingAgaingstTraffic;

        int startedDrivingOnPavement;
        int startedDrivingAgainstTraffic;

        int cumulativeTimeOnPavement;
        int cumulativeTimeDrivingAgainstTraffic;
        int raceStartTime;
        int raceEndTime;

        String car;

        Vector3 car_selection = new Vector3(-786.5052f, -2429.885f, 14.57072f);
        Vector3 car1_spawnpoint = new Vector3(-789.7347f, -2428.485f, 14.57072f);
        Vector3 car2_spawnpoint = new Vector3(-795.5708f, -2425.815f, 14.57072f);
        float car_spawn_heading = 147.0f;
        float car_spawn_player_heading = 48.0f;
        Vector3 race1Start = new Vector3(-1015.348f, -2715.956f, 12.58948f);
        Vector3 race1End = new Vector3(-45.45972f, -784.222f, 44.34782f);

        // Main Script
        public Main()
        {
            trafficSignalHashes.Add(-655644382);
            trafficSignalHashes.Add(862871082);
            trafficSignalHashes.Add(1043035044);
            // World.CreateProp(new Model(-1359996601), Game.Player.Character.Position, new Vector3(0f, 5f, 0f), false, false);

            // Tick Interval
            //Interval = 10;

            // Initialize Events
            Tick += this.OnTickEvent;
            KeyDown += this.KeyDownEvent;
            KeyUp += this.KeyUpEvent;

            UI.ShowSubtitle("Press [F10] to start first race", 1250);
        }

        #region Events

        // OnTick Event
        public void OnTickEvent(object sender, EventArgs e)
        {
            SizeF res = UIMenu.GetScreenResolutionMantainRatio();
            Point safe = UIMenu.GetSafezoneBounds();
            /*
            *   SET_PED_CAN_BE_SHOT_IN_VEHICLE
            *   make it so that AI can not be shot
            */

            if (race_started &&
                (Game.Player.IsDead ||
                Function.Call<Boolean>(Hash.IS_PLAYER_BEING_ARRESTED, Game.Player, true))) {
                clearStuffUp();
                resetLoggingVariables();
                return;
            }

            if (Game.Player.Character.IsInVehicle()) {
                logVariables(res, safe);

                if (race_started)
                {
                    if (currentCheckpoint >= 0)
                    {
                        new UIResText(string.Format("currentCheckpoint is {0}/{1}", currentCheckpoint, checkpoints.Length), new Point(Convert.ToInt32(res.Width) - safe.X - 180, Convert.ToInt32(res.Height) - safe.Y - 275), 0.3f, Color.White).Draw();
                    }
                }

                if (race_started && 
                    Game.Player.Character.IsInRangeOf(checkpoints[currentCheckpoint], 5f))
                {
                    // FINISHED, if last checkpoint is reached
                    if ((currentCheckpoint + 1) == checkpoints.Length) {
                        UI.ShowSubtitle(String.Format("Race finished! - Time: {0}s", (Game.GameTime - raceStartTime) / 1000), 3000);
                        Function.Call(Hash.SET_PLAYER_WANTED_LEVEL, Game.Player, 0, false);
                        Function.Call(Hash.SET_PLAYER_WANTED_LEVEL_NOW, Game.Player, false);

                        Game.Player.CanControlCharacter = false;

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

                        raceEndTime = Game.GameTime;

                        Game.Player.CanControlCharacter = true;

                        writeRaceDataToLog();
                        clearStuffUp();
                        return;
                    }

                    // show the players current position
                    UI.ShowSubtitle(string.Format("checkpoint {0}/{1} reached", currentCheckpoint + 1, checkpoints.Length), 3000);
                    // play sound
                    Audio.PlaySoundFrontend("NAV_UP_DOWN", "HUD_FRONTEND_DEFAULT_SOUNDSET");

                    // set next checkpoint
                    Function.Call(Hash.DELETE_CHECKPOINT, currentMarker);
                    currentCheckpoint++;
                    Vector3 coords = checkpoints[currentCheckpoint];
                    Vector3 nextCoords;
                    int type;
                    if (currentCheckpoint < (checkpoints.Length - 1)) {
                        // if there are checkpoints left, get the next one's coordinates
                        nextCoords = checkpoints[currentCheckpoint + 1];
                        type = 2;
                    } else {
                        type = 14;
                        nextCoords = new Vector3(0,0,0);
                        coords.Z = coords.Z + 3f;
                    }
                    
                    currentMarker = Function.Call<int>(Hash.CREATE_CHECKPOINT, 
                        type, // type
                        coords.X,
                        coords.Y,
                        coords.Z - 1,
                        nextCoords.X, // facing next checkpoint?
                        nextCoords.Y,
                        nextCoords.Z,
                        5.0f,    // radius
                        255,    // R
                        155,     // G
                        0,        // B
                        100,    // Alpha
                        0 // number displayed in marker, if type is 42-44
                        );

                    currentBlip.Remove();
                    currentBlip = World.CreateBlip(checkpoints[currentCheckpoint]);
                    Function.Call(Hash.SET_BLIP_ROUTE, currentBlip, true);
                }

                // check which car player is using
                if (Game.Player.Character.CurrentVehicle.Equals(vehicles[1]))
                {
                    new UIResText("Car with good acceleration", new Point(Convert.ToInt32(res.Width) - safe.X - 180, Convert.ToInt32(res.Height) - safe.Y - 250), 0.3f, Color.White).Draw();
                    if (!copsCalled) {
                        Function.Call(Hash.SET_PLAYER_WANTED_LEVEL, Game.Player, 3, false);
                        Function.Call(Hash.SET_PLAYER_WANTED_LEVEL_NOW, Game.Player, false);
                        vehicles[1].StartAlarm();
                        copsCalled = true;
                        car = "Player is in fast car";
                    }
                    playerInRaceCar = true;
                }
                if (Game.Player.Character.CurrentVehicle.Equals(vehicles[0]))
                {
                    new UIResText("Car with good traction", new Point(Convert.ToInt32(res.Width) - safe.X - 180, Convert.ToInt32(res.Height) - safe.Y - 200), 0.3f, Color.White).Draw();
                    playerInRaceCar = true;
                    car = "Player is in car with good traction";
                }

                // start the race and set first marker + blip
                if (playerInRaceCar &&
                    !race_started) {
                    race_started = true;
                    UI.ShowSubtitle("Race started!", 1250);

                    // don't let player exit his racecar by conventional means
                    Game.DisableControl(0, GTA.Control.VehicleExit);

                    // disable shooting from car?
                    Game.DisableControl(0, GTA.Control.VehiclePassengerAim);

                    // select the first checkpoint
                    Vector3 coords = checkpoints[currentCheckpoint];
                    Vector3 nextCoords;
                    if (currentCheckpoint < (checkpoints.Length - 1))
                    {
                        // if there are checkpoints left, get the next one's coordinates
                        nextCoords = checkpoints[currentCheckpoint + 1];
                    }
                    else {
                        nextCoords = new Vector3(0, 0, 0);
                    }
                    currentMarker = Function.Call<int>(Hash.CREATE_CHECKPOINT,
                        2, // type
                        coords.X,
                        coords.Y,
                        coords.Z - 1,
                        nextCoords.X, // facing next checkpoint?
                        nextCoords.Y,
                        nextCoords.Z,
                        5.0f,    // radius
                        255,    // R
                        155,     // G
                        0,        // B
                        100,    // Alpha
                        0
                        );
                    currentBlip = World.CreateBlip(checkpoints[currentCheckpoint]);

                    if (currentCheckpoint == (checkpoints.Length - 1)) {
                        currentBlip.Sprite = BlipSprite.RaceFinish;
                    }

                    Function.Call(Hash.SET_BLIP_ROUTE, currentBlip, true);

                    raceStartTime = Game.GameTime;
                }
                
            }
        }

        // KeyDown Event
        public void KeyDownEvent(object sender, KeyEventArgs e)
        {
            // Check KeyDown KeyCode
            switch (e.KeyCode)
            {
                case Keys.E:
                    UI.ShowSubtitle("[E] KeyDown", 1250);
                    break;
                case Keys.S:
                    lastTimeBrake = Game.GameTime;
                    break;
                case Keys.Space:
                    lastTimeHandbrake = Game.GameTime;
                    break;
                default:
                    break;
            }
        }

        // KeyUp Event
        public void KeyUpEvent(object sender, KeyEventArgs e)
        {
            // Check KeyUp KeyCode
            switch (e.KeyCode)
            {
                case Keys.E:
                    UI.ShowSubtitle("[E] KeyUp", 1250);
                    break;
                case Keys.F10:
                    UI.ShowSubtitle("trying to call race", 1250);
                    initFirstRace();
                    break;
                case Keys.F11:
                    UI.ShowSubtitle("Teleport Player to customization", 1250);
                    teleportPlayerToCarCustomization();
                    break;
                case Keys.S:
                    if (lastTimeBrake > 0) {
                        numBrakeApplied++;
                        cumulativeTimeBraking += (Game.GameTime - lastTimeBrake);
                        lastTimeBrake = 0;
                    }
                    break;
                case Keys.Space:
                    if (lastTimeHandbrake > 0)
                    {
                        numHandBrakeApplied++;
                        cumulativeTimeHandbraking += (Game.GameTime - lastTimeHandbrake);
                        lastTimeHandbrake = 0;
                    }
                    break;
                default:
                    break;
            }
        }

        // Dispose Event
        protected override void Dispose(bool A_0)
        {
            if (A_0)
            {
                //remove any ped,vehucle,Blip,prop,.... that you create
                clearStuffUp();
            }
        }

        protected void logVariables(SizeF res, Point safe) {

            // logging some variables
            int currentTimeSinceHitVehicle = Function.Call<int>(Hash.GET_TIME_SINCE_PLAYER_HIT_VEHICLE, Game.Player);
            int currentTimeSinceHitPed = Function.Call<int>(Hash.GET_TIME_SINCE_PLAYER_HIT_PED, Game.Player);
            int currentTimeSinceDrivingOnPavement = Function.Call<int>(Hash.GET_TIME_SINCE_PLAYER_DROVE_ON_PAVEMENT, Game.Player);
            int currentTimeSinceDrivingAgainstTraffic = Function.Call<int>(Hash.GET_TIME_SINCE_PLAYER_DROVE_AGAINST_TRAFFIC, Game.Player);

            var currentSpeed = Game.Player.Character.CurrentVehicle.Speed;

            speeds += (int)Math.Round(currentSpeed);
            numOfSpeeds++;

            if (currentSpeed > maxSpeed) {
                maxSpeed = (int)Math.Round(currentSpeed);
            }

            new UIResText(String.Format("average speed: {0}", Math.Round((float)speeds / (float)numOfSpeeds)), new Point(Convert.ToInt32(res.Width) - safe.X - 300, Convert.ToInt32(res.Height) - safe.Y - 475), 0.3f, Color.White).Draw();
            new UIResText(String.Format("speed: {0}", Math.Round(Game.Player.Character.CurrentVehicle.Speed)), new Point(Convert.ToInt32(res.Width) - safe.X - 180, Convert.ToInt32(res.Height) - safe.Y - 400), 0.3f, Color.White).Draw();
            new UIResText(String.Format("speed (km/h?): {0}", Math.Round(Game.Player.Character.CurrentVehicle.Speed * mTokm)), new Point(Convert.ToInt32(res.Width) - safe.X - 250, Convert.ToInt32(res.Height) - safe.Y - 425), 0.3f, Color.White).Draw();

            // if the timer was reset, there was a collision
            if (currentTimeSinceHitVehicle < lastMaxTimeSinceHitVehicle)
            {
                numOfHitVehicles++;
            }
            // either way, save new timer
            lastMaxTimeSinceHitVehicle = currentTimeSinceHitVehicle;

            // if the timer was reset, there was a collision
            if (currentTimeSinceHitPed < lastMaxTimeSinceHitPed)
            {
                numOfHitPeds++;
            }
            // either way, save new timer
            lastMaxTimeSinceHitPed = currentTimeSinceHitPed;

            // player is currently driving on pavement
            if (currentTimeSinceDrivingOnPavement == 0)
            {
                // start counter
                if (startedDrivingOnPavement == 0)
                {
                    startedDrivingOnPavement = Game.GameTime;
                }
                // show status
                new UIResText(
                    String.Format(
                        "on pavement for {0}s",
                        Math.Round(((float)(Game.GameTime - startedDrivingOnPavement) / 1000), 1)),
                    new Point(Convert.ToInt32(res.Width) - safe.X - 180,
                    Convert.ToInt32(res.Height) - safe.Y - 350),
                    0.3f,
                    Color.OrangeRed
                    ).Draw();
            }
            else if (currentTimeSinceDrivingOnPavement > 0)
            { // player drove on pavement, but isn't any longer
                if (startedDrivingOnPavement > 0)
                {
                    // add the time interval
                    cumulativeTimeOnPavement += Game.GameTime - startedDrivingOnPavement;
                    // reset counter
                    startedDrivingOnPavement = 0;
                }
            }

            // if the timer was reset, player drove on pavement
            if (currentTimeSinceDrivingOnPavement < lastMaxTimeSincePavement)
            {
                numOfTimesDrivingOnPavement++;
            }
            // either way, save new timer
            lastMaxTimeSincePavement = currentTimeSinceDrivingOnPavement;

            // player is currently driving against traffic
            if (currentTimeSinceDrivingAgainstTraffic == 0)
            {
                // start counter
                if (startedDrivingAgainstTraffic == 0)
                {
                    startedDrivingAgainstTraffic = Game.GameTime;
                }
                // show status
                new UIResText(
                    String.Format(
                        "against traffic {0}s",
                        Math.Round(((float)(Game.GameTime - startedDrivingAgainstTraffic) / 1000), 1)),
                    new Point(Convert.ToInt32(res.Width) - safe.X - 180,
                    Convert.ToInt32(res.Height) - safe.Y - 375),
                    0.3f,
                    Color.OrangeRed
                    ).Draw();
            }
            else if (currentTimeSinceDrivingAgainstTraffic > 0)
            { // player drove on pavement, but isn't any longer
                if (startedDrivingAgainstTraffic > 0)
                {
                    // add the time interval
                    cumulativeTimeDrivingAgainstTraffic += Game.GameTime - startedDrivingAgainstTraffic;
                    // reset counter
                    startedDrivingAgainstTraffic = 0;
                }
            }

            // if the timer was reset, player drove against traffic
            if (currentTimeSinceDrivingAgainstTraffic < lastMaxTimeSinceAgainstTraffic)
            {
                numOfTimesDrivingAgaingstTraffic++;
            }
            // either way, save new timer
            lastMaxTimeSinceAgainstTraffic = currentTimeSinceDrivingAgainstTraffic;

            checkForRedlights(res, safe);
        }

        protected Boolean checkForRedlights(SizeF res, Point safe) {
            // get forward vector to check for traffic lights in front of car
            var fv = Game.Player.Character.CurrentVehicle.ForwardVector;
            var pos = Game.Player.Character.CurrentVehicle.Position;

            foreach (Entity ent in World.GetNearbyEntities(Game.Player.Character.Position, 50))
            {
                if (trafficSignalHashes.Contains(ent.Model.Hash) &&
                    Math.Abs(ent.Heading - Game.Player.Character.CurrentVehicle.Heading) < 70 &&
                    ent.IsInArea(pos, pos + (fv * 50f), 50f))
                {
                    // do something with that info
                    // ent.ForwardVector
                    // TODO: span vector v* between player and entity and calculate angle between v* and Game.Player.Character.ForwardVector
                    var dist = World.GetDistance(Game.Player.Character.Position, ent.Position);
                    new UIResText(
                        String.Format(
                            "traffic light is near at {0}, heading {1}, position {2}, is playing anim {3}",
                            dist,
                            ent.Heading,
                            ent.Position,
                            Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, ent, "v_traffic_lights", "prop_trafficdiv_02_uv_5", 3)
                        ),
                        new Point(Convert.ToInt32(res.Width) - safe.X - 300,
                        Convert.ToInt32(res.Height) - safe.Y - 900),
                        0.3f,
                        Color.Aqua
                    ).Draw();

                    World.DrawMarker(MarkerType.VerticalCylinder, ent.Position, new Vector3(0, 0, 0), new Vector3(0, 0, 0), new Vector3(5f, 5f, 1f), Color.Aqua);
                    return true;
                }
            }
            return false;
        }

        protected void initFirstRace() {
            UI.ShowSubtitle("initializing first race", 1250);
            World.CurrentDayTime = new TimeSpan(9, 0, 0);
            Function.Call(Hash.SET_WEATHER_TYPE_NOW_PERSIST, "RAIN");
            Ped player = Game.Player.Character;
            player.Task.ClearAllImmediately(); // give back control to player

            // teleport player and turn him towards cars
            Game.Player.Character.Position = car_selection;
            Game.Player.Character.Heading = car_spawn_player_heading;

            /*

                stored for later, maybe create some props?

                    Object CREATE_OBJECT(Hash modelHash, float x, float y, float z, BOOL networkHandle,
  BOOL createHandle, BOOL dynamic)

                BOOL PLACE_OBJECT_ON_GROUND_PROPERLY(Object object)    

            */
            // set up everything for logging
            resetLoggingVariables();

            // add some checkpoints for our race
            checkpoints = new Vector3[19];
            checkpoints[0] = new Vector3(-807.8585f, -2466.344f, 14.45607f);
            checkpoints[1] = new Vector3(-810.6682f, -2249.965f, 17.24915f);
            checkpoints[2] = new Vector3(-693.1211f, -2117.945f, 13.12339f);
            checkpoints[3] = new Vector3(-382.7377f, -1838.06f, 21.37794f);
            checkpoints[4] = new Vector3(-246.3216f, -1826.909f, 28.96538f);
            checkpoints[5] = new Vector3(-144.3558f, -1749.146f, 30.12419f);
            checkpoints[6] = new Vector3(-44.43723f, -1630.049f, 28.96328f);
            checkpoints[7] = new Vector3(64.23303f, -1515.456f, 28.93484f);
            checkpoints[8] = new Vector3(138.5995f, -1370.135f, 28.83117f);
            checkpoints[9] = new Vector3(117.2407f, -1356.73f, 28.88704f);
            checkpoints[10] = new Vector3(64.65392f, -1285.516f, 29.33747f);
            checkpoints[11] = new Vector3(64.58669f, -1160.968f, 28.951f);
            checkpoints[12] = new Vector3(96.58853f, -1026.16f, 29.03582f);
            checkpoints[13] = new Vector3(82.72692f, -982.808f, 29.01929f);
            checkpoints[14] = new Vector3(-11.1925f, -931.7217f, 28.90791f);
            checkpoints[15] = new Vector3(32.22876f, -773.1832f, 43.85289f);
            checkpoints[16] = new Vector3(-65.03999f, -725.1259f, 43.86914f);
            checkpoints[17] = new Vector3(-76.04148f, -749.7919f, 43.77972f);
            checkpoints[18] = race1End;
            currentCheckpoint = 0;

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

                Function.Call(Hash.SET_VEHICLE_CUSTOM_PRIMARY_COLOUR, vehicle2, 255,0,0);
                Function.Call(Hash.SET_VEHICLE_CUSTOM_SECONDARY_COLOUR, vehicle2, 255, 50, 0);

                Function.Call(Hash.SET_VEHICLE_DOORS_LOCKED, vehicle2, 4);

                Function.Call(Hash.SET_VEHICLE_INTERIORLIGHT, vehicle1, true);

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
                );
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

        protected void teleportPlayerToCarCustomization() {
            if (Game.Player.Character.IsSittingInVehicle() && !car_config_done)
            {
                Game.Player.LastVehicle.Position = new Vector3(-1140.731f, -1985.894f, 12.78301f);
                Game.Player.LastVehicle.Rotation = new Vector3(0.0f, 0.0f, 135.0f);
                Logger.Log("Player is entering garage");
                Game.Player.Character.Task.DriveTo(
                    Game.Player.LastVehicle,
                    new GTA.Math.Vector3(-1147.906f, -1993.416f, 12.7937f),
                    1.0f,
                    5.0f
                );
                
                //Game.Player.LastVehicle.Position = new Vector3(-1156.724f, -2007.222f, 12.79617f);
            }
        }

        protected void clearStuffUp() {
            Ped player = Game.Player.Character;
            player.Task.ClearAllImmediately(); // give back control to player

            // clear vehicles
            foreach (Vehicle car in vehicles)
            {
                car.MarkAsNoLongerNeeded();
                car.Delete();
            }

            vehicles = new List<Vehicle>();

            // clear map blip
            currentBlip.Remove();
            currentBlip = null;

            // delete 3D marker
            Function.Call(Hash.DELETE_CHECKPOINT, currentMarker);
            currentMarker = -1;

            race_started = false;
            currentCheckpoint = -1;

            resetLoggingVariables();
            Wait(3000);
            UI.ShowSubtitle("Everything reset", 3000);
        }

        protected void writeRaceDataToLog() {
            Logger.Log("--------------------------------");
            Logger.Log(car);
            Logger.Log(String.Format("race started: {0}ms", raceStartTime));
            Logger.Log(String.Format("race ended: {0}ms", raceEndTime));
            Logger.Log(String.Format("time taken: {0}s", Math.Round((float)(raceEndTime - raceStartTime) / 1000, 2)));
            Logger.Log(String.Format("average speed: {0}m/h", Math.Round((float)speeds/(float)numOfSpeeds)));
            Logger.Log(String.Format("average speed: {0}km/h", Math.Round(((float)speeds / (float)numOfSpeeds) * mTokm)));
            Logger.Log(String.Format("maximum speed: {0}m/h", maxSpeed));
            Logger.Log(String.Format("maximum speed: {0}km/h", Math.Round((float)maxSpeed * mTokm)));
            Logger.Log(String.Format("Number of times player applied brakes: {0}", numBrakeApplied));
            Logger.Log(String.Format("Number of times player applied handbrake: {0}", numHandBrakeApplied));
            Logger.Log(String.Format("Cumulative time spent braking: {0}s", Math.Round((float)cumulativeTimeBraking/1000, 2)));
            Logger.Log(String.Format("Cumulative time spent on handbrake: {0}s", Math.Round((float)cumulativeTimeHandbraking/1000, 2)));
            Logger.Log(String.Format("Vehicle collisions: {0}", numOfHitVehicles));
            Logger.Log(String.Format("Pedestrian collisions: {0}", numOfHitPeds));
            Logger.Log(String.Format("Number of times player has driven against traffic: {0}", numOfTimesDrivingAgaingstTraffic));
            Logger.Log(String.Format("Number of times player has driven against on pavement: {0}", numOfTimesDrivingOnPavement));
            Logger.Log(String.Format("Cumulative time on pavement: {0}", Math.Round((float)cumulativeTimeOnPavement/1000, 2)));
            Logger.Log(String.Format("Cumulative time driving against traffic: {0}", Math.Round((float)cumulativeTimeDrivingAgainstTraffic/1000, 2)));
        }

        protected void resetLoggingVariables() {
            // reset logging variables
            speeds = 0;
            numOfSpeeds = 0;
            maxSpeed = 0;

            numBrakeApplied = 0;
            numHandBrakeApplied = 0;
            lastTimeBrake = 0;
            lastTimeHandbrake = 0;
            cumulativeTimeBraking = 0;
            cumulativeTimeHandbraking = 0;

            lastMaxTimeSinceHitVehicle = -1;
            lastMaxTimeSinceHitPed = -1;
            lastMaxTimeSincePavement = -1;
            lastMaxTimeSinceAgainstTraffic = -1;
            numOfHitVehicles = 0;
            numOfHitPeds = 0;
            numOfTimesDrivingOnPavement = 0;
            numOfTimesDrivingAgaingstTraffic = 0;

            startedDrivingOnPavement = 0;
            startedDrivingAgainstTraffic = 0;

            raceStartTime = -1;
            raceEndTime = -1;

            cumulativeTimeDrivingAgainstTraffic = 0;
            cumulativeTimeOnPavement = 0;
        }

        #endregion
    }
}