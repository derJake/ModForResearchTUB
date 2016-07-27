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
    class Utilities
    {
        private List<Vehicle> cars;
        private List<Ped> peds;

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
    }
}
