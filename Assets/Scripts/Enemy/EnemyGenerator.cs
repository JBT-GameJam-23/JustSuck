using System;
using System.Collections.Generic;
using Hmxs.Toolkit.Base.Singleton;
using Hmxs.Toolkit.Flow.FungusTools;
using Hmxs.Toolkit.Flow.Timer;
using Sirenix.OdinInspector;
using Sucker;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

namespace Enemy
{
    public class EnemyGenerator : SingletonMono<EnemyGenerator>
    {
        [Title("Enemy Time Line Data")] public List<EnemyGenerateData> enemyWeaveData = new();
        [Title("Enemy Type")] public SerializedDictionary<EnemyType, GameObject> enemies = new();
        public Transform enemyRoot;
        public int attackWeaveIndex;
        [Title("Info")]
        [ReadOnly] public List<Timer> timers;
        [ReadOnly] public int weaveIndex;
        [ReadOnly] public bool weaveRegisterFinished;
        [ReadOnly] public bool allEnemyDied;

        private bool _attackTutorial = true;

        public void Generate(int weaveDataIndex)
        {
            if (weaveDataIndex == enemyWeaveData.Count)
            {
                Debug.Log("Finish");
                FlowchartManager.ExecuteBlock("Finish");
                SuckerManager.Instance.Finish();
                return;
            }

            Debug.Log("Weave" + weaveDataIndex);

            if (weaveDataIndex == attackWeaveIndex && _attackTutorial)
            {
                _attackTutorial = false;
                FlowchartManager.ExecuteBlock("FirstAttackWeave");
            }

            ClearTimers();
            foreach (Transform child in enemyRoot.transform)
                Destroy(child.gameObject);
            weaveRegisterFinished = false;

            var data = enemyWeaveData[weaveDataIndex];
            for (int i = 0; i < data.enemyTimeline.Count; i++)
            {
                var enemy = data.enemyTimeline[i];
                var index = i;
                var timer = Timer.Register(
                    duration: enemy.time,
                    onComplete: () =>
                    {
                        var randAng = Random.Range(0f,2 * Mathf.PI);
                        var pos = new Vector2(Mathf.Cos(randAng), Mathf.Sin(randAng)) * enemy.distance;
                        Instantiate(enemies[enemy.type], pos, Quaternion.identity, enemyRoot);
                        if (index == data.enemyTimeline.Count - 1)
                            weaveRegisterFinished = true;
                    });
                timers.Add(timer);
            }
        }

        public void ClearTimers()
        {
            foreach (var timer in timers)
            {
                timer.Remove();
            }
            timers.Clear();
        }

        private void Start()
        {
            Debug.Log(enemyWeaveData.Count);
            Generate(weaveIndex);
        }

        private void Update()
        {
            Check();
        }

        private void Check()
        {
            allEnemyDied = enemyRoot.GetComponentsInChildren<Enemy>().Length < 1;

            if (weaveRegisterFinished && allEnemyDied && !SuckerManager.Instance.hasDied)
            {
                weaveRegisterFinished = false;
                FlowchartManager.ExecuteBlock("SuckComplete");
                Timer.Register(5f, NextWeave);
            }
        }

        private void NextWeave()
        {
            Debug.Log("NextWeave");
            weaveIndex++;
            Generate(weaveIndex);
        }
    }
}