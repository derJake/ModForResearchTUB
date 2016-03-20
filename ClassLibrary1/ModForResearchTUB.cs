﻿#region Using references

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
        List<int> trafficSignalHashes = new List<int>(3);
        Blip currentBlip = null;
        Vector3[] checkpoints;
        int currentMarker;
        bool car_config_done = false;
        bool race_started = false;
        
        int currentCheckpoint = -1;

        int currentRace = -1;
        RaceInterface[] races;
        delegate void CurrentInitHandler();
        delegate void CurrentStartHandler();
        delegate void CurrentOnTickHandler();
        delegate void CurrentFinishHandler();
        CurrentInitHandler currentInit;
        CurrentStartHandler currentStart;
        CurrentOnTickHandler currentOnTick;
        CurrentFinishHandler currentFinish;

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

        int car_health;

        // Main Script
        public Main()
        {
            trafficSignalHashes.Add(-655644382);
            trafficSignalHashes.Add(862871082);
            trafficSignalHashes.Add(1043035044);

            // separator to show start of new log
            // TO DO: Should this be on a day-by-day basis?
            Logger.Log("----------------------------------------------------------");

            // registers the races / courses / whatever you want to call it
            setUpRaces();

            // delegates handling to the current race object
            setCurrentRaceFunctions();

            // World.CreateProp(new Model(-1359996601), Game.Player.Character.Position, new Vector3(0f, 5f, 0f), false, false);

            // Tick Interval
            //Interval = 10;

            // Initialize Events
            Tick += this.OnTickEvent;
            KeyDown += this.KeyDownEvent;
            KeyUp += this.KeyUpEvent;

            UI.ShowSubtitle("Press [F10] to start first race", 1250);
        }

        private void setUpRaces() {
            races = new RaceInterface[1];
            races[0] = new RaceCarvsCar();
            currentRace = 0;
        }

        private void setCurrentRaceFunctions() {
            currentInit = races[currentRace].initRace;
            currentStart = races[currentRace].startRace;
            currentOnTick = races[currentRace].handleOnTick;
            currentFinish = races[currentRace].finishRace;
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
                if (race_started)
                {
                    logVariables(res, safe);

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

                    if (currentCheckpoint >= 0)
                    {
                        new UIResText(string.Format("currentCheckpoint is {0}/{1}", currentCheckpoint, checkpoints.Length), new Point(Convert.ToInt32(res.Width) - safe.X - 180, Convert.ToInt32(res.Height) - safe.Y - 275), 0.3f, Color.White).Draw();
                    }

                    if (Game.Player.Character.IsInRangeOf(checkpoints[currentCheckpoint], 5f))
                    {

                        // show the players current position
                        UI.ShowSubtitle(string.Format("checkpoint {0}/{1} reached", currentCheckpoint + 1, checkpoints.Length), 3000);
                        UI.Notify(string.Format("checkpoint {0}/{1} reached", currentCheckpoint + 1, checkpoints.Length));

                        // FINISHED, if last checkpoint is reached
                        if ((currentCheckpoint + 1) == checkpoints.Length)
                        {
                            currentFinish();

                            writeRaceDataToLog();
                            clearStuffUp();
                            return;
                        }

                        currentCheckpoint++;
                        drawCurrentCheckpoint();
                    }
                }
                else if (races[currentRace].checkRaceStartCondition()) {
                    // start the race and set first marker + blip
                    race_started = true;
                    currentStart();
                    drawCurrentCheckpoint();
                }
            }
        }

        protected void drawCurrentCheckpoint() {
            // play sound
            Audio.PlaySoundFrontend("NAV_UP_DOWN", "HUD_FRONTEND_DEFAULT_SOUNDSET");

            // set next checkpoint
            if (currentMarker > 0)
            {
                Function.Call(Hash.DELETE_CHECKPOINT, currentMarker);
            }
            // select the first checkpoint
            Vector3 coords = checkpoints[currentCheckpoint];
            Vector3 nextCoords;
            int type;
            if (currentCheckpoint < (checkpoints.Length - 1))
            {
                // if there are checkpoints left, get the next one's coordinates
                nextCoords = checkpoints[currentCheckpoint + 1];
                type = 2;
            }
            else {
                type = 14;
                nextCoords = new Vector3(0, 0, 0);
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

            if (currentBlip != null)
            {
                currentBlip.Remove();
            }
            currentBlip = World.CreateBlip(checkpoints[currentCheckpoint]);
            Function.Call(Hash.SET_BLIP_ROUTE, currentBlip, true);
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
                    currentInit();
                    checkpoints = races[currentRace].getCheckpoints();
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
            Logger.Log(String.Format("car health: {0}", car_health));
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

            car_health = -1;
        }

        #endregion
    }
}