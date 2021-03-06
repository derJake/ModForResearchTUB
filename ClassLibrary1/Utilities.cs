﻿using GTA;
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

    public class Utilities : Script
    {
        #region classVariables
        private List<Vehicle> cars;
        private List<Ped> peds;
        private List<Blip> blips;
        private List<int> markers;
        private Camera cam;
        private bool hasCamChanged = false;
        #endregion classVariables

        public Utilities() {
            cars = new List<Vehicle>();
            peds = new List<Ped>();
            blips = new List<Blip>();
            markers = new List<int>();
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

        public void addBlip(Blip blip) {
            blips.Add(blip);
        }

        public void addMarker(int markerId) {
            markers.Add(markerId);
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

            foreach (Blip blip in blips) {
                blip.Remove();
            }

            foreach (int markerId in markers) {
                Function.Call(Hash.DELETE_CHECKPOINT, markerId);
            }
        }

        public Camera cloneCamera()
        {
            Vector3 pos, rot;
            float fov;
            try
            {
                pos = GameplayCamera.Position;
            }
            catch (Exception e)
            {
                pos = Game.Player.Character.Position;
            }
            try
            {
                rot = GameplayCamera.Rotation;
            }
            catch (Exception e)
            {
                rot = new Vector3(0, 0, 0);
            }
            try
            {
                fov = GameplayCamera.FieldOfView;
            }
            catch (Exception e)
            {
                fov = 50;
            }

            cam = World.CreateCamera(
                pos,
                rot,
                fov
                );
            Function.Call(Hash.RENDER_SCRIPT_CAMS, true, false, cam, 0, 0);
            return cam;
        }

        public void moveCamera(Direction dir, float amount) {
            if (cam == null) {
                return;
            }
            var pos = cam.Position;
            var rot = cam.Rotation;
            var fv = rotationToDirection(rot);
            var lv = new Vector3(-fv.Y, fv.X, 0);
            
            switch (dir) {
                case Direction.Left:
                    cam.Position = pos + amount*lv;
                    break;
                case Direction.Right:
                    cam.Position = pos - amount*lv;
                    break;
                case Direction.Forward:
                    cam.Position = pos + amount*fv;
                    break;
                case Direction.Backward:
                    cam.Position = pos - amount*fv;
                    break;
                case Direction.Up:
                    cam.Position = new Vector3(pos.X, pos.Y, pos.Z + amount);
                    break;
                case Direction.Down:
                    cam.Position = new Vector3(pos.X, pos.Y, pos.Z - amount);
                    break;
                case Direction.TurnLeft:
                    cam.Rotation = new Vector3(rot.X, rot.Y, rot.Z + amount);
                    break;
                case Direction.TurnRight:
                    cam.Rotation = new Vector3(rot.X, rot.Y, rot.Z - amount);
                    break;
                case Direction.TurnUp:
                    cam.Rotation = new Vector3(rot.X + amount, rot.Y, rot.Z);
                    break;
                case Direction.TurnDown:
                    cam.Rotation = new Vector3(rot.X - amount, rot.Y, rot.Z);
                    break;
            }

            hasCamChanged = true;
        }

        public void changeCamFieldOfView(Direction dir, float amount) {
            if (cam == null)
            {
                return;
            }

            switch (dir) {
                case Direction.Up:
                    cam.FieldOfView += amount;
                    break;
                case Direction.Down:
                    cam.FieldOfView -= amount;
                    break;
            }

            hasCamChanged = true;
        }

        public void setScriptCam(Camera camera) {
            cam = camera;
        }

        public void activateScriptCam() {
            if (cam == null) {
                return;
            }
            Function.Call(Hash.RENDER_SCRIPT_CAMS, true, false, cam, 0, 0);
        }

        public void deactivateScriptCam() {
            Function.Call(Hash.RENDER_SCRIPT_CAMS, false, false, cam, 0, 0);
        }

        public void deleteScriptCams() {
            if (cam == null) {
                return;
            }
            deactivateScriptCam();
            Function.Call(Hash.SET_CAM_ACTIVE, cam, false);
            Function.Call(Hash.DESTROY_CAM, cam, true);
            cam = null;
        }

        public Vector3 rotationToDirection(Vector3 rotationVector) {
            double theta = degreeToRadians(rotationVector.X + 90),
                phi = degreeToRadians(rotationVector.Z + 90);

            return new Vector3(
                Convert.ToSingle(Math.Sin(theta) * Math.Cos(phi)),
                Convert.ToSingle(Math.Sin(theta) * Math.Sin(phi)),
                -Convert.ToSingle(Math.Cos(theta))
            );
        }

        public double degreeToRadians(double degrees) {
            return degrees * Math.PI / 180;
        }

        public int mod(int x, int m)
        {
            int r = x % m;
            return r < 0 ? r + m : r;
        }

        public bool getHasCamChanged() {
            bool value = hasCamChanged;
            hasCamChanged = false;

            return value;
        }
    }
}
