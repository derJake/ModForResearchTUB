#region Using references

using System;
using GTA.Native;
using GTA;
using GTA.Math;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;
using NativeUI;
using System.Linq;
using System.Globalization;
using ModForResearchTUB.Properties;
using System.Resources;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.ComponentModel;
#endregion

namespace ModForResearchTUB
{
    public class Main : Script
    {
        #region variables
        // Variables
        List<int> trafficSignalHashes = new List<int>(3);
        Blip currentBlip = null, currentAltBlip = null, nextBlip = null, nextAltBlip = null;
        Tuple<Vector3, Vector3?>[] checkpoints;
        float checkpoint_radius = 5;
        int currentMarker;
        int currentAlternativeMarker;
        Tuple<int, int, int> regular_checkpoint_color = new Tuple<int, int, int>(255, 155, 0);
        Tuple<int, int, int> alternative_checkpoint_color = new Tuple<int, int, int>(255, 0, 0);
        bool car_config_done = false;
        bool race_started = false;
        bool race_initialized = false;
        bool abort_race = false;
        bool race_has_on_tick = true;

        int currentCheckpoint = 0;
        bool altCheckpointAvailable = false;

        int currentRace = -1;
        RaceInterface[] races;

        float speeds;
        int numOfSpeeds;
        float maxSpeed;

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

        List<Vehicle> knownVehicles = new List<Vehicle>();
        int possibleCollisionRadius = 10;
        int possibleCollisions = 0;
        int distanceMeasurements = 0;
        float meanDistance = 0;

        int numOfHitVehicles;
        int numOfHitPeds;
        int numOfTimesDrivingOnPavement;
        int numOfTimesDrivingAgaingstTraffic;

        int numOfCollisions = 0;
        int numOfDamagedProps = 0;

        int startedDrivingOnPavement;
        int startedDrivingAgainstTraffic;

        int lastTimeUpsideDown = -1;
        int numOfTimesUpsideDown = 0;

        int cumulativeTimeOnPavement;
        int cumulativeTimeDrivingAgainstTraffic;
        int raceStartTime;
        int raceEndTime;
        int lastCheckointTime;

        int car_health;

        bool raceCarHasBlip = false;
        Blip raceCarBlip;

        String currentPlayerName = "";

        List<Tuple<Entity, Vehicle>> carsStoppedNearestToTrafficLights = new List<Tuple<Entity, Vehicle>>();
        Entity lastRedlight;
        Vehicle lastNearestVehicleToRedlight;
        float lastNearestVehicleDistance = 0;
        int numOfRedlights = 0;
        float checkDistance = 50f;
        float stoppedRadius = 50f;

        //Dictionary<Keys, int> controls = new Dictionary<Keys, int>(){
        //    {Keys.W, 0}
        //};

        // length of time the key was held down and amount of times it was held that long
        static double roundTo = 5;
        static int roundToMultiplier = Convert.ToInt32(roundTo);
        Dictionary<string, double> keypressLengths = new Dictionary<string, double>();
        int lastKeydownA;
        int lastKeydownD;

        // 
        Dictionary<string, double> speedBySecond = new Dictionary<string, double>();

        // controller inputs
        Dictionary<string, double> gasPedalInputs = new Dictionary<string, double>();
        Dictionary<string, double> steeringInputs = new Dictionary<string, double>();
        Dictionary<string, double> brakingInputs = new Dictionary<string, double>();

        private Dictionary<string, Dictionary<string, double>> collectedData = new Dictionary<string, Dictionary<string, double>>();

        protected String[] scenarioGroups { get; private set; }
        protected String[] scenarios { get; private set; }

        public CultureInfo CultureInfo { get; private set; }

        private Utilities ut = new Utilities();

        // UI
        ResourceManager rm;

        BigMessageHandler bmsg;
        int countdown_interval = 2000;

        private UIMenu myMenu;
        private MenuPool _myMenuPool = new MenuPool();

        // modding tools
        private bool route_designer_active = false,
            cam_designer_active = false,
            debug = true;
        private List<Tuple<Vector3, int, Blip>> route_checkpoints;

        Camera designer_cam;
        float cam_movement_amount = 0.8f;

        private directorGUI director_gui;
        public String director_cam_position = "",
            director_cam_rotation = "";

        // route deviation
        private float off_track_distance = 50;
        private int time_player_got_lost,
            max_lost_time = 10000;

        // database
        private DBI database_interface;
        private int current_data_set_id;

        #endregion

        // Main Script
        public Main()
        {
            trafficSignalHashes.Add(-655644382);
            trafficSignalHashes.Add(862871082);
            trafficSignalHashes.Add(1043035044);

            CultureInfo = CultureInfo.CurrentCulture;
            rm = new ResourceManager(typeof(Resources));
            bmsg = BigMessageThread.MessageInstance;

            // registers the races / courses / whatever you want to call it
            setUpRaces();

            // try to disable scenarios like angry bikers
            setScenarioList();
            setScenarioGroupList();
            toggleScenarios(false);

            // World.CreateProp(new Model(-1359996601), Game.Player.Character.Position, new Vector3(0f, 5f, 0f), false, false);

            // Tick Interval
            //Interval = 10;

            buildMenu();

            // Initialize Events
            Tick += this.OnTickEvent;
            KeyDown += this.KeyDownEvent;
            KeyUp += this.KeyUpEvent;

            //UI.ShowSubtitle("Press [F10] to start first race", 1250);
            UI.ShowSubtitle(rm.GetString("startracepromp", CultureInfo));

            director_gui = new directorGUI();
            director_gui.ut = ut;

            BackgroundWorker myWorker = new BackgroundWorker();
            myWorker.DoWork += (sender, e) =>
            {
                try
                {
                    Application.EnableVisualStyles();
                    Application.Run(director_gui);
                }
                catch (Exception ex)
                {
                    Logger.Log(ex.ToString());
                }
            };
            myWorker.RunWorkerAsync();

            database_interface = new DBI();
        }

