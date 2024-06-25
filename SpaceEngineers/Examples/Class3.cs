using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using VRageMath;
using VRage.Game;
using VRage.Game.GUI.TextPanel;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Ingame;
using Sandbox.Game.EntityComponents;
using VRage.Game.Components;
using VRage.Collections;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using Sandbox.Game.Entities;

namespace SpaceEngineers.Examples.Class3
{
    // пример сортировки контейнеров
    internal class Program: MyGridProgram
    {

        string[] CargoTypes =
        {
            "ORE",
            "COMPONENT",
            "STUFF",
            "ICE",
            "INGOT"
        };

        Dictionary<string, List<IMyTerminalBlock>> Cargos;
        List<IMyAssembler> Assemblers;
        List<IMyRefinery> Refineries;



        public Program()
        {
            Cargos = new Dictionary<string, List<IMyTerminalBlock>>();
            Assemblers = new List<IMyAssembler>();
            Refineries = new List<IMyRefinery>();

            GridTerminalSystem.GetBlocksOfType<IMyAssembler>(Assemblers);
            GridTerminalSystem.GetBlocksOfType<IMyRefinery>(Refineries);

            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        void Main()
        {

            foreach (string CargoType in CargoTypes)
            {
                Cargos[CargoType] = new List<IMyTerminalBlock>();

                GridTerminalSystem.SearchBlocksOfName("[" + CargoType + "]", Cargos[CargoType]);
            }

            SortAssemberItems();
            SortRefineryItems();
            SortCargos();
        }

        void SortAssemberItems()
        {
            if (Assemblers.Count > 0)
            {
                foreach (IMyAssembler Assembler in Assemblers)
                {
                    if (Assembler.GetInventory(1).ItemCount > 0)
                    {
                        List<MyInventoryItem> Items;
                        Items = new List<MyInventoryItem>();

                        Assembler.GetInventory(1).GetItems(Items);

                        foreach (MyInventoryItem Item in Items)
                        {
                            if (Cargos.ContainsKey("COMPONENT"))
                            {
                                foreach (IMyCargoContainer Cargo in Cargos["COMPONENT"])
                                {
                                    if (!Cargo.GetInventory(0).IsFull)
                                    {
                                        Assembler.GetInventory(1).TransferItemTo(Cargo.GetInventory(0), Item);
                                        break;
                                    }

                                }
                            }
                        }
                    }
                }
            }
        }

        void SortRefineryItems()
        {
            if (Refineries.Count > 0)
            {
                foreach (IMyRefinery Refinery in Refineries)
                {
                    if (Refinery.GetInventory(1).ItemCount > 0)
                    {
                        List<MyInventoryItem> Items;
                        Items = new List<MyInventoryItem>();

                        Refinery.GetInventory(1).GetItems(Items);

                        foreach (MyInventoryItem Item in Items)
                        {
                            if (Cargos.ContainsKey("INGOT"))
                            {
                                foreach (IMyCargoContainer Cargo in Cargos["INGOT"])
                                {
                                    if (!Cargo.GetInventory(0).IsFull)
                                    {
                                        Refinery.GetInventory(1).TransferItemTo(Cargo.GetInventory(0), Item);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        void SortCargos()
        {
            List<MyInventoryItem> Items;

            // IMyDefensiveCombatBlock
            //IMyOffensiveCombatBlock

            foreach (string CargoType in CargoTypes)
            {
                if (Cargos.ContainsKey(CargoType))
                {
                    foreach (IMyCargoContainer Cargo in Cargos[CargoType])
                    {
                        if (Cargo.GetInventory(0).ItemCount > 0)
                        {
                            Items = new List<MyInventoryItem>();
                            Cargo.GetInventory(0).GetItems(Items);
                            foreach (MyInventoryItem Item in Items)
                            {
                                string ItemFullType = Item.Type.ToString();

                                int firstStringPosition = ItemFullType.IndexOf("_");
                                int secondStringPosition = ItemFullType.IndexOf("/");
                                string ItemType = ItemFullType.Substring(firstStringPosition + 1, secondStringPosition - firstStringPosition - 1);

                                if (ItemFullType.IndexOf("Ice") != -1) ItemType = "Ice";

                                if (ItemType.ToLower() != CargoType.ToLower())
                                {
                                    if (Cargos.ContainsKey(ItemType.ToUpper()))
                                    {
                                        foreach (IMyCargoContainer CargoDst in Cargos[ItemType.ToUpper()])
                                        {
                                            if (!CargoDst.GetInventory(0).IsFull)
                                            {
                                                Cargo.GetInventory(0).TransferItemTo(CargoDst.GetInventory(0), Item);
                                                break;
                                            }
                                        }
                                    }
                                    else if (Cargos.ContainsKey("STUFF"))
                                    {
                                        foreach (IMyCargoContainer CargoDst in Cargos["STUFF"])
                                        {
                                            if (!CargoDst.GetInventory(0).IsFull)
                                            {
                                                Cargo.GetInventory(0).TransferItemTo(CargoDst.GetInventory(0), Item);
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
