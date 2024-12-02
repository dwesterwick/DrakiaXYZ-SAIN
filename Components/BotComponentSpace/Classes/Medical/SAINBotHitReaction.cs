﻿using EFT;
using EFT.HealthSystem;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public class SAINBotHitReaction : BotMedicalBase, IBotClass
    {
        public EHitReaction HitReaction { get; private set; }
        public IHealthController HealthController => Player.HealthController;
        public BodyPartHitEffectClass BodyHitEffect { get; private set; }

        public AimHitEffectClass AimHitEffect { get; private set; }

        public SAINBotHitReaction(SAINBotMedicalClass medical) : base(medical)
        {
            BodyHitEffect = new BodyPartHitEffectClass(medical);
            AimHitEffect = new AimHitEffectClass(medical);

            addPart(EBodyPart.Head);
            addPart(EBodyPart.Chest);
            addPart(EBodyPart.LeftArm);
            addPart(EBodyPart.RightArm);
            addPart(EBodyPart.LeftLeg);
            addPart(EBodyPart.RightLeg);
            addPart(EBodyPart.Stomach);
        }

        private void addPart(EBodyPart part)
        {
            BodyParts.Add(part, new BodyPartStatus(part, this));
        }

        public void Init()
        {
            BodyHitEffect.Init();
            AimHitEffect.Init();
        }

        public void Update()
        {
            BodyHitEffect.Update();
            AimHitEffect.Update();
        }

        public EInjurySeverity LeftArmInjury { get; private set; }
        public EInjurySeverity RightArmInjury { get; private set; }

        public bool ArmsInjured => BodyHitEffect.LeftArmInjury != EInjurySeverity.None || BodyHitEffect.RightArmInjury != EInjurySeverity.None;

        public void Dispose()
        {
            BodyHitEffect.Dispose();
            BodyParts.Clear();
            AimHitEffect.Dispose();
        }

        public void GetHit(DamageInfoStruct DamageInfoStruct, EBodyPart bodyPart, float floatVal)
        {
            BodyHitEffect.GetHit(DamageInfoStruct, bodyPart, floatVal);
            AimHitEffect.GetHit(DamageInfoStruct);
        }

        private const float StunDamageThreshold = 50;
        private const float BaseStunTime = 3f;

        private float TimeStunHappened;
        private float StunTime;

        public bool IsStunned {
            get
            {
                if (_isStunned && StunTime < Time.time) {
                    _isStunned = false;
                }
                return _isStunned;
            }
            set
            {
                if (value) {
                    TimeStunHappened = Time.time;
                    StunTime = Time.time + BaseStunTime * UnityEngine.Random.Range(0.75f, 1.25f);
                }
                _isStunned = value;
            }
        }

        private bool _isStunned;

        private bool IsStunnedFromDamage(DamageInfoStruct DamageInfoStruct)
        {
            return false;
        }

        public Dictionary<EBodyPart, BodyPartStatus> BodyParts = new Dictionary<EBodyPart, BodyPartStatus>();
    }
}