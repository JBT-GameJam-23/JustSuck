using Fungus;
using Hmxs.Toolkit.Flow.FungusTools;
using Hmxs.Toolkit.Module.Events;
using MoreMountains.Feedbacks;
using Sucker;
using UnityEngine;
using Collision2D = UnityEngine.Collision2D;

namespace Enemy
{
    [RequireComponent(typeof(Rigidbody2D))]
    public abstract class Enemy : MonoBehaviour
    {
        public MMF_Player bounceEffect;
        public MMF_Player spawnEffect;
        public MMF_Player suckedEffect;
        public GameObject particle;
        
        public float collisionForce;
        
        protected Rigidbody2D Rb;
        protected Transform Sucker;
        protected Vector2 Direction;
        
        private bool _isBeingSuckedIn;

        private void OnEnable()
        {
            Events.AddListener(EventGroups.Sucker.Push, BePushed);
        }

        private void OnDisable()
        {
            Events.RemoveListener(EventGroups.Sucker.Push, BePushed);
        }

        protected virtual void Start()
        {
            Rb = GetComponent<Rigidbody2D>();
            Sucker = GameObject.FindWithTag("Player").transform;
            bounceEffect.Initialization();
            spawnEffect.Initialization();
            suckedEffect.Initialization();
            
            spawnEffect.PlayFeedbacks();
            Instantiate(particle, transform.position, Quaternion.identity, transform);
        }

        private void OnDestroy() => Destroy(particle);

        private bool _effectTriggered;
        
        protected virtual void FixedUpdate()
        {
            Direction = ((Vector2)(transform.position - Sucker.position)).normalized;
            _isBeingSuckedIn = SuckerManager.Instance.isSucking;
            if (_isBeingSuckedIn) 
                BeingSuckedIn();
            else
                NotBeingSuckedIn();

            if (_isBeingSuckedIn && !_effectTriggered)
            {
                suckedEffect.PlayFeedbacks();
                _effectTriggered = true;
            }

            if (!_isBeingSuckedIn && _effectTriggered)
            {
                suckedEffect.PlayFeedbacks();
                _effectTriggered = false;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player")) 
                Died();
        }

        private void OnCollisionEnter2D(Collision2D other)
        {
            if (other.gameObject.CompareTag("Enemy"))
            {
                var direction = ((Vector2)(other.transform.position - transform.position)).normalized;
                Rb.AddForce(collisionForce * direction, ForceMode2D.Impulse);
                bounceEffect.PlayFeedbacks();
            }
        }

        // 玩家没吸时
        protected abstract void NotBeingSuckedIn();
        
        // 玩家正在吸时
        protected abstract void BeingSuckedIn();

        // 被推出
        protected abstract void BePushed();
        
        // 被吸入了
        protected virtual void Died()
        {
            SuckerManager.Instance.getBallEffect.PlayFeedbacks();
            Destroy(gameObject);
        }
        
        // 超出边界了
        public virtual void Lost()
        {
            var lookPoint = GameObject.Find("LookPoint").transform;
            lookPoint.position = transform.position * 0.4f;
            Events.Trigger(EventGroups.General.GameOver, FailedType.Lost);
        }
    }
}