﻿using System.Collections.Generic;
using System.Linq;
using Herbfunk.GarrisonBase.Cache;
using Herbfunk.GarrisonBase.Garrison.Enums;
using Herbfunk.GarrisonBase.Garrison.Objects;
using Styx;
using Styx.CommonBot.Profiles;

namespace Herbfunk.GarrisonBase.Garrison
{
    /// <summary>
    /// Object Manager for garrison related things:
    /// -Missions
    /// -Followers
    /// -Buildings
    /// </summary>
    public static class GarrisonManager
    {

        public static int GarrisonLevel { get; set; }
        public static int CommandTableEntryId { get; set; }
        public static int SellRepairNpcId { get; set; }
        public static int GarrisonResourceCacheEntryId { get; set; }

        /// <summary>
        /// Buildings contain enchanting
        /// </summary>
        public static bool HasDisenchant { get; set; }
        internal static int DisenchantingEntryId = 0;
        /// <summary>
        /// Buildings contains a forge (blacksmithing)
        /// </summary>
        public static bool HasForge { get; set; }
        public static BuildingType ForgeBuilding { get; set; }

        public static int PrimalTraderID { get; set; }
        public static WoWPoint PrimalTraderPoint { get; set; }

        internal static bool Initalized = false;
        internal static void Initalize()
        {
            RefreshBuildings();
            UpdateMissionIds();


            if (!Player.IsAlliance)
            {
                GarrisonLevel = Player.MapID == 1153? 3: Player.MapID == 1330 ? 2: 1;

                CommandTableEntryId = Player.MapID == 1153 ? 85805 : 86031;
                //1153 == level 3 garrison
                //1330 == level 2 garrison

                GarrisonResourceCacheEntryId = 237191;
                //237720
                SellRepairNpcId = 76872;
                PrimalTraderID = 84967;
                PrimalTraderPoint = GarrisonLevel == 2 ? MovementCache.HordePrimalTraderLevel2: MovementCache.HordePrimalTraderLevel3;

                ProfileManager.LoadNew(GarrisonBase.GarrisonBasePath + @"\Profiles\HordeGarrison.xml", false);
            }
            else
            {
                GarrisonLevel = Player.MapID == 1159 ? 3 : Player.MapID == 1331 ? 2 : 1;

                CommandTableEntryId = Player.MapID == 1331 ? 84224 : 84698;
                //1331 == level 2 garrison
                //1159 == level 3 garrison
                
                GarrisonResourceCacheEntryId = 236916;
                SellRepairNpcId = 81346;
                PrimalTraderID = 84246;
                PrimalTraderPoint = GarrisonLevel == 2 ? MovementCache.AlliancePrimalTraderLevel2 : MovementCache.AlliancePrimalTraderLevel3;


                if (Player.MapID == 1331)
                    ProfileManager.LoadNew(GarrisonBase.GarrisonBasePath + @"\Profiles\AllianceGarrison.xml", false);
                else
                    ProfileManager.LoadNew(GarrisonBase.GarrisonBasePath + @"\Profiles\AllianceGarrisonLevel3.xml", false);
            }

            GarrisonBase.Log("Has Forge {0}", HasForge);
            GarrisonBase.Log("Has Disenchanting {0} {1}", HasDisenchant,
                DisenchantingEntryId != 0 ? DisenchantingEntryId.ToString() : "");

            Initalized = true;
        }

        public static List<Follower> Followers = new List<Follower>();
        internal static List<int> FollowerIdsAll = new List<int>();
        internal static List<int> FollowerIdsCollected = new List<int>();
        internal static void RefreshFollowers()
        {
            Followers.Clear();
            FollowerIdsAll = LuaCommands.GetAllFollowerIDs();
            foreach (var id in FollowerIdsAll)
            {
                if (LuaCommands.IsFollowerCollected(id))
                    FollowerIdsCollected.Add(id);
            }


            GarrisonBase.Log("Found a total of {0} collected followers!", FollowerIdsCollected.Count);
            foreach (var f in FollowerIdsCollected)
            {
                var follower = LuaCommands.GetFollowerInfo(f);
                Followers.Add(follower);
            }
            Followers = Followers.OrderByDescending(f => f.Level)
                    .ThenByDescending(f => f.ItemLevel)
                    .ThenByDescending(f => f.Quality)
                    .ThenBy(f => f.Name)
                    .ToList();

            foreach (var f in Followers)
            {
                GarrisonBase.Log("Follower: {0}", f.ToString());
            }
        }


        public static List<Mission> AvailableMissions = new List<Mission>();
        public static List<int> AvailableMissionIds = new List<int>();
        public static List<int> CompletedMissionIds = new List<int>();
        internal static void UpdateMissionIds()
        {
            CompletedMissionIds = LuaCommands.GetCompletedMissionIds();
            AvailableMissionIds = LuaCommands.GetAvailableMissionIds();
        }

