﻿#region Using references

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
#endregion

namespace ModForResearchTUB
{
    public class Main : Script
    {
        // Variables
        List<int> trafficSignalHashes = new List<int>(3);
        Blip currentBlip = null, currentAltBlip = null, nextBlip = null, nextAltBlip = null;
        Tuple<Vector3, Vector3?>[] checkpoints;
        float checkpoint_radius = 5;
        int currentMarker;
        int currentAlternativeMarker;
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
        List<Tuple<String, double>> keypressLengths = new List<Tuple<String, double>>();
        int lastKeydownA;
        int lastKeydownD;

        // 
        List<Tuple<String, double>> speedBySecond = new List<Tuple<String, double>>();

        // controller inputs
        List<Tuple<String, double>> gasPedalInputs = new List<Tuple<String, double>>();
        List<Tuple<String, double>> steeringInputs = new List<Tuple<String, double>>();
        List<Tuple<String, double>> brakingInputs = new List<Tuple<String, double>>();

        private List<Tuple<String, List<Tuple<String, double>>>> collectedData = new List<Tuple<String, List<Tuple<String, double>>>>();

        protected String[] scenarioGroups { get; private set; }
        protected String[] scenarios { get; private set; }

        public CultureInfo CultureInfo { get; private set; }

        private Utilities ut = new Utilities();

        ResourceManager rm;

        BigMessageHandler bmsg;
        int countdown_interval = 2000;

        private UIMenu myMenu;
        private MenuPool _myMenuPool = new MenuPool();
        private bool route_designer_active = false,
            debug = true;
        private List<Vector3> route_checkpoints;
        private List<Blip> route_blips;

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
        }

