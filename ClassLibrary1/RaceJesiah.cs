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

        private Vector3 vehicleSpawnPosition = new Vector3(-223, 3886.454f, 37.09313f);
        private float vehicleSpawnHeading = 1.4f;

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
                new Tuple<Vector3, Vector3?>(new Vector3(-958.5329f, 4150.155f, 134.4068f), new Vector3(-969.2205f, 4180.824f, 134.0562f)),
                new Tuple<Vector3, Vector3?>(new Vector3(-1040.253f, 4250.594f, 114.3315f), null),
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
            Function.Call(Hash.CANCEL_STUNT_JUMP);
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
                vehicleSpawnPosition,
                vehicleSpawnHeading
            );

            ped.SetIntoVehicle(raceVehicle, VehicleSeat.Driver);

            bmsg.ShowOldMessage(rm.GetString("jesiah_intro_1"), regularIntroSceneLength);

            Camera cam = World.CreateCamera(
                new Vector3(-227.4976f, 3883.823f, 39.39329f),
                new Vector3(-3.245464f, 3.201651E-07f, -50.24673f),
                61.19999f
            );

            cam.IsActive = true;
            World.RenderingCamera = cam;

            cam.InterpTo(
                World.CreateCamera(
                    new Vector3(-212.0282f, 3878.323f, 37.97884f),
                    new Vector3(5.618358f, -1.28066E-06f, 47.25439f),
                    36.40001f
                ),
                regularIntroSceneLength,
                true,
                true
            );

            Wait(regularIntroSceneLength);

            // show ramp
            raceVehicle.Position = new Vector3(-883.2684f, 4096.976f, 163.0778f);

            bmsg.ShowOldMessage(rm.GetString("jesiah_intro_2"), regularIntroSceneLength);

            Camera cam2 = World.CreateCamera(
                new Vector3(-886.0941f, 4089.522f, 165.773f),
                new Vector3(-14.47898f, -2.561321E-06f, 38.35268f),
                50f
            );

            World.RenderingCamera = cam2;

            cam2.InterpTo(
                World.CreateCamera(
                    new Vector3(-931.7852f, 4140.451f, 165.0232f),
                    new Vector3(-36.34506f, 8.537736E-07f, 56.64264f),
                    50f
                ),
                regularIntroSceneLength,
                true,
                true
            );

            Wait(regularIntroSceneLength);

            bmsg.ShowOldMessage(rm.GetString("jesiah_intro_3"), regularIntroSceneLength);

            Camera cam3 = World.CreateCamera(
                new Vector3(-973.6657f, 4148.937f, 157.4709f),
                new Vector3(-37.56199f, -2.134434E-06f, 4.579413f),
                56.39999f
            );
            World.RenderingCamera = cam3;

            Wait(regularIntroSceneLength);

            bmsg.ShowOldMessage(rm.GetString("jesiah_intro_4"), regularIntroSceneLength);

            Camera cam4 = World.CreateCamera(
                new Vector3(-1048.025f, 4239.32f, 145.8998f),
                new Vector3(-12.56698f, -1.28066E-06f, -145.7042f),
                50f
            );
            World.RenderingCamera = cam4;

            Wait(regularIntroSceneLength);

            // reset vehicle to start position

            raceVehicle.Position = vehicleSpawnPosition;
            raceVehicle.Heading = vehicleSpawnHeading;

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
