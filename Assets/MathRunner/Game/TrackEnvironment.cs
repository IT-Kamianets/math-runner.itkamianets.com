using System.Collections.Generic;
using UnityEngine;

namespace MathRunner.Game
{
    /// <summary>
    /// The street around the runner: road, sidewalks, lane marks, buildings and props.
    /// Built in 12 m segments ahead of the runner and removed behind it. Looks only, no rules.
    /// </summary>
    public sealed class TrackEnvironment
    {
        public const float SegmentLength = 12f;

        private static readonly string[] Buildings =
        {
            "04_shop_red", "07_apartment_tall", "05_shop_teal", "08_apartment_wide",
            "06_shop_yellow", "09_corner_building",
        };

        private static readonly string[] Skyline = { "10_skyline_a", "11_skyline_b" };

        private static readonly string[] Props =
        {
            "15_bench", "16_trash_bin", "17_mailbox", "22_hydrant", "19_flower_planter", "18_bus_shelter",
        };

        private readonly System.Func<string, GameObject> _prefab;
        private readonly Transform _root;
        private readonly Dictionary<int, GameObject> _segments = new Dictionary<int, GameObject>();
        private readonly List<int> _stale = new List<int>();

        /// <summary>Yaw that turns a building so its front faces the road. Tuned for the starter models.</summary>
        public float LeftBuildingYaw { get; set; } = 90f;
        public float RightBuildingYaw { get; set; } = -90f;

        public TrackEnvironment(System.Func<string, GameObject> prefab, Transform root)
        {
            _prefab = prefab;
            _root = root;
        }

        public void Clear()
        {
            foreach (var segment in _segments.Values)
                if (segment != null) Object.Destroy(segment);
            _segments.Clear();
        }

        /// <summary>Makes sure every segment from just behind to far ahead of the runner exists.</summary>
        public void Update(float runnerZ, float ahead, float behind)
        {
            var first = Mathf.FloorToInt((runnerZ - behind) / SegmentLength);
            var last = Mathf.FloorToInt((runnerZ + ahead) / SegmentLength);
            for (var i = first; i <= last; i++)
                if (!_segments.ContainsKey(i)) _segments[i] = BuildSegment(i);

            _stale.Clear();
            foreach (var pair in _segments)
                if (pair.Key < first) _stale.Add(pair.Key);
            foreach (var index in _stale)
            {
                if (_segments[index] != null) Object.Destroy(_segments[index]);
                _segments.Remove(index);
            }
        }

        private GameObject BuildSegment(int index)
        {
            var z0 = index * SegmentLength;
            var segment = new GameObject("Segment " + index);
            segment.transform.SetParent(_root, false);
            var t = segment.transform;

            Place("road_segment", t, new Vector3(0f, 0f, z0 + 6f), 0f);
            Place("sidewalk_segment", t, new Vector3(-4f, 0f, z0 + 6f), 0f);
            Place("sidewalk_segment", t, new Vector3(4f, 0f, z0 + 6f), 0f);
            Place("ground_fill", t, new Vector3(-11f, 0f, z0 + 6f), 0f);
            Place("ground_fill", t, new Vector3(11f, 0f, z0 + 6f), 0f);

            // Dashed lines between the three lanes.
            for (var k = 0; k < 4; k++)
            {
                var z = z0 + 1.5f + 3f * k;
                Place("lane_marking", t, new Vector3(-1f, 0.1f, z), 0f);
                Place("lane_marking", t, new Vector3(1f, 0.1f, z), 0f);
            }

            for (var side = 0; side < 2; side++)
            {
                var sign = side == 0 ? -1f : 1f;
                var yaw = side == 0 ? LeftBuildingYaw : RightBuildingYaw;
                for (var k = 0; k < 2; k++)
                {
                    var pick = Hash(index, side, k);
                    Place(Buildings[pick % Buildings.Length], t, new Vector3(sign * 8.6f, 0f, z0 + 3f + 6f * k), yaw);
                }

                var far = Hash(index, side, 7);
                Place(Skyline[far % Skyline.Length], t, new Vector3(sign * 16f, 0f, z0 + 6f), yaw);

                Place("12_street_lamp", t, new Vector3(sign * 4.6f, 0.36f, z0 + 2f), yaw);
                var dressing = Hash(index, side, 3) % 5;
                if (dressing == 0) Place("13_tree_round", t, new Vector3(sign * 4.5f, 0.36f, z0 + 8f), 0f);
                else if (dressing == 1) Place("14_bush", t, new Vector3(sign * 4.5f, 0.36f, z0 + 8f), 0f);
                else Place(Props[Hash(index, side, 5) % Props.Length], t,
                    new Vector3(sign * 4.5f, 0.36f, z0 + 8f), yaw);
            }

            return segment;
        }

        private void Place(string key, Transform parent, Vector3 position, float yaw)
        {
            var prefab = _prefab(key);
            if (prefab == null) return;
            var instance = Object.Instantiate(prefab, parent);
            instance.name = key;
            instance.transform.localPosition = position;
            instance.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            foreach (var collider in instance.GetComponentsInChildren<Collider>())
                collider.enabled = false;
        }

        private static int Hash(int a, int b, int c)
        {
            unchecked
            {
                var h = a * 73856093 ^ b * 19349663 ^ c * 83492791;
                return (h & 0x7fffffff) >> 3;
            }
        }
    }
}
