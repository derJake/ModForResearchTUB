using GTA;
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
    class RaceJesiah : Script, RaceInterface
    {
        private Tuple<Vector3, Vector3?>[] checkpoints;
        Vehicle raceVehicle;

        int regularIntroSceneLength = 10000;

        public CultureInfo CultureInfo { get; private set; }
        ResourceManager rm;
        Utilities ut;

        public String canonicalName { get; private set; }

        public RaceJesiah(ResourceManager resman, Utilities utils, String taskKey) {
            this.canonicalName = taskKey;
            CultureInfo = CultureInfo.CurrentCulture;
            rm = resman;
            ut = utils;

            // Alamo Sea 
            checkpoints = new Tuple<Vector3, Vector3?>[] {
                new Tuple<Vector3, Vector3?>(new Vector3(-251.6717f, 3919.293f, 39.14542f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-289.006f, 3969.008f, 42.17033f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-348.9973f, 4009.486f, 46.73026f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-387.9517f, 3966.397f, 56.22687f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-435.3084f, 3948.528f, 67.08296f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-489.7459f, 3966.803f, 79.56168f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-557.8207f, 3964.746f, 102.9096f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-627.6522f, 3995.306f, 120.9519f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-692.2209f, 4013.629f, 130.4938f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-756.4674f, 4043.302f, 148.2406f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-826.0171f, 4053.514f, 162.6224f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-885.3699f, 4094.885f, 162.5385f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-989.8645f, 4142.516f, 124.0036f), new Vector3(-969.2205f, 4180.824f, 134.0562f)),
                new Tuple<Vector3, Vector3?>(new Vector3(-1036.606f, 4215.612f, 117.0383f), new Vector3(-1015.542f, 4227.466f, 111.6766f)),
                new Tuple<Vector3, Vector3?>(new Vector3(-1047.547f, 4259.354f, 110.8161f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-1088.466f, 4277.344f, 95.2999f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-1140.096f, 4286.141f, 84.89371f), null),
                new Tuple<Vector3, Vector3?>(new Vector3(-1217.466f, 4300.221f, 74.59245f), null)
            };
            // Raton Canyon
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
            throw new NotImplementedException();
        }

        public void initRace()
        {
            // set time of day
            World.CurrentDayTime = new TimeSpan(11, 45, 0);

            // set weather to rain
            Function.Call(Hash.SET_WEATHER_TYPE_NOW_PERSIST, "EXTRASUNNY");

            var bmsg = BigMessageThread.MessageInstance;

            Game.Player.CanControlCharacter = false;
            var ped = Game.Player.Character;
            ped.IsInvincible = true;
            ped.Position = new Vector3(-223.8517f, 3886.29f, 37.57345f);

            raceVehicle = ut.createCarAt(
                VehicleHash.Sanchez,
                new Vector3(-223, 3886.454f, 37.09313f),
                1.4f
            );

            ped.SetIntoVehicle(raceVehicle, VehicleSeat.Driver);

            //Camera cam = World.CreateCamera(
            //    new Vector3(890.9266f, -3074.217f, 22.59832f),
            //    new Vector3(-20.58238f, 8.537736E-07f, 60.61644f),
            //    64.39999f
            //);

            //cam.IsActive = true;
            //World.RenderingCamera = cam;

            //cam.InterpTo(
            //    World.CreateCamera(
            //        new Vector3(805.2513f, -3043.604f, 5.435494f),
            //        new Vector3(20.04803f, 0f, -65.11039f),
            //        70f
            //    ),
            //    regularIntroSceneLength,
            //    true,
            //    true
            //);

            //bmsg.ShowOldMessage(rm.GetString("terminal_intro_1"), regularIntroSceneLength);
            //Wait(regularIntroSceneLength);

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
