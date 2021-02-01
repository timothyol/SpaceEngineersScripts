﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpaceEngineersScripts.Scripts
{
    class DecodeItemName
    {

        private static readonly Dictionary<string, string> ItemNameMap = new Dictionary<string, string>
        {
            {"Construction", "Construction Component"},
            {"MetalGrid", "Metal Grid"},
            {"InteriorPlate", "Interior Plate"},
            {"SteelPlate", "Steel Plate"},
            {"SmallTube", "Small Steel Tube"},
            {"LargeTube", "Large Steel Tube"},
            {"BulletproofGlass", "Bulletproof Glass"},
            {"Reactor", "Reactor Component"},
            {"Thrust", "Thruster Component"},
            {"GravityGenerator", "GravGen Component"},
            {"Medical", "Medical Component"},
            {"RadioCommunication", "Radio Component"},
            {"Detector", "Detector Component"},
            {"SolarCell", "Solar Cell"},
            {"PowerCell", "Power Cell"},
            {"AutomaticRifleItem", "Rifle"},
            {"AutomaticRocketLauncher", "Rocket Launcher"},
            {"WelderItem", "Welder"},
            {"Welder2Item", "Welder Lvl 2"},
            {"Welder3Item", "Welder Lvl 3"},
            {"Welder4Item", "Welder Lvl 4"},
            {"Welder5Item", "Welder Lvl 5"},
            {"AngleGrinderItem", "Grinder"},
            {"AngleGrinder2Item", "Grinder Lvl 2"},
            {"AngleGrinder3Item", "Grinder Lvl 3"},
            {"AngleGrinder4Item", "Grinder Lvl 4"},
            {"AngleGrinder5Item", "Grinder Lvl 5"},
            {"HandDrillItem", "Hand Drill"},
            {"HandDrill2Item", "Hand Drill Lvl 2"},
            {"HandDrill3Item", "Hand Drill Lvl 3"},
            {"HandDrill4Item", "Hand Drill Lvl 4"},
            {"HandDrill5Item", "Hand Drill Lvl 5"},
        };


        //Originally Stolen from TheFinalFrontier.se
        //http://thefinalfrontier.se/inventory-display-and-management/
        //Updated a little
        String decodeItemName(String name, String typeId)
        {
            if (ItemNameMap.ContainsKey(name))
            {
                return ItemNameMap[name];
            }

            if (typeId.EndsWith("_Ore"))
            {
                if (name.Equals("Stone"))
                {
                    return name;
                }
                else if (name.Equals("Ice"))
                {
                    return name;
                }
                return name + " Ore";
            }
            if (typeId.EndsWith("_Ingot"))
            {
                if (name.Equals("Stone"))
                {
                    return "Gravel";
                }
                if (name.Equals("Magnesium"))
                {
                    return name + " Powder";
                }
                if (name.Equals("Silicon"))
                {
                    return name + " Wafer";
                }
                return name + " Ingot";
            }
            return name;
        }

    }
}
