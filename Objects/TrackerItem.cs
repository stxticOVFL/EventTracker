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

        public void ChangeOpacity(float opacity, float? start = null) { 
            if (_opacityEaser != null)
                StopCoroutine(_opacityEaser);
            _opacityEaser = StartCoroutine(Co_Opacity(start ?? _opacity, opacity, 1 / speed));
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
                var pos = Screen.height - (index * EventTracker.Settings.Padding.Value) - EventTracker.Settings.Y.Value;
                var pre = pos - EventTracker.Settings.Padding.Value;

                _easer = StartCoroutine(Co_Move(pre, pos, 1 / speed));
                if (!EventTracker.Settings.EndingOnly.Value && !TrackerHolder.hidden)
                    ChangeOpacity(1);
            }
        }

        private void Update()
        {
            UpdateText();
            var pos = transform.position;
            if (scroll != 0)
            {
                pos.y += EventTracker.Settings.ScrollSpeed.Value * Time.unscaledDeltaTime;
                if (pos.y > scroll)
                    pos.y -= scroll + EventTracker.Settings.Padding.Value;
                transform.position = pos;
            }
            if (holder.revealed)
            {
                if (_opacity < 1)
                    _opacity += Time.unscaledDeltaTime * speed; // just do this one linearly idrc
                pos.x = EventTracker.Settings.EndingX.Value;
                transform.position = pos;
            }
        }

        void Done()
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
            Done();

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
            Done();

            yield return null;

        }

        public void Move(bool end)
        {
            if (_easer != null)
                StopCoroutine(_easer);
            var pos = Screen.height - (index * EventTracker.Settings.Padding.Value) - EventTracker.Settings.Y.Value;
            var pre = pos - EventTracker.Settings.Padding.Value;
            _easer = StartCoroutine(Co_Move(pre, pos, 1 / speed));
            if (end)
                ChangeOpacity(0);
        }

        public void Reveal()
        {
            gameObject.SetActive(true);
            if (_easer != null)
                StopCoroutine(_easer);
            leaving = false;
            transform.position = new Vector3(EventTracker.Settings.X.Value, Screen.height - (index * EventTracker.Settings.Padding.Value) - EventTracker.Settings.Y.Value, 0);
        }
    }
}
