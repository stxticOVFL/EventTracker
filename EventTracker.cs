using System;
using System.IO;
using EventTracker.Objects;
using MelonLoader;
using MelonLoader.Preferences;
using MelonLoader.TinyJSON;
using UnityEngine;
using UniverseLib.Input;

namespace EventTracker
{
    public class EventTracker : MelonMod
    {
        public static TrackerHolder holder;
        public static Game Game { get; private set; }
        public static new HarmonyLib.Harmony Harmony { get; private set; }
        public override void OnLateInitializeMelon()
        {
            Game = Singleton<Game>.Instance;
            Harmony = new("EventTracker");

            Game.OnLevelLoadComplete += OnLevelLoadComplete;

            Settings.Register();

            TrackerHolder.Spawn();
            UnityEngine.Object.DontDestroyOnLoad(holder.gameObject);
        }

        private void OnLevelLoadComplete()
        {
            holder.Clear();

            if (Settings.Enabled.Value && Singleton<Game>.Instance.GetCurrentLevelType() != LevelData.LevelType.Hub)
            {
                Hooks.isSwap = true;
                Hooks.isJump = true;
                Hooks.dnfTimer = 0;
                Hooks.parryTimer = 0;
                Hooks.redwood = 0;
                if (Game.GetCurrentLevel().isBossFight)
                    Hooks.bossWave = 1;
                else Hooks.bossWave = 0;
                holder.Initialize();
            }
        }

        public override void OnUpdate()
        {
            if (holder != null && holder.initialized)
            {
                if (InputManager.GetKeyDown(Settings.ToggleKey.Value))
                    holder.ToggleVisibility();
                if (!holder.revealed &&
                        (InputManager.GetKeyDown(Settings.EarlyScroll.Value) ||
                        (InputManager.GetKeyDown(KeyCode.LeftControl) && InputManager.MouseScrollDelta.y != 0)))
                    holder.Reveal(false, true, true);
                if (InputManager.MouseScrollDelta.y != 0)
                    holder.HandleMouseScroll(InputManager.MouseScrollDelta.y * Settings.ScrollStrength.Value);
                if (InputManager.GetKeyDown(Settings.PlaceKey.Value) && Settings.UseJSON.Value && Settings.PlaceVisible.Value)
                    holder.PlaceTrigger();
            }
            if (Hooks.dnfTimer > 0)
                Hooks.dnfTimer -= Time.unscaledDeltaTime;
            if (Hooks.parryTimer > 0)
                Hooks.parryTimer -= Time.unscaledDeltaTime;
        }

        public enum PlacementShapes
        {
            Cube,
            Sphere,
            Plane
        }

        public static class Settings
        {

            class MinOnly<T>(T val) : ValueValidator where T : IComparable
            {
                public T Min { get; } = val;

                public override bool IsValid(object value) => Min.CompareTo(value) <= 0;
                public override object EnsureValid(object value)
                {
                    if (Min.CompareTo(value) > 0)
                        return Min;
                    return value;
                }
            }

            public static MelonPreferences_Category MainCategory;

            public static MelonPreferences_Entry<bool> Enabled;
            public static MelonPreferences_Entry<KeyCode> ToggleKey;
            public static MelonPreferences_Entry<float> X;
            public static MelonPreferences_Entry<float> Y;
            public static MelonPreferences_Entry<float> Scale;
            public static MelonPreferences_Entry<int> Limit;
            public static MelonPreferences_Entry<int> Padding;
            public static MelonPreferences_Entry<bool> ShowPBDiff;
            public static MelonPreferences_Entry<bool> EndingOnly;
            public static MelonPreferences_Entry<KeyCode> EarlyScroll;
            public static MelonPreferences_Entry<bool> Animated;
            public static MelonPreferences_Entry<bool> Bouncy;
            public static MelonPreferences_Entry<Color> DefaultColor;
            public static MelonPreferences_Entry<int> TimerPadding; // temp?

            public static MelonPreferences_Category EndingCategory;
            public static MelonPreferences_Entry<float> EndingX;
            public static MelonPreferences_Entry<float> EndingY;
            public static MelonPreferences_Entry<float> DefaultScroll;
            public static MelonPreferences_Entry<float> ScrollStrength;
            public static MelonPreferences_Entry<int> EndingLimit;
            public static MelonPreferences_Entry<bool> EndingPBDiff;
            public static MelonPreferences_Entry<float> EndingScale;

            public static MelonPreferences_Category EventsCategory;

