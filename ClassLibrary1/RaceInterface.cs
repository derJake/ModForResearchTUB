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
        bool checkRaceStartCondition();
        void startRace();
        void handleOnTick();
        void finishRace();

        List<Tuple<String, List<Tuple<String, double>>>> getCollectedData();
        Tuple<Vector3, Nullable<Vector3>>[] getCheckpoints();
    }
}
