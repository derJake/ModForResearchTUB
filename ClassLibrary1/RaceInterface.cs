using GTA.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModForResearchTUB
{
    interface RaceInterface
    {
        void initRace();
        void startRace();
        void handleOnTick();
        void finishRace();

        void setCurrentCheckpoint(int index);
        Vector3[] getCheckpoints();
    }
}
