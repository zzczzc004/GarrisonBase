﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Documents;
using Herbfunk.GarrisonBase.Cache.Enums;
using Styx;
using Styx.Common;
using Styx.Common.Helpers;
using Styx.Helpers;
using Styx.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.World;
using Styx.WoWInternals.WoWObjects;
using MathEx = Tripper.Tools.Math.MathEx;

namespace Herbfunk.GarrisonBase.Cache.Objects
{
    public abstract class C_WoWObject
    {
        protected C_WoWObject(WoWObject obj)
        {
            RefWoWObject = obj;
            Location = obj.Location;
            Guid = obj.Guid;
            Entry = obj.Entry;
            Name = obj.Name;
            Type = obj.Type;
            BlacklistType = BlacklistType.None;
            LineOfSight = new CachedValue<bool>(() => TestLineOfSight(this));

        }



        public WoWObject RefWoWObject;
        /// <summary>
        /// Used to update the object reference to the HB WoWObject. (Used for cached objects that ignore removal)
        /// </summary>
        /// <param name="obj"></param>
        public virtual void UpdateReference(WoWObject obj)
        {
            RefWoWObject = obj;
        }

        public WoWPoint Location { get; set; }
        public WoWGuid Guid { get; set; }
        public uint Entry { get; set; }
        public string Name { get; set; }
        public WoWObjectType Type { get; set; }
        public BlacklistType BlacklistType { get; set; }
        public bool ShouldLoot { get; set; }
        public bool ShouldKill { get; set; }

        public WaitTimer IgnoredTimer
        {
            get { return _ignoredTimer; }
            set
            {
                _ignoredTimer = value;
                _ignoredTimer.Reset();
            }
        }
        private WaitTimer _ignoredTimer= new WaitTimer(new TimeSpan(0,0,0,0,0));
        public bool IgnoresRemoval { get; set; }
        public float InteractRange
        {
            get { return _interactRange; }
            set { _interactRange = value; }
        }
        private float _interactRange=4f;

        public WoWObjectTypes SubType
        {
            get { return _subType; }
            set { _subType = value; }
        }
        private WoWObjectTypes _subType= WoWObjectTypes.Unknown;

        //
        public double Distance
        {
            get { return Player.Location.Distance(Location); }
        }

        public double DistanceSqr
        {
            get
            {
                return Player.Location.DistanceSqr(Location);
            }
        }

        public double ZDifference
        {
            get
            {
                if (Player.Location.Z>Location.Z)
                    return Player.Location.Z - Location.Z;

                return Location.Z - Player.Location.Z;
            }
        }

        
        ///<summary>
        ///Flag that determines if the object should be removed from the collection.
        ///</summary>
        public bool NeedsRemoved
        {
            get
            {
                return removal_;
            }
            set
            {
                removal_ = value;
                //This helps reduce code by flagging this here instead of after everytime we flag removal of an object!
                if (value) ObjectCacheManager.RemovalCheck = true;
            }
        }
        internal bool removal_;

        public int LoopsUnseen { get; set; }


        public virtual bool IsValid
        {
            get
            {
                //Check Object
                ObjectValidCheck = RefWoWObject != null && 
                    RefWoWObject.IsValid && 
                    RefWoWObject.BaseAddress != IntPtr.Zero;

                return ObjectValidCheck;
            }
        }
        internal bool ObjectValidCheck=true;

        /// <summary>
        /// This is used to reset and update values that need to reference the actual WoWObject.
        /// </summary>
        /// <returns></returns>
        public virtual bool Update()
        {
            if (!IsValid) return false;
            LineOfSight.Reset();
            return true;
        }

