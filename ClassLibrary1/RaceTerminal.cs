﻿using GTA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA.Math;
using System.Resources;
using System.Globalization;
using GTA.Native;
using NativeUI;

namespace ModForResearchTUB
{
    class RaceTerminal : Script, RaceInterface
    {
        private Tuple<Vector3, Vector3?>[] checkpoints;
        private Vector3 rampLightPosition = new Vector3(832.2669f, -2367f, 33.28733f);
        Vehicle raceVehicle;

        int regularIntroSceneLength = 10000;

        public CultureInfo CultureInfo { get; private set; }
        ResourceManager rm;
        Utilities ut;

        public String canonicalName { get; private set; }

        public RaceTerminal(ResourceManager resman, Utilities utils, String taskKey) {
            this.canonicalName = taskKey;
            CultureInfo = CultureInfo.CurrentCulture;
            rm = resman;
            ut = utils;

            checkpoints = new Tuple<Vector3, Vector3?>[] {
                new Tuple<Vector3, Vector3?>(new Vector3(789.1943f, -3023.545f, 4.861694f), null),
	            new Tuple<Vector3, Vector3?>(new Vector3(771.9685f, -3002.299f, 4.852684f), null),
	            new Tuple<Vector3, Vector3?>(new Vector3(748.5812f, -2988.023f, 4.800598f), null),
	            new Tuple<Vector3, Vector3?>(new Vector3(743.1962f, -2935.766f, 4.80072f), null),
	            new Tuple<Vector3, Vector3?>(new Vector3(743.5944f, -2868.998f, 5.063251f), null),
	            new Tuple<Vector3, Vector3?>(new Vector3(743.2222f, -2823.746f, 5.254286f), null),
	            new Tuple<Vector3, Vector3?>(new Vector3(742.1288f, -2757.158f, 5.559158f), null),
	            new Tuple<Vector3, Vector3?>(new Vector3(741.46f, -2662.995f, 11.56062f), null),
	            new Tuple<Vector3, Vector3?>(new Vector3(741.0419f, -2609.744f, 17.17934f), null),
	            new Tuple<Vector3, Vector3?>(new Vector3(739.6995f, -2562.756f, 18.4841f), null),
	            new Tuple<Vector3, Vector3?>(new Vector3(753.1043f, -2423.506f, 18.9865f), new Vector3(769.5274f, -2459.852f, 19.32144f)),
	            new Tuple<Vector3, Vector3?>(new Vector3(753.8768f, -2397.184f, 19.87691f), new Vector3(825.9399f, -2437.061f, 24.38288f)),
	            new Tuple<Vector3, Vector3?>(new Vector3(758.623f, -2363.549f, 22.13659f), new Vector3(848.8362f, -2319.517f, 29.33731f)),
	            new Tuple<Vector3, Vector3?>(new Vector3(762.8474f, -2320.73f, 25.4758f), new Vector3(855.2336f, -2233.166f, 29.40659f)),
	            new Tuple<Vector3, Vector3?>(new Vector3(764.9367f, -2285.781f, 27.44416f), new Vector3(861.1987f, -2173.303f, 29.58646f)),
	            new Tuple<Vector3, Vector3?>(new Vector3(767.9237f, -2253.358f, 28.23525f), new Vector3(870.136f, -2048.595f, 29.39186f)),
	            new Tuple<Vector3, Vector3?>(new Vector3(771.8262f, -2211.243f, 28.23751f), new Vector3(882.5863f, -1944.593f, 29.38597f)),
	            new Tuple<Vector3, Vector3?>(new Vector3(772.5306f, -2179.828f, 28.0921f), new Vector3(898.8347f, -1796.529f, 29.61712f)),
	            new Tuple<Vector3, Vector3?>(new Vector3(779.6558f, -2122.186f, 28.23111f), new Vector3(918.048f, -1764.156f, 29.8816f)),
	            new Tuple<Vector3, Vector3?>(new Vector3(786.9689f, -2045.898f, 28.19247f), new Vector3(960.0214f, -1744.569f, 30.20039f)),
	            new Tuple<Vector3, Vector3?>(new Vector3(790.4257f, -2011.813f, 28.23874f), new Vector3(970.0087f, -1661.991f, 28.2218f)),
	            new Tuple<Vector3, Vector3?>(new Vector3(799.9363f, -1954.059f, 28.24397f), new Vector3(970.6074f, -1589.803f, 29.59485f)),
	            new Tuple<Vector3, Vector3?>(new Vector3(815.4943f, -1890.099f, 28.29138f), new Vector3(971.4412f, -1552.299f, 29.61152f)),
	            new Tuple<Vector3, Vector3?>(new Vector3(828.8164f, -1816.888f, 28.12889f), new Vector3(971.6487f, -1525.2f, 30.02095f)),
	            new Tuple<Vector3, Vector3?>(new Vector3(837.7931f, -1723.208f, 28.3188f), new Vector3(971.2016f, -1489.578f, 30.241f)),
	            new Tuple<Vector3, Vector3?>(new Vector3(840.5416f, -1684.013f, 28.36295f), new Vector3(970.5289f, -1457.915f, 30.34735f)),
	            new Tuple<Vector3, Vector3?>(new Vector3(845.5842f, -1629.794f, 29.98753f), new Vector3(969.5491f, -1448.927f, 30.10118f)),
	            new Tuple<Vector3, Vector3?>(new Vector3(838.0269f, -1555.333f, 28.82468f), new Vector3(969.7175f, -1430.76f, 30.39734f)),
	            new Tuple<Vector3, Vector3?>(new Vector3(813.6743f, -1455.924f, 26.07412f), new Vector3(974.4886f, -1419.688f, 30.41562f)),
	            new Tuple<Vector3, Vector3?>(new Vector3(868.1639f, -1445.845f, 28.20782f), new Vector3(980.6461f, -1416.286f, 30.25241f)),
	            new Tuple<Vector3, Vector3?>(new Vector3(934.9111f, -1440.128f, 30.4038f), new Vector3(985.1896f, -1412.939f, 30.23849f)),
	            new Tuple<Vector3, Vector3?>(new Vector3(990.7612f, -1408.204f, 30.25992f), null),
            };
        }

