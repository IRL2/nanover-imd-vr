using System;
using System.Collections;
using System.Collections.Generic;
using Nanover.Core.Math;
using Nanover.Frame;
using Nanover.Frontend.Input;
using Nanover.Frontend.Manipulation;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using Random = UnityEngine.Random;

namespace NanoverImd.PathFollower
{
    public class PathFollower : MonoBehaviour
    {
#pragma warning disable 0649
        [SerializeField]
        private NanoverImdSimulation simulation;

        [SerializeField]
        private LineRenderer debugLine;
#pragma warning restore 0649
        private List<Vector3> path = new List<Vector3>();

        private Transform targetSphere;

        private float targetDistance = 0;
        private DirectPosedObject targetPose = new DirectPosedObject();
        private AttemptableManipulator targetManipulator;

        private void Start()
        {
            var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
            visual.gameObject.name = "Path Follower Target";
            visual.localScale = Vector3.one * 0.01f;
            visual.SetParent(transform);

            targetSphere = visual;

            targetPose.PoseChanged += () => {
                if (targetPose.Pose is { } pose)
                {
                    targetSphere.gameObject.SetActive(true);
                    targetSphere.position = pose.Position;
                }
                else
                {
                    targetSphere.gameObject.SetActive(false);
                }
            };
        }

        private Vector3 SimPointToWorldPoint(Vector3 sim)
        {
            return simulation.interactableScene.transform.TransformPoint(sim);
        }

        private void OnEnable()
        {
            targetManipulator = new AttemptableManipulator(targetPose, simulation.ManipulableParticles.StartParticleGrab);

            var frame = simulation.FrameSynchronizer.CurrentFrame;

            RandomisePath(frame.ParticlePositions[0]);
            targetDistance = 0;

            Update();

            targetManipulator.AttemptManipulation();
        }

        private void OnDisable()
        {
            targetManipulator.EndActiveManipulation();
        }

        private void Update()
        {
            targetDistance += Time.deltaTime * 0.1f;
            var remaining = targetDistance;

            for (int i = 1; i < path.Count; ++i)
            {
                var delta = path[i] - path[i - 1];
                var length = delta.magnitude;
                var direction = delta.normalized;

                if (remaining < length)
                {
                    var local = path[i - 1] + direction * remaining;
                    var world = SimPointToWorldPoint(local);

                    targetPose.SetPose(new Transformation(world, Quaternion.identity, Vector3.one));
                    break;
                }
                else
                {
                    remaining -= length;
                }
            }
        }

        public void RandomisePath(Vector3 origin)
        {
            path.Clear();
            path.Add(origin);

            for (int i = 1; i < 50; ++i)
            {
                path.Add(path[^1] + Random.insideUnitSphere);
            }

            debugLine.positionCount = path.Count;
            debugLine.SetPositions(path.ToArray());
        }
    }
}