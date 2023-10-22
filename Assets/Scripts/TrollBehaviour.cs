using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NPBehave;
using System.Linq;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using static UnityEngine.GraphicsBuffer;
using System.IO;

namespace UnityMovementAI
{
    public class TrollBehaviour : MonoBehaviour
    {
        private const int TROLL_IDLE = 0;
        private const int TROLL_WANDER = 1;
        private const int TROLL_ATTACK = 2;
        private int currentAction;
        private List<int> utilityScores;

        public float thiefMinDist = 3.0f;
        public float chiefMinDist = 30.0f;

        public Transform thief;
        public Transform trollChief;


        private Blackboard blackboard;
        private Root behaviorTree;

        SteeringBasics steeringBasics;
        MovementAIRigidbody rb;
        Wander2 wander;
        WallAvoidanceMod wallAvoidance;

        void Start()
        {
            steeringBasics = GetComponent<SteeringBasics>();
            wander = GetComponent<Wander2>();
            wallAvoidance = GetComponent<WallAvoidanceMod>();


            rb = GetComponent<MovementAIRigidbody>();

            // Set initial action
            currentAction = TROLL_IDLE;
            SwitchTree(SelectBehaviourTree(currentAction));

            // Set utility scores to zero
            utilityScores = new List<int>();
            utilityScores.Add(0); // Idle
            utilityScores.Add(0); // Wander
            utilityScores.Add(0); // Attack

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
            utilityScores[TROLL_IDLE] = 10;
            utilityScores[TROLL_WANDER] = 5;
            utilityScores[TROLL_ATTACK] = 5;

            thief = GameObject.FindGameObjectWithTag("Thief").transform;
            trollChief = GameObject.FindGameObjectWithTag("TrollChief").transform;
            float thiefDist, trollChiefDist;

            if (thief != null)
            {
                thiefDist = Vector3.Distance(thief.position,transform.position);
            }
            else thiefDist = 50.0f;
            if (trollChief != null)
            {
                trollChiefDist = Vector3.Distance(trollChief.position, transform.position);
            }
            else trollChiefDist = 50.0f;

            if(trollChiefDist <= chiefMinDist)
            {
                utilityScores[TROLL_WANDER] = 15;
            }
            if(thiefDist <= thiefMinDist)
            {
                utilityScores[TROLL_ATTACK] = 20;
            }
        }
        private void SwitchTree(Root t)
        {
            if (behaviorTree != null) behaviorTree.Stop();

            behaviorTree = t;
            blackboard = behaviorTree.Blackboard;

            behaviorTree.Start();
        }
        private Root SelectBehaviourTree(int action)
        {
            switch (action)
            {
                case TROLL_IDLE:
                    return LazyBehaviour();

                case TROLL_WANDER:
                    return WanderBehaviour();

                case TROLL_ATTACK:
                    return AttackBehaviour();

                default:
                    return new Root(LazyBehaviour());
            }
        }
        private Root WanderBehaviour()
        {
            return new Root(
                new Action(()=>Wander())
                );
        }
        private Root LazyBehaviour()
        {
            return new Root(
                new Action(() => Lazy())
                );
        }
        private void Lazy()
        {
            Vector3 accel = rb.Velocity * -1;

            steeringBasics.Steer(accel);
            steeringBasics.LookWhereYoureGoing();
        }
        private Root AttackBehaviour()
        {
            return new Root(
                new Action(() => Attack())
                );
        }
        private void Attack()
        {
            Vector3 accel = steeringBasics.Seek(thief.position);

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
