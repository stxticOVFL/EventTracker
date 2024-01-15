using MelonLoader;
using UnityEngine;
using UniverseLib.Input;

namespace EventTracker
{
    public class EventTracker : MelonMod
    {
        public static Objects.TrackerHolder holder;
        public static Game Game { get; private set; }
        public static new HarmonyLib.Harmony Harmony { get; private set; }
        public override void OnLateInitializeMelon()
        {
            Game = Singleton<Game>.Instance;
            Harmony = new("EventTracker");

            Game.OnLevelLoadComplete += OnLevelLoadComplete;

            Settings.Register();
        }

        private void OnLevelLoadComplete()
        { 

            holder = null;
            if (Settings.Enabled.Value)
            {
                Hooks.isSwap = true;
                Hooks.isJump = true;
                if (Game.GetCurrentLevel().isBossFight)
                    Hooks.bossWave = 1;
                else Hooks.bossWave = 0;
                Objects.TrackerHolder.Initialize();
            }
        }

        public override void OnUpdate()
        {
            if (holder != null && InputManager.GetKeyDown(Settings.ToggleKey.Value))
                holder.ToggleVisibility();
            if (Hooks.dnfTimer > 0)
                Hooks.dnfTimer -= Time.unscaledDeltaTime;
        }

        public static class Settings
        {
            public static MelonPreferences_Category MainCategory;
            public static MelonPreferences_Category EventsCategory;
            public static MelonPreferences_Category AdvancedCategory;

            public static MelonPreferences_Entry<bool> Enabled;
            public static MelonPreferences_Entry<int> X;
            public static MelonPreferences_Entry<int> Y;
            public static MelonPreferences_Entry<int> Padding;
            public static MelonPreferences_Entry<int> Limit;
            public static MelonPreferences_Entry<int> EndingX;
            public static MelonPreferences_Entry<float> ScrollSpeed;
            public static MelonPreferences_Entry<bool> EndingOnly;
            public static MelonPreferences_Entry<bool> PBs;
            public static MelonPreferences_Entry<bool> PBEndingOnly;
            public static MelonPreferences_Entry<KeyCode> ToggleKey;

            public static MelonPreferences_Entry<bool> Fire;
            public static MelonPreferences_Entry<bool> Discard;
            public static MelonPreferences_Entry<bool> Pickup;
            public static MelonPreferences_Entry<bool> Swap;
            public static MelonPreferences_Entry<bool> Jump;
            public static MelonPreferences_Entry<bool> Boost;
            public static MelonPreferences_Entry<bool> Parry;
            public static MelonPreferences_Entry<bool> EnemyDeath;
            public static MelonPreferences_Entry<bool> TrivialDeath;
            public static MelonPreferences_Entry<bool> BossWaves;
            public static MelonPreferences_Entry<bool> RedDestructable;
            public static MelonPreferences_Entry<bool> OtherDestructables;

            public static MelonPreferences_Entry<bool> AdvancedMode;
            public static MelonPreferences_Entry<string> ReadFilename;


            public static void Register()
            {
                MainCategory = MelonPreferences.CreateCategory("Event Tracker");

                Enabled = MainCategory.CreateEntry("Enabled", true);
                X = MainCategory.CreateEntry("X Position", 30);
                Y = MainCategory.CreateEntry("Y Position", 240);
                Padding = MainCategory.CreateEntry("Padding", 25, description: "The padding between each entry in the list.");
                Limit = MainCategory.CreateEntry("Entry Limit", 10);
                EndingOnly = MainCategory.CreateEntry("Show on Ending Only", false);
                EndingX = MainCategory.CreateEntry("Ending X Position", 30);
                ScrollSpeed = MainCategory.CreateEntry("Scroll Speed", 30f, description: "The scroll speed to use at the end if it goes over the top of the screen.");
                PBs = MainCategory.CreateEntry("Show PBs", true, description: "Shows a comparison between your PB and this run.");
                PBEndingOnly = MainCategory.CreateEntry("Only show comparison at end", true);

                ToggleKey = MainCategory.CreateEntry("Toggle Visibility Key", KeyCode.Tab, description: "Pressing the assigned key will toggle display of the sidebar display.");

                EventsCategory = MelonPreferences.CreateCategory("Event Tracker Event Customization");

                Fire = EventsCategory.CreateEntry("Fire", true);
                Discard = EventsCategory.CreateEntry("Discard", true);
                Pickup = EventsCategory.CreateEntry("Pickup", true);
                Swap = EventsCategory.CreateEntry("Swap", true);
                Jump = EventsCategory.CreateEntry("Jump", true);
                Boost = EventsCategory.CreateEntry("Boost", true, description: "e.g. boosts from explosions/letting go of zipline");
                Parry = EventsCategory.CreateEntry("Parry", true);
                EnemyDeath = EventsCategory.CreateEntry("Enemy Death", true, description: "NOTE: This event provides synchronization!\nEvents could fall out of sync for PB comparsion!");
                TrivialDeath = EventsCategory.CreateEntry("Non-counted Enemy Death", false, description: "e.g. book of life stages");
                BossWaves = EventsCategory.CreateEntry("Boss Waves", true, description: "NOTE: This event provides synchronization!\nEvents could fall out of sync for PB comparsion!");

                RedDestructable = EventsCategory.CreateEntry("Important Destructables", true, description: "e.g. red walls and floors");
                OtherDestructables = EventsCategory.CreateEntry("Other Destructables", false, description: "e.g. chests, crystals");

                AdvancedCategory = MelonPreferences.CreateCategory("Event Tracker Advanced");

                AdvancedMode = AdvancedCategory.CreateEntry("Advanced Mode", false, description: "Files will be written to like \"tracker-12.345-DNF.txt\" and \"tracker-67.890.txt\" instead of managing PBs for you.\nThis enabled DNFs to be written.");
                ReadFilename = AdvancedCategory.CreateEntry("Comparison File", "trackerPB.txt", description: "The file to compare against in advanced mode.");
            }
        }

    }
}
