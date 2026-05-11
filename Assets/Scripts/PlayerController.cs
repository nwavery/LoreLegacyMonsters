using UnityEngine;
using UnityEngine.InputSystem;
using LoreLegacyMonsters.World;
using LoreLegacyMonsters.Core;
using LoreLegacyMonsters;

namespace LoreLegacyMonsters
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] float moveSpeed = 3f;
        [SerializeField] float collisionRadius = 0.28f;
        [SerializeField] string playerName = "Hero";

        Vector3 lastPosition;
        Transform spriteRoot;
        Vector3 spriteBaseLocalPosition;
        Vector3 spriteBaseLocalScale = Vector3.one;
        int facing = 1;

        public bool InputLocked { get; set; }
        public float DistanceMovedThisFrame { get; private set; }
        public string PlayerName => playerName;

        void Awake()
        {
            lastPosition = transform.position;
        }

        void Update()
        {
            DistanceMovedThisFrame = 0f;
            if (InputLocked)
            {
                lastPosition = transform.position;
                return;
            }

            var kb = Keyboard.current;
            var v = Vector2.zero;
            if (kb != null)
            {
                if (kb[GameSettings.MoveUp].isPressed) v.y += 1;
                if (kb[GameSettings.MoveDown].isPressed) v.y -= 1;
                if (kb[GameSettings.MoveLeft].isPressed) v.x -= 1;
                if (kb[GameSettings.MoveRight].isPressed) v.x += 1;
            }

            var pad = Gamepad.current;
            if (pad != null)
            {
                v += pad.leftStick.ReadValue();
                var dpad = pad.dpad.ReadValue();
                v += dpad;
            }
            if (v.sqrMagnitude > 0.01f)
            {
                var speedMult = GameManager.Instance?.Loadout?.Snapshot.MoveSpeedMult ?? 1f;
                speedMult = Mathf.Max(0.2f, speedMult);
                var from = new Vector2(transform.position.x, transform.position.y);
                var desired = from + v.normalized * moveSpeed * speedMult * Time.deltaTime;
                var resolved = WorldMapLayout.ResolveNavigation(from, desired, collisionRadius);
                transform.position = new Vector3(resolved.x, resolved.y, transform.position.z);
                if (Mathf.Abs(v.x) > 0.01f)
                    facing = v.x >= 0f ? 1 : -1;
            }
            UpdateSpriteMotion(v);
            DistanceMovedThisFrame = Vector3.Distance(lastPosition, transform.position);
            lastPosition = transform.position;
        }

        void UpdateSpriteMotion(Vector2 input)
        {
            spriteRoot ??= transform.Find("PlayerSprite");
            if (spriteRoot == null) return;
            if (spriteBaseLocalPosition == Vector3.zero && spriteRoot.localPosition != Vector3.zero)
                spriteBaseLocalPosition = spriteRoot.localPosition;
            if (spriteBaseLocalScale == Vector3.one && spriteRoot.localScale != Vector3.one)
                spriteBaseLocalScale = spriteRoot.localScale;

            var moving = input.sqrMagnitude > 0.01f;
            var bob = moving ? Mathf.Sin(Time.time * 12f) * 0.035f : 0f;
            spriteRoot.localPosition = spriteBaseLocalPosition + new Vector3(0f, bob, 0f);
            spriteRoot.localScale = new Vector3(Mathf.Abs(spriteBaseLocalScale.x) * facing, spriteBaseLocalScale.y, spriteBaseLocalScale.z);
        }
    }
}
