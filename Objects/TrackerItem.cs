using UnityEngine;
using TMPro;
using System;
using static EventTracker.EventTracker;

namespace EventTracker.Objects
{



    public class TrackerItem : MonoBehaviour
    {
        public bool revealed = false;
        public bool autoScroll = true;
        public float manualScroll { get { return _scrollT.result; } }
        public bool leaving;

        public bool pbDiff = false;
        public bool landmark = false;
        public bool goal = false;

        public Color color;
        public string text;
        public string time;
        public long longTime;

        public int index;

        public TrackerHolder holder;

        private TextMeshProUGUI _text;

        private float speed = 4;
        public bool skip = false;

        public class Transition(Func<float, float, float, float> ease, Action finish)
        {
            Func<float, float, float, float> easeFunc = ease;
            Action finishFunc = finish;

            public bool skip = false;
            public float start;
            public float goal;
            public float result;
            public bool running;
            public float speed;
            public float time;

            public void Start(float? s, float g)
            {
                running = true;
                time = 0;
                start = s ?? result;
                result = start;
                goal = g;
            }

            public void Process()
            {
                if (time == 1f)
                {
                    result = goal;
                    finishFunc?.Invoke();
                    running = false;
                }
                if (!running)
                    return;
                time = Math.Min(1f, time + (Time.unscaledDeltaTime * speed));
                result = easeFunc(start, goal, time);
            }
        }

        private Transition _opacityT = new(AxKEasing.Linear, null);
        private Transition _moveT = new(AxKEasing.EaseOutExpo, null);
        private Transition _moveXT = new(AxKEasing.EaseOutBack, null);
        public Transition _scrollT = new(AxKEasing.EaseOutCubic, null);

        private float _opacity { get { return _opacityT.result; } set { _opacityT.result = value; } }

        private bool _teleport;

        public float TargetY() { return TargetY(index); }
        public static float TargetY(int index) => -index * Settings.Padding.Value;

        void UpdateText()
        {
            _text.faceColor = (!pbDiff ? Color.black : color).Alpha(holder.forceHide ? 0 : _opacity);
            _text.outlineColor = (!pbDiff ? color : Color.black).Alpha(holder.forceHide ? 0 : _opacity);
            if (time != null)
                _text.text = $"{text.PadRight(Settings.TimerPadding.Value)} {time}";
            else _text.text = text;
        }

        public void ChangeOpacity(float opacity, float? start = null)
        {
            if (!holder.revealed && !EventTracker.Settings.Animated.Value)
                _opacity = opacity;
            else if (opacity != _opacity || start != null)
                _opacityT.Start(start, opacity);
        }

        private void Awake()
        {
            _opacityT = new(AxKEasing.Linear, OpacityDone);
            // _moveT = new(AxKEasing.EaseOutExpo, MoveDone);
        }

        private void Start()
        {
            _text = gameObject.AddMissingComponent<TextMeshProUGUI>();

            _text.margin = new Vector4(100, 0, -1000, 0);
            _text.fontSize = 24;
            if (!pbDiff)
            {
                _text.font = Resources.Load<TMP_FontAsset>("fonts/source code pro/SourceCodePro-Black SDF");
                _text.outlineWidth = 0.15f;
            }
            else
            {
                _text.font = Resources.Load<TMP_FontAsset>("fonts/source code pro/SourceCodePro-MediumItalic SDF");
                _text.outlineWidth = 0.13f;
            }
            UpdateText();
            if (!pbDiff || !Settings.EndingPBDiff.Value)
            {
                Move(false, true);
                if ((!goal && !Settings.EndingOnly.Value && !holder.hidden) || (goal && holder.scroll == 0))
                {
                    ChangeOpacity(1);
                    if (Settings.Animated.Value && Settings.Bouncy.Value)
                        _moveXT.Start(-30, 0);
                }
            }
            else
            {
                _moveXT.result = 0;
                _moveT.result = TargetY();
            }
        }

        private void Update()
        {
            transform.localScale = Vector3.one;
            UpdateText();
            var pos = transform.localPosition;
            if (holder.scroll != 0)
            {
                if (autoScroll)
                    pos.y += Settings.DefaultScroll.Value * Time.unscaledDeltaTime;
                else if (manualScroll != 0)
                {
                    _scrollT.speed = 4;
                    pos.y += manualScroll;
                    _scrollT.Process();
                }

                if (pos.y > holder.scroll)
                {
                    _teleport = false;
                    while (pos.y > holder.scroll)
                        pos.y -= holder.scroll - holder.emerge;
                }
                else if (pos.y < holder.emerge && !_teleport)
                {
                    _teleport = true;
                    ChangeOpacity(0);
                }

                if (pos.y > 0)
                {
                    if (!_opacityT.running || _opacityT.goal != 0)
                        ChangeOpacity(0);
                }
                else if (!_teleport)
                {
                    if (!holder.hidden && (!_opacityT.running || _opacityT.goal != 1))
                        ChangeOpacity(1);
                    else if (holder.hidden && (!_opacityT.running || _opacityT.goal != 0))
                        ChangeOpacity(0);
                }
            }
            else
            {
                pos.y = _moveT.result;
                if (_moveXT.running)
                    pos.x = _moveXT.result;
                else pos.x = 0;
            }
            transform.localPosition = pos;

            _opacityT.speed = speed;
            _moveT.speed = speed;
            _moveXT.speed = speed;

            if (holder.forceHide)
            {
                _opacityT.time = 1;
                _moveT.time = 1;
                _moveXT.time = 1;
            }

            _opacityT.Process();
            _moveT.Process();
            _moveXT.Process();

        }

        void OpacityDone()
        {
            if (_teleport)
            {
                //Debug.Log($"{text} teleport!");
                var pos = transform.localPosition;
                while (pos.y < holder.emerge)
                {
                    pos.y += holder.scroll - holder.emerge;
                    ///Debug.Log($"{text} loop");
                }
                transform.localPosition = pos;
                _teleport = false;
            }
            if (leaving)
                gameObject.SetActive(false);
        }
        public void Move(bool end, bool transition = true)
        {
            var endPos = TargetY();
            if ((goal && holder.scroll != 0) || !Settings.Animated.Value || (_opacity == 0 && !transition))
            {
                _moveT.result = endPos;
                OpacityDone();
            }
            else
            {
                var pos = endPos;
                var pre = pos - Settings.Padding.Value;
                _moveT.Start(pre, pos);
            }
            if (end && (!_opacityT.running || _opacityT.goal != 0))
            {
                if (Settings.Animated.Value && Settings.Bouncy.Value)
                    _moveXT.Start(0, -30);
                ChangeOpacity(0);
            }
        }

        public void Reveal()
        {
            speed = 10;
            leaving = false;
            revealed = true;
            gameObject.SetActive(true);
            _moveT.running = false;
            _moveT.goal = TargetY();
            transform.localPosition = new Vector3(0, TargetY(), 0);
            if (holder.scroll == 0)
                ChangeOpacity(1);
        }
    }
}
