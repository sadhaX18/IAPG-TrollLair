using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NPBehave;
using static UnityEngine.GraphicsBuffer;
using System.Linq;
using UnityEngine.UIElements;

namespace UnityMovementAI
{
    public class TrollChiefBehaviour : MonoBehaviour
    {
        private const int TROLLCHIEF_WANDER = 0;
        private const int TROLLCHIEF_ATTACK = 1;
        private const int TROLLCHIEF_ABILITY = 2;
        private int currentAction;
        private List<int> utilityScores;

        public float thiefMinDist = 10.0f;

        public Transform thief;

        private Root behaviorTree;

        SteeringBasics steeringBasics;
        Wander2 wander;
        WallAvoidanceMod wallAvoidance;

        bool abilityUsed = false;

        MovementAIRigidbody rb;

        void Start()
        {
            steeringBasics = GetComponent<SteeringBasics>();
            wander = GetComponent<Wander2>();
            wallAvoidance = GetComponent<WallAvoidanceMod>();

            rb = GetComponent<MovementAIRigidbody>();

            currentAction = TROLLCHIEF_WANDER;
            SwitchTree(SelectBehaviourTree(currentAction));

            // Set utility scores to zero
            utilityScores = new List<int>();
            utilityScores.Add(0); // Wander
            utilityScores.Add(0); // Attack
            utilityScores.Add(0); // Ability

        }
        private void Update()
        {
            UpdateScores();
            int maxValue = utilityScores.Max(t => t);
            int maxIndex = utilityScores.IndexOf(maxValue);

            if (currentAction != maxIndex)
            {
                currentAction = maxIndex;
                SwitchTree(SelectBehaviourTree(currentAction));
            }
        }
        private void UpdateScores()
        {
            utilityScores[TROLLCHIEF_WANDER] = 10;
            utilityScores[TROLLCHIEF_ATTACK] = 5;
            utilityScores[TROLLCHIEF_ABILITY] = 5;

            thief = GameObject.FindGameObjectWithTag("Thief").transform;
            float thiefDist;

            if (thief != null)
            {
                thiefDist = Vector3.Distance(thief.position, transform.position);
            }
            else thiefDist = 50.0f;
            if (thiefDist <= thiefMinDist)
            {
                utilityScores[TROLLCHIEF_ATTACK] = 15;
            }
            if(GameObject.FindGameObjectWithTag("Gem") == null && abilityUsed == false)
            {
                utilityScores[TROLLCHIEF_ABILITY] = 20;
            }
        }
        private void SwitchTree(Root t)
        {
            if (behaviorTree != null) behaviorTree.Stop();

            behaviorTree = t;

            behaviorTree.Start();
        }
        private Root SelectBehaviourTree(int action)
        {
            switch (action)
            {
                case TROLLCHIEF_WANDER:
                    return WanderBehaviour();

                case TROLLCHIEF_ATTACK:
                    return AttackBehaviour();

                case TROLLCHIEF_ABILITY:
                    return AbilityBehaviour();

                default:
                    return new Root(WanderBehaviour());
            }
        }

        private Root AttackBehaviour()
        {
            return new Root(
                new Action(() => Attack())
                );
        }

        private Root WanderBehaviour()
        {
            return new Root(
                new Action(() => Wander())
                );
        }
        private Root AbilityBehaviour()
        {
            return new Root(
                new Action(() => Ability())
                );
        }

        private void Ability()
        {
            GameObject[] trolls = GameObject.FindGameObjectsWithTag("Troll");
            GameObject ClosestTroll = trolls[0];
            float minTrollDist = 99.9f;
            foreach (GameObject troll in trolls)
            {
                float trollDist = Vector3.Distance(troll.transform.position, thief.position);
                if (trollDist < minTrollDist)
                {
                    minTrollDist = trollDist;
                    ClosestTroll = troll;
                }
            }
            Vector3 temp = transform.position;
            transform.position = ClosestTroll.transform.position;
            ClosestTroll.transform.position = temp;
            abilityUsed = true;
            thiefMinDist = 90.0f;
        }

        private void Attack()
        {
            Vector3 accel = wallAvoidance.GetSteering();

            if (accel.magnitude < 0.005f)
            {
                accel = steeringBasics.Seek(thief.position);
            }

            steeringBasics.Steer(accel);
            steeringBasics.LookWhereYoureGoing();
        }
        private void Wander()
        {
            Vector3 accel = wallAvoidance.GetSteering();

            if (accel.magnitude < 0.005f)
            {
                accel = wander.GetSteering();
            }

            steeringBasics.Steer(accel);
            steeringBasics.LookWhereYoureGoing();
        }
    }
}