        private void setUpRaces() {
            races = new RaceInterface[6];
            races[0] = new RaceIntro(rm, ut);
            races[1] = new RaceConvoy(rm, ut);
            races[2] = new RaceSuburban();
            races[3] = new RaceDesert();
            races[4] = new RaceCarvsCar();
            races[5] = new RaceToWoodmill();
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

                    if (checkpoints != null)
                    {
                        new UIResText(String.Format("checkpoints: {0}", checkpoints.Length), new Point((Convert.ToInt32(res.Width) - safe.X - 250), 75), 0.3f, Color.White).Draw();
                    }

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
                        (checkpoints[currentCheckpoint].Item2.HasValue && Game.Player.Character.IsInRangeOf(checkpoints[currentCheckpoint].Item2.Value, checkpoint_radius)))
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
                        if ((currentCheckpoint + 1) == checkpoints.Length)
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
                                UI.Notify("This was the last race!");
                            }
                            return;
                        }

                        currentCheckpoint++;
                        setupNextCheckpoint();
                    }
                }
                else if (currentRace >= 0 && 
                    currentRace < races.Length &&
                    races[currentRace] != null &&
                    races[currentRace].checkRaceStartCondition()) {
                   
                    // show countdown
                    countDown();

                    UI.Notify(String.Format("Started race {0}/{1}", currentRace + 1, races.Length));
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

            if (route_designer_active) {
                renderRouteCheckpoints();
            }
        }

        protected void renderDiagrams() {

            foreach (Tuple<String, List<Tuple<String, double>>> item in collectedData) {
                DrawDiagram.renderDiagramToDisk(
                    item.Item2,
                    item.Item1,
                    item.Item1,
                    currentPlayerName + "-race-" + (currentRace + 1) + "-" + item.Item1
                );
            }

            // draw diagram for keypresses by length
            DrawDiagram.renderDiagramToDisk(
                keypressLengths,
                "keypresses by length",
                "times pressed for given amount of time",
                currentPlayerName + "-race-" + currentRace + 1 + "-keypress-lengths"
            );

            // draw diagram for speed every tick
            DrawDiagram.renderDiagramToDisk(
                speedBySecond,
                "speed by second",
                "speed",
                currentPlayerName + "-race-" + currentRace + 1 + "-speed-by-second"
            );

            // draw diagram for gas pedal input
            DrawDiagram.renderDiagramToDisk(
                gasPedalInputs,
                "gas pedal pressed",
                "gas",
                currentPlayerName + "-race-" + currentRace + 1 + "-gas"
            );

            // draw diagram for brake pedal input
            DrawDiagram.renderDiagramToDisk(
                brakingInputs,
                "brake pedal pressed",
                "brake",
                currentPlayerName + "-race-" + currentRace + 1 + "-brake"
            );

            // draw diagram for brake pedal input
            DrawDiagram.renderDiagramToDisk(
                steeringInputs,
                "steering wheel input",
                "value",
                currentPlayerName + "-race-" + currentRace + 1 + "-steering"
            );
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
            Function.Call(Hash.SET_BLIP_ROUTE, currentBlip, true);
            Function.Call(Hash.SHOW_NUMBER_ON_BLIP, currentBlip, currentCheckpoint + 1);

            if (nextCoords.HasValue) {
                nextBlip = World.CreateBlip(nextCoords.Value);
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
            currentAltBlip.Color = BlipColor.Red;
            Function.Call(Hash.SET_NEW_WAYPOINT, coords.X, coords.Y);
            Function.Call(Hash.SHOW_NUMBER_ON_BLIP, currentAltBlip, currentCheckpoint + 1);

            if (nextCoords.HasValue)
            {
                nextAltBlip = World.CreateBlip(nextCoords.Value);
                nextAltBlip.Color = BlipColor.Red;
                Function.Call(Hash.SHOW_NUMBER_ON_BLIP, nextAltBlip, currentCheckpoint + 2);
            }
        }

        protected int drawCurrentCheckpoint(Vector3 coords, Vector3? possibleNextCoords, int R, int G, int B, int type) {
            //UI.Notify("drawCurrentCheckpoint()");
            Vector3 nextCoords = new Vector3();
            // set next checkpoint

            // set graphics depending on wether it's the last checkpoint or not
            if (possibleNextCoords.HasValue) {
                nextCoords = possibleNextCoords.Value;
            } else {
                coords.Z = coords.Z + 3f;
                currentBlip.Sprite = BlipSprite.RaceFinish;
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
                    handleRouteInput();
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
                case Keys.A:
                    if (race_started && lastKeydownA > 0) {
                        var length = (Math.Round((Convert.ToDouble(Game.GameTime) - Convert.ToDouble(lastKeydownA))/ roundTo) * roundToMultiplier).ToString();
                        //UI.ShowSubtitle(String.Format("keyup [A], length {0}", length));
                        if (keypressLengths.Exists(x => x.Item1 == length))
                        {
                            var index = keypressLengths.FindIndex(x => x.Item1 == length);
                            keypressLengths[index] = new Tuple<String, double>(length, keypressLengths[index].Item2 + 1);
                        }
                        else {
                            keypressLengths.Add(new Tuple<String, double>(length, 1));
                        }
                    }
                    break;
                case Keys.D:
                    if (race_started && lastKeydownA > 0)
                    {
                        var length = (Math.Round((Convert.ToDouble(Game.GameTime) - Convert.ToDouble(lastKeydownD)) / roundTo) * roundToMultiplier).ToString();
                        //UI.ShowSubtitle(String.Format("keyup [D], length {0}", length));
                        if (keypressLengths.Exists(x => x.Item1 == length))
                        {
                            var index = keypressLengths.FindIndex(x => x.Item1 == length);
                            keypressLengths[index] = new Tuple<String, double>(length, keypressLengths[index].Item2 + 1);
                        }
                        else {
                            keypressLengths.Add(new Tuple<String, double>(length, 1));
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

        private void handleRouteInput() {
            if (route_designer_active)
            {
                var pos = Game.Player.Character.Position;
                if (route_checkpoints.Count > 0)
                {
                    // see if checkpoint is near and if so, remove it and its blip
                    foreach (Vector3 cp in route_checkpoints)
                    {
                        if (World.GetDistance(cp, pos) <= checkpoint_radius)
                        {
                            var index = route_checkpoints.IndexOf(cp);
                            route_blips.ElementAt(index).Remove();
                            route_blips.RemoveAt(index);
                            route_checkpoints.Remove(cp);
                            return;
                        }
                    }
                }

                // if there are no checkpoints near, create one
                route_checkpoints.Add(pos);
                Blip new_blip = World.CreateBlip(pos);
                Function.Call(Hash.SHOW_NUMBER_ON_BLIP, new_blip, route_checkpoints.Count);
                new_blip.Color = BlipColor.Yellow;
                route_blips.Add(new_blip);
            }
        }

        private void renderRouteCheckpoints() {
            foreach (Vector3 cp in route_checkpoints) {
                World.DrawMarker(MarkerType.VerticalCylinder, cp, new Vector3(), new Vector3(), new Vector3(checkpoint_radius, checkpoint_radius, 15), Color.Yellow);
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
            speedBySecond.Add(new Tuple<String, double>(raceTimeElapsed.ToString(), car.Speed));

            // get controller values
            gasPedalInputs.Add(new Tuple<String, double>(Game.GameTime.ToString(), Convert.ToDouble(Function.Call<int>(Hash.GET_CONTROL_VALUE, 0, 11) - 127)));
            brakingInputs.Add(new Tuple<String, double>(Game.GameTime.ToString(), Convert.ToDouble(Function.Call<int>(Hash.GET_CONTROL_VALUE, 0, 10) - 127)));
            steeringInputs.Add(new Tuple<String, double>(Game.GameTime.ToString(), Convert.ToDouble(Function.Call<int>(Hash.GET_CONTROL_VALUE, 0, 9))));

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
                (lastNearestVehicleDistance * 0.75f) > World.GetDistance(pos,lastRedlight.Position)) {
                World.DrawMarker(MarkerType.UpsideDownCone, lastRedlight.Position, lastRedlight.ForwardVector, new Vector3(0,0,0), new Vector3(3f, 3f, 3f), Color.Red);
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
                    var stoppedNearlimit = entPos + 0.25f * pad * new Vector3(-fvTl.Y, fvTl.X, 0) + new Vector3(0,0,pad);
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
                                World.DrawMarker(MarkerType.VerticalCylinder, car.Position, new Vector3(0, 0, 0), new Vector3(0, 0, 0), new Vector3(5f, 5f, 1f), Color.Red);
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
        public Vector3 displayClosestVehicleNode() {
            var pos = Game.Player.Character.Position;

            OutputArgument outArgA = new OutputArgument();

            if (Function.Call<bool>(Hash.GET_CLOSEST_VEHICLE_NODE, pos.X, pos.Y, pos.Z, outArgA, 1, 3.0, 0))
            {
                var res = outArgA.GetResult<Vector3>();

                World.DrawMarker(MarkerType.UpsideDownCone, res, new Vector3(0, 0, 0), new Vector3(0, 0, 0), new Vector3(3f, 3f, 3f), Color.White);

                return res; //getting heading if the native returns true
            }

            return new Vector3();
        }

        public float ConvertToRadians(float angle)
        {
            return ((float)Math.PI / 180) * angle;
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

            // hopefully this prevents falling through the ground
            Function.Call(Hash.CLEAR_HD_AREA);
            Function.Call(Hash.CLEAR_FOCUS);

            // drop wanted level
            Function.Call(Hash.SET_PLAYER_WANTED_LEVEL, Game.Player, 0, false);
            Function.Call(Hash.SET_PLAYER_WANTED_LEVEL_NOW, Game.Player, false);

            // try and remove left cars
            Function.Call(Hash.CLEAR_AREA_OF_VEHICLES,
               pos.X, pos.Y, pos.Z, 1000, false, false, false, false, false
            );

            foreach (Vehicle car in World.GetNearbyVehicles(player, 100)) {
                car.Delete();
            }

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
            Logger.Log(String.Format("average speed: {0}mph", speeds/(float)numOfSpeeds));
            Logger.Log(String.Format("average speed: {0}km/h",(speeds / (float)numOfSpeeds) * mTokm));
            Logger.Log(String.Format("maximum speed: {0}mph", maxSpeed));
            Logger.Log(String.Format("maximum speed: {0}km/h", maxSpeed * mTokm));
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
            Logger.Log(String.Format("Times vehicle was upside down: {0}", numOfTimesUpsideDown));
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

            numOfCollisions = 0;
            numOfDamagedProps = 0;

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

            keypressLengths = new List<Tuple<String, double>>();
            speedBySecond = new List<Tuple<String, double>>();
            gasPedalInputs = new List<Tuple<String, double>>();
            steeringInputs = new List<Tuple<String, double>>();
            brakingInputs = new List<Tuple<String, double>>();
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
            try
            {
                foreach (Tuple<String, List<Tuple<String, double>>> dataList in races[currentRace].getCollectedData())
                {
                    collectedData.Add(dataList);
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
            var route_designer_checkbox = new UIMenuCheckboxItem("Route Designer", route_designer_active, "Activate the route designer?");
            myMenu.AddItem(route_designer_checkbox);
            myMenu.RefreshIndex();

            myMenu.OnCheckboxChange += (sender, item, checked_) =>
            {
                if (item == route_designer_checkbox)
                {
                    if (race_initialized || race_started) {
                        UI.Notify("This can't be done during the tasks.");
                        return;
                    }

                    route_designer_active = checked_;
                    toggleRouteDesigner();
                    UI.Notify("~r~route designer active: ~b~" + route_designer_active);
                }
            };

            // checkbox for debug mode
            var debug_checkbox = new UIMenuCheckboxItem("Debug mode", debug, "Activate the debug mode?");
            myMenu.AddItem(debug_checkbox);
            myMenu.RefreshIndex();

            myMenu.OnCheckboxChange += (sender, item, checked_) =>
            {
                if (item == debug_checkbox)
                {
                    if (race_initialized || race_started)
                    {
                        UI.Notify("This can't be done during the tasks.");
                        return;
                    }

                    debug = checked_;
                    UI.Notify("~r~debug mode: ~b~" + debug);
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
                route_checkpoints = new List<Vector3>();
                route_blips = new List<Blip>();
            }
            else {
                File.AppendAllText("route.log", "Vector3[] route = {" + Environment.NewLine);
                foreach (Vector3 cp in route_checkpoints) {
                    File.AppendAllText("route.log", String.Format("new Vector3({0}, {1}, {2}),", cp.X, cp.Y, cp.Z) + Environment.NewLine);
                }
                File.AppendAllText("route.log", "};" + Environment.NewLine);
                route_checkpoints = null;

                foreach (Blip b in route_blips) {
                    b.Remove();
                }
                route_blips = null;
            }
        }

        private void startMod() {
            if (!race_initialized &&
                !race_started &&
                currentRace >= 0 &&
                currentRace < races.Length)
            {
                myMenu.Visible = false;
                UI.ShowSubtitle("trying to call race", 1250);

                makePlayerInputName();

                race_initialized = true;
                checkpoints = races[currentRace].getCheckpoints();
                races[currentRace].initRace();
            }
        }
        #endregion
    }
}