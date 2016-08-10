using GTA;
using GTA.Math;
using GTA.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModForResearchTUB
{
    public enum Direction {
        Left = 0,
        Right = 1,
        Forward = 2,
        Backward = 3,
        Up = 4,
        Down = 5,
        TurnLeft = 6,
        TurnRight = 7,
        TurnDown = 8,
        TurnUp = 9
    };

    public class Utilities
    {
        private List<Vehicle> cars;
        private List<Ped> peds;
        private Camera cam;

        public Utilities() {
            cars = new List<Vehicle>();
            peds = new List<Ped>();
        }

        public Ped createPedAt(PedHash hash, Vector3 pos)
        {
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

            throw new Exception("ped model could not be loaded");
        }

        public Vehicle createCarAt(VehicleHash carmodelhash, Vector3 coordinates, float heading)
        {
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

        public void cleanUp()
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

        public void cloneCamera() {
            cam = World.CreateCamera(
                World.RenderingCamera.Position,
                World.RenderingCamera.Rotation,
                World.RenderingCamera.FieldOfView
                );
            Function.Call(Hash.RENDER_SCRIPT_CAMS, 1, 0, cam, 0, 0);
        }

        public void moveCamera(Direction dir, float amount) {
            if (cam == null) {
                cloneCamera();
            }
            var pos = cam.Position;
            var rot = cam.Rotation;
            var fv = getCamForwardVector(cam);
            var lv = new Vector3(-fv.Y, fv.X, 0);
            
            switch (dir) {
                case Direction.Left:
                    pos += amount*lv;
                    break;
                case Direction.Right:
                    pos -= amount*lv;
                    break;
                case Direction.Forward:
                    pos += amount*fv;
                    break;
                case Direction.Backward:
                    pos -= amount*fv;
                    break;
                case Direction.Up:
                    pos.Z += amount;
                    break;
                case Direction.Down:
                    pos.Z += amount;
                    break;
                case Direction.TurnLeft:
                    cam.Rotation = new Vector3(rot.X, rot.Y, rot.Z - amount);
                    break;
                case Direction.TurnRight:
                    cam.Rotation = new Vector3(rot.X, rot.Y, rot.Z + amount);
                    break;
                case Direction.TurnUp:
                    cam.Rotation = new Vector3(rot.X + amount, rot.Y, rot.Z);
                    break;
                case Direction.TurnDown:
                    cam.Rotation = new Vector3(rot.X - amount, rot.Y, rot.Z);
                    break;
            }
        }

        public void changeCamFieldOfView(Direction dir, float amount) {
            if (cam == null)
            {
                cloneCamera();
            }

            switch (dir) {
                case Direction.Up:
                    cam.FieldOfView += amount;
                    break;
                case Direction.Down:
                    cam.FieldOfView -= amount;
                    break;
            }
        }

        public void deleteScriptCams() {
            World.DestroyAllCameras();
            World.RenderingCamera = null;
        }

        public Vector3 getCamForwardVector(Camera camera) {
            return new Vector3(
                Convert.ToSingle(Math.Cos(camera.Rotation.Z) * Math.Cos(camera.Rotation.X)),
                Convert.ToSingle(Math.Sin(camera.Rotation.Z) * Math.Cos(camera.Rotation.X)),
                Convert.ToSingle(Math.Sin(camera.Rotation.X))
            );
        }
    }
}
