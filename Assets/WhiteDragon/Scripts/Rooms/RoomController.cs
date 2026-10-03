using System.Collections.Generic;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>
    /// Box trigger room. First player entry seals the doors and wakes the enemies; when every enemy
    /// is dead the doors open, RoomCleared fires, and the optional reward pedestal appears.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class RoomController : MonoBehaviour
    {
        [Tooltip("Empty uses the GameObject name.")]
        public string roomId = "";
        public List<Enemy> enemies = new List<Enemy>();
        [Tooltip("Door blockers: their colliders and renderers are on while the room is sealed.")]
        public List<GameObject> doors = new List<GameObject>();
        public ItemPedestal rewardPedestal;

        readonly RoomProgress progress = new RoomProgress();
        bool initialized;

        public RoomProgress.Phase Phase => progress.Current;
        public string Id => string.IsNullOrEmpty(roomId) ? name : roomId;

        void Awake() => Initialize();

        public void Initialize()
        {
            if (initialized) return;
            initialized = true;
            GetComponent<BoxCollider>().isTrigger = true;
            SetDoorsClosed(false);
            // Stable per-enemy random key: room id + index in this list (reordering the list changes the keys).
            for (int i = 0; i < enemies.Count; i++)
                if (enemies[i] != null) enemies[i].SetSpawnKey(Id + "#" + i);
            foreach (var e in enemies)
                if (e != null) e.SetDormant(true);
            if (rewardPedestal != null) rewardPedestal.gameObject.SetActive(false);
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerController>() != null) Enter();
        }

        public void Enter()
        {
            Initialize();
            if (!progress.Enter()) return;
            SetDoorsClosed(true);
            foreach (var e in enemies)
                if (e != null) e.SetDormant(false);
            RunSession.NotifyRoomEntered(Id);
            CheckCleared();
        }

        void Update() => CheckCleared();

        public void CheckCleared()
        {
            if (!progress.Update(AliveCount())) return;
            SetDoorsClosed(false);
            if (rewardPedestal != null) rewardPedestal.gameObject.SetActive(true);
            RunSession.NotifyRoomCleared(Id);
        }

        public int AliveCount()
        {
            int alive = 0;
            foreach (var e in enemies)
                if (e != null && !e.IsDead) alive++;
            return alive;
        }

        public void SetDoorsClosed(bool closed)
        {
            foreach (var door in doors)
            {
                if (door == null) continue;
                foreach (var c in door.GetComponentsInChildren<Collider>(true)) c.enabled = closed;
                foreach (var r in door.GetComponentsInChildren<Renderer>(true)) r.enabled = closed;
            }
        }
    }
}
