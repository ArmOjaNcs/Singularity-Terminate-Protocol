using UnityEngine;
using UnityEngine.Rendering;
using WeaponConfigs;

namespace Gameplay.View
{
    internal sealed class TurretView : MonoBehaviour
    {
        internal const int DirectionCount = 8;
        private const float DegreesPerStep = 360f / DirectionCount;
        private const float RecoilDuration = 0.08f;
        private const float RecoilDistance = 0.10f;
        private const float DischargeShakeDegrees = 0.65f;
        private const float DischargeShakeFrequency = 13f;
        private const float DischargeShakeFade = 0.08f;

        private TurretWeaponConfig _config;
        private SpriteRenderer _head;
        private Vector3 _headRestPosition;
        private int _facing;
        private int _nextFacing;
        private int _turnStep;
        private float _turnElapsed;
        private float _recoilRemaining;
        private float _burstRemaining;
        private float _dischargeElapsed;
        private float _dischargeRemaining;

        internal Vector3 AimDirection => DirectionFor(_facing);
        internal Vector3 AimOrigin => transform.position + Vector3.up * _config.AimHeight;
        internal Vector3 ShotOrigin => _config.MuzzleOffsets is { Length: DirectionCount }
            ? _head.transform.TransformPoint(_config.MuzzleOffsets[_facing])
            : AimOrigin;

        internal static TurretView Create(TurretWeaponConfig config, Transform parent, Vector3 position)
        {
            var root = new GameObject(config.Type.ToString());
            root.transform.SetParent(parent, false);
            root.transform.position = position;
            root.AddComponent<SortingGroup>().sortingOrder = -Mathf.RoundToInt(position.y * 100f);

            var view = root.AddComponent<TurretView>();
            view._config = config;
            SpriteRenderer pedestal = view.CreateRenderer("Base", 0);
            pedestal.sprite = config.BaseSprite;
            view._head = view.CreateRenderer("Head", 1);
            view._headRestPosition = Vector3.up * config.HeadHeight;
            view._head.transform.localPosition = view._headRestPosition;
            view._head.sprite = config.HeadSprites[0];
            return view;
        }

        internal void UpdateAim(Vector3 direction, bool isAttacking, float deltaTime)
        {
            _recoilRemaining = Mathf.Max(0f, _recoilRemaining - deltaTime);
            _burstRemaining = Mathf.Max(0f, _burstRemaining - deltaTime);
            float recoil = RecoilDistance * (_recoilRemaining / RecoilDuration);
            _head.transform.localPosition = _headRestPosition - AimDirection * recoil;

            if (_dischargeRemaining > 0f)
                UpdateDischargeShake(isAttacking, deltaTime);

            if (isAttacking || _burstRemaining > 0f)
                return;

            if (_turnStep != 0)
            {
                _turnElapsed += deltaTime;
                float progress = Mathf.Clamp01(_turnElapsed / Mathf.Max(0.01f, _config.TurnStepDuration));
                DrawTurn(progress);
                if (progress < 1f)
                    return;

                _facing = _nextFacing;
                _turnStep = 0;
            }

            if (direction.sqrMagnitude <= 0.0001f)
                return;

            int targetFacing = FacingFor(direction);
            if (targetFacing == _facing)
                return;

            int steps = (targetFacing - _facing + DirectionCount) % DirectionCount;
            _turnStep = steps <= DirectionCount / 2 ? 1 : -1;
            _nextFacing = (_facing + _turnStep + DirectionCount) % DirectionCount;
            _turnElapsed = 0f;
            DrawTurn(0f);
        }

        internal bool IsAimedAt(Vector3 direction)
        {
            direction.z = 0f;
            return _turnStep == 0 && _burstRemaining <= 0f &&
                   direction.sqrMagnitude > 0.0001f && FacingFor(direction) == _facing;
        }

        internal void PlayBurst(float shotInterval, int shots)
        {
            _burstRemaining = Mathf.Max(_burstRemaining, Mathf.Max(0f, shotInterval) * (shots - 1) + RecoilDuration);
            PlayShot();
        }

        internal void PlayShot()
        {
            _burstRemaining = Mathf.Max(_burstRemaining, RecoilDuration);
            _recoilRemaining = RecoilDuration;
            _head.transform.localPosition = _headRestPosition - AimDirection * RecoilDistance;
        }

        internal void PlayDischarge(float duration)
        {
            _dischargeElapsed = 0f;
            _dischargeRemaining = duration;
        }

        private void UpdateDischargeShake(bool isAttacking, float deltaTime)
        {
            _dischargeElapsed += deltaTime;
            _dischargeRemaining = isAttacking ? Mathf.Max(0f, _dischargeRemaining - deltaTime) : 0f;
            float strength = Mathf.Clamp01(Mathf.Min(_dischargeElapsed, _dischargeRemaining) / DischargeShakeFade);
            float phase = _dischargeElapsed * DischargeShakeFrequency * (2f * Mathf.PI);
            Quaternion rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(phase) * DischargeShakeDegrees * strength);
            Vector3 muzzleOffset = _config.MuzzleOffsets is { Length: DirectionCount }
                ? (Vector3)_config.MuzzleOffsets[_facing]
                : transform.InverseTransformPoint(AimOrigin) - _headRestPosition;

            // Counter the head rotation at the muzzle so the fixed discharge stays attached.
            Vector3 position = _headRestPosition + muzzleOffset - (rotation * muzzleOffset);
            _head.transform.SetLocalPositionAndRotation(position, rotation);
        }

        internal static Vector3 DirectionFor(int facing)
        {
            float angle = facing * DegreesPerStep * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
        }

        private static int FacingFor(Vector3 direction)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            return (Mathf.RoundToInt(angle / DegreesPerStep) + DirectionCount) % DirectionCount;
        }

        private void DrawTurn(float progress)
        {
            float eased = Mathf.SmoothStep(0f, 1f, progress);
            bool useNext = eased >= 0.5f;
            _head.sprite = _config.HeadSprites[useNext ? _nextFacing : _facing];
            float turn = (useNext ? eased - 1f : eased) * DegreesPerStep * _turnStep;
            _head.transform.localRotation = Quaternion.Euler(0f, 0f, turn);
        }

        private SpriteRenderer CreateRenderer(string label, int order)
        {
            var part = new GameObject(label);
            part.transform.SetParent(transform, false);
            SpriteRenderer renderer = part.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = order;
            return renderer;
        }
    }
}
