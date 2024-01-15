using MelonLoader;

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

        public static class Settings
        {
            public static MelonPreferences_Category MainCategory;
            public static MelonPreferences_Category EventsCategory;

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


            public static void Register()
            {
                MainCategory = MelonPreferences.CreateCategory("Event Tracker");

                Enabled = MainCategory.CreateEntry("Enabled", true);
                X = MainCategory.CreateEntry("X Position", 30);
                Y = MainCategory.CreateEntry("Y Position", 200);
                Padding = MainCategory.CreateEntry("Padding", 22, description: "The padding between each entry in the list.");
                Limit = MainCategory.CreateEntry("Entry Limit", 10);
                EndingOnly = MainCategory.CreateEntry("Show on Ending Only", false);
                EndingX = MainCategory.CreateEntry("Ending X Position", 30);
                ScrollSpeed = MainCategory.CreateEntry("Scroll Speed", 30f, description: "The scroll speed to use at the end if it goes over or under the top or bottom of the screen.");
                PBs = MainCategory.CreateEntry("Show PBs", true, description: "Shows a comparison between your PB and this run.");
                PBEndingOnly = MainCategory.CreateEntry("Only show comparison at end", true, is_hidden: !PBs.Value);
                PBs.OnEntryValueChanged.Subscribe((bool before, bool after) => PBEndingOnly.IsHidden = !after);

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
            }
        }

    }
}
