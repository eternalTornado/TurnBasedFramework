using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TurnBasedFramework.TurnManagement
{
    /// <summary>
    /// Quản lý thứ tự lượt chơi của các unit trong combat
    /// Hỗ trợ cả queue-based và initiative-based turn order
    /// </summary>
    public class TurnQueueManager : MonoBehaviour
    {
        #region Events
        public event Action<TurnQueueEntry> OnTurnStarted;
        public event Action<TurnQueueEntry> OnTurnEnded;
        public event Action<List<TurnQueueEntry>> OnQueueUpdated;
        public event Action OnRoundCompleted;
        #endregion

        #region Configuration
        [Header("Turn Order Settings")]
        [SerializeField] private TurnOrderMode turnOrderMode = TurnOrderMode.Initiative;
        [SerializeField] private bool autoStartNextTurn = true;
        [SerializeField] private float turnDelaySeconds = 0.5f;
        #endregion

        #region Private Fields
        private Queue<TurnQueueEntry> turnQueue = new Queue<TurnQueueEntry>();
        private List<TurnQueueEntry> allEntries = new List<TurnQueueEntry>();
        private TurnQueueEntry currentTurn;
        private int currentRound = 0;
        private bool isProcessing = false;
        #endregion

        #region Properties
        public TurnQueueEntry CurrentTurn => currentTurn;
        public int CurrentRound => currentRound;
        public bool IsProcessing => isProcessing;
        public IReadOnlyList<TurnQueueEntry> TurnOrder => allEntries.AsReadOnly();
        #endregion

        #region Initialization
        /// <summary>
        /// Khởi tạo turn queue với danh sách units
        /// </summary>
        public void Initialize(List<ITurnBasedUnit> units)
        {
            allEntries.Clear();
            turnQueue.Clear();
            currentRound = 0;

            foreach (var unit in units)
            {
                if (unit != null && unit.IsAlive)
                {
                    var entry = new TurnQueueEntry(unit);
                    allEntries.Add(entry);
                }
            }

            SortAndBuildQueue();
            OnQueueUpdated?.Invoke(allEntries);
        }

        /// <summary>
        /// Thêm unit vào queue (cho unit spawn giữa combat)
        /// </summary>
        public void AddUnit(ITurnBasedUnit unit)
        {
            if (unit == null || !unit.IsAlive)
            {
                Debug.LogWarning("Cannot add null or dead unit to turn queue");
                return;
            }

            var entry = new TurnQueueEntry(unit);
            allEntries.Add(entry);
            
            // Insert vào đúng vị trí dựa trên initiative
            InsertIntoQueue(entry);
            OnQueueUpdated?.Invoke(allEntries);
        }

        /// <summary>
        /// Xóa unit khỏi queue (khi unit chết hoặc flee)
        /// </summary>
        public void RemoveUnit(ITurnBasedUnit unit)
        {
            var entry = allEntries.FirstOrDefault(e => e.Unit == unit);
            if (entry != null)
            {
                allEntries.Remove(entry);
                RebuildQueue();
                OnQueueUpdated?.Invoke(allEntries);
            }
        }
        #endregion

        #region Turn Flow Control
        /// <summary>
        /// Bắt đầu combat - khởi động turn đầu tiên
        /// </summary>
        public void StartCombat()
        {
            currentRound = 1;
            Debug.Log($"=== Combat Started - Round {currentRound} ===");
            
            if (turnQueue.Count > 0)
            {
                StartNextTurn();
            }
            else
            {
                Debug.LogError("Cannot start combat - turn queue is empty!");
            }
        }

        /// <summary>
        /// Bắt đầu lượt tiếp theo
        /// </summary>
        public void StartNextTurn()
        {
            if (isProcessing)
            {
                Debug.LogWarning("Cannot start next turn - still processing current turn");
                return;
            }

            // Kết thúc turn hiện tại nếu có
            if (currentTurn != null)
            {
                EndCurrentTurn();
            }

            // Kiểm tra queue còn turns không
            if (turnQueue.Count == 0)
            {
                OnRoundCompleted?.Invoke();
                StartNewRound();
                return;
            }

            // Lấy turn tiếp theo
            currentTurn = turnQueue.Dequeue();
            
            // Skip nếu unit đã chết
            while (currentTurn != null && !currentTurn.Unit.IsAlive)
            {
                if (turnQueue.Count == 0)
                {
                    OnRoundCompleted?.Invoke();
                    StartNewRound();
                    return;
                }
                currentTurn = turnQueue.Dequeue();
            }

            if (currentTurn != null && currentTurn.Unit.IsAlive)
            {
                currentTurn.StartTurn();
                isProcessing = true;
                
                Debug.Log($"[Round {currentRound}] {currentTurn.Unit.UnitName}'s turn started");
                OnTurnStarted?.Invoke(currentTurn);

                // Auto start nếu được config
                if (autoStartNextTurn && !currentTurn.Unit.IsPlayerControlled)
                {
                    StartCoroutine(AutoEndTurnAfterDelay());
                }
            }
        }

        /// <summary>
        /// Kết thúc lượt hiện tại
        /// </summary>
        public void EndCurrentTurn()
        {
            if (currentTurn == null) return;

            currentTurn.EndTurn();
            Debug.Log($"[Round {currentRound}] {currentTurn.Unit.UnitName}'s turn ended");
            
            OnTurnEnded?.Invoke(currentTurn);
            isProcessing = false;
        }

        /// <summary>
        /// Force kết thúc lượt (dùng cho player skip turn)
        /// </summary>
        public void ForceEndTurn()
        {
            if (currentTurn != null && isProcessing)
            {
                EndCurrentTurn();
                
                if (autoStartNextTurn)
                {
                    StartNextTurn();
                }
            }
        }

        /// <summary>
        /// Bắt đầu round mới
        /// </summary>
        private void StartNewRound()
        {
            currentRound++;
            Debug.Log($"=== Round {currentRound} Started ===");
            
            RebuildQueue();
            
            if (turnQueue.Count > 0)
            {
                StartNextTurn();
            }
            else
            {
                Debug.LogError("No units available for new round!");
            }
        }
        #endregion

        #region Queue Management
        /// <summary>
        /// Sắp xếp và build queue theo turn order mode
        /// </summary>
        private void SortAndBuildQueue()
        {
            switch (turnOrderMode)
            {
                case TurnOrderMode.Initiative:
                    // Sắp xếp theo initiative (cao -> thấp), sau đó theo speed
                    allEntries = allEntries
                        .OrderByDescending(e => e.Initiative)
                        .ThenByDescending(e => e.Unit.Speed)
                        .ToList();
                    break;

                case TurnOrderMode.Speed:
                    // Sắp xếp theo speed (cao -> thấp)
                    allEntries = allEntries
                        .OrderByDescending(e => e.Unit.Speed)
                        .ToList();
                    break;

                case TurnOrderMode.Manual:
                    // Giữ nguyên thứ tự được add vào
                    break;
            }

            RebuildQueue();
        }

        /// <summary>
        /// Rebuild queue từ allEntries
        /// </summary>
        private void RebuildQueue()
        {
            turnQueue.Clear();
            
            foreach (var entry in allEntries)
            {
                if (entry.Unit.IsAlive)
                {
                    turnQueue.Enqueue(entry);
                }
            }
        }

        /// <summary>
        /// Insert entry vào queue theo initiative
        /// </summary>
        private void InsertIntoQueue(TurnQueueEntry newEntry)
        {
            if (turnOrderMode == TurnOrderMode.Initiative)
            {
                // Tìm vị trí chèn dựa trên initiative
                int insertIndex = allEntries.FindIndex(e => 
                    e.Initiative < newEntry.Initiative || 
                    (e.Initiative == newEntry.Initiative && e.Unit.Speed < newEntry.Unit.Speed)
                );

                if (insertIndex >= 0)
                {
                    allEntries.Insert(insertIndex, newEntry);
                }
            }
            
            RebuildQueue();
        }
        #endregion

        #region Utility Methods
        /// <summary>
        /// Kiểm tra có phải lượt của unit này không
        /// </summary>
        public bool IsUnitTurn(ITurnBasedUnit unit)
        {
            return currentTurn != null && currentTurn.Unit == unit;
        }

        /// <summary>
        /// Lấy unit sẽ đến lượt tiếp theo
        /// </summary>
        public ITurnBasedUnit PeekNextUnit()
        {
            return turnQueue.Count > 0 ? turnQueue.Peek().Unit : null;
        }

        /// <summary>
        /// Lấy preview của N turns tiếp theo
        /// </summary>
        public List<ITurnBasedUnit> GetUpcomingTurns(int count)
        {
            return turnQueue.Take(count).Select(e => e.Unit).ToList();
        }

        /// <summary>
        /// Reset toàn bộ turn system
        /// </summary>
        public void Reset()
        {
            turnQueue.Clear();
            allEntries.Clear();
            currentTurn = null;
            currentRound = 0;
            isProcessing = false;
        }
        #endregion

        #region Coroutines
        private System.Collections.IEnumerator AutoEndTurnAfterDelay()
        {
            yield return new UnityEngine.WaitForSeconds(turnDelaySeconds);
            
            if (isProcessing)
            {
                EndCurrentTurn();
                StartNextTurn();
            }
        }
        #endregion
    }

    #region Supporting Classes
    /// <summary>
    /// Entry trong turn queue
    /// </summary>
    [System.Serializable]
    public class TurnQueueEntry
    {
        public ITurnBasedUnit Unit { get; private set; }
        public float Initiative { get; private set; }
        public float TurnStartTime { get; private set; }
        public float TurnEndTime { get; private set; }
        public int ActionsPerformed { get; private set; }

        public TurnQueueEntry(ITurnBasedUnit unit)
        {
            Unit = unit;
            Initiative = CalculateInitiative(unit);
            ActionsPerformed = 0;
        }

        private float CalculateInitiative(ITurnBasedUnit unit)
        {
            // Initiative = Speed + random factor
            return unit.Speed + UnityEngine.Random.Range(0f, 10f);
        }

        public void StartTurn()
        {
            TurnStartTime = Time.time;
            ActionsPerformed = 0;
        }

        public void EndTurn()
        {
            TurnEndTime = Time.time;
        }

        public void IncrementActions()
        {
            ActionsPerformed++;
        }
    }

    /// <summary>
    /// Chế độ sắp xếp turn order
    /// </summary>
    public enum TurnOrderMode
    {
        Initiative,  // Dựa trên initiative roll
        Speed,       // Dựa trên speed stat
        Manual       // Thứ tự thủ công
    }
    #endregion
}