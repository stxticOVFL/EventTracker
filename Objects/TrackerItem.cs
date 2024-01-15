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

        void UpdateText()
        {
            _text.faceColor = (!pbDiff ? Color.black : color).Alpha(_opacity);
            _text.outlineColor = (!pbDiff ? color : Color.black).Alpha(_opacity);
            if (time != null)
                _text.text = $"{text,-20} {time}";
            else _text.text = text;
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

                _easer = StartCoroutine(Co_Move(pre, pos, 1 / speed, 0, EventTracker.Settings.EndingOnly.Value ? 0 : 1));
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

        IEnumerator Co_Move(float start, float end, float length, float opacity_start, float opacity_end)
        {
            // yoinked from AxkEasing
            // tweaked for good :tm:
            float t2 = 0f;
            var pos = transform.position;
            pos.x = EventTracker.Settings.X.Value;
            pos.y = start;
            _opacity = opacity_start;
            transform.position = pos;
            yield return null;

            for (t2 += Time.unscaledDeltaTime; t2 < length; t2 += Time.unscaledDeltaTime)
            {
                float dist = t2 / length;
                pos.y = AxKEasing.EaseOutExpo(start, end, dist);
                _opacity = AxKEasing.Linear(opacity_start, opacity_end, dist);
                transform.position = pos;
                yield return null;
            }

            pos.y = end;
            _opacity = opacity_end;
            transform.position = pos;
            Done();

            yield return null;
        }

        public void Move(bool end)
        {
            if (_easer != null)
                StopCoroutine(_easer);
            var pos = Screen.height - (index * EventTracker.Settings.Padding.Value) - EventTracker.Settings.Y.Value;
            var pre = pos - EventTracker.Settings.Padding.Value;
            _easer = StartCoroutine(Co_Move(pre, pos, 1 / speed, _opacity, end ? 0 : (EventTracker.Settings.EndingOnly.Value ? 0 : 1)));
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
