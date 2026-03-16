using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MelonLoader.TinyJSON;
using UnityEngine;
using Settings = EventTracker.EventTracker.Settings;

namespace EventTracker.Objects
{
    public class TrackerHolder : MonoBehaviour
    {
        public bool initialized = false;
        public bool hidden = false;
        public bool forceHide = false;
        bool hiddenMemory = false;

        private Canvas _canvas;
        private Transform _realHolder = null;
        private GameObject _textBase;
        public bool revealed = false;
        public int active = 0;
        private int countOffset = 0;

        public int Count { get { return (revealed ? Settings.EndingLimit.Value : Settings.Limit.Value) + countOffset; } }
        public float scroll; // used by the items
        public float emerge;

        Dictionary<string, List<Tuple<string, long>>> pools;
        string currentPool;
        int poolIndex;

        public static Regex regexFilter = null;
        public static bool regexMatch = true;

        private readonly TrackerItem.Transition _moveX = new(AxKEasing.EaseOutQuint, null);
        private readonly TrackerItem.Transition _moveY = new(AxKEasing.EaseOutQuint, null);
        private readonly TrackerItem.Transition _scaleT = new(AxKEasing.EaseOutQuint, null);

        private LevelData loadedLevel = null;
        private string loadedFile = null;
        private DateTime loadedTime = DateTime.MinValue;


        public static string GetGhostDirectory(LevelData level = null)
        {
            if (level == null)
                level = Singleton<Game>.Instance.GetCurrentLevel();
            string path = GhostRecorder.GetCompressedSavePathForLevel(level);
            path = Path.GetDirectoryName(path) + "/";
            return path;
        }

        internal static void Spawn() => EventTracker.holder = new GameObject("TrackerHolder", typeof(TrackerHolder)).GetComponent<TrackerHolder>();

        private void Awake()
        {
            _moveX.speed = 4;
            _moveY.speed = 4;
            _scaleT.speed = 4;

            // create a canvas for the text to go into
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceCamera; // this seems to work better

            _realHolder = new GameObject("Holder").transform;
            _realHolder.parent = transform;
            Update();

            _textBase = new GameObject("TrackerItemBase", typeof(TrackerItem));
            _textBase.SetActive(false);
            _textBase.GetComponent<TrackerItem>().skip = true;
            _textBase.transform.localPosition = Vector3.zero;
            _textBase.transform.localScale = Vector3.one;
            _textBase.transform.parent = _realHolder;
        }

        public void Clear()
        {
            hidden = hiddenMemory;
            revealed = false;
            initialized = false;
            active = 0;
            scroll = 0;
            emerge = 0;
            countOffset = 0;
            poolIndex = 0;
            foreach (Transform child in _realHolder)
            {
                if (child.GetSiblingIndex() == 0) continue;
                Destroy(child.gameObject);
            }
        }

        public void LoadComparison()
        {
            if (!LevelRush.IsLevelRush())
            {
                string path = GetGhostDirectory(loadedLevel);
                string file = Settings.ReadFilename.Value;
                if (!Settings.AdvancedMode.Value)
                    file = "trackerPB.txt";
                path += file;

                if (File.Exists(path) && (loadedLevel != EventTracker.Game.GetCurrentLevel() || loadedFile != file || loadedTime < File.GetLastWriteTimeUtc(path)))
                {
                    pools = [];
                    loadedLevel = EventTracker.Game.GetCurrentLevel();
                    loadedFile = file;
                    loadedTime = File.GetLastWriteTimeUtc(path);
                    bool header = true;
                    currentPool = "";
                    pools.Add("", []);
                    foreach (var line in File.ReadLines(path))
                    {
                        if (header)
                        {
                            header = false;
                            continue;
                        }
                        var details = line.Split('\t');
                        if (details[3] == "!")
                        {
                            currentPool = details[0].TrimEnd();
                            pools.Add(currentPool, []);
                        }
                        pools[currentPool].Add(new(details[0].TrimEnd(), long.Parse(details[2])));
                    }
                }
                else if (!File.Exists(path))
                    pools = [];
            }
            else
                pools = [];
        }

        public void Initialize()
        {
            currentPool = "";
            initialized = true;
            if (pools.Count > 0 && Settings.AdvancedMode.Value)
            {
                var text = PushText($"(comparing to {loadedFile})", false);
                text.time = null;
            }
        }

        public void LoadJSON()
        {
            EventTracker.JSON.Load();
            TrackerTrigger.holder = new GameObject("Trigger Holder").transform;
            bool resave = false;
            foreach (Variant data in EventTracker.JSON.json["triggers"] as ProxyArray)
            {
                var trigger = TrackerTrigger.Decode(data);
                try
                {
                    _ = trigger.data["id"];
                }
                catch
                {
                    trigger.data = JSON.Load(trigger.Encode());
                    resave = true;
                }
            }

            try
            {
                regexFilter = new(EventTracker.JSON.json["regex"], RegexOptions.Compiled | RegexOptions.IgnoreCase);
                regexMatch = EventTracker.JSON.json["regexMatch"];
            }
            catch
            {
                regexFilter = null;
                EventTracker.JSON.json["regex"] = null;
                EventTracker.JSON.json["regexMatch"] = new ProxyBoolean(true);
                resave = true;
            }
            
            if (resave)
            {
                var newArray = new ProxyArray();

                foreach (Transform tt in TrackerTrigger.holder)
                    newArray.Add(tt.GetComponent<TrackerTrigger>().data);

                EventTracker.JSON.json["triggers"] = newArray;
                EventTracker.JSON.Save();
            }
        }

        private void Update()
        {
            if (!_moveX.running)
                _moveX.result = Settings.EndingX.Value;
            if (!_moveY.running)
                _moveY.result = Settings.EndingY.Value;
            if (!_scaleT.running)
                _scaleT.result = Settings.EndingScale.Value;
            float x = revealed ? _moveX.result : Settings.X.Value;
            float y = revealed ? _moveY.result : Settings.Y.Value;
            float scale = revealed ? _scaleT.result : Settings.Scale.Value;
            _realHolder.position = new Vector3(x * Screen.width, Screen.height - (y * Screen.height), 0);
            _realHolder.localScale = new Vector3(scale * (Screen.height / 1080f), scale * (Screen.height / 1080f), 1);
            if (forceHide)
            {
                _moveX.time = 1;
                _moveY.time = 1;
                _scaleT.time = 1;
            }
            _moveX.Process();
            _moveY.Process();
            _scaleT.Process();
        }

        public TrackerItem PushText(string text, bool skip) => PushText(text, Color.clear, skip);

        public TrackerItem PushText(string text, Color border, bool skip, bool landmark = false, bool goal = false)
        {
            if (!initialized)
                return null;
            if (border == Color.clear)
                border = Settings.DefaultColor.Value;
            if (!skip && regexFilter != null && !goal)
                skip = regexFilter.IsMatch(text) ^ regexMatch;
            try
            {
                if (revealed && !goal)
                    return null;
                else if (goal)
                    countOffset++;
                if (!skip && active > Count && !revealed)
                {
                    foreach (Transform child in _realHolder)
                    {
                        if (child.GetSiblingIndex() == 0) continue;
                        var item = child.GetComponent<TrackerItem>();
                        if (item.skip) continue;
                        item.index--;
                        if (!child.gameObject.activeSelf) continue;
                        item.Move(item.index < 0);
                        if (!item.leaving && item.index < 0)
                        {
                            if (!item.pbDiff)
                                active--;
                            item.leaving = true;
                        }
                    }
                }
                var newText = Instantiate(_textBase, _realHolder).GetComponent<TrackerItem>();
                newText.name = "Tracker Item";
                newText.text = text;
                newText.longTime = EventTracker.Game.GetCurrentLevelTimerMicroseconds();
                newText.time = Game.GetTimerFormattedMillisecond(newText.longTime);
                newText.color = border;
                newText.gameObject.SetActive(!skip);
                if (!skip)
                    newText.index = active++;
                newText.holder = this;
                newText.landmark = landmark;
                newText.goal = goal;
                newText.skip = skip;

                if (pools != null)
                {
                    if (landmark)
                    {
                        poolIndex = 0;
                        // check pools
                        if (text.Contains("Demon"))
                        {
                            string num = text.Split(' ')[0];
                            // don't look directly, look by number
                            foreach (string key in pools.Keys)
                            {
                                if (key.StartsWith(num))
                                    currentPool = key;
                            }
                        }
                        else // it just is the key
                            currentPool = text;
                    }
                    if (!skip)
                    {

                        try
                        {
                            int index = poolIndex;
                            var desctime = pools[currentPool][index];
                            string compareF = desctime.Item1.Split('(')[0];
                            string compareT = text.Split('(')[0];
                            while (true)
                            {
                                index++;
                                if (compareF == compareT)
                                {
                                    poolIndex = index;
                                    break;
                                }
                                desctime = pools[currentPool][index];
                                compareF = desctime.Item1.Split('(')[0];
                            }
                            var diff = newText.longTime - desctime.Item2;
                            string sign = diff.ToString("+0;-#").First().ToString(); // lol
                            diff = Math.Abs(diff);

                            var diffText = Instantiate(newText.gameObject, _realHolder).GetComponent<TrackerItem>();
                            diffText.name = "PB Diff";
                            diffText.text = $"{sign.PadLeft(12 + Settings.TimerPadding.Value)}{Game.GetTimerFormattedMillisecond(diff)}";
                            diffText.time = null;
                            diffText.pbDiff = true;
                            diffText.color = sign == "-" ? Color.green : Color.red;

                            diffText.gameObject.SetActive(!skip && Settings.ShowPBDiff.Value);
                        }
                        catch { } 
                    }
                }

                return newText;
            }
            catch (Exception e)
            {
                Debug.LogError($"error during PushText!!! {e}");
                return null;
            }
        }

        public void ToggleVisibility()
        {
            hiddenMemory = hidden = !hidden;
            if (!revealed || scroll == 0)
            {
                foreach (Transform child in _realHolder)
                {
                    if (!child.gameObject.activeSelf)
                        continue;
                    child.GetComponent<TrackerItem>().ChangeOpacity(hidden ? 0 : 1);
                }
            }
        }

        public void Reveal(bool pb, bool finished, bool early = false)
        {
            if (!finished)
            {
                PushText("DNF", new Color32(191, 120, 48, 255), false, false, Settings.AdvancedMode.Value);
                if (!Settings.AdvancedMode.Value)
                    return;
            }
            else
            {
                if (active == 0)
                {
                    revealed = finished;
                    return;
                }
                if (!Settings.EndingOnly.Value)
                {
                    _moveX.Start(Settings.X.Value, Settings.EndingX.Value);
                    _moveY.Start(Settings.Y.Value, Settings.EndingY.Value);
                    _scaleT.Start(Settings.Scale.Value, Settings.EndingScale.Value);
                }
            }
            hidden = false;

            long time = EventTracker.Game.GetCurrentLevelTimerMicroseconds();

            if (!Settings.AdvancedMode.Value && !pb && pools != null && !early)
            {
                // we might not have our actual PB but it might be better than what we have on record
                if (pools.TryGetValue("Goal", out var goal) && time < goal[0].Item2)
                    pb = true;
            }

            StreamWriter stream = null;
            string path = GetGhostDirectory();
            if (!LevelRush.IsLevelRush())
            {
                if (!Settings.AdvancedMode.Value)
                {
                    path += "trackerPB.txt";

                    if (File.Exists(path) && !pb)
                        path = Path.GetDirectoryName(path) + "/tracker.txt";
                    else if (File.Exists(path))
                        File.Copy(path, Path.GetDirectoryName(path) + "/trackerOldPB.txt", true);
                }
                else if (!early)
                {
                    float ftime = time / 1e6f;
                    float fastest = float.MaxValue;
                    if (!pb)
                    {
                        foreach (string filename in Directory.EnumerateFiles(path))
                        {
                            try
                            {
                                var split = filename.Split('-');
                                if (split.Last() == "DNF") continue;
                                var fnameTime = float.Parse(split[1]);
                                if (fastest < fnameTime)
                                    fastest = fnameTime;
                            }
                            catch { continue; }
                        }
                        if (ftime < fastest)
                            pb = true;
                    }
                    if (pb)
                    {
                        path += $"tracker-{ftime:0.000}";
                        if (!finished) path += "-DNF";

                        int iter = 1;
                        string pre = path;
                        while (File.Exists(path + ".txt"))
                            path = pre + $"-{++iter}";

                        path += ".txt";
                    }
                    else if (finished) path += "tracker.txt";
                    else return;
                }

                try
                {
                    if (!early)
                        stream = new StreamWriter(path, false);
                }
                catch { }
            }

            stream?.WriteLine("Event               \tTime    \t             \t \tDifference to PB on-record");

            revealed = finished;

            if (finished && !early)
                PushText("Goal", new Color32(0xFF, 0xCF, 0x40, 255), false, true, true);

            TrackerItem firstChild = _realHolder.Cast<Transform>().DefaultIfEmpty(null).FirstOrDefault(x => !x.GetComponent<TrackerItem>().skip)?.GetComponent<TrackerItem>();
            // check the first real item
            // if it's over the height we start Scroll Moding :tm:
            if (firstChild && firstChild.index < 0)
            {
                // emerge = -((Settings.EndingBottom.Value * Screen.height) + Settings.Y.Value - Screen.height) / Settings.Padding.Value;
                // emerge = Screen.height - TrackerItem.TargetY((int)Math.Ceiling(emerge));
                scroll = TrackerItem.TargetY(firstChild.index + (Settings.EndingLimit.Value - Settings.Limit.Value - 3));
            }
            else
                scroll = 0;


            foreach (Transform child in _realHolder)
            {
                if (child.GetSiblingIndex() == 0) continue;
                var item = child.GetComponent<TrackerItem>();
                if (finished)
                {
                    if (!item.skip && (!item.pbDiff || Settings.ShowPBDiff.Value))
                        item.Reveal();
                }
                if (item.time != null)
                {
                    string diff = "";
                    try
                    {
                        if (pools != null)
                        {
                            var diffitem = _realHolder.GetChild(child.GetSiblingIndex() + 1).GetComponent<TrackerItem>();
                            if (!diffitem.pbDiff) throw new Exception();
                            diff = diffitem.text.TrimStart();
                        }
                    }
                    catch { }

                    stream?.WriteLine($"{item.text.PadRight(Settings.TimerPadding.Value)}\t{item.time}\t{item.longTime,14}\t{(item.landmark ? '!' : ' ')}\t{diff}");
                }
            }

            if (stream != null)
            {
                stream.Close();

                var notif = PushText($"(saved to {Path.GetFileName(path)}!)", Color.white, false, false, finished);
                notif.time = null; // but not pbdiff
                notif.Reveal();
            }
            emerge = TrackerItem.TargetY(Count);
        }
        public void HandleMouseScroll(float scroll)
        {
            if (!revealed) return;
            foreach (Transform child in _realHolder)
            {
                if (child.GetSiblingIndex() == 0) continue;
                var item = child.GetComponent<TrackerItem>();
                item.autoScroll = false;
                item._scrollT.Start(item.manualScroll + scroll, 0);
            }
        }

        public void PlaceTrigger()
        {
            var trigger = TrackerTrigger.Spawn(Settings.DefaultShape.Value);
            trigger.placed = true;
            trigger.transform.position = RM.playerPosition;
            trigger.transform.rotation = RM.mechController.playerCamera.PlayerCam.transform.rotation;
            trigger.transform.localScale = new Vector3(Settings.DefaultSize.Value, Settings.DefaultSize.Value, Settings.DefaultSize.Value);
            trigger.color = UnityEngine.Random.ColorHSV(0, 1, .5f, .5f, .4f, .4f);
            trigger.opacityT.Start(0, 1);
            var encoded = trigger.Encode();
            (EventTracker.JSON.json["triggers"] as ProxyArray).Add(JSON.Load(encoded));
            EventTracker.JSON.Save();
            var pushed = PushText($"Placed {trigger.name}", trigger.popColor, false);
            pushed.time = null;
        }
    }
}
