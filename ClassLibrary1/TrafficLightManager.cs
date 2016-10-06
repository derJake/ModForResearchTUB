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

        Prop targetedLight,
            currentTrafficLight;
        List<Trafficlight> trafficLights;
        Vector3 position,
            haltZoneFrom,
            haltZoneTo,
            intersectionFrom,
            intersectionTo;

        public TrafficLightManager() {
            trafficLights = new List<Trafficlight>();
        }

        public void handleOnTick() {
            searchForLights();

            highlightCurrentTrafficLight();
        }

        private void searchForLights() {
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
                foreach (Prop ent in World.GetNearbyProps(rcr.HitCoords, 10))
                {
                    if (trafficSignalHashes.Contains(ent.Model.Hash))
                    {
                        

                        targetedLight = ent;
                    }
                }
            }
        }

        public void handleInput() {
            if (currentTrafficLight == null)
            {
                if (targetedLight != null)
                {
                    currentTrafficLight = targetedLight;
                    position = currentTrafficLight.Position;
                }
            }
            else {
                if (targetedLight != null
                    && targetedLight == currentTrafficLight)
                {
                    // TODO confirm user wants to discard traffic light
                }
                else {
                    // handle zones
                }
            }
        }

        private void highlightCurrentTrafficLight() {
            Vector3? pos = null;
            Color color = Color.Red;

            if (currentTrafficLight != null) {
                pos = currentTrafficLight.Position;
                color = Color.Green;
            }

            if (targetedLight != null) {
                pos = targetedLight.Position;
            }

            if (pos.HasValue)
            {
                World.DrawMarker(
                    MarkerType.VerticalCylinder,
                    pos.Value,
                    new Vector3(),
                    new Vector3(),
                    new Vector3(7.5f, 7.5f, 10f),
                    color
                );
            }
        }
    }
}