            public static MelonPreferences_Entry<bool> C_fire;
            public static MelonPreferences_Entry<bool> C_discard;
            public static MelonPreferences_Entry<bool> C_pickup;
            public static MelonPreferences_Entry<bool> C_swap;
            public static MelonPreferences_Entry<bool> C_jump;
            public static MelonPreferences_Entry<bool> C_boost;
            public static MelonPreferences_Entry<bool> C_parry;
            public static MelonPreferences_Entry<bool> C_enemyDeath;
            public static MelonPreferences_Entry<bool> C_trivialDeath;
            public static MelonPreferences_Entry<bool> C_bossWaves;
            public static MelonPreferences_Entry<bool> C_redDestruct;
            public static MelonPreferences_Entry<bool> C_otherDestruct;
            public static MelonPreferences_Entry<bool> C_death;
            public static MelonPreferences_Entry<bool> C_triggers;

            public static MelonPreferences_Category AdvancedCategory;

            public static MelonPreferences_Entry<bool> AdvancedMode;
            public static MelonPreferences_Entry<string> ReadFilename;
            public static MelonPreferences_Entry<bool> UseJSON;
            public static MelonPreferences_Entry<bool> PlaceVisible;
            public static MelonPreferences_Entry<KeyCode> PlaceKey;
            public static MelonPreferences_Entry<KeyCode> PlaceRemove;
            public static MelonPreferences_Entry<PlacementShapes> DefaultShape;
            public static MelonPreferences_Entry<float> DefaultSize;

