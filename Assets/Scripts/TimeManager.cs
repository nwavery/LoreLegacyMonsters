using UnityEngine;

namespace LoreLegacyMonsters
{
    public class TimeManager : MonoBehaviour
    {
        [SerializeField] float dayLengthSeconds = 120f;
        float t;

        public float NormalizedDayTime => dayLengthSeconds > 0 ? (t % dayLengthSeconds) / dayLengthSeconds : 0f;

        void Update() => t += Time.deltaTime;
    }
}
