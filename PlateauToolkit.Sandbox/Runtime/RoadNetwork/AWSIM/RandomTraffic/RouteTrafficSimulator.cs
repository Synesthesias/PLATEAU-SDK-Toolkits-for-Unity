using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Random = UnityEngine.Random;

namespace AWSIM.TrafficSimulation
{
    [Serializable]
    public struct RouteTrafficSimulatorConfiguration {
        /// <summary>
        /// Available NPC prefabs
        /// </summary>
        [Tooltip("NPCs to be spawned.")]
        public GameObject[] npcPrefabs;

        [Tooltip("Route to follow. The first element also acts as a spawn lane.")]
        public TrafficLane[] route;

        [Tooltip("Lanes that vehicles can spawn from. If null, uses the entire route for spawning.")]
        public TrafficLane[] spawnableLanes;

        [Tooltip("Describes the lifetime of a traffic simulator instance by specifying how many vehicles this traffic simulator will spawn. Setting it makes the spawner live longer or shorter, while it can also be set to infinity if needed (endless lifetime).")]
        public int maximumSpawns;

        [Tooltip("Is this traffic simulation enabled.")]
        public bool enabled;

        [Tooltip("Time interval between vehicle spawns in seconds.")]
        public float spawnIntervalTime;
    }
    /// <summary>
    /// Simulate NPC traffic. (Currently only Vehicle is supported)
    /// - Use provided route
    /// - Continue driving randomly when the route ends
    /// - Traffic light control based on the Vienna Convention
    /// </summary>
    public class RouteTrafficSimulator : ITrafficSimulator
    {
        public bool enabled = true;

        private TrafficLane[] route;
        private TrafficLane[] spawnableLanes;
        private int maximumSpawns = 0;
        private float spawnIntervalTime = 0f;
        private float lastSpawnTime = 0f;
        private NPCVehicleSimulator npcVehicleSimulator;
        private NPCVehicleSpawner npcVehicleSpawner;
        private int currentSpawnNumber = 0;
        private int spawnPriority = 0;
        private GameObject nextPrefabToSpawn = null;

        public void IncreasePriority(int priority)
        {
            spawnPriority += priority;
        }

        public void ResetPriority()
        {
            spawnPriority = 0;
        }

        public int GetCurrentPriority()
        {
            return spawnPriority;
        }

        public bool IsEnabled()
        {
            // TODO additional interface to disable route traffic
            return enabled && !IsMaximumSpawnsNumberReached();
        }

        public RouteTrafficSimulator(GameObject parent,
            GameObject[] npcPrefabs,
            TrafficLane[] npcRoute,
            NPCVehicleSimulator vehicleSimulator,
            int maxSpawns = 0,
            float spawnInterval = 0f,
            TrafficLane[] spawnableLanes = null)
        {
            route = npcRoute;
            this.spawnableLanes = spawnableLanes;
            maximumSpawns = maxSpawns;
            spawnIntervalTime = spawnInterval;
            npcVehicleSimulator = vehicleSimulator;
            // spawnableLanesがnullの場合はroute全体を使用（後方互換性）
            TrafficLane[] spawnableLane = spawnableLanes ?? npcRoute;
            npcVehicleSpawner = new NPCVehicleSpawner(parent, npcPrefabs, spawnableLane);
        }

        public void GetRandomSpawnInfo(out NPCVehicleSpawnPoint spawnPoint, out GameObject prefab)
        {
            // NPC prefab is randomly chosen and is fixed until it is spawned. This is due to avoid prefab "bound" race conditions
            // when smaller cars will always be chosen over larger ones while the spawning process checks if the given prefab can be
            // put in the given position.
            if (nextPrefabToSpawn == null)
            {
                nextPrefabToSpawn = npcVehicleSpawner.GetRandomPrefab();
            }
            prefab = nextPrefabToSpawn;

            // Spawn position is always chosen randomly from the set of all available lanes.
            // This avoids waiting for lanes which are already occupied, but there is another one available somewhere else.
            spawnPoint = npcVehicleSpawner.GetRandomSpawnPoint();
        }

        public bool Spawn(GameObject prefab, NPCVehicleSpawnPoint spawnPoint, out NPCVehicle spawnedVehicle)
        {
            if(IsMaximumSpawnsNumberReached()) {
                spawnedVehicle = null;
                return false;
            };

            if (npcVehicleSimulator.VehicleStates.Count >= npcVehicleSimulator.maxVehicleCount)
            {
                spawnedVehicle = null;
                return false;
            }

            // スポーン間隔のチェック（spawnIntervalTimeが0より大きい場合のみ）
            if (spawnIntervalTime > 0f && Time.time - lastSpawnTime < spawnIntervalTime)
            {
                spawnedVehicle = null;
                return false;
            }

            var vehicle = npcVehicleSpawner.Spawn(prefab, SpawnIdGenerator.Generate(), spawnPoint);
            // spawnableLanesが設定されている場合は、スポーン車線のみをルートとして設定
            // （その車線終了後は自動的にNextLanesからランダム選択される）
            List<TrafficLane> vehicleRoute;
            if (spawnableLanes != null)
            {
                // スポーンした車線のみをルートとして設定
                vehicleRoute = new List<TrafficLane> { spawnPoint.Lane };
            }
            else
            {
                // 従来通りrouteを使用
                vehicleRoute = route.ToList<TrafficLane>();
            }
            npcVehicleSimulator.Register(vehicle, vehicleRoute, spawnPoint.WaypointIndex);
            nextPrefabToSpawn = null;
            lastSpawnTime = Time.time;

            if(maximumSpawns > 0)
                currentSpawnNumber++;

            spawnedVehicle = vehicle;
            return true;
        }

        private bool IsMaximumSpawnsNumberReached()
        {
            return (currentSpawnNumber == maximumSpawns && maximumSpawns > 0);
        }
    }
}
