using System.Collections;
using UnityEngine;
using TMPro;

namespace EventTracker.Objects
{
    public class TrackerItem : MonoBehaviour
    {
        public float scroll;
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
        private float _opacity = 0.0f;

        private readonly float speed = 4;
        private Coroutine _easer;
        private Coroutine _opacityEaser;

        void UpdateText()
        {
            _text.faceColor = (!pbDiff ? Color.black : color).Alpha(_opacity);
            _text.outlineColor = (!pbDiff ? color : Color.black).Alpha(_opacity);
            if (time != null)
                _text.text = $"{text,-20} {time}";
            else _text.text = text;
        }

        public void ChangeOpacity(float opacity, float? start = null)
        {
            if (!EventTracker.Settings.Animated.Value)
                _opacity = opacity;
            else
            {
                if (_opacityEaser != null)
                    StopCoroutine(_opacityEaser);
                _opacityEaser = StartCoroutine(Co_Opacity(start ?? _opacity, opacity, 1 / speed));
            }
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
            if (!goal && (!pbDiff || !EventTracker.Settings.PBEndingOnly.Value))
            {
                Move(false, true);
                if (!EventTracker.Settings.EndingOnly.Value && !TrackerHolder.hidden)
                    ChangeOpacity(1);
            }
        }

        private void Update()
        {
            UpdateText();
            var pos = transform.position;
            pos.x = EventTracker.Settings.X.Value;
            if (scroll != 0)
            {
                pos.y += EventTracker.Settings.ScrollSpeed.Value * Time.unscaledDeltaTime;
                if (pos.y > scroll)
                    pos.y -= scroll + EventTracker.Settings.Padding.Value;
            }
            if (holder.revealed)
            {
                if (_opacity < 1)
                    _opacity += Time.unscaledDeltaTime * speed; // just do this one linearly idrc
                pos.x = EventTracker.Settings.EndingX.Value;
            }
            transform.position = pos;
        }

        void MoveDone()
        {
            _easer = null;
            if (leaving)
                gameObject.SetActive(false);
        }

        IEnumerator Co_Move(float start, float end, float length)
        {
            // yoinked from AxkEasing
            // tweaked for good :tm:
            float t2 = 0f;
            var pos = transform.position;
            pos.x = EventTracker.Settings.X.Value;
            pos.y = start;
            transform.position = pos;
            yield return null;

            for (t2 += Time.unscaledDeltaTime; t2 < length; t2 += Time.unscaledDeltaTime)
            {
                float dist = t2 / length;
                pos.y = AxKEasing.EaseOutExpo(start, end, dist);
                transform.position = pos;
                yield return null;
            }

            pos.y = end;
            transform.position = pos;
            MoveDone();

            yield return null;
        }

        IEnumerator Co_Opacity(float start, float end, float length)
        {
            float t2 = 0f;
            _opacity = start;
            yield return null;

            for (t2 += Time.unscaledDeltaTime; t2 < length; t2 += Time.unscaledDeltaTime)
            {
                float dist = t2 / length;
                _opacity = AxKEasing.Linear(start, end, dist);
                yield return null;
            }

            _opacity = end;
            _opacityEaser = null;

            yield return null;

        }

        public void Move(bool end, bool transition = true)
        {
            var endPos = Screen.height - (index * EventTracker.Settings.Padding.Value) - EventTracker.Settings.Y.Value;
            if (!EventTracker.Settings.Animated.Value || (_opacity == 0 && !transition))
            {
                var pos = transform.position;
                pos.y = endPos;
                transform.position = pos;
                MoveDone();
            }
            else
            {
                if (_easer != null)
                    StopCoroutine(_easer);
                var pos = endPos;
                var pre = pos - EventTracker.Settings.Padding.Value;
                _easer = StartCoroutine(Co_Move(pre, pos, 1 / speed));
            }
            if (end)
                ChangeOpacity(0);
        }

        public void Reveal()
        {
            gameObject.SetActive(true);
            if (_easer != null)
                StopCoroutine(_easer);
            leaving = false;
            transform.position = new Vector3(EventTracker.Settings.EndingX.Value, Screen.height - (index * EventTracker.Settings.Padding.Value) - EventTracker.Settings.Y.Value, 0);
        }
    }
}
