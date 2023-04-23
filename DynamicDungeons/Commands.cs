using Jotunn.Entities;

namespace DynamicDungeons
{
    class Commands
    {
        public class SavePrefabListCommand : ConsoleCommand
        {
            public override string Name => "save_prefab_list";

            public override string Help => "Saves all prefabs to config/listed_prefabs";

            public override void Run(string[] args)
            {
                Util.SavePrefabList();
                Jotunn.Logger.LogInfo("Saved prefabs list to config/listed_prefabs");
            }
        }
        public class LogDungeonInfoCommand : ConsoleCommand
        {
            public override string Name => "dd";

            public override string Help => "DynamicDungeons general command";

            public override void Run(string[] args)
            {
                if (!Util.IsAdmin()) { Jotunn.Logger.LogInfo("Only admins can use this command"); return; }
                if (DynamicDungeons.currentDungeon == null) { Jotunn.Logger.LogInfo("Not inside a dungeon"); return; }
                if (args.Length == 0 || args[0].Length == 0) return;
                switch (args[0])
                {
                    case "info":
                        DynamicDungeons.currentDungeon.LogDungeonInfo();
                        ZRoutedRpc.instance.InvokeRoutedRPC("DynamicDungeons RequestInfo", DynamicDungeons.currentDungeon.dungeon.name);
                        break;
                    case "start":
                        if (args.Length == 1)
                        {
                            ZRoutedRpc.instance.InvokeRoutedRPC("DynamicDungeons StartEvent", DynamicDungeons.currentDungeon.dungeon.name);
                            break;
                        }
                        ZRoutedRpc.instance.InvokeRoutedRPC("DynamicDungeons FinishEvent", args[1]);
                        break;
                    case "stop":
                        if (args.Length == 1)
                        {
                            ZRoutedRpc.instance.InvokeRoutedRPC("DynamicDungeons StopEvent", DynamicDungeons.currentDungeon.dungeon.name);
                            break;
                        }
                        ZRoutedRpc.instance.InvokeRoutedRPC("DynamicDungeons FinishEvent", args[1]);
                        break;
                    case "reload":
                        if (args[1] == null) break;
                        if (args[1] == "all") ZRoutedRpc.instance.InvokeRoutedRPC("DynamicDungeons ReloadAllDungeons");
                        //ZRoutedRpc.instance.InvokeRoutedRPC("DynamicDungeons ReloadDungeon");
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
