﻿using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using SAIN.Helpers;
using SAIN.Patches.Vision;
using SAIN.Plugin;
using SAIN.Preset.GlobalSettings;
using SAIN.SAINComponent.Classes.Sense;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Enemy
{
    public class SAINEnemyVision : EnemyBase
    {
        public SAINEnemyVision(SAINEnemy enemy) : base(enemy)
        {
        }

        public void Update(bool isCurrentEnemy)
        {
            UpdateVisible(false);
            UpdateCanShoot(false);
        }

        public bool FirstContactOccured { get; private set; }
        public bool ShallReportRepeatContact { get; set; }
        public bool ShallReportLostVisual { get; set; }

        private const float _repeatContactMinSeenTime = 12f;
        private const float _lostContactMinSeenTime = 12f;

        private float _realLostVisionTime;

        public void UpdateVisible(bool forceOff)
        {
            bool wasVisible = IsVisible;
            bool lineOfSight = InLineOfSight || SAIN.Memory.VisiblePlayers.Contains(EnemyPlayer);
            if (forceOff)
            {
                IsVisible = false;
            }
            else
            {
                IsVisible = EnemyInfo?.IsVisible == true && lineOfSight;
            }

            if (IsVisible)
            {
                if (!wasVisible)
                {
                    VisibleStartTime = Time.time;
                    if (Seen && TimeSinceSeen >= _repeatContactMinSeenTime)
                    {
                        ShallReportRepeatContact = true;
                    }
                }
                if (!Seen)
                {
                    FirstContactOccured = true;
                    TimeFirstSeen = Time.time;
                    Seen = true;
                }
                _realLostVisionTime = Time.time;
                TimeLastSeen = Time.time;
                LastSeenPosition = EnemyPerson.Position;
            }

            if (Time.time - _realLostVisionTime < 1f)
            {
                Enemy.UpdateSeenPosition(EnemyPerson.Position);
            }

            if (!IsVisible)
            {
                if (Seen
                    && TimeSinceSeen > _lostContactMinSeenTime
                    && _nextReportLostVisualTime < Time.time)
                {
                    _nextReportLostVisualTime = Time.time + 20f;
                    ShallReportLostVisual = true;
                }
                VisibleStartTime = -1f;
            }

            if (IsVisible != wasVisible)
            {
                LastChangeVisionTime = Time.time;
            }
        }

        private float _nextReportLostVisualTime;

        public void UpdateCanShoot(bool forceOff)
        {
            if (forceOff)
            {
                CanShoot = false;
                return;
            }
            CanShoot = EnemyInfo?.CanShoot == true;
        }

        public bool InLineOfSight { get; set; }
        public bool IsVisible { get; private set; }
        public bool CanShoot { get; private set; }
        public Vector3? LastSeenPosition { get; set; }
        public float VisibleStartTime { get; private set; }
        public float TimeSinceSeen => Seen ? Time.time - TimeLastSeen : -1f;
        public bool Seen { get; private set; }
        public float TimeFirstSeen { get; private set; }
        public float TimeLastSeen { get; private set; }
        public float LastChangeVisionTime { get; private set; }

        public float GainSightCoef
        {
            get
            {
                calcVisionModifiers();
                return _gainSightModifier;
            }
        }

        public float VisionDistanceModifier
        {
            get
            {
                calcVisionModifiers();
                return _visionDistanceModifier;
            }
        }

        private void calcVisionModifiers()
        {
            if (_nextCheckVisTime < Time.time)
            {
                _nextCheckVisTime = Time.time + 0.1f;
                _gainSightModifier = GetGainSightModifier(EnemyInfo, SAIN) * calcRepeatSeenCoef();
                _visionDistanceModifier = 0f;
            }
        }

        private float calcRepeatSeenCoef()
        {
            float result = 1f;
            if (Seen)
            {
                Vector3? lastSeenPos = LastSeenPosition;
                if (lastSeenPos != null)
                {
                    result = calcVisionSpeedPositional(
                        lastSeenPos.Value, 
                        _minSeenSpeedCoef, 
                        _minDistRepeatSeen, 
                        _maxDistRepeatSeen, 
                        SeenSpeedCheck.Vision);
                }
            }

            if (Enemy.Heard)
            {
                Vector3? lastHeardPosition = Enemy.LastHeardPosition;
                if (lastHeardPosition != null)
                {
                    result *= calcVisionSpeedPositional(
                        lastHeardPosition.Value, 
                        _minHeardSpeedCoef, 
                        _minDistRepeatHeard, 
                        _maxDistRepeatHeard, 
                        SeenSpeedCheck.Audio);
                }
            }
            return result;
        }

        private enum SeenSpeedCheck
        {
            None = 0,
            Vision = 1,
            Audio = 2,
        }

        private float calcVisionSpeedPositional(Vector3 position, float minSpeedCoef, float minDist, float maxDist, SeenSpeedCheck check)
        {
            float distance = (position - EnemyPosition).magnitude;
            if (distance <= minDist)
            {
                return minSpeedCoef;
            }
            if (distance >= maxDist)
            {
                return 1f;
            }
            
            float seenSpeedDiff = maxDist - minDist;
            float distanceDiff = distance - minDist;
            float scaled = distanceDiff / seenSpeedDiff;
            float result = Mathf.Lerp(minSpeedCoef, 1f, scaled);
            //Logger.LogInfo($"{check} Distance from Position: {distance} Result: {result}");
            return result;
        }

        private float _minSeenSpeedCoef = 1E-05f;
        private float _minDistRepeatSeen = 3f;
        private float _maxDistRepeatSeen = 15f;

        private float _minHeardSpeedCoef = 0.2f;
        private float _minDistRepeatHeard = 5f;
        private float _maxDistRepeatHeard = 25f;

        private float _gainSightModifier;
        private float _visionDistanceModifier;
        private float _nextCheckVisTime;

        public static float GetGainSightModifier(EnemyInfo enemyInfo, BotComponent sain)
        {
            float result = 1f;
            float dist = (enemyInfo.Owner.Position - enemyInfo.CurrPosition).magnitude;
            float weatherModifier = SAINPlugin.BotController.WeatherVision.VisibilityNum;
            float inverseWeatherModifier = Mathf.Sqrt(2f - weatherModifier);

            WildSpawnType wildSpawnType = enemyInfo.Owner.Profile.Info.Settings.Role;
            if (PresetHandler.LoadedPreset.BotSettings.SAINSettings.TryGetValue(wildSpawnType, out var sainSettings))
            {
                BotDifficulty diff = enemyInfo.Owner.Profile.Info.Settings.BotDifficulty;
                result *= Math.CalcVisSpeed(dist, sainSettings.Settings[diff]);
            }

            var person = enemyInfo.Person;
            if (person != null)
            {
                Player player = EFTInfo.GetPlayer(enemyInfo.Person.ProfileId);
                if (player != null)
                {
                    var gearInfo = SAINGearInfoHandler.GetGearInfo(player);
                    if (gearInfo != null)
                    {
                        result *= gearInfo.GetStealthModifier(enemyInfo.Distance);
                    }

                    bool flare = person.AIData.GetFlare;
                    bool suppressedFlare =
                        flare
                        && player.HandsController.Item is Weapon weapon
                        && gearInfo?.GetWeaponInfo(weapon)?.HasSuppressor == true;

                    // Only apply vision speed debuff from weather if their enemy has not shot an unsuppressed weapon
                    if (!flare || suppressedFlare)
                    {
                        result *= inverseWeatherModifier;
                    }

                    LookSettings globalLookSettings = SAINPlugin.LoadedPreset.GlobalSettings.Look;
                    if (player.IsSprintEnabled)
                    {
                        result *= Mathf.Lerp(1, globalLookSettings.SprintingVisionModifier, Mathf.InverseLerp(0, 5f, player.Velocity.magnitude)); // 5f is the observed max sprinting speed with gameplays (with Realism, which gives faster sprinting)
                    }

                    Vector3 botEyeToPlayerBody = enemyInfo.Person.MainParts[BodyPartType.body].Position - enemyInfo.Owner.MainParts[BodyPartType.head].Position;
                    var visionAngleDeviation = Vector3.Angle(new Vector3(botEyeToPlayerBody.x, 0f, botEyeToPlayerBody.z), botEyeToPlayerBody);

                    if (botEyeToPlayerBody.y >= 0)
                    {
                        float angleFactor = Mathf.InverseLerp(0, globalLookSettings.HighElevationMaxAngle, visionAngleDeviation);
                        result *= Mathf.Lerp(1f, globalLookSettings.HighElevationVisionModifier, angleFactor);
                    }
                    else
                    {
                        float angleFactor = Mathf.InverseLerp(0, globalLookSettings.LowElevationMaxAngle, visionAngleDeviation);
                        result *= Mathf.Lerp(1f, globalLookSettings.LowElevationVisionModifier, angleFactor);
                    }

                    if (!player.IsAI)
                    {
                        result *= SAINNotLooking.GetVisionSpeedDecrease(enemyInfo);
                    }

                    SAINEnemy sainEnemy = sain?.EnemyController.GetEnemy(player.ProfileId);
                    if (sainEnemy?.EnemyStatus.PositionalFlareEnabled == true
                        && sainEnemy.Heard
                        && sainEnemy.TimeSinceHeard < 300f)
                    {
                        result *= 0.9f;
                    }

                    // Testing, will log results
                    //getVisionAngleCoef(enemyInfo);
                }
            }

            return result;
        }

        private static bool _reduceVisionSpeedOnPeriphVis = true;
        private static float _periphVisionStart = 30f;
        private static float _maxPeriphVisionSpeedReduction = 0.5f;

        private static float getVisionAngleCoef(EnemyInfo enemyInfo)
        {
            if (!_reduceVisionSpeedOnPeriphVis)
            {
                return 1f;
            }

            if (!enemyInfo.Owner.LookSensor.IsPointInVisibleSector(enemyInfo.CurrPosition))
            {
                return 1f;
            }

            Vector3 myLookDir = enemyInfo.Owner.LookDirection;
            myLookDir.y = 0f;
            Vector3 enemyDir = enemyInfo.Direction;
            enemyDir.y = 0f;
            float angle = Vector3.Angle(myLookDir, enemyDir);

            if (angle < _periphVisionStart || angle > 90)
            {
                return 1f;
            }

            float maxVisionAngle = enemyInfo.Owner.Settings.FileSettings.Core.VisibleAngle / 2f;

            float angleDiff = maxVisionAngle - _periphVisionStart;
            float enemyAngleDiff = angle - _periphVisionStart;

            float modifier = 1f - enemyAngleDiff / angleDiff;

            float finalModifier = Mathf.Lerp(_maxPeriphVisionSpeedReduction, 1f, modifier);

            if (enemyInfo.Person.IsYourPlayer && 
                _nextLogTime < Time.time)
            {
                _nextLogTime = Time.time + 0.25f;
                Logger.LogDebug($"{enemyInfo.Owner.name} Vision Angle Coef: Final Modifier: [{finalModifier}] Angle: [{angle}] Enemy Angle Difference: [{enemyAngleDiff}] Max Vision Angle: [{maxVisionAngle}]");
            }

            return finalModifier;
        }

        private static float _nextLogTime;
    }
}