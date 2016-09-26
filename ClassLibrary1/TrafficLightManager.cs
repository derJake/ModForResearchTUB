using GTA;
using GTA.Math;
using NativeUI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModForResearchTUB
{
    class TrafficLightManager : Script
    {
        List<int> trafficSignalHashes = new List<int> {
            -655644382,
            862871082,
            1043035044
        };

        public TrafficLightManager() {
        }

        public void handleOnTick() {
            UI.ShowHudComponentThisFrame(HudComponent.Reticle);
            var ped = Game.Player.Character;
            Vector3 fv = GameplayCamera.Position - ped.Position;
            fv.Normalize();

            RaycastResult rcr = World.Raycast(
                ped.Position,
                ped.Position - 210 * fv,
                IntersectOptions.Map
                );

            if (rcr.DitHitAnything
                && rcr.HitCoords != null)
            {
                foreach (Prop ent in World.GetNearbyProps(rcr.HitCoords, 10)) {
                    if (trafficSignalHashes.Contains(ent.Model.Hash))
                    {
                        World.DrawMarker(
                            MarkerType.VerticalCylinder,
                            ent.Position,
                            new Vector3(),
                            new Vector3(),
                            new Vector3(7.5f, 7.5f, 10f),
                            Color.Red
                        );
                    }
                }
            }
        }
    }
}