        /// <summary>
        /// Determines if object should be considered for targeting.
        /// </summary>
        public virtual bool ValidForTargeting
        {
            get
            {
                if (!IsValid)
                {
                    if (!IgnoresRemoval)
                    {
                        NeedsRemoved = true;
                        IgnoredTimer = WaitTimer.ThirtySeconds;
                    }
                    return false;
                }

                if (!IgnoredTimer.IsFinished)
                {
                    return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Determines if object should be considered for looting.
        /// </summary>
        public virtual bool ValidForLooting
        {
            get
            {
                if (!IsValid)
                {
                    if (!IgnoresRemoval)
                    {
                        NeedsRemoved = true;
                        IgnoredTimer = WaitTimer.ThirtySeconds;
                    }
                    return false;
                }

                if (!IgnoredTimer.IsFinished)
                {
                    return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Determines if object should be considered for combat.
        /// </summary>
        public virtual bool ValidForCombat
        {
            get
            {
                if (!IsValid)
                {
                    if (!IgnoresRemoval)
                    {
                        NeedsRemoved = true;
                        IgnoredTimer = WaitTimer.ThirtySeconds;
                    }

                    return false;
                }

                if (!IgnoredTimer.IsFinished)
                {
                    return false;
                }

                return true;
            }
        }


        private bool _requiresUpdate = true;
        public bool RequiresUpdate
        {
            get { return _requiresUpdate; }
            set { _requiresUpdate = value; }
        }


        public bool WithinInteractRange
        {
            get
            {
                if (!IsValid) return false;
                float num = InteractRange - 0.1f;
                return DistanceSqr < num * num;
                //return ref_WoWObject.WithinInteractRange;
            }
        }



        public void Interact()
        {
            if (!IsValid) return;
            RefWoWObject.Interact();
        }

        public CachedValue<bool> LineOfSight;
        internal bool LineofsightResult = false;
        public WoWPoint LineofsightPoint
        {
            get
            {
                if (_lineofsightPoint == WoWPoint.Zero) 
                    return Location;
                return _lineofsightPoint;
            }
            set { _lineofsightPoint = value; }
        }
        private WoWPoint _lineofsightPoint= WoWPoint.Zero;

        public WaitTimer LineofSightWaitTimer
        {
            get
            {
                return _lineofSightWaitTimer;
            }
            set
            {
                _lineofSightWaitTimer = value;
                _lineofSightWaitTimer.Reset();
            }
        }
        private WaitTimer _lineofSightWaitTimer=WaitTimer.FiveSeconds;

        internal static bool TestLineOfSight(C_WoWObject obj)
        {
            if (obj.LineofsightResult && obj.Distance <= obj.InteractRange)
                return true;

            if (!obj._lineofSightWaitTimer.IsFinished && (obj.Distance>20f || obj.LineofSightWaitTimer.TimeLeft.TotalMilliseconds>750))
                return obj.LineofsightResult;
            

            var neededFacing=WoWMathHelper.CalculateNeededFacing(Player.Location, obj.Location);

            var testPoints = new List<WoWPoint>
            {
                WoWMathHelper.GetPointAt(obj.Location, obj.InteractRange, neededFacing - 3.141593f, 0f),
                WoWMathHelper.GetPointAt(obj.Location, obj.InteractRange, neededFacing - 2.356194f, 0f),
                WoWMathHelper.GetPointAt(obj.Location, obj.InteractRange, neededFacing - 1.570796f, 0f),
                WoWMathHelper.GetPointAt(obj.Location, obj.InteractRange, neededFacing - 0.7853982f, 0f),
                WoWMathHelper.GetPointAt(obj.Location, obj.InteractRange, neededFacing, 0f),
                WoWMathHelper.GetPointAt(obj.Location, obj.InteractRange, neededFacing + 3.141593f, 0f),
                WoWMathHelper.GetPointAt(obj.Location, obj.InteractRange, neededFacing + 2.356194f, 0f),
                WoWMathHelper.GetPointAt(obj.Location, obj.InteractRange, neededFacing + 1.570796f, 0f),
                WoWMathHelper.GetPointAt(obj.Location, obj.InteractRange, neededFacing + 0.7853982f, 0f)
            };

            foreach (var testPoint in testPoints)
            {
                if (GameWorld.IsInLineOfSight(Player.TraceLinePosition,testPoint.Add(0f,0f,1.24f)))
                {
                    obj.LineofsightResult = true;
                    obj._lineofsightPoint = testPoint;
                    break;
                }
            }


            obj.LineofSightWaitTimer.Reset();
            return obj.LineofsightResult;
        }


 

        public override bool Equals(object obj)
        {
            //Check for null and compare run-time types. 
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            else
            {
                C_WoWObject p = (C_WoWObject)obj;
                return Guid == p.Guid;
            }
        }


        public override string ToString()
        {
            return String.Format("{0}, //{1}\r\n" +
                                 "[{2}] {3} [{4}]\r\n" +
                                 "{5} ({6})\r\n" +
                                 "LOS {7} ( {8} ) Tested {9}ms\r\n" +
                                 "Valid {10}\r\n" +
                                 "Update {11}",
                                 Entry,Name,Guid, Type, SubType,
                                 Location, Distance, 
                                 LineofsightResult, _lineofsightPoint.ToString(), LineofSightWaitTimer.TimeLeft.TotalMilliseconds,
                                 ObjectValidCheck,
                                 RequiresUpdate);
        }


    }
}
