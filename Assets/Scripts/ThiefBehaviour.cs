using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NPBehave;
using static UnityEngine.GraphicsBuffer;
using System.IO;

namespace UnityMovementAI
{
    public class ThiefBehaviour : MonoBehaviour
    {

        private Blackboard blackboard;
        private Root behaviorTree;

        SteeringBasics steeringBasics;
        WallAvoidanceMod wallAvoidance;

        Evade evade;
        MovementAIRigidbody ClosestTroll;
        Transform closestTorch;
        GameObject gem, goal;
        Vector3 destination;

        private float distancePerception = 3.0f;

        void Start()
        {
            steeringBasics = GetComponent<SteeringBasics>();
            wallAvoidance = GetComponent<WallAvoidanceMod>();
            evade = GetComponent<Evade>();

            behaviorTree = CreateBehaviourTree();
            blackboard = behaviorTree.Blackboard;

            behaviorTree.Start();

        }

        private Root CreateBehaviourTree()
        {
            return new Root(
                new Service(0.05f, PerceptionUpdate,
                    new Selector(
                        EvadeTrolls(),
                        TorchClose(),
                        new Action(() => goalBehaviour())
                        )
                    )
                );
        }

        private BlackboardCondition TorchClose()
        {
            return new BlackboardCondition("torchDist", Operator.IS_SMALLER, distancePerception, Stops.IMMEDIATE_RESTART,
                new Action(() => SeekTorchBehaviour())
                );
        }

        private BlackboardCondition EvadeTrolls()
        {
            return new BlackboardCondition("closestTrollDist", Operator.IS_SMALLER, distancePerception, Stops.IMMEDIATE_RESTART,
                new Action(() => EvadeBehaviour())
                );
        }
        private void PerceptionUpdate()
        {
            GameObject[] trolls = GameObject.FindGameObjectsWithTag("Troll");
            float minTrollDist = 99.9f;
            foreach (GameObject troll in trolls)
            {
                float trollDist = Vector3.Distance(troll.transform.position, transform.position);
                if (trollDist < minTrollDist)
                {
                    minTrollDist = trollDist;
                    ClosestTroll = troll.GetComponent<MovementAIRigidbody>();
                }
            }
            GameObject trollChief = GameObject.FindGameObjectWithTag("TrollChief");
            float trollChiefDist = Vector3.Distance(trollChief.transform.position, transform.position);
            if (trollChiefDist < minTrollDist)
            {
                minTrollDist = trollChiefDist;
                ClosestTroll = trollChief.GetComponent<MovementAIRigidbody>();
            }
            GameObject[] torches = GameObject.FindGameObjectsWithTag("Torch");
            float minTorchDist = 99.9f;
            if (torches.Length > 0)
            {
                foreach (GameObject torch in torches)
                {
                    float torchDist = Vector3.Distance(torch.transform.position, transform.position);
                    if (torchDist < minTorchDist)
                    {
                        minTorchDist = torchDist;
                        closestTorch = torch.GetComponent<Transform>();
                    }
                }
            }
            gem = GameObject.FindGameObjectWithTag("Gem");
            goal = GameObject.FindGameObjectWithTag("Goal");
            if (gem == null) { destination = goal.transform.position; }
            else destination = gem.transform.position;

            blackboard["torchDist"] = minTorchDist;
            blackboard["closestTrollDist"] = minTrollDist;
        }

        private void EvadeBehaviour()
        {
            Vector3 accel = wallAvoidance.GetSteering();

            if (accel.magnitude < 0.005f)
            {
                accel = evade.GetSteering(ClosestTroll);
            }

            steeringBasics.Steer(accel);
            steeringBasics.LookWhereYoureGoing();
        }
        private void goalBehaviour()
        {
            Vector3 accel = wallAvoidance.GetSteering();

            if (accel.magnitude < 0.005f)
            {
                accel = steeringBasics.Seek(destination);
            }

            steeringBasics.Steer(accel);
            steeringBasics.LookWhereYoureGoing();
        }
        private void SeekTorchBehaviour()
        {
            Vector3 accel = wallAvoidance.GetSteering();

            if (accel.magnitude < 0.005f)
            {
                accel = steeringBasics.Seek(closestTorch.position);
            }

            steeringBasics.Steer(accel);
            steeringBasics.LookWhereYoureGoing();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            Debug.Log(collision.collider.tag);
            switch(collision.collider.tag)
            {
                case "Gem":
                    Destroy(collision.gameObject);
                    destination = goal.transform.position;
                    break;
                case "Torch":
                    Destroy(collision.gameObject);
                    increasePerception();
                    break;
                case "Goal":
                    GameWin();
                    break;
                case "Troll":
                    GameLost();
                    break;
                case "TrollChief":
                    GameLost();
                    break;
            }
        }

        private void GameLost()
        {
            Debug.Log("TrollsWin");
        }

        private void GameWin()
        {
            Debug.Log("ThiefWins");
        }

        private void increasePerception()
        {
            distancePerception = 5.0f; 
        }
    }
}