        public bool checkAlternativeBreakCondition()
        {
            return false;
        }

        public bool checkRaceStartCondition()
        {
            // check which car player is using
            return (Game.Player.Character.IsInVehicle() && Game.Player.Character.CurrentVehicle.Equals(raceVehicle));
        }

        public void finishRace()
        {
            Game.Player.CanControlCharacter = false;
            Game.Player.Character.IsInvincible = true;

            // camera FX
            Function.Call(Hash._START_SCREEN_EFFECT, "HeistCelebPass", 0, true);
            if (Game.Player.Character.IsInVehicle())
                Game.Player.Character.CurrentVehicle.HandbrakeOn = true;
            World.DestroyAllCameras();
            World.RenderingCamera = World.CreateCamera(
                new Vector3(979.2184f, -1403.284f, 32.61693f),
                new Vector3(0.5409608f, -2.934847E-07f, -88.0972f),
                91.60009f
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

            raceVehicle.Delete();
        }

        public string getCanonicalName()
        {
            return canonicalName;
        }

        public Tuple<Vector3, Vector3?>[] getCheckpoints()
        {
            return checkpoints;
        }

        public Dictionary<string, Dictionary<string, double>> getCollectedData()
        {
            throw new NotImplementedException();
        }

        public Dictionary<string, float> getSingularDataValues()
        {
            throw new NotImplementedException();
        }

        public void handleOnTick(object sender, EventArgs e)
        {
            World.DrawSpotLightWithShadow(
                rampLightPosition,
                Vector3.WorldDown - new Vector3(0, 0.2f, 0),
                System.Drawing.Color.White,
                30, // distance
                60, // brightness
                1, // roundness
                40, // radius
                15 // fadeout
            );
        }

        public void initRace()
        {
            // set time of day
            World.CurrentDayTime = new TimeSpan(15, 35, 0);

            // set weather to rain
            Function.Call(Hash.SET_WEATHER_TYPE_NOW_PERSIST, "CLEAR");

            var bmsg = BigMessageThread.MessageInstance;

            Game.Player.CanControlCharacter = false;
            var ped = Game.Player.Character;
            ped.IsInvincible = true;

            // intro
            ped.Position = new Vector3(834.0682f, -2460.903f, 26.09346f);

            bmsg.ShowOldMessage(rm.GetString("terminal_intro_1"), regularIntroSceneLength);

            Camera cam = World.CreateCamera(
                new Vector3(825.8586f, -2450.914f, 25.3525f),
                new Vector3(5.946738f, 1.334021E-07f, -3.860392f),
                50f
            );

            Camera cam2 = World.CreateCamera(
                new Vector3(829.6897f, -2374.563f, 29.85822f),
                new Vector3(0.3948801f, 2.134434E-07f, -23.37468f),
                62.79999f
            );

            cam.IsActive = true;
            World.RenderingCamera = cam;
            cam.InterpTo(cam2, 10000, true, true);

            Wait(regularIntroSceneLength);

            bmsg.ShowOldMessage(rm.GetString("terminal_intro_2"), regularIntroSceneLength);

            Camera cam3 = World.CreateCamera(
                new Vector3(846.8107f, -2328.636f, 49.60936f),
                new Vector3(-35.63461f, 1.28066E-06f, -8.205003f),
                82.00005f
            );

            World.RenderingCamera = cam2;
            cam2.InterpTo(cam3, 10000, true, true);

            Wait(regularIntroSceneLength);

            // regular route
            bmsg.ShowOldMessage(rm.GetString("terminal_intro_3"), regularIntroSceneLength);

            Camera cam4 = World.CreateCamera(
                new Vector3(747.2258f, -2460.846f, 34.20919f),
                new Vector3(-25.7344f, 4.268869E-07f, -16.06508f),
                74.80003f
            );
            World.RenderingCamera = cam4;

            Camera cam5 = World.CreateCamera(
                new Vector3(771.1152f, -2240.101f, 30.27721f),
                new Vector3(2.739829f, -2.668042E-08f, -9.541548f),
                50.01287f
            );
            cam4.InterpTo(cam5, 10000, true, true);

            Wait(regularIntroSceneLength);

            // show target
            bmsg.ShowOldMessage(rm.GetString("terminal_intro_4"), regularIntroSceneLength);
            ped.Position = new Vector3(950.2383f, -1409.826f, 31.48705f);
            Camera cam6 = World.CreateCamera(
                new Vector3(947.9563f, -1434.416f, 32.11513f),
                new Vector3(4.151041f, 6.403302E-07f, -45.72819f),
                33.20002f
            );
            World.RenderingCamera = cam6;
            Wait(regularIntroSceneLength);

            // take ped to actual harbor terminal
            ped.Position = new Vector3(806.9824f, -3042.711f, 5.74216f);

            raceVehicle = ut.createCarAt(VehicleHash.Bifta, new Vector3(808.551f, -3041.54f, 5.274242f), 56.59f);

            ped.SetIntoVehicle(raceVehicle, VehicleSeat.Driver);

            Camera camLast = World.CreateCamera(
                new Vector3(890.9266f, -3074.217f, 22.59832f),
                new Vector3(-20.58238f, 8.537736E-07f, 60.61644f),
                64.39999f
            );

            camLast.IsActive = true;
            World.RenderingCamera = camLast;

            bmsg.ShowOldMessage(rm.GetString("terminal_intro_5"), regularIntroSceneLength);

            camLast.InterpTo(
                World.CreateCamera(
                    new Vector3(805.2513f, -3043.604f, 5.435494f),
                    new Vector3(20.04803f, 0f, -65.11039f),
                    70f
                ),
                regularIntroSceneLength / 2,
                true,
                true
            );

            Wait(regularIntroSceneLength);

            World.DestroyAllCameras();
            World.RenderingCamera = null;
            Game.Player.CanControlCharacter = true;
            ped.IsInvincible = false;
        }

        public void startRace()
        {
        }
    }
}