        private void setUpRaces() {
            races = new RaceInterface[5];
            races[0] = new RaceIntro(rm, ut, "intro");
            races[1] = new RaceConvoy(rm, ut, "convoy");
            races[2] = new RaceSuburban(rm, ut, "garbagetruck");
            races[3] = new RaceDesert(rm, ut, "desert");
            races[4] = new RaceCarvsCar(rm, ut, "car_vs_car");
            //races[5] = new RaceToWoodmill();
            currentRace = 0;
            if (debug) {
                UI.Notify(rm.GetString("racessetup"));
            }
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
            Function.Call(Hash.CLEAR_ALL_HELP_MESSAGES);

            _myMenuPool.ProcessMenus();

            if (race_started) {
                // player has died -> abort race
                if (Game.Player.IsDead) {
                    abort_race = true;
                    Logger.Log("player died");
                }
                // player is being arrested -> abort race
                if (Function.Call<Boolean>(Hash.IS_PLAYER_BEING_ARRESTED, Game.Player, true)) {
                    abort_race = true;
                    Logger.Log("player was arrested");
                }

                // reset everything after aborting race
                if (abort_race)
                {
                    clearStuffUp();
                    resetLoggingVariables();
                    return;
                }
            }

            // check if player is still in his vehicle and mark it on the map otherwise
            toggleRaceCarBlip();

            if (Game.Player.Character.IsInVehicle()) {
                if (race_started)
                {
                    // call race specific onTick instructions
                    if (race_has_on_tick)
                    {
                        try
                        {
                            races[currentRace].handleOnTick(sender, e);
                        }
                        catch (NotImplementedException)
                        {
                            race_has_on_tick = false;
                        }
                    }

                    // display how many checkpoints there are
                    if (debug && checkpoints != null)
                    {
                        new UIResText(String.Format("checkpoints: {0}", checkpoints.Length), new Point((Convert.ToInt32(res.Width) - safe.X - 250), 75), 0.3f, Color.White).Draw();
                    }

                    // display what the current race is and how many there are
                    new UIResText(String.Format("race {0}/{1}", currentRace + 1, races.Length), new Point((Convert.ToInt32(res.Width) - safe.X - 250), 50), 0.3f, Color.White).Draw();

                    // log speed, collisions, brakes, etc.
                    logVariables(res, safe);

                    // have stay player in car and not shoot at things
                    disableUnwantedControls();

                    // display what the next checkpoint to be reached is
                    if (currentCheckpoint >= 0)
                    {
                        new UIResText(string.Format("currentCheckpoint is {0}/{1}", currentCheckpoint, checkpoints.Length), new Point(Convert.ToInt32(res.Width) - safe.X - 180, Convert.ToInt32(res.Height) - safe.Y - 275), 0.3f, Color.White).Draw();
                    }

                    // debug stuff
                    if (debug)
                    {
                        new UIResText(String.Format("altCheckpointAvailable: {0}", altCheckpointAvailable), new Point((Convert.ToInt32(res.Width) - safe.X - 250), 100), 0.3f, Color.White).Draw();
                        if (altCheckpointAvailable)
                        {
                            var coords = checkpoints[currentCheckpoint].Item2.Value;
                            new UIResText(String.Format("coords: {0}, {1}, {2}", coords.X, coords.Y, coords.Z), new Point((Convert.ToInt32(res.Width) - safe.X - 350), 125), 0.3f, Color.White).Draw();
                            new UIResText(String.Format("distance: {0}", World.GetDistance(coords, Game.Player.Character.Position)), new Point((Convert.ToInt32(res.Width) - safe.X - 250), 145), 0.3f, Color.AntiqueWhite).Draw();
                        }
                    }

                    // check if player is near (alternative) checkpoint
                    if (Game.Player.Character.IsInRangeOf(checkpoints[currentCheckpoint].Item1, checkpoint_radius) ||
                        (checkpoints[currentCheckpoint].Item2.HasValue && Game.Player.Character.IsInRangeOf(checkpoints[currentCheckpoint].Item2.Value, checkpoint_radius))
                         || races[currentRace].checkAlternativeBreakCondition())
                    {
                        // play sound
                        Audio.PlaySoundFrontend("NAV_UP_DOWN", "HUD_FRONTEND_DEFAULT_SOUNDSET");

                        // show the players current position
                        UI.ShowSubtitle(string.Format("checkpoint {0}/{1} reached", currentCheckpoint + 1, checkpoints.Length), 3000);
                        UI.Notify(string.Format("checkpoint {0}/{1} reached", currentCheckpoint + 1, checkpoints.Length));

                        // make log entry
                        var altCheckpointReached = (checkpoints[currentCheckpoint].Item2.HasValue && Game.Player.Character.IsInRangeOf(checkpoints[currentCheckpoint].Item2.Value, checkpoint_radius)) ? "alternative " : "";
                        Logger.Log(altCheckpointReached + String.Format("checkpoint {0}/{1}", currentCheckpoint + 1, checkpoints.Length));
                        var intervalStart = lastCheckointTime > 0 ? lastCheckointTime : raceStartTime;
                        Logger.Log(String.Format("time elapsed last: {0}s", Math.Round(((float)Game.GameTime - (float)intervalStart) / 1000, 2)));
                        Logger.Log(String.Format("time elapsed overall: {0}s", Math.Round(((float)Game.GameTime - (float)raceStartTime) / 1000, 2)));
                        lastCheckointTime = Game.GameTime;

                        // FINISHED, if last checkpoint is reached
                        if ((currentCheckpoint + 1) == checkpoints.Length || races[currentRace].checkAlternativeBreakCondition())
                        {
                            // save race car's health
                            car_health = Game.Player.Character.CurrentVehicle.Health;

                            // have current race do it's finish stuff
                            races[currentRace].finishRace();
                            Function.Call(Hash.CLEAR_GPS_PLAYER_WAYPOINT);
                            Function.Call(Hash.SET_WAYPOINT_OFF);

                            processCollectedData();

                            // log the current time
                            raceEndTime = Game.GameTime;

                            writeRaceDataToLog();
                            writeRaceDataToDB();

                            renderDiagrams();

                            // reset variables and remove vehicles/props etc.
                            clearStuffUp();

                            // switch to next race, if there is one
                            if (currentRace < (races.Length - 1)) {
                                ++currentRace;
                                checkpoints = races[currentRace].getCheckpoints();
                                races[currentRace].initRace();
                            } else {
                                toggleScenarios(true);
                                UI.Notify(rm.GetString("last_race"));
                            }
                            return;
                        }

                        currentCheckpoint++;
                        setupNextCheckpoint();
                    }

                    keepPlayerOnTrack();
                    hideUIComponents();
                }
                else if (currentRace >= 0 &&
                    currentRace < races.Length &&
                    races[currentRace] != null &&
                    races[currentRace].checkRaceStartCondition()) {

                    // show countdown
                    countDown();

                    UI.Notify(String.Format(rm.GetString("race_started"), currentRace + 1, races.Length));
                    // start the race and set first marker + blip
                    race_started = true;

                    // heal player before start of race
                    Game.Player.Character.Health = 100;
                    raceStartTime = Game.GameTime;

                    // separator to show start of new log
                    Logger.Log("----------------------------------------------------------");

                    setupNextCheckpoint();
                    races[currentRace].startRace();
                }
            }

            // stop bringing up phone on arrow keys
            if (cam_designer_active) {
                Function.Call(Hash.DESTROY_MOBILE_PHONE);
                debugCamDesigner();
            }

            if (route_designer_active) {
                handleRouteDesigner(res, safe);
            }
        }

        protected void renderDiagrams() {
            foreach (KeyValuePair<string, Dictionary<string, double>> item in collectedData) {
                DrawDiagram.renderDiagramToDisk(
                    item.Value,
                    item.Key,
                    item.Key,
                    currentPlayerName + "-race-" + races[currentRace].getCanonicalName() + "-" + item.Key
                );
            }
        }

        protected void setupNextCheckpoint() {
            if (currentMarker > 0)
            {
                Function.Call(Hash.DELETE_CHECKPOINT, currentMarker);
            }

            if (currentAlternativeMarker > 0)
            {
                Function.Call(Hash.DELETE_CHECKPOINT, currentAlternativeMarker);
            }

            Vector3? nextCoords = null;
            int type = 14; // finish checkpoint
            if (currentCheckpoint < (checkpoints.Length - 1)) {
                nextCoords = checkpoints[currentCheckpoint + 1].Item1;
                type = 2;
            }
            // draw a regular checkpoint
            currentMarker = drawCurrentCheckpoint(
                checkpoints[currentCheckpoint].Item1,
                nextCoords,
                255,
                155,
                0,
                type
            );

            // set the map blip
            setCurrentBlip(checkpoints[currentCheckpoint].Item1, nextCoords);

            // alternative (dangerous) checkpoint
            if (checkpoints[currentCheckpoint].Item2.HasValue)
            {

                // debug stuff
                altCheckpointAvailable = true;

                // if the alternative route isn't finished, point to the next alternative checkpoint
                if (currentCheckpoint < (checkpoints.Length - 1) &&
                    checkpoints[currentCheckpoint + 1].Item2.HasValue)
                {
                    nextCoords = checkpoints[currentCheckpoint + 1].Item2.Value;
                }

                // draw alternative checkpoint in red
                currentAlternativeMarker = drawCurrentCheckpoint(
                    checkpoints[currentCheckpoint].Item2.Value,
                    nextCoords,
                    255,
                    0,
                    0,
                    type
                );

                setCurrentAltBlip(checkpoints[currentCheckpoint].Item2.Value, nextCoords);
            }
            else {
                altCheckpointAvailable = false;
            }
        }

        protected void setCurrentBlip(Vector3 coords, Vector3? nextCoords) {
            // create /replace a blip on the map
            if (currentBlip != null)
            {
                currentBlip.Remove();
            }

            if (nextBlip != null)
            {
                nextBlip.Remove();
            }

            currentBlip = World.CreateBlip(coords);
            ut.addBlip(currentBlip);
            Function.Call(Hash.SET_BLIP_ROUTE, currentBlip, true);
            Function.Call(Hash.SHOW_NUMBER_ON_BLIP, currentBlip, currentCheckpoint + 1);

            if (nextCoords.HasValue) {
                nextBlip = World.CreateBlip(nextCoords.Value);
                ut.addBlip(nextBlip);
                Function.Call(Hash.SHOW_NUMBER_ON_BLIP, nextBlip, currentCheckpoint + 2);
            }
        }

        protected void setCurrentAltBlip(Vector3 coords, Vector3? nextCoords) {
            // create /replace a blip on the map
            if (currentAltBlip != null)
            {
                currentAltBlip.Remove();
            }

            if (nextAltBlip != null)
            {
                nextAltBlip.Remove();
            }
            currentAltBlip = World.CreateBlip(coords);
            ut.addBlip(currentAltBlip);
            currentAltBlip.Color = BlipColor.Red;
            Function.Call(Hash.SET_NEW_WAYPOINT, coords.X, coords.Y);
            Function.Call(Hash.SHOW_NUMBER_ON_BLIP, currentAltBlip, currentCheckpoint + 1);

            if (nextCoords.HasValue)
            {
                nextAltBlip = World.CreateBlip(nextCoords.Value);
                ut.addBlip(nextAltBlip);
                nextAltBlip.Color = BlipColor.Red;
                Function.Call(Hash.SHOW_NUMBER_ON_BLIP, nextAltBlip, currentCheckpoint + 2);
            }
        }

        protected int drawCurrentCheckpoint(Vector3 coords, Vector3? possibleNextCoords, int R, int G, int B, int type) {
            Vector3 nextCoords = new Vector3();
            // set next checkpoint

            // set graphics depending on wether it's the last checkpoint or not
            if (possibleNextCoords.HasValue) {
                nextCoords = possibleNextCoords.Value;
            } else {
                coords.Z = coords.Z + 3f;
                if (currentBlip != null) {
                    currentBlip.Sprite = BlipSprite.RaceFinish;
                }
            }

            // actually create the 3D marker
            return Function.Call<int>(Hash.CREATE_CHECKPOINT,
                type, // type
                coords.X,
                coords.Y,
                coords.Z - 1,
                nextCoords.X, // facing next checkpoint?
                nextCoords.Y,
                nextCoords.Z,
                checkpoint_radius,    // radius
                R,    // R
                G,     // G
                B,        // B
                100,    // Alpha
                0 // number displayed in marker, if type is 42-44
                );
        }

