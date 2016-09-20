using GTA;
using GTA.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModForResearchTUB
{
    class Trafficlight : Script
    {
        Vector3 position,
            haltZoneFrom,
            haltZoneTo,
            intersectionFrom,
            intersectionTo;

        public Trafficlight(Vector3 pos, Vector3 hzf, Vector3 hzt, Vector3 isf, Vector3 ist) {
            Position = pos;
            HaltZoneFrom = hzf;
            HaltZoneTo = hzt;
            IntersectionFrom = isf;
            IntersectionTo = ist;
        }

        public Vector3 HaltZoneFrom
        {
            get
            {
                return haltZoneFrom;
            }

            set
            {
                haltZoneFrom = value;
            }
        }

        public Vector3 HaltZoneTo
        {
            get
            {
                return haltZoneTo;
            }

            set
            {
                haltZoneTo = value;
            }
        }

        public Vector3 IntersectionFrom
        {
            get
            {
                return intersectionFrom;
            }

            set
            {
                intersectionFrom = value;
            }
        }

        public Vector3 IntersectionTo
        {
            get
            {
                return intersectionTo;
            }

            set
            {
                intersectionTo = value;
            }
        }

        public Vector3 Position
        {
            get
            {
                return position;
            }

            set
            {
                position = value;
            }
        }
    }
}
