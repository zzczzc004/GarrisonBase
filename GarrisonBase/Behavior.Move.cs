﻿using System.Threading.Tasks;
using Styx;
using Styx.Common;

namespace Herbfunk.GarrisonBase
{
    public partial class Behaviors
    {
        public class BehaviorMove : Behavior
        {
            public override BehaviorType Type { get { return BehaviorType.MoveTo; } }

            public BehaviorMove(WoWPoint loc, float distance=10f)
            {
                _movement = new Movement(loc, distance);
            }
            public BehaviorMove(WoWPoint[] locs, float distance = 10f)
            {
                _movement = new Movement(locs, distance);
            }

            private Movement _movement;

            public override async Task<bool> BehaviorRoutine()
            {
                if (IsDone) return false;

                if (await base.BehaviorRoutine()) return true;


                if (await _movement.MoveTo())
                    return true;

                return false;
            }
        }
    }
}