        protected void disableUnwantedControls() {
            // just in case
            // Function.Call(Hash.STOP_PLAYER_SWITCH); doesn't really stop player from switching

            // disable controls that might interfere with the test
            Game.DisableControl(0, GTA.Control.SelectCharacterFranklin);
            Game.DisableControl(0, GTA.Control.SelectCharacterMichael);
            Game.DisableControl(0, GTA.Control.SelectNextWeapon);
            Game.DisableControl(0, GTA.Control.SelectPrevWeapon);
            Game.DisableControl(0, GTA.Control.SelectWeapon);
            // disable shooting from car?
            Game.DisableControl(0, GTA.Control.AccurateAim);
            Game.DisableControl(0, GTA.Control.VehiclePassengerAim);
            // this actually seems to prevent shooting out of the car's window
            Game.DisableControl(0, GTA.Control.Aim);
            Game.DisableControl(0, GTA.Control.VehicleAim);
            Game.DisableControl(0, GTA.Control.VehicleSelectNextWeapon);
            Game.DisableControl(0, GTA.Control.VehicleSelectPrevWeapon);

            // don't let player exit his racecar by conventional means
            Game.DisableControl(0, GTA.Control.VehicleExit);
        }

        private void toggleRaceCarBlip() {
            if (race_started &&
                !raceCarHasBlip &&
                !Game.Player.Character.IsInVehicle())
            {
                raceCarBlip = Game.Player.LastVehicle.AddBlip();
                raceCarBlip.Color = BlipColor.Blue;
                raceCarHasBlip = true;
            }
            else if (raceCarHasBlip &&
              Game.Player.Character.IsInVehicle()) {
                raceCarBlip.Remove();
                raceCarHasBlip = false;
            }
        }

        // KeyDown Event
        public void KeyDownEvent(object sender, KeyEventArgs e)
        {
            // Check KeyDown KeyCode
            switch (e.KeyCode)
            {
                case Keys.A:
                    if (race_started) {
                        lastKeydownA = Game.GameTime;
                    }
                    break;
                case Keys.D:
                    if (race_started)
                    {
                        lastKeydownD = Game.GameTime;
                    }
                    break;
                case Keys.E:
                    //UI.ShowSubtitle("[E] KeyDown", 1250);
                    break;
                case Keys.S:
                    lastTimeBrake = Game.GameTime;
                    break;
                case Keys.Space:
                    lastTimeHandbrake = Game.GameTime;
                    break;
                case Keys.X:
                    handleRouteInput(false);
                    break;
                case Keys.Y:
                    handleRouteInput(true);
                    break;
                default:
                    break;
            }

            if (cam_designer_active && !myMenu.Visible) {
                handleCamMovement(sender, e);
            }
        }

