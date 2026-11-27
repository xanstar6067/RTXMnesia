using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RTXMnesia.Props;
using RTXMnesia.ScriptApi;

namespace RTXMnesia.Dev
{
    /// <summary>
    /// Hand written progress steps of a level for testing. A step calls the real functions of the level script and
    /// does only what their normal trigger does not (a waiting flashback, a collide callback already spent). Timers
    /// and moving objects continue when the panel closes.
    /// </summary>
    public static class HplDebugStages
    {
        public sealed class Stage
        {
            public string Title, Note, Place;
            public Func<HplMap, bool> Done;
            public Action<HplDebugWorld, HplMap> Run;
        }

        public static IReadOnlyList<Stage> ForMap(string map) => map == "03_archives" ? Archives : Array.Empty<Stage>();

        /// <summary>Runs the unfinished steps up to <paramref name="index"/>, or only that one.</summary>
        public static void Run(HplDebugWorld world, HplMap map, int index, bool withPrevious)
        {
            var stages = ForMap(map.MapName);
            if (map.Script == null || index < 0 || index >= stages.Count) return;
            HplDebugSession.MarkTest(map);
            for (int i = withPrevious ? 0 : index; i <= index; i++)
                if (!stages[i].Done(map)) stages[i].Run(world, map);
            map.DestroyPendingEntitiesNow();
        }

        /// <summary>A level script function or engine function of HplScript by name.</summary>
        static object Call(HplMap map, string function, params object[] args)
        {
            var method = map.Script.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .First(m => m.Name == function && m.GetParameters().Length == args.Length);
            return method.Invoke(map.Script, args);
        }

        static int Local(HplMap map, string name) => (int)Call(map, "GetLocalVarInt", name);
        static bool Global(string name) => HplGame.GlobalVars.TryGetValue(name, out var value) && value != "";
        static bool Completed(string quest) => HplGame.GetQuest(quest) is { active: false };

        static readonly Stage[] Archives =
        {
            new()
            {
                Title = "Дневники прочитаны, флешбэк завершён",
                Note = "Три дневника в журнале, сфера исчезла.",
                Place = "diary_paper01_4",
                Done = map => Local(map, "diary") >= 3,
                Run = (world, map) =>
                {
                    foreach (var name in new[] { "diary_paper01_2", "diary_paper01_3", "diary_paper01_4" })
                    {
                        if (map.GetEntityByName(name) is not PropItem diary) continue;
                        world.Give(world.Items.First(i => i.Name == name));
                        map.DestroyEntity(diary);
                    }
                    Call(map, "SetLocalVarInt", "diary", 3);
                    // The value the flashback timer ends on.
                    Call(map, "SetLocalVarInt", "TimerFlashBack", 19);
                    Call(map, "SetEntityActive", "orb_1", false);
                },
            },
            new()
            {
                Title = "Книги нажаты, полка открыта",
                Note = "Задание про книги выполнено, потайная комната открывается.",
                Place = "shelf_secret_door_1",
                Done = map => Completed("03Books"),
                Run = (world, map) =>
                {
                    // The flashback area that gives the quest must not run again.
                    Call(map, "SetEntityActive", "FlashbackArea_1", false);
                    Call(map, "VisionOverBooks");
                    for (int i = 1; i <= 3; i++) Call(map, "CollideSecretBook", "SecretBook_" + i, "AreaSecretBook_" + i, 1);
                },
            },
            new()
            {
                Title = "Ключ взят, дверь выломана",
                Note = "Событие со стражем у двери идёт несколько секунд после закрытия панели.",
                Place = "mansion_8",
                Done = map => Global("key_study_1"),
                Run = (world, map) => world.PickUp(world.Prop("key_study_1")),
            },
            new()
            {
                Title = "Обвал случился",
                Note = "Коридор завален, mansion_2 заперта.",
                Place = "AreaCaveInQuest",
                Done = map => map.GetEntityByName("AreaCaveInQuest") is { Active: true } || HplGame.GetQuest("03cavein") != null,
                Run = (world, map) =>
                {
                    Call(map, "RemoveEntityCollideCallback", "Player", "AreaCloudActivate_5");
                    Call(map, "CollideCaveIn", "Player", "AreaCloudActivate_5", 1);
                },
            },
        };
    }
}
