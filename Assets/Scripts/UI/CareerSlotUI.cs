using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using RacingGame.Save;
using RacingGame.Career;

namespace RacingGame.UI
{
    public class CareerSlotUI : MonoBehaviour
    {
        public static CareerSlotUI Instance { get; private set; }

        [Header("Slot list")]
        public RectTransform slotListContainer;
        public CareerSlotRow slotRowPrefab;

        [Header("Create new slot")]
        public TMP_InputField slotNameInput;
        public Button createDriverButton;
        public Button createManagerButton;
        public Button refreshButton;

        List<CareerSlotRow> _rows = new();

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start()
        {
            createDriverButton?.onClick.AddListener(CreateDriverSlot);
            createManagerButton?.onClick.AddListener(CreateManagerSlot);
            refreshButton?.onClick.AddListener(RefreshSlots);

            if (CareerSlotManager.Instance != null)
            {
                CareerSlotManager.Instance.OnSlotsUpdated += RefreshSlotList;
            }

            RefreshSlots();
        }

        void OnDestroy()
        {
            if (CareerSlotManager.Instance != null)
                CareerSlotManager.Instance.OnSlotsUpdated -= RefreshSlotList;
        }

        void RefreshSlots()
        {
            CareerSlotManager.Instance?.RefreshSlots();
        }

        void RefreshSlotList(List<SaveSlot> slots)
        {
            if (slotListContainer == null || slotRowPrefab == null) return;

            foreach (var row in _rows)
                Destroy(row.gameObject);
            _rows.Clear();

            if (slots == null) return;

            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                var row = Instantiate(slotRowPrefab, slotListContainer);
                row.Bind(slot, i);
                _rows.Add(row);
            }
        }

        void CreateDriverSlot()
        {
            if (string.IsNullOrWhiteSpace(slotNameInput?.text)) return;
            CareerSlotManager.Instance?.CreateDriverSlot(slotNameInput.text.Trim());
            slotNameInput.text = string.Empty;
        }

        void CreateManagerSlot()
        {
            if (string.IsNullOrWhiteSpace(slotNameInput?.text)) return;
            CareerSlotManager.Instance?.CreateManagerSlot(slotNameInput.text.Trim());
            slotNameInput.text = string.Empty;
        }

        public void SelectSlot(int index)
        {
            CareerSlotManager.Instance?.SelectSlot(index);
        }

        public void DeleteSlot(int index)
        {
            CareerSlotManager.Instance?.DeleteSlot(index);
        }
    }
}