        // KeyUp Event
        public void KeyUpEvent(object sender, KeyEventArgs e)
        {
            // Check KeyUp KeyCode
            switch (e.KeyCode)
            {
                case Keys.A:
                    if (race_started && lastKeydownA > 0) {
                        var length = (Math.Round((Convert.ToDouble(Game.GameTime) - Convert.ToDouble(lastKeydownA)) / roundTo) * roundToMultiplier).ToString();
                        //UI.ShowSubtitle(String.Format("keyup [A], length {0}", length));
                        if (keypressLengths.ContainsKey(length))
                        {
                            keypressLengths[length]++;
                        }
                        else {
                            keypressLengths.Add(length, 1);
                        }
                    }
                    break;
                case Keys.D:
                    if (race_started && lastKeydownA > 0)
                    {
                        var length = (Math.Round((Convert.ToDouble(Game.GameTime) - Convert.ToDouble(lastKeydownD)) / roundTo) * roundToMultiplier).ToString();
                        //UI.ShowSubtitle(String.Format("keyup [D], length {0}", length));
                        if (keypressLengths.ContainsKey(length))
                        {
                            keypressLengths[length]++;
                        }
                        else {
                            keypressLengths.Add(length, 1);
                        }

                    }
                    break;
                case Keys.E:
                    //UI.ShowSubtitle("[E] KeyUp", 1250);
                    break;
                case Keys.F10:
                    myMenu.Visible = !myMenu.Visible;
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

        private bool makePlayerInputName() {
            Function.Call(Hash.DISPLAY_ONSCREEN_KEYBOARD, 1, "FMMC_MPM_NA", "", "", "", "", "", 30);
            while (Function.Call<int>(Hash.UPDATE_ONSCREEN_KEYBOARD) == 0)
            {
                Function.Call(Hash.DISABLE_ALL_CONTROL_ACTIONS, 0);
                Script.Wait(0);
            }
            String name = Function.Call<String>(Hash.GET_ONSCREEN_KEYBOARD_RESULT);
            if (name == null || name.Length == 0) return false;
            UI.Notify(name);
            currentPlayerName = name;
            current_data_set_id = database_interface.createDataset(currentPlayerName);
            if (debug) {
                UI.Notify(String.Format("Data set: {0}", current_data_set_id));
            }
            return true;
        }

        // Dispose Event
        protected override void Dispose(bool A_0)
        {
            if (A_0)
            {
                //remove any ped,vehicle,Blip,prop,.... that you create
                clearStuffUp();
                ut.cleanUp();
            }
        }

        private void handleRouteInput(bool alternative) {
            if (route_designer_active)
            {
                Vector3 pos = Game.Player.Character.Position;
                if (route_checkpoints.Count > 0)
                {
                    // regular checkpoint
                    if (!alternative)
                    {
                        // see if checkpoint is near and if so, remove it and its blip
                        foreach (Tuple<Vector3, int, Blip> cp in route_checkpoints)
                        {
                            if (World.GetDistance(cp.Item1, pos) <= checkpoint_radius)
                            {
                                var index = route_checkpoints.IndexOf(cp);
                                // delete blip
                                cp.Item3.Remove();
                                // delete 3D marker
                                Function.Call(Hash.DELETE_CHECKPOINT, cp.Item2);

                                // decrease blip numbers for following blips
                                for (int i = index; i < route_checkpoints.Count; i++)
                                {
                                    route_checkpoints.ElementAt(i).Item3.ShowNumber(i);
                                }
                                route_checkpoints.Remove(cp);
                                return;
                            }
                        }
                    } else
                    {
                        // TODO: think of data structure to incorporate alt checkpoints here
                        //foreach (Tuple<Vector3, int, Blip> cp in route_checkpoints) {

                        //}
                    }
                }
                else if (alternative) {
                    return;
                }

                // if there are no checkpoints near, create one
                Blip new_blip = World.CreateBlip(pos);
                int type = 14;
                Vector3? next_coords = new Vector3(0.0f, 0.0f, 0.0f);
                ut.addBlip(new_blip);
                new_blip.Color = BlipColor.Yellow;
                route_checkpoints.Add(
                    new Tuple<Vector3, int, Blip>(
                        pos,
                        drawCurrentCheckpoint(
                            pos,
                            next_coords,
                            regular_checkpoint_color.Item1,
                            regular_checkpoint_color.Item2,
                            regular_checkpoint_color.Item3,
                            type
                        ),
                        new_blip
                    )
                );
                Function.Call(Hash.SHOW_NUMBER_ON_BLIP, new_blip, route_checkpoints.Count);
                renderRouteCheckpoints();

                updateRouteCodeOutput();
            }
        }

        private void handleRouteDesigner(SizeF res, Point safe) {
            new UIResText("press X to toggle a waypoint", new Point(Convert.ToInt32(res.Width/2) - safe.X - 250, Convert.ToInt32(res.Height) - safe.Y - 100), 0.75f, Color.White).Draw();
        }

        private void renderRouteCheckpoints() {

            for (int i = 0; i < route_checkpoints.Count; i++) {
                Vector3? next_coords = null;
                int type = 14;
                // if it's not the last checkpoint, set coordinates to point arrows to
                if (i < route_checkpoints.Count - 1) {
                    UI.Notify(String.Format("checkpoint {0}", i));
                    next_coords = route_checkpoints[i + 1].Item1;
                    type = 2;
                }
                // store values
                Vector3 pos = route_checkpoints[i].Item1;
                Blip blip = route_checkpoints[i].Item3;
                // delete 3D marker
                Function.Call(Hash.DELETE_CHECKPOINT, route_checkpoints[i].Item2);
                // replace checkpoint with tuple containing new marker reference
                route_checkpoints[i] = new Tuple<Vector3, int, Blip>(
                    pos, drawCurrentCheckpoint(
                        route_checkpoints[i].Item1,
                        next_coords,
                        regular_checkpoint_color.Item1,
                        regular_checkpoint_color.Item2,
                        regular_checkpoint_color.Item3,
                        type
                    ),
                    blip
                );
            }
        }

        private void updateRouteCodeOutput() {
            if (route_checkpoints.Count > 0) {
                String route_code = "Tuple<Vector3, Vector3?>[] checkpointlist = { " + Environment.NewLine;
                foreach (Tuple<Vector3, int, Blip> checkpoint in route_checkpoints) {
                    var cp = checkpoint.Item1;
                    route_code += String.Format(
                        "\tnew Tuple<Vector3, Vector3?>(new Vector3({0}f, {1}f, {2}f), null),",
                        cp.X.ToString(CultureInfo.InvariantCulture),
                        cp.Y.ToString(CultureInfo.InvariantCulture),
                        cp.Z.ToString(CultureInfo.InvariantCulture)
                        ) + Environment.NewLine;
                }
                route_code += "};";
                director_gui.SetRouteCodeText(route_code);
            }
        }

        protected void logVariables(SizeF res, Point safe) {

            // logging some variables
            int currentTimeSinceHitVehicle = Function.Call<int>(Hash.GET_TIME_SINCE_PLAYER_HIT_VEHICLE, Game.Player);
            int currentTimeSinceHitPed = Function.Call<int>(Hash.GET_TIME_SINCE_PLAYER_HIT_PED, Game.Player);
            int currentTimeSinceDrivingOnPavement = Function.Call<int>(Hash.GET_TIME_SINCE_PLAYER_DROVE_ON_PAVEMENT, Game.Player);
            int currentTimeSinceDrivingAgainstTraffic = Function.Call<int>(Hash.GET_TIME_SINCE_PLAYER_DROVE_AGAINST_TRAFFIC, Game.Player);

            var currentSpeed = Game.Player.Character.CurrentVehicle.Speed;
            var ud = Game.Player.Character.CurrentVehicle.IsUpsideDown;

            // log time if player's vehicle is upside down
            if (ud
                && lastTimeUpsideDown == -1)
            {
                lastTimeUpsideDown = Game.GameTime;
            }
            // if player's vehicle has been upside down, but is no longer
            else if (!ud &&
                lastTimeUpsideDown > 0) {
                lastTimeUpsideDown = -1;
                numOfTimesUpsideDown++;
            }

            // check if player's car collided with stuff
            var car = Game.Player.Character.CurrentVehicle;
            if (Function.Call<bool>(Hash.HAS_ENTITY_COLLIDED_WITH_ANYTHING, car)) {
                foreach (Entity ent in World.GetNearbyEntities(Game.Player.Character.Position, 1f)) {
                    if (ent.IsNearEntity(car, new Vector3(0, 0, 0.1f))) {
                        numOfCollisions++;
                    }
                    //new UIResText(String.Format("average speed: {0}", Math.Round((float)speeds / (float)numOfSpeeds, 3)), new Point(Convert.ToInt32(res.Width) - safe.X - 300, Convert.ToInt32(res.Height) - safe.Y - 475), 0.3f, Color.White).Draw();
                    if (Function.Call<bool>(Hash.HAS_ENTITY_BEEN_DAMAGED_BY_ENTITY, ent, car, true)) {
                        if (debug)
                        {
                            UI.Notify(String.Format("dmg: {0}", ent.GetType()));
                        }
                        if (ent.GetType().ToString().Equals("GTA.Prop")) {
                            numOfDamagedProps++;
                        }
                    }
                }
            }

            // display collisions
            if (numOfCollisions > 0 && debug) {
                new UIResText(String.Format("collisions: {0}", numOfCollisions), new Point(Convert.ToInt32(res.Width) - safe.X - 300, Convert.ToInt32(res.Height) - safe.Y - 500), 0.3f, Color.Orange).Draw();
            }

            if (numOfDamagedProps > 0 && debug)
            {
                new UIResText(String.Format("damaged props: {0}", numOfDamagedProps), new Point(Convert.ToInt32(res.Width) - safe.X - 300, Convert.ToInt32(res.Height) - safe.Y - 550), 0.3f, Color.Orange).Draw();
            }

            speeds += currentSpeed;
            numOfSpeeds++;

            if (currentSpeed > maxSpeed) {
                maxSpeed = currentSpeed;
            }

            if (debug)
            {
                new UIResText(String.Format("average speed: {0}", Math.Round((float)speeds / (float)numOfSpeeds, 3)), new Point(Convert.ToInt32(res.Width) - safe.X - 300, Convert.ToInt32(res.Height) - safe.Y - 475), 0.3f, Color.White).Draw();
                new UIResText(String.Format("speed: {0}", Math.Round(Game.Player.Character.CurrentVehicle.Speed, 2)), new Point(Convert.ToInt32(res.Width) - safe.X - 180, Convert.ToInt32(res.Height) - safe.Y - 400), 0.3f, Color.White).Draw();
                new UIResText(String.Format("speed (km/h?): {0}", Math.Round(Game.Player.Character.CurrentVehicle.Speed * mTokm, 2)), new Point(Convert.ToInt32(res.Width) - safe.X - 250, Convert.ToInt32(res.Height) - safe.Y - 425), 0.3f, Color.White).Draw();
            }

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
                if (debug)
                {
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
                if (debug)
                {
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

            // save the vehicle's speed for this second
            var raceTimeElapsed = (Game.GameTime - raceStartTime);
            speedBySecond.Add(raceTimeElapsed.ToString(), car.Speed);

            // get controller values
            gasPedalInputs.Add(Game.GameTime.ToString(), Convert.ToDouble(Function.Call<int>(Hash.GET_CONTROL_VALUE, 0, 11) - 127));
            brakingInputs.Add(Game.GameTime.ToString(), Convert.ToDouble(Function.Call<int>(Hash.GET_CONTROL_VALUE, 0, 10) - 127));
            steeringInputs.Add(Game.GameTime.ToString(), Convert.ToDouble(Function.Call<int>(Hash.GET_CONTROL_VALUE, 0, 9)));

            if (debug)
            {
                // show current timer
                new UIResText(
                    String.Format(
                        "timer {0}s",
                        Math.Round(Convert.ToDecimal(raceTimeElapsed) / 1000, 2)),
                    new Point(Convert.ToInt32(res.Width) - safe.X - 180,
                    Convert.ToInt32(res.Height) - safe.Y - 525),
                    0.3f,
                    Color.White
                    ).Draw();
            }
            try
            {
                checkForRedlights(res, safe);
            }
            catch (NullReferenceException nr) {
                Logger.Log(nr.Source);
                Logger.Log(nr.StackTrace);
                Logger.Log(nr.Message);
            }

            if (numOfRedlights > 0 && debug) {
                new UIResText(String.Format("red lights: {0}", numOfRedlights),
                    new Point(Convert.ToInt32(res.Width) - safe.X - 180,
                    Convert.ToInt32(res.Height) - safe.Y - 375),
                    0.3f,
                    Color.Orange).Draw();
            }

            //displayClosestVehicleNode();

            logPossibleCollisions();
        }

        private void logPossibleCollisions() {
            foreach (Vehicle car in World.GetNearbyVehicles(Game.Player.Character.Position, possibleCollisionRadius)) {
                if (!knownVehicles.Contains(car)) {
                    possibleCollisions++;
                    knownVehicles.Add(car);
                }
            }
            foreach (Vehicle knownCar in knownVehicles) {
                var distance = World.GetDistance(knownCar.Position, Game.Player.Character.Position);
                distanceMeasurements++;
                meanDistance += distance / distanceMeasurements;
                if (distance > possibleCollisionRadius) {
                    knownVehicles.Remove(knownCar);
                    break;
                }
            }
        }

        protected Boolean checkForRedlights(SizeF res, Point safe) {
            if (res == null) {
                throw new ArgumentNullException("res should not be null");
            }

            if (safe == null)
            {
                throw new ArgumentNullException("safe should not be null");
            }

            // get position and forward vector to check for traffic lights in front of car
            var fv = Game.Player.Character.CurrentVehicle.ForwardVector;
            if (fv == null) { return false; }
            var pos = Game.Player.Character.CurrentVehicle.Position;
            if (pos == null) { return false; }
            var heading = Game.Player.Character.CurrentVehicle.Heading;

            // define an area in which to look for objects
            var pad = 25f;
            var nearLimit = pos + pad * new Vector3(-fv.Y, fv.X, 0);
            var farLimit = (pos + (checkDistance * fv) + pad * new Vector3(fv.Y, -fv.X, 0)) + new Vector3(0, 0, -pad);
            var color = Color.White;

            // did player run the red light?
            if (lastRedlight != null &&
                lastNearestVehicleToRedlight != null &&
                Function.Call<bool>(Hash.IS_VEHICLE_STOPPED_AT_TRAFFIC_LIGHTS, lastNearestVehicleToRedlight) &&
                (lastNearestVehicleDistance * 0.75f) > World.GetDistance(pos, lastRedlight.Position)
                && debug) {
                World.DrawMarker(MarkerType.UpsideDownCone, lastRedlight.Position, lastRedlight.ForwardVector, new Vector3(0, 0, 0), new Vector3(3f, 3f, 3f), Color.Red);
            }

            Entity lastTrafficLight = null;
            Vehicle carStoppedNearestToTrafficLight = null;

            try
            {
                // look at all entities around the player
                foreach (Entity ent in World.GetNearbyEntities(pos, checkDistance))
                {
                    // get traffic lights in front of player, that look roughly in the same direction
                    if (trafficSignalHashes.Contains(ent.Model.Hash) &&
                        Math.Abs(ent.Heading - heading) < 30f &&
                        ent.IsInArea(nearLimit, farLimit, 0)
                        && debug)
                    {
                        var dist = World.GetDistance(pos, ent.Position);
                        new UIResText(
                            String.Format(
                                "traffic light is near at {0}, heading {1}, position {2}",
                                dist,
                                ent.Heading,
                                ent.Position
                            ),
                            new Point(Convert.ToInt32(res.Width) - safe.X - 300,
                            Convert.ToInt32(res.Height) - safe.Y - 900),
                            0.3f,
                            Color.Aqua
                        ).Draw();

                        World.DrawMarker(MarkerType.VerticalCylinder, ent.Position, new Vector3(0, 0, 0), new Vector3(0, 0, 0), new Vector3(5f, 5f, 1f), Color.Aqua);
                        //World.DrawMarker(MarkerType.UpsideDownCone, ent.Position, ent.ForwardVector, new Vector3(90f,0,0), new Vector3(3f, 3f, 3f), Color.Green);

                        lastTrafficLight = ent;
                    }
                }
            }
            catch (NullReferenceException) {
                Logger.Log("finding traffic lights made an oopsie");
            }

            try {
                // if a traffic light was near, get cars nearby
                if (lastTrafficLight != null) {

                    var fvTl = -lastTrafficLight.ForwardVector;
                    var entPos = lastTrafficLight.Position;
                    var stoppedNearlimit = entPos + 0.25f * pad * new Vector3(-fvTl.Y, fvTl.X, 0) + new Vector3(0, 0, pad);
                    var stoppedFarLimit = (entPos + (1.5f * checkDistance * fvTl) + 0.5f * pad * new Vector3(fvTl.Y, -fvTl.X, 0)) + new Vector3(0, 0, -pad);

                    //World.DrawMarker(MarkerType.UpsideDownCone, stoppedNearlimit, new Vector3(0, 0, 0), new Vector3(0, 0, 0), new Vector3(5f, 5f, 5f), Color.Blue);
                    //World.DrawMarker(MarkerType.UpsideDownCone, stoppedFarLimit + new Vector3(0, 0, pad), new Vector3(0, 0, 0), new Vector3(0, 0, 0), new Vector3(5f, 5f, 5f), Color.Yellow);
                    //World.DrawMarker(MarkerType.VerticalCylinder, entPos + fvTl * checkDistance, new Vector3(0, 0, 0), new Vector3(0, 0, 0), new Vector3(stoppedRadius, stoppedRadius, 2f), Color.Aqua);

                    foreach (Vehicle car in World.GetNearbyVehicles(entPos + fvTl * checkDistance, stoppedRadius))
                    {
                        System.Diagnostics.Debug.Assert(car != null, "Assert: car != null");
                        System.Diagnostics.Debug.Assert(nearLimit != null, "Assert: nearLimit != null");
                        System.Diagnostics.Debug.Assert(farLimit != null, "Assert: farLimit != null");

                        // check for other cars in front of player looking in the same direction
                        if (Math.Abs(car.Heading - lastTrafficLight.Heading) < 30f &&
                            car.IsInArea(stoppedNearlimit, stoppedFarLimit, 0))
                        {

                            // check if they are stopped at a red light
                            if (Function.Call<bool>(Hash.IS_VEHICLE_STOPPED_AT_TRAFFIC_LIGHTS, car))
                            {
                                System.Diagnostics.Debug.Assert(car.Position != null, "Assert: car.Position != null");
                                if (debug)
                                {
                                    World.DrawMarker(MarkerType.VerticalCylinder, car.Position, new Vector3(0, 0, 0), new Vector3(0, 0, 0), new Vector3(5f, 5f, 1f), Color.Red);
                                }
                                color = Color.Red;

                                // store the car that's nearest to the traffic light and it's distance
                                if (carStoppedNearestToTrafficLight == null) {
                                    lastNearestVehicleToRedlight = carStoppedNearestToTrafficLight;
                                } else {
                                    System.Diagnostics.Debug.Assert(carStoppedNearestToTrafficLight != null, "Assert: carStoppedNearestToTrafficLight != null");
                                    if (World.GetDistance(lastTrafficLight.Position, car.Position) < World.GetDistance(lastTrafficLight.Position, carStoppedNearestToTrafficLight.Position))
                                    {
                                        lastNearestVehicleDistance = World.GetDistance(lastTrafficLight.Position, carStoppedNearestToTrafficLight.Position);
                                        lastNearestVehicleToRedlight = carStoppedNearestToTrafficLight;
                                        lastRedlight = lastTrafficLight;
                                    }
                                }
                            }
                        }
                    }
                }
            } catch (NullReferenceException) {
                Logger.Log("getting stopped cars made an oopsie");
            }

            //World.DrawMarker(MarkerType.DebugSphere, pos + fv * checkDistance, new Vector3(0, 0, 0), new Vector3(0f, 0f, 0f), new Vector3(2f, 2f, 2f), color);

            //World.DrawMarker(MarkerType.UpsideDownCone, nearLimit, new Vector3(0, 0, 0), new Vector3(0, 0, 0), new Vector3(2f, 2f, 2f), Color.Aqua);

            //World.DrawMarker(MarkerType.UpsideDownCone, farLimit + new Vector3(0, 0, pad), new Vector3(0, 0, 0), new Vector3(0, 0, 0), new Vector3(1f, 1f, 2f), Color.Aqua);

            return false;
        }
        public Tuple<Vector3, float> getClosestVehicleNodeAndHeading() {
            var pos = Game.Player.Character.Position;

            OutputArgument outArgA = new OutputArgument();
            OutputArgument outArgB = new OutputArgument();

            if (Function.Call<bool>(Hash.GET_CLOSEST_VEHICLE_NODE_WITH_HEADING, pos.X, pos.Y, pos.Z, outArgA, outArgB, 1, 3.0, 0))
            {
                var res = new Tuple<Vector3, float>(outArgA.GetResult<Vector3>(), outArgB.GetResult<float>());

                if (debug) {
                    World.DrawMarker(MarkerType.UpsideDownCone, res.Item1, new Vector3(0, 0, 0), new Vector3(0, 0, 0), new Vector3(1f, 1f, 1f), Color.Aqua);
                }

                return res; //getting heading if the native returns true
            }

            return new Tuple<Vector3, float>(new Vector3(), 0);
        }

        public bool isPlayerLost() {
            var pos = Game.Player.Character.Position;
            return (Game.Player.Character.CurrentVehicle.IsInWater
                || Game.Player.Character.IsInWater
                || Function.Call<float>(Hash.GET_ENTITY_SUBMERGED_LEVEL, Game.Player.Character) > 0.1f
                || Function.Call<float>(Hash.GET_ENTITY_SUBMERGED_LEVEL, Game.Player.Character.CurrentVehicle) > 0.1f
                || !Game.Player.Character.CurrentVehicle.IsOnAllWheels
                || Game.Player.Character.CurrentVehicle.IsUpsideDown
                || Game.Player.Character.CurrentVehicle.Rotation.Y > 100
                || (getClosestVehicleNodeAndHeading().Item1 == null
                || (World.GetDistance(pos, getClosestVehicleNodeAndHeading().Item1) > off_track_distance
                && World.GetDistance(pos, checkpoints[currentCheckpoint].Item1) > off_track_distance
                && (!checkpoints[currentCheckpoint].Item2.HasValue || World.GetDistance(pos, checkpoints[currentCheckpoint].Item2.Value) > off_track_distance)
                )));
        }

        public void keepPlayerOnTrack() {
            if (isPlayerLost())
            {
                if (time_player_got_lost == 0)
                {
                    time_player_got_lost = Game.GameTime;
                }
                else {
                    // set player on road again, if he is lost for too long
                    if (Game.GameTime - time_player_got_lost > max_lost_time)
                    {
                        float heading = getClosestVehicleNodeAndHeading().Item2;

                        Game.Player.Character.CurrentVehicle.PlaceOnNextStreet();
                        Game.Player.Character.CurrentVehicle.Heading = heading;
                    }

                    if (debug)
                    {
                        new UIResText(String.Format("player is lost! {0}", (Game.GameTime - time_player_got_lost) / 1000), new Point(850, 75), 0.4f, Color.Orange).Draw();
                    }
                }
            }
            else {
                time_player_got_lost = 0;
            }
        }

        public float ConvertToRadians(float angle)
        {
            return ((float)Math.PI / 180) * angle;
        }

        protected void teleportPlayerToCarCustomization() {
            if (Game.Player.Character.IsSittingInVehicle() && !car_config_done)
            {
                var car = Game.Player.Character.CurrentVehicle;
                car.Position = new Vector3(-1140.731f, -1985.894f, 12.78301f);
                car.Rotation = new Vector3(0.0f, 0.0f, 135.0f);
                Logger.Log("Player is entering garage");
                Game.Player.Character.Task.DriveTo(
                    car,
                    new Vector3(-1147.906f, -1993.416f, 12.7937f),
                    1.0f,
                    5.0f
                );

                //Game.Player.LastVehicle.Position = new Vector3(-1156.724f, -2007.222f, 12.79617f);
            }
        }

        protected void clearStuffUp() {
            ut.cleanUp();
            Ped player = Game.Player.Character;
            Vector3 pos = player.Position;
            player.Task.ClearAllImmediately(); // give back control to player

            // clear map blip
            if (currentBlip != null)
            {
                currentBlip.Remove();
                currentBlip = null;
            }

            // clear map blip
            if (nextBlip != null)
            {
                nextBlip.Remove();
                nextBlip = null;
            }

            // clear map blip
            if (currentAltBlip != null)
            {
                currentAltBlip.Remove();
                currentAltBlip = null;
            }

            // clear map blip
            if (nextAltBlip != null)
            {
                nextAltBlip.Remove();
                nextAltBlip = null;
            }

            // delete 3D marker
            if (currentMarker > 0) {
                Function.Call(Hash.DELETE_CHECKPOINT, currentMarker);
                currentMarker = -1;
            }
            if (currentAlternativeMarker > 0) {
                Function.Call(Hash.DELETE_CHECKPOINT, currentAlternativeMarker);
                currentMarker = -1;
            }

            // hopefully this prevents falling through the ground
            Function.Call(Hash.CLEAR_HD_AREA);
            Function.Call(Hash.CLEAR_FOCUS);

            // drop wanted level
            Function.Call(Hash.SET_PLAYER_WANTED_LEVEL, Game.Player, 0, false);
            Function.Call(Hash.SET_PLAYER_WANTED_LEVEL_NOW, Game.Player, false);

            race_started = false;
            race_initialized = false;
            currentCheckpoint = 0;

            resetLoggingVariables();
            //Wait(3000);
            if (debug)
            {
                UI.ShowSubtitle("Everything reset", 3000);
            }
        }

        protected void writeRaceDataToLog() {
            Logger.Log(String.Format("race started: {0}ms", raceStartTime));
            Logger.Log(String.Format("race ended: {0}ms", raceEndTime));
            Logger.Log(String.Format("time taken: {0}s", Math.Round((float)(raceEndTime - raceStartTime) / 1000, 2)));
            Logger.Log(String.Format("player health: {0}/100", Game.Player.Character.Health));
            Logger.Log(String.Format("car health: {0}/1000", car_health));
            Logger.Log(String.Format("average speed: {0}mph", speeds / (float)numOfSpeeds));
            Logger.Log(String.Format("average speed: {0}km/h", (speeds / (float)numOfSpeeds) * mTokm));
            Logger.Log(String.Format("maximum speed: {0}mph", maxSpeed));
            Logger.Log(String.Format("maximum speed: {0}km/h", maxSpeed * mTokm));
            Logger.Log(String.Format("Number of times player applied brakes: {0}", numBrakeApplied));
            Logger.Log(String.Format("Number of times player applied handbrake: {0}", numHandBrakeApplied));
            Logger.Log(String.Format("Cumulative time spent braking: {0}s", Math.Round((float)cumulativeTimeBraking / 1000, 2)));
            Logger.Log(String.Format("Cumulative time spent on handbrake: {0}s", Math.Round((float)cumulativeTimeHandbraking / 1000, 2)));
            Logger.Log(String.Format("Vehicle collisions: {0}", numOfHitVehicles));
            Logger.Log(String.Format("Pedestrian collisions: {0}", numOfHitPeds));
            Logger.Log(String.Format("Number of times player has driven against traffic: {0}", numOfTimesDrivingAgaingstTraffic));
            Logger.Log(String.Format("Number of times player has driven against on pavement: {0}", numOfTimesDrivingOnPavement));
            Logger.Log(String.Format("Cumulative time on pavement: {0}", Math.Round((float)cumulativeTimeOnPavement / 1000, 2)));
            Logger.Log(String.Format("Cumulative time driving against traffic: {0}", Math.Round((float)cumulativeTimeDrivingAgainstTraffic / 1000, 2)));
            Logger.Log(String.Format("Times vehicle was upside down: {0}", numOfTimesUpsideDown));
            Logger.Log(String.Format("Possible collisions: {0}", possibleCollisions));
            Logger.Log(String.Format("Mean distance: {0}", meanDistance));
        }

        protected void writeRaceDataToDB() {
            // retrieve the ID for the current task
            String taskName = races[currentRace].getCanonicalName();
            int taskId = database_interface.getTaskIdByName(taskName);
            if (!(taskId > 0)) {
                taskId = database_interface.createTask(taskName);
            }

            // single values
            Dictionary<String, float> map = mapCollectedDataForDB();
            foreach (KeyValuePair<String, float> item in map) {
                int attributeId = database_interface.getAttributeId(item.Key);
                if (!(attributeId > 0)) {
                    attributeId = database_interface.createAttribute(item.Key, rm.GetString(item.Key));
                }

                database_interface.insertValue(item.Key, taskId, current_data_set_id, item.Value);
            }

            // sets of lots of values
            foreach (KeyValuePair<string, Dictionary<string, double>> data in collectedData) {
                int attributeId = database_interface.getAttributeId(data.Key);
                if (!(attributeId > 0))
                {
                    attributeId = database_interface.createAttribute(data.Key, rm.GetString(data.Key));
                }

                database_interface.insertDataCollection(attributeId, taskId, current_data_set_id, data.Value);
            }
        }

        protected Dictionary<String, float> mapCollectedDataForDB() {
            Dictionary<String, float> mappings = new Dictionary<string, float> {
                {"task_start_time", raceStartTime},
                {"task_end_time", raceEndTime},
                {"task_total_time", (float)(raceEndTime - raceStartTime) / 1000},
                {"player_health", Game.Player.Character.Health},
                {"car_health", car_health},
                {"average_speed", speeds / (float)numOfSpeeds},
                {"maximum_speed", maxSpeed},
                {"times_braked", numBrakeApplied},
                {"times_handbraked", numHandBrakeApplied},
                {"duration_brake", (float)cumulativeTimeBraking / 1000},
                {"duration_handbrake", (float)cumulativeTimeHandbraking / 1000},
                {"vehicle_collisions", numOfHitVehicles},
                {"pedestrian_collisions", numOfHitPeds},
                {"times_against_traffic", numOfTimesDrivingAgaingstTraffic},
                {"times_on_pavement", numOfTimesDrivingOnPavement},
                {"duration_on_pavement", (float)cumulativeTimeOnPavement / 1000},
                {"duration_against_traffic", (float)cumulativeTimeDrivingAgainstTraffic / 1000},
                {"upside_down", numOfTimesUpsideDown},
                {"possible_vehicle_collisions", possibleCollisions},
                {"possible_vc_mean_distance", meanDistance}
            };

            return mergeRaceSpecificLogValues(mappings);
        }

        private Dictionary<string, float> mergeRaceSpecificLogValues(Dictionary<string, float> mappings) {

            try {
                var raceLogs = races[currentRace].getSingularDataValues();
                mappings = mappings.Concat(raceLogs).ToDictionary(x => x.Key, x => x.Value);
            } catch (NotImplementedException) {}

            return mappings;
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

            possibleCollisions = 0;
            numOfCollisions = 0;
            numOfDamagedProps = 0;
            meanDistance = 0;
            distanceMeasurements = 0;

            startedDrivingOnPavement = 0;
            startedDrivingAgainstTraffic = 0;

            raceStartTime = -1;
            raceEndTime = -1;

            cumulativeTimeDrivingAgainstTraffic = 0;
            cumulativeTimeOnPavement = 0;

            lastTimeUpsideDown = -1;
            numOfTimesUpsideDown = 0;

            car_health = -1;

            raceCarHasBlip = false;
            if (raceCarBlip != null) {
                raceCarBlip.Remove();
            }

            numOfRedlights = 0;

            keypressLengths = new Dictionary<string, double>();
            speedBySecond = new Dictionary<string, double>();
            gasPedalInputs = new Dictionary<string, double>();
            steeringInputs = new Dictionary<string, double>();
            brakingInputs = new Dictionary<string, double>();
            knownVehicles = new List<Vehicle>();
        }

        private void toggleScenarios(bool b) {
            foreach (String scenarioName in scenarios) {
                Function.Call(Hash.SET_SCENARIO_TYPE_ENABLED, scenarioName, b);
            }
            foreach (String scenarioGroup in scenarioGroups) {
                Function.Call(Hash.SET_SCENARIO_GROUP_ENABLED, scenarioGroup, b);
            }
        }

        private void setScenarioGroupList() {
            scenarioGroups = new String[] {
                "ALAMO_PLANES",
                 "ARMENIAN_CATS",
                 "ARMY_GUARD",
                 "ARMY_HELI",
                 "ATTRACT_PAP",
                 "BLIMP",
                 "CHINESE2_HILLBILLIES",
                 "Chinese2_Lunch",
                 "Cinema_Downtown",
                 "Cinema_Morningwood",
                 "Cinema_Textile",
                 "City_Banks",
                 "Countryside_Banks",
                 "DEALERSHIP",
                 "FIB_GROUP_1",
                 "FIB_GROUP_2",
                 "GRAPESEED_PLANES",
                 "Grapeseed_Planes",
                 "KORTZ_SECURITY",
                 "LOST_BIKERS",
                 "LOST_BIKERS",
                "LOST_BIKERS",
                "LSA_Planes",
                 "MOVIE_STUDIO_SECURITY",
                 "MOVIE_STUDIO_SECURITY",
                "MP_POLICE",
                 "Observatory_Bikers",
                 "POLICE_POUND1",
                 "POLICE_POUND2",
                 "POLICE_POUND3",
                 "POLICE_POUND4",
                 "POLICE_POUND5",
                 "PRISON_TOWERS",
                 "QUARRY",
                 "Rampage1",
                 "SANDY_PLANES",
                 "SCRAP_SECURITY",
                 "SEW_MACHINE",
                 "SOLOMON_GATE",
                 "Triathlon_1",
                 "Triathlon_1_Start",
                 "Triathlon_2",
                 "Triathlon_2_Start",
                 "Triathlon_3",
                 "Triathlon_3_Start",
            };
        }

        private void setScenarioList()
        {
            scenarios = new String[] {
                "WORLD_HUMAN_AA_COFFEE",
                "WORLD_HUMAN_AA_SMOKE",
                "WORLD_HUMAN_BINOCULARS",
                "WORLD_HUMAN_BUM_FREEWAY",
                "WORLD_HUMAN_BUM_SLUMPED",
                "WORLD_HUMAN_BUM_STANDING",
                "WORLD_HUMAN_BUM_WASH",
                "WORLD_HUMAN_CAR_PARK_ATTENDANT",
                "WORLD_HUMAN_CHEERING",
                "WORLD_HUMAN_CLIPBOARD",
                "WORLD_HUMAN_CONST_DRILL",
                "WORLD_HUMAN_COP_IDLES",
                "WORLD_HUMAN_DRINKING",
                "WORLD_HUMAN_DRUG_DEALER",
                "WORLD_HUMAN_DRUG_DEALER_HARD",
                "WORLD_HUMAN_MOBILE_FILM_SHOCKING",
                "WORLD_HUMAN_GARDENER_LEAF_BLOWER",
                "WORLD_HUMAN_GARDENER_PLANT",
                "WORLD_HUMAN_GOLF_PLAYER",
                "WORLD_HUMAN_GUARD_PATROL",
                "WORLD_HUMAN_GUARD_STAND",
                "WORLD_HUMAN_GUARD_STAND_ARMY",
                "WORLD_HUMAN_HAMMERING",
                "WORLD_HUMAN_HANG_OUT_STREET",
                "WORLD_HUMAN_HIKER_STANDING",
                "WORLD_HUMAN_HUMAN_STATUE",
                "WORLD_HUMAN_JANITOR",
                "WORLD_HUMAN_JOG_STANDING",
                "WORLD_HUMAN_LEANING",
                "WORLD_HUMAN_MAID_CLEAN",
                "WORLD_HUMAN_MUSCLE_FLEX",
                "WORLD_HUMAN_MUSCLE_FREE_WEIGHTS",
                "WORLD_HUMAN_MUSICIAN",
                "WORLD_HUMAN_PAPARAZZI",
                "WORLD_HUMAN_PARTYING",
                "WORLD_HUMAN_PICNIC",
                "WORLD_HUMAN_PROSTITUTE_HIGH_CLASS",
                "WORLD_HUMAN_PROSTITUTE_LOW_CLASS",
                "WORLD_HUMAN_PUSH_UPS",
                "WORLD_HUMAN_SEAT_LEDGE",
                "WORLD_HUMAN_SEAT_LEDGE_EATING",
                "WORLD_HUMAN_SEAT_STEPS",
                "WORLD_HUMAN_SEAT_WALL",
                "WORLD_HUMAN_SEAT_WALL_EATING",
                "WORLD_HUMAN_SEAT_WALL_TABLET",
                "WORLD_HUMAN_SECURITY_SHINE_TORCH",
                "WORLD_HUMAN_SIT_UPS",
                "WORLD_HUMAN_SMOKING",
                "WORLD_HUMAN_SMOKING_POT",
                "WORLD_HUMAN_STAND_FIRE",
                "WORLD_HUMAN_STAND_FISHING",
                "WORLD_HUMAN_STAND_IMPATIENT",
                "WORLD_HUMAN_STAND_IMPATIENT_UPRIGHT",
                "WORLD_HUMAN_STAND_MOBILE",
                "WORLD_HUMAN_STAND_MOBILE_UPRIGHT",
                "WORLD_HUMAN_STRIP_WATCH_STAND",
                "WORLD_HUMAN_STUPOR",
                "WORLD_HUMAN_SUNBATHE",
                "WORLD_HUMAN_SUNBATHE_BACK",
                "WORLD_HUMAN_SUPERHERO",
                "WORLD_HUMAN_SWIMMING",
                "WORLD_HUMAN_TENNIS_PLAYER",
                "WORLD_HUMAN_TOURIST_MAP",
                "WORLD_HUMAN_TOURIST_MOBILE",
                "WORLD_HUMAN_VEHICLE_MECHANIC",
                "WORLD_HUMAN_WELDING",
                "WORLD_HUMAN_WINDOW_SHOP_BROWSE",
                "WORLD_HUMAN_YOGA",
                "WORLD_BOAR_GRAZING",
                "WORLD_CAT_SLEEPING_GROUND",
                "WORLD_CAT_SLEEPING_LEDGE",
                "WORLD_COW_GRAZING",
                "WORLD_COYOTE_HOWL",
                "WORLD_COYOTE_REST",
                "WORLD_COYOTE_WANDER",
                "WORLD_CHICKENHAWK_FEEDING",
                "WORLD_CHICKENHAWK_STANDING",
                "WORLD_CORMORANT_STANDING",
                "WORLD_CROW_FEEDING",
                "WORLD_CROW_STANDING",
                "WORLD_DEER_GRAZING",
                "WORLD_DOG_BARKING_ROTTWEILER",
                "WORLD_DOG_BARKING_RETRIEVER",
                "WORLD_DOG_BARKING_SHEPHERD",
                "WORLD_DOG_SITTING_ROTTWEILER",
                "WORLD_DOG_SITTING_RETRIEVER",
                "WORLD_DOG_SITTING_SHEPHERD",
                "WORLD_DOG_BARKING_SMALL",
                "WORLD_DOG_SITTING_SMALL",
                "WORLD_FISH_IDLE",
                "WORLD_GULL_FEEDING",
                "WORLD_GULL_STANDING",
                "WORLD_HEN_PECKING",
                "WORLD_HEN_STANDING",
                "WORLD_MOUNTAIN_LION_REST",
                "WORLD_MOUNTAIN_LION_WANDER",
                "WORLD_PIG_GRAZING",
                "WORLD_PIGEON_FEEDING",
                "WORLD_PIGEON_STANDING",
                "WORLD_RABBIT_EATING",
                "WORLD_RATS_EATING",
                "WORLD_SHARK_SWIM",
                "PROP_BIRD_IN_TREE",
                "PROP_BIRD_TELEGRAPH_POLE",
                "PROP_HUMAN_ATM",
                "PROP_HUMAN_BBQ",
                "PROP_HUMAN_BUM_BIN",
                "PROP_HUMAN_BUM_SHOPPING_CART",
                "PROP_HUMAN_MUSCLE_CHIN_UPS",
                "PROP_HUMAN_MUSCLE_CHIN_UPS_ARMY",
                "PROP_HUMAN_MUSCLE_CHIN_UPS_PRISON",
                "PROP_HUMAN_PARKING_METER",
                "PROP_HUMAN_SEAT_ARMCHAIR",
                "PROP_HUMAN_SEAT_BAR",
                "PROP_HUMAN_SEAT_BENCH",
                "PROP_HUMAN_SEAT_BENCH_DRINK",
                "PROP_HUMAN_SEAT_BENCH_DRINK_BEER",
                "PROP_HUMAN_SEAT_BENCH_FOOD",
                "PROP_HUMAN_SEAT_BUS_STOP_WAIT",
                "PROP_HUMAN_SEAT_CHAIR",
                "PROP_HUMAN_SEAT_CHAIR_DRINK",
                "PROP_HUMAN_SEAT_CHAIR_DRINK_BEER",
                "PROP_HUMAN_SEAT_CHAIR_FOOD",
                "PROP_HUMAN_SEAT_CHAIR_UPRIGHT",
                "PROP_HUMAN_SEAT_CHAIR_MP_PLAYER",
                "PROP_HUMAN_SEAT_COMPUTER",
                "PROP_HUMAN_SEAT_DECKCHAIR",
                "PROP_HUMAN_SEAT_DECKCHAIR_DRINK",
                "PROP_HUMAN_SEAT_MUSCLE_BENCH_PRESS",
                "PROP_HUMAN_SEAT_MUSCLE_BENCH_PRESS_PRISON",
                "PROP_HUMAN_SEAT_SEWING",
                "PROP_HUMAN_SEAT_STRIP_WATCH",
                "PROP_HUMAN_SEAT_SUNLOUNGER",
                "PROP_HUMAN_STAND_IMPATIENT",
                "CODE_HUMAN_COWER",
                "CODE_HUMAN_CROSS_ROAD_WAIT",
                "CODE_HUMAN_PARK_CAR",
                "PROP_HUMAN_MOVIE_BULB",
                "PROP_HUMAN_MOVIE_STUDIO_LIGHT",
                "CODE_HUMAN_MEDIC_KNEEL",
                "CODE_HUMAN_MEDIC_TEND_TO_DEAD",
                "CODE_HUMAN_MEDIC_TIME_OF_DEATH",
                "CODE_HUMAN_POLICE_CROWD_CONTROL",
                "CODE_HUMAN_POLICE_INVESTIGATE",
                "CODE_HUMAN_STAND_COWER",
                "EAR_TO_TEXT",
                "EAR_TO_TEXT_FAT"
            };
        }

        private void processCollectedData() {
            /*var lomont = new Lomont.LomontFFT();
            // make input array length a power of two
            var fftInputLength = Convert.ToInt32(Math.Ceiling(Math.Log(steeringInputs.Count, 2)));
            double[] inputValues = new double[fftInputLength];
            List<Tuple<String, double>> inputFrequency = new List<Tuple<String, double>>(steeringInputs.Count);
            int i = 0;

            // put list values into array
            foreach (Tuple<String, double> point in steeringInputs) {
                inputValues[i] = point.Item2;
                i++;
            }

            // write a 0 to the last array index, if there is no actual value to be filled
            if (fftInputLength > steeringInputs.Count) {
                inputValues[steeringInputs.Count] = 0;
            }

            // do fft calculation
            lomont.FFT(inputValues, false);

            i = 0;

            // put back calculated values into new list
            foreach (double value in inputValues) {
                inputFrequency.Add(new Tuple<String, double>(i.ToString(), value));
            }

            collectedData.Add(new Tuple<String, List<Tuple<String, double>>>("input frequency", inputFrequency));*/

            // get data point lists from current race, if there are any
            collectedData.Add("speed", speedBySecond);
            collectedData.Add("gas", gasPedalInputs);
            collectedData.Add("brake", brakingInputs);
            collectedData.Add("steering", steeringInputs);

            try
            {
                foreach (KeyValuePair<string, Dictionary<string, double>> dataList in races[currentRace].getCollectedData())
                {
                    collectedData.Add(dataList.Key, dataList.Value);
                }
            }
            catch (NotImplementedException) { }
        }

        protected void countDown()
        {
            Game.Player.CanControlCharacter = false;
            Game.Player.Character.IsInvincible = true;
            bmsg = BigMessageThread.MessageInstance;
            for (int i = 3; i > 0; i--)
            {
                bmsg.ShowMpMessageLarge(String.Format("{0}", i), countdown_interval);
                Wait(countdown_interval);
            }

            Game.Player.CanControlCharacter = true;
            Game.Player.Character.IsInvincible = false;
        }

        protected void buildMenu() {
            myMenu = new UIMenu("Mod4ResearchTUB", "~b~meh");

            // checkbox for route designer
            var route_designer_checkbox = new UIMenuCheckboxItem("Route Designer", route_designer_active, rm.GetString("menu_toggle_route_designer"));
            myMenu.AddItem(route_designer_checkbox);
            myMenu.RefreshIndex();

            myMenu.OnCheckboxChange += (sender, item, checked_) =>
            {
                if (item == route_designer_checkbox)
                {
                    if (race_initialized || race_started) {
                        UI.Notify(rm.GetString("menu_not_during_task"));
                        return;
                    }

                    route_designer_active = checked_;
                    toggleRouteDesigner();
                    UI.Notify(String.Format(rm.GetString("route_designer_active"), route_designer_active));
                }
            };

            // checkbox for cam designer
            var cam_designer_checkbox = new UIMenuCheckboxItem("Cam Designer", cam_designer_active, rm.GetString("menu_toggle_cam_designer"));
            myMenu.AddItem(cam_designer_checkbox);
            myMenu.RefreshIndex();

            myMenu.OnCheckboxChange += (sender, item, checked_) =>
            {
                if (item == cam_designer_checkbox)
                {
                    if (race_initialized || race_started)
                    {
                        UI.Notify(rm.GetString("menu_not_during_task"));
                        return;
                    }

                    cam_designer_active = checked_;
                    toggleCamDesigner();
                    UI.Notify(String.Format(rm.GetString("cam_designer_active"), cam_designer_active));
                }
            };

            // checkbox for debug mode
            var debug_checkbox = new UIMenuCheckboxItem("Debug mode", debug, rm.GetString("menu_toggle_debug"));
            myMenu.AddItem(debug_checkbox);
            myMenu.RefreshIndex();

            myMenu.OnCheckboxChange += (sender, item, checked_) =>
            {
                if (item == debug_checkbox)
                {
                    debug = checked_;
                    UI.Notify(String.Format(rm.GetString("debug_mode_active"), debug));
                }
            };

            // list of languages available
            var language_list = new UIMenuListItem(
                    rm.GetString("menu_languages"),
                    new List<dynamic> {rm.GetString("menu_german"), rm.GetString("menu_english") },
                    0
                    );

            myMenu.AddItem(language_list);

            myMenu.OnListChange += (sender, item, index) =>
            {
                if (item == language_list)
                {
                    CultureInfo culture = CultureInfo;
                    if (index == 0)
                    {
                        culture = CultureInfo.CreateSpecificCulture("de-DE");
                    }
                    else if (index == 1) {
                        culture = CultureInfo.CreateSpecificCulture("en-US");
                    }
                    Thread.CurrentThread.CurrentCulture = culture;
                    Thread.CurrentThread.CurrentUICulture = culture;
                    UI.Notify(rm.GetString("language_selected") + index.ToString() + " " + item.IndexToItem(index).ToString());
                }

            };

            // button for starting the mod
            var newitem = new UIMenuItem("Start", "Start the intro and tasks.");
            myMenu.OnItemSelect += (sender, item, index) =>
            {
                if (item == newitem)
                {
                    startMod();
                }
            };
            myMenu.AddItem(newitem);

            _myMenuPool.Add(myMenu);
        }

        private void toggleRouteDesigner() {
            if (route_designer_active)
            {
                route_checkpoints = new List<Tuple<Vector3, int, Blip>>();
            }
            else {
                File.AppendAllText("route.log", " Tuple<Vector3, Vector3?>[] checkpointlist = {" + Environment.NewLine);
                foreach (Tuple<Vector3, int, Blip> cp in route_checkpoints) {
                    File.AppendAllText(
                        "route.log",
                        String.Format(
                            "new Tuple<Vector3, Vector3?>(new Vector3({0}, {1}, {2}), null),",
                            cp.Item1.X,
                            cp.Item1.Y,
                            cp.Item1.Z
                            ) + Environment.NewLine);
                    // delete checkpoint marker
                    Function.Call(Hash.DELETE_CHECKPOINT, cp.Item2);
                    // remove blip
                    cp.Item3.Remove();
                }
                File.AppendAllText("route.log", "};" + Environment.NewLine);
                route_checkpoints = null;
            }
        }

        private void toggleCamDesigner() {
            Game.Player.Character.IsInvincible = cam_designer_active;
            if (cam_designer_active)
            {
                designer_cam = World.CreateCamera(
                    GameplayCamera.Position,
                    GameplayCamera.Rotation,
                    GameplayCamera.FieldOfView);
                ut.setScriptCam(designer_cam);
                ut.activateScriptCam();
                Game.Player.CanControlCharacter = false;
            }
            else {
                Game.Player.CanControlCharacter = true;
                ut.deleteScriptCams();
                World.DestroyAllCameras();
            }
        }

        private void debugCamDesigner() {
            if (debug && designer_cam != null) {
                Vector3 pos = designer_cam.Position,
                    fv = ut.getCamForwardVector(designer_cam),
                    rot = designer_cam.Rotation;

                World.DrawMarker(MarkerType.DebugSphere, pos + fv*5, fv, rot, new Vector3(1,1,1), Color.White);
            }
        }

        private void handleCamMovement(object sender, KeyEventArgs e) {
            switch (e.KeyCode)
            {
                case Keys.W:
                    ut.moveCamera(Direction.Forward, cam_movement_amount/4);
                    break;
                case Keys.S:
                    ut.moveCamera(Direction.Backward, cam_movement_amount/4);
                    break;
                case Keys.A:
                    ut.moveCamera(Direction.Left, cam_movement_amount/4);
                    break;
                case Keys.D:
                    ut.moveCamera(Direction.Right, cam_movement_amount/4);
                    break;
                case Keys.NumPad7:
                    ut.moveCamera(Direction.TurnLeft, cam_movement_amount);
                    break;
                case Keys.NumPad9:
                    ut.moveCamera(Direction.TurnRight, cam_movement_amount);
                    break;
                case Keys.NumPad8:
                    ut.moveCamera(Direction.TurnDown, cam_movement_amount);
                    break;
                case Keys.NumPad5:
                    ut.moveCamera(Direction.TurnUp, cam_movement_amount);
                    break;
                case Keys.Up:
                    ut.moveCamera(Direction.Up, cam_movement_amount/4);
                    break;
                case Keys.Down:
                    ut.moveCamera(Direction.Down, cam_movement_amount/4);
                    break;
                case Keys.Add:
                    ut.changeCamFieldOfView(Direction.Up, cam_movement_amount);
                    break;
                case Keys.Subtract:
                    ut.changeCamFieldOfView(Direction.Up, -cam_movement_amount);
                    break;
            }

            director_cam_position = "new Vector3(" +
                String.Format(
                    "{0}f, {1}f, {2}f)",
                    designer_cam.Position.X.ToString(CultureInfo.InvariantCulture),
                    designer_cam.Position.Y.ToString(CultureInfo.InvariantCulture),
                    designer_cam.Position.Z.ToString(CultureInfo.InvariantCulture)
                    );
            director_cam_rotation = String.Format(
                    "new Vector3({0}f, {1}f, {2}f)",
                    designer_cam.Rotation.X.ToString(CultureInfo.InvariantCulture),
                    designer_cam.Rotation.Y.ToString(CultureInfo.InvariantCulture),
                    designer_cam.Rotation.Z.ToString(CultureInfo.InvariantCulture)
                    );

            String cam_code = "Camera cam = World.CreateCamera(" + Environment.NewLine +
                "\t" + director_cam_position + "," + Environment.NewLine + 
                "\t" + director_cam_rotation + "," + Environment.NewLine +
                "\t" + designer_cam.FieldOfView.ToString(CultureInfo.InvariantCulture) + "f" + Environment.NewLine + ");" +
                Environment.NewLine + Environment.NewLine +
                "cam.Position = " + director_cam_position + ";" + Environment.NewLine +
                "cam.Rotation = " + director_cam_rotation + ";" + Environment.NewLine +
                "cam.FieldOfView = " + designer_cam.FieldOfView.ToString(CultureInfo.InvariantCulture) + "f;";
            director_gui.SetText(cam_code);
        }

        private void startMod() {
            if (!race_initialized &&
                !race_started &&
                currentRace >= 0 &&
                currentRace < races.Length)
            {
                myMenu.Visible = false;

                makePlayerInputName();

                race_initialized = true;
                checkpoints = races[currentRace].getCheckpoints();
                races[currentRace].initRace();
            }
        }

        private void hideUIComponents() {
            UI.HideHudComponentThisFrame(HudComponent.Cash);
            UI.HideHudComponentThisFrame(HudComponent.WeaponWheelStats);
            UI.HideHudComponentThisFrame(HudComponent.WeaponWheel);
            UI.HideHudComponentThisFrame(HudComponent.WeaponIcon);
            UI.HideHudComponentThisFrame(HudComponent.VehicleName);
        }
        #endregion
    }
}