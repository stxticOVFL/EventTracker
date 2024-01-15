using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace EventTracker.Objects
{
    public class TrackerHolder : MonoBehaviour
    {
        private Canvas _canvas;
        private GameObject _textBase;
        public bool revealed = false;
        public int active = 0;

        Dictionary<string, List<Tuple<string, long>>> pools;
        string currentPool;
        int poolIndex;

        internal static void Initialize() => new GameObject("TrackerHolder", typeof(TrackerHolder));

        private void Start()
        {
            transform.SetParent(RM.ui.transform, false); // for ease of access while debugging

            // create a canvas for the text to go into
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceCamera; // this seems to work better
            _textBase = new GameObject("TrackerItemBase", typeof(TrackerItem));
            _textBase.SetActive(false);
            _textBase.transform.parent = transform;

            if (!LevelRush.IsLevelRush())
            {
                string path = GhostRecorder.GetCompressedSavePathForLevel(EventTracker.Game.GetCurrentLevel());
                path = Path.GetDirectoryName(path);
                path += "/trackerPB.txt";

                if (File.Exists(path))
                {
                    bool header = true;
                    pools = [];
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
            }
            currentPool = "";
            EventTracker.holder = this;
        }

        public void PushText(string text) => PushText(text, Color.white);

        public TrackerItem PushText(string text, Color border, bool landmark = false, bool goal = false)
        {
            if (revealed && !goal) return null;
            if (active > EventTracker.Settings.Limit.Value && !goal)
            {
                foreach (Transform child in transform)
                {
                    if (child.GetSiblingIndex() == 0) continue;
                    var item = child.GetComponent<TrackerItem>();
                    item.index--;
                    if (item.leaving || !child.gameObject.activeSelf) continue;
                    item.Move(item.index < 0);
                    if (item.index < 0)
                    {
                        if (item.time != null)
                            active--;
                        item.leaving = true;
                    }
                }
            }
            var newText = Instantiate(_textBase, transform).GetComponent<TrackerItem>();
            newText.name = "Tracker Item";
            newText.text = text;
            newText.longTime = EventTracker.Game.GetCurrentLevelTimerMicroseconds();
            newText.time = Game.GetTimerFormattedMillisecond(newText.longTime);
            newText.color = border;
            newText.gameObject.SetActive(true);
            newText.index = active++;
            newText.holder = this;
            newText.landmark = landmark;
            newText.goal = goal;

            if (pools != null)
            {
                if (landmark)
                {
                    poolIndex = 0;
                    // check pools
                    if (text.Contains("Demons"))
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

                try
                {
                    int index = poolIndex;
                    var desctime = pools[currentPool][index];
                    while (true)
                    {
                        index++;
                        if (desctime.Item1 == text.TrimEnd())
                        {
                            poolIndex = index;
                            break;
                        }
                        desctime = pools[currentPool][index];
                    }
                    var diff = newText.longTime - desctime.Item2;
                    string sign = diff.ToString("+0;-#").First().ToString(); // lol
                    diff = Math.Abs(diff);

                    var diffText = Instantiate(newText.gameObject, transform).GetComponent<TrackerItem>();
                    diffText.name = "PB Diff";
                    diffText.text = $"{sign,32}{Game.GetTimerFormattedMillisecond(diff)}";
                    diffText.time = null;
                    diffText.pbDiff = true;
                    diffText.color = sign == "-" ? Color.green : Color.red;

                    diffText.gameObject.SetActive(EventTracker.Settings.PBs.Value && !EventTracker.Settings.PBEndingOnly.Value);
                }
                catch { }
            }

            return newText;
        }

        public void Reveal(bool pb, bool finished)
        {
            revealed = true;

            if (!pb && pools != null && finished)
            {
                // we might not have our actual PB but it might be better than what we have on record
                if (EventTracker.Game.GetCurrentLevelTimerMicroseconds() < pools["Goal"][0].Item2)
                    pb = true;
            }

            FileStream file = null;
            StreamWriter stream = null;
            string path = GhostRecorder.GetCompressedSavePathForLevel(EventTracker.Game.GetCurrentLevel());
            if (!LevelRush.IsLevelRush())
            {
                path = Path.GetDirectoryName(path);
                path += "/trackerPB.txt";

                if (File.Exists(path) && !pb)
                    path = Path.GetDirectoryName(path) + "/tracker.txt";
                else if (File.Exists(path))
                    File.Copy(path, Path.GetDirectoryName(path) + "/trackerOldPB.txt", true);

                try
                {
                    file = File.OpenWrite(path);
                    stream = new StreamWriter(file);
                }
                catch { }
            }

            stream?.WriteLine("Event               \tTime    \t             \t \tDifference to PB on-record");

            float scroll = 0;
            foreach (Transform child in transform)
            {
                if (child.GetSiblingIndex() == 0) continue;
                var item = child.GetComponent<TrackerItem>();
                if (item.time != null || EventTracker.Settings.PBs.Value)
                    item.Reveal();
                item.scroll = scroll;
                if (child.GetSiblingIndex() == 1)
                {
                    // check the first real item
                    // if it's over the height we start Scroll Moding :tm:
                    if (child.position.y > Screen.height)
                        scroll = child.position.y;
                    item.scroll = scroll;
                }
                if (item.time != null)
                {
                    string diff = "";
                    try
                    {
                        if (pools != null)
                        {
                            var diffitem = transform.GetChild(child.GetSiblingIndex() + 1).GetComponent<TrackerItem>();
                            if (diffitem.time != null) throw new Exception();
                            diff = diffitem.text.TrimStart();
                        }
                    }
                    catch { }
                    stream?.WriteLine($"{item.text,-20}\t{item.time}\t{item.longTime,14}\t{(item.landmark ? '!' : ' ')}\t{diff}");
                }
            }

            var goal = PushText("Goal", new Color32(0xFF, 0xCF, 0x40, 255), true, true);
            goal.scroll = scroll;
            goal.transform.position = new Vector3(EventTracker.Settings.X.Value, Screen.height - (--active * EventTracker.Settings.Padding.Value) - EventTracker.Settings.Y.Value, 0);

            // PB diff, basically copy
            if (pools != null)
            {
                var goald = transform.GetChild(transform.childCount - 1).GetComponent<TrackerItem>();
                goald.gameObject.SetActive(true);
                goald.pbDiff = true;
                goald.scroll = scroll;
                goald.transform.position = new Vector3(EventTracker.Settings.X.Value, Screen.height - (active * EventTracker.Settings.Padding.Value) - EventTracker.Settings.Y.Value, 0);
                stream?.WriteLine($"{goal.text,-20}\t{goal.time}\t{goal.longTime,14}\t!\t{goald.text.TrimStart()}");
            }
            else
                stream?.WriteLine($"{goal.text,-20}\t{goal.time}\t{goal.longTime,14}\t!");

            if (stream != null)
            {
                var notif = PushText($"(saved to {Path.GetFileName(path)}!)", Color.white, false, true);
                notif.time = null; // but not pbdiff
                notif.scroll = scroll;
                notif.transform.position = new Vector3(EventTracker.Settings.X.Value, Screen.height - (active * EventTracker.Settings.Padding.Value) - EventTracker.Settings.Y.Value, 0);

                stream.Close();
                file.Close();
            }

        }
    }
}