        /// <summary>
        /// Dictionary of building objects!
        /// </summary>
        public static Dictionary<BuildingType, Building> Buildings = new Dictionary<BuildingType, Building>();

        public static List<string> BuildingIDs = new List<string>();

        internal static void RefreshBuildingIDs()
        {
            BuildingIDs.Clear();
            BuildingIDs = LuaCommands.GetBuildingIds();
            GarrisonBase.Log("Found a total of {0} building IDs", BuildingIDs.Count);
        }
        internal static void RefreshBuildings()
        {
            Buildings.Clear();
            HasDisenchant = false;
            HasForge = false;
            ForgeBuilding = BuildingType.Unknown;

            RefreshBuildingIDs();


            foreach (var id in BuildingIDs)
            {
                var b = new Building(id);
                Buildings.Add(b.Type, b);
                if (b.Type == BuildingType.TheForge && !b.IsBuilding && !b.CanActivate)
                {
                    HasForge = true;
                    ForgeBuilding = BuildingType.TheForge;
                }
                if (b.Type == BuildingType.EngineeringWorks && !b.IsBuilding && !b.CanActivate)
                {
                    HasForge = true;
                    ForgeBuilding = BuildingType.EngineeringWorks;
                }

                if (b.Type == BuildingType.EnchantersStudy && !b.IsBuilding && !b.CanActivate)
                {
                    HasDisenchant = true;

                    if (!Player.IsAlliance)
                    {
                        if(b.Level==1)
                            DisenchantingEntryId = 237132;
                        else if (b.Level == 2)
                            DisenchantingEntryId = 237134;
                        else
                            DisenchantingEntryId = 237136;
                    }
                    else
                    {
                        if (b.Level == 1)
                            DisenchantingEntryId = 237335;
                        else if(b.Level == 2)
                            DisenchantingEntryId = 228618;
                        else if (b.Level == 3)
                            DisenchantingEntryId = 228591;
                    }
                }
                //GarrisonBase.Log("{0}", b.ToString());
            }

            //if not completed the quest to unlock than we manually insert the buildings (since they do not show up in the building ids)
            if (!Buildings.ContainsKey(BuildingType.Mines))
                Buildings.Add(BuildingType.Mines, new Building("61"));
            if (!Buildings.ContainsKey(BuildingType.HerbGarden))
                Buildings.Add(BuildingType.HerbGarden, new Building("29"));
        }

        public enum PrimalTraderItemTypes
        {
            SorcerousAir,
            SorcerousFire,
            SorcerousEarth,
            SorcerousWater,
            SavageBlood, 
            AlchemicalCatalyst,
            TruesteelIngot,
            GearspringParts,
            WarPaints,
            TaladiteCrystal,
            BurnishedLeather,
            HexweaveCloth,
        }
        public class PrimalTraderItem
        {
            public PrimalTraderItemTypes Type { get; set; }
            public string Name { get; set; }
            public int Cost { get; set; }

            public PrimalTraderItem(PrimalTraderItemTypes type, string name, int cost)
            {
                Type = type;
                Name = name;
                Cost = cost;
            }
        }

        public static readonly List<PrimalTraderItem> PrimalTraderItems = new List<PrimalTraderItem>
        {
            new PrimalTraderItem(PrimalTraderItemTypes.SorcerousAir, "Sorcerous Air", 25),
            new PrimalTraderItem(PrimalTraderItemTypes.SorcerousEarth, "Sorcerous Earth", 25),
            new PrimalTraderItem(PrimalTraderItemTypes.SorcerousFire, "Sorcerous Fire", 25),
            new PrimalTraderItem(PrimalTraderItemTypes.SorcerousWater, "Sorcerous Water", 25),

            new PrimalTraderItem(PrimalTraderItemTypes.SavageBlood, "Savage Blood", 50),

            new PrimalTraderItem(PrimalTraderItemTypes.AlchemicalCatalyst, "Alchemical Catalyst", 10),
            new PrimalTraderItem(PrimalTraderItemTypes.TruesteelIngot, "Truesteel Ingot", 10),
            new PrimalTraderItem(PrimalTraderItemTypes.GearspringParts, "Gearspring Parts", 10),
            new PrimalTraderItem(PrimalTraderItemTypes.WarPaints, "War Paints", 10),
            new PrimalTraderItem(PrimalTraderItemTypes.TaladiteCrystal, "Taladite Crystal", 10),
            new PrimalTraderItem(PrimalTraderItemTypes.BurnishedLeather, "Burnished Leather", 10),
            new PrimalTraderItem(PrimalTraderItemTypes.HexweaveCloth, "Hexweave Cloth", 10),
        };

        /*
         * Primal Trader EntryID
         * Horde - 84967
         * Alliance - 84246
         */
    }
}