            public static void Register()
            {
                MainCategory = MelonPreferences.CreateCategory("Event Tracker");

                Enabled = MainCategory.CreateEntry("Enabled", true);
                X = MainCategory.CreateEntry("XNew", 0.015625f, display_name: "X Position", validator: new ValueRange<float>(0, 1)); // 30 / 1920
                Y = MainCategory.CreateEntry("YNew", 0.225f, display_name: "Y Position", validator: new ValueRange<float>(0, 1)); // 243 / 1080 or smth of the sort
                Scale = MainCategory.CreateEntry("Scale", 1f, validator: new ValueRange<float>(0, 5));
                Limit = MainCategory.CreateEntry("Entry Limit", 8);
                Padding = MainCategory.CreateEntry("Padding", 25, description: "The padding between each entry in the list.");
                Animated = MainCategory.CreateEntry("Movement Animation", true, description: "Enables the movement animation.");
                Bouncy = MainCategory.CreateEntry("Bounce Animation", true, description: "Enables the bounce animation on new/leaving entry.");
                ShowPBDiff = MainCategory.CreateEntry("Show PB Comparison", true, description: "Shows a comparison between your PB and this run.");
                EndingOnly = MainCategory.CreateEntry("Show on Ending Only", false);
                ToggleKey = MainCategory.CreateEntry("Toggle Visibility Key", KeyCode.Tab, description: "Pressing the assigned key will toggle display of the sidebar display.");
                EarlyScroll = MainCategory.CreateEntry("Early Scrolling Key", KeyCode.UpArrow, description: "A key that lets you end entry collection early and scroll freely mid-level.");
                DefaultColor = MainCategory.CreateEntry("Default Event Color", Color.white, description: "The default color to use for events like jumping or enemy death.");
                TimerPadding = MainCategory.CreateEntry("Event Character Count", 20, description: "The amount of characters the event part of the tracking should have (the \"62 Demons (Tripwire)\" part) for padding between the event and timer.");

                EndingCategory = MelonPreferences.CreateCategory("Event Tracker Ending");

                EndingX = EndingCategory.CreateEntry("EXNew", 0.015625f, display_name: "Ending X Position", validator: new ValueRange<float>(0, 1));
                EndingY = EndingCategory.CreateEntry("EYNew", 0.125f, display_name: "Ending Y Position", validator: new ValueRange<float>(0, 1));
                DefaultScroll = EndingCategory.CreateEntry("Auto-scroll Speed", 30f, description: "The scroll speed to use at the end if it overflows.");
                ScrollStrength = EndingCategory.CreateEntry("Manual Scroll Strength", -0.1f, description: "The **manual** scroll wheel speed to use at the end.");
                EndingLimit = EndingCategory.CreateEntry("Ending Entry Limit", 8);
                EndingPBDiff = EndingCategory.CreateEntry("Only show comparison at end", true);
                EndingScale = EndingCategory.CreateEntry("Ending Scale", 1f, validator: new ValueRange<float>(0, 5));

                EventsCategory = MelonPreferences.CreateCategory("Event Tracker Event Customization");

                C_fire = EventsCategory.CreateEntry("Fire", false);
                C_discard = EventsCategory.CreateEntry("Discard", false);
                C_pickup = EventsCategory.CreateEntry("Pickup", true);
                C_swap = EventsCategory.CreateEntry("Swap", false);
                C_jump = EventsCategory.CreateEntry("Jump", false);
                C_boost = EventsCategory.CreateEntry("Boost", false, description: "e.g. boosts from explosions/letting go of zipline");
                C_parry = EventsCategory.CreateEntry("Parry", false);
                C_enemyDeath = EventsCategory.CreateEntry("Enemy Death", true, description: "NOTE: This event provides synchronization.");
                C_trivialDeath = EventsCategory.CreateEntry("Non-counted Enemy Death", false, description: "e.g. book of life stages");
                C_bossWaves = EventsCategory.CreateEntry("Boss Waves", true, description: "NOTE: This event provides synchronization.");
                C_redDestruct = EventsCategory.CreateEntry("Important Destructables", true, description: "e.g. red walls and floors\nNOTE: This event provides synchronization.", display_name: "Important Destructibles");
                C_otherDestruct = EventsCategory.CreateEntry("Other Destructables", false, description: "e.g. chests, crystals", display_name: "Other Destructibles");
                C_death = EventsCategory.CreateEntry("Player Death", true);
                C_triggers = EventsCategory.CreateEntry("Triggers", true, description: "Only works if the advanced JSON mode is enabled.\nNOTE: This event provides synchronization.");

                AdvancedCategory = MelonPreferences.CreateCategory("Event Tracker Advanced");

                AdvancedMode = AdvancedCategory.CreateEntry("Advanced Mode", false, description: "Files will be written to like \"tracker-12.345-DNF.txt\" and \"tracker-67.890.txt\" instead of managing PBs for you.\nThis enabled DNFs to be written.");
                ReadFilename = AdvancedCategory.CreateEntry("Comparison File", "trackerPB.txt", description: "The file to compare against in advanced mode.");
                UseJSON = AdvancedCategory.CreateEntry("Use advanced JSON", false, description: "Loads/creates a JSON in the ghost directory for adjusting triggers and per-level customizations.");
                PlaceVisible = AdvancedCategory.CreateEntry("Triggers Visible", true);
                PlaceKey = AdvancedCategory.CreateEntry("Place Trigger Key", KeyCode.RightBracket, description: "The key to place and save a trigger using the below default settings.");
                PlaceRemove = AdvancedCategory.CreateEntry("Remove Trigger Key", KeyCode.LeftBracket, description: "The key to remove and save a trigger using the below default settings.");
                DefaultShape = AdvancedCategory.CreateEntry("Trigger Default Shape", PlacementShapes.Plane, description: "The default shape to use.\nPlanes are one-sided, so they will be placed opposite to where you face, completely vertical to make it easier.");
                DefaultSize = AdvancedCategory.CreateEntry("Trigger Default Size", 15f, description: "The default size of the placed trigger.", validator: new MinOnly<float>(0));
            }
        }

        public class JSON
        {
            public static Variant json = null;
            private static string loaded;

            public static void Load(LevelData level = null)
            {
                json = null;
                if (level == null)
                    level = Game.GetCurrentLevel();

                loaded = TrackerHolder.GetGhostDirectory(level) + "tracker.json";
                Debug.Log("trying to load json " + loaded);

                try
                {
                    json = MelonLoader.TinyJSON.JSON.Load(File.ReadAllText(loaded));
                }
                catch (Exception e)
                {
                    if (e.GetType() != typeof(FileNotFoundException))
                        Debug.LogError("failed to load json: " + e);
                    json = MelonLoader.TinyJSON.JSON.Load("{\"settings\": {}, \"triggers\": []}");
                    Save();
                }
            }

            public static void Save()
            {
                if (json == null) return;
                var dump = MelonLoader.TinyJSON.JSON.Dump(json, EncodeOptions.PrettyPrint);
                try
                {
                    File.WriteAllText(loaded, dump);
                }
                catch (Exception e)
                {
                    Debug.LogError("error writing json: " + e);
                }
            }

            public static bool GetSetting(string setting)
            {
                try
                {
                    return json["settings"][setting] as ProxyBoolean;
                }
                catch
                {
                    return (typeof(Settings).GetField("C_" + setting).GetValue(null) as MelonPreferences_Entry<bool>).Value;
                }
            }
        }
    }
}
