using System;
using Enemy;
using Hmxs.Toolkit.Base.Singleton;
using Hmxs.Toolkit.Flow.FungusTools;
using Hmxs.Toolkit.Flow.Timer;
using Hmxs.Toolkit.Module.Events;
using MoreMountains.Feedbacks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Sucker
{
    public enum FailedType
    {
        BeAttack,
        Lost
    }

    public class SuckerManager : SingletonMono<SuckerManager>
    {
        public MMF_Player getBallEffect;
        public MMF_Player peakEffect;
        public MMF_Player defendEffect;
        
        public float maxSuckTimeSet;
        public float respirePunishmentTimeSet;

        public GameObject dieParticle;
        public GameObject enemyRoot;
        
        [ReadOnly] public float suckTimer;
        [ReadOnly] public bool isSucking;
        [ReadOnly] public bool hasDied = false;

        private SpriteRenderer _spriteRenderer;
        private Collider2D _collider;
        [ShowInInspector] [ReadOnly] private bool _canPush = false;
        private bool _canRespire = true;
        private bool _canRespawn = false;

        private void OnEnable()
        {
            Events.AddListener<FailedType>(EventGroups.General.GameOver, Die);
        }

        private void OnDisable()
        {
            Events.RemoveListener<FailedType>(EventGroups.General.GameOver, Die);
        }

        private void Start()
        {
            _canPush = false;
            suckTimer = maxSuckTimeSet;
            _collider = GetComponent<Collider2D>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            getBallEffect.Initialization();
            peakEffect.Initialization();
            defendEffect.Initialization();
        }

        private void Update()
        {
            if (_canRespawn)
            {
                if (InputHandler.Instance.IsSucking)
                {
                    _canRespawn = false;
                    Respawn();
                }
                return;
            }
            if (!_canRespire) return;
            if (InputHandler.Instance.IsSucking)
            {
                // 按着空格
                if (suckTimer >= 0)
                {
                    // 正常吸
                    _canPush = true;
                    isSucking = true;
                    suckTimer -= Time.deltaTime;
                }
                else
                {
                    // 吸过头了
                    Debug.Log("SuckToMuch");
                    _canPush = false;
                    isSucking = false;
                    RespirePunishment();
                    Events.Trigger(EventGroups.Sucker.Push);
                    peakEffect.PlayFeedbacks();
                }
            }
            else
            {
                // 没按着空格时
                isSucking = false;
                if (suckTimer <= maxSuckTimeSet)
                    // 回复吸气条
                    suckTimer += Time.deltaTime;
                if (_canPush)
                {
                    //Debug.Log("SuckPush");
                    _canPush = false;
                    Events.Trigger(EventGroups.Sucker.Push);
                }
                _canPush = false;
            }
        }

        private void RespirePunishment()
        {
            _canRespire = false;
            this.AttachTimer(respirePunishmentTimeSet, () => _canRespire = true);
        }

        private void Die(FailedType type)
        {
            Timer.Register(2f, () => _canRespawn = true, useRealTime: true);
            hasDied = true;
            EnemyGenerator.Instance.weaveRegisterFinished = false;
            _spriteRenderer.enabled = false;
            _collider.enabled = false;
            Instantiate(dieParticle, transform.position, Quaternion.identity);
            Time.timeScale = 0.1f;
            switch (type)
            {
                case FailedType.BeAttack:
                    FlowchartManager.ExecuteBlock("Die");
                    break;
                case FailedType.Lost:
                    FlowchartManager.ExecuteBlock("Lost");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public void Finish()
        {
            _spriteRenderer.enabled = false;
            _collider.enabled = false;
            foreach (Transform child in enemyRoot.transform) 
                Destroy(child.gameObject);
        }
        
        private void OnTriggerStay2D(Collider2D other)
        {
            if (other.CompareTag("Bullet") && !isSucking && !hasDied)
            {
                Events.Trigger(EventGroups.General.GameOver, FailedType.BeAttack);
            }

            if (other.CompareTag("Bullet") && isSucking && !hasDied)
            {
                defendEffect.PlayFeedbacks();
            }
        }

        private void Respawn()
        {
            Time.timeScale = 1;
            foreach (Transform child in enemyRoot.transform) 
                Destroy(child.gameObject);
            FlowchartManager.ExecuteBlock("Respawn");

            _spriteRenderer.enabled = true;
            var color = _spriteRenderer.color;
            color.a = 0;
            _spriteRenderer.color = color;
            Timer.Register(1f,
                onComplete: () =>
                {
                    hasDied = false;
                    _collider.enabled = true;
                    _canRespire = true;
                    suckTimer = maxSuckTimeSet;
                    Debug.Log("Restart");
                    EnemyGenerator.Instance.Generate(EnemyGenerator.Instance.weaveIndex);
                },
                onUpdate: t =>
                {
                    color.a = t;
                    _spriteRenderer.color = color;
                });
        }
    }
}