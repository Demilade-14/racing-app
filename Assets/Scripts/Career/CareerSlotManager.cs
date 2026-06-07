using System;
using System.Collections.Generic;
using UnityEngine;
using RacingGame.Data;
using RacingGame.Save;

namespace RacingGame.Career
{
    public class CareerSlotManager : MonoBehaviour
    {
        public static CareerSlotManager Instance { get; private set; }

        public event Action<List<SaveSlot>> OnSlotsUpdated;
        public event Action<SaveSlot> OnSlotSelected;

        void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            RefreshSlots();
        }

        public List<SaveSlot> GetSlots() => SaveManager.Instance?.Slots;
        public SaveSlot GetCurrentSlot() => SaveManager.Instance?.Current;
        public CareerType GetCurrentCareerType() => SaveManager.Instance?.Current?.slotType ?? CareerType.Driver;

        public void RefreshSlots()
        {
            if (SaveManager.Instance == null) return;
            SaveManager.Instance.LoadSlots();
            OnSlotsUpdated?.Invoke(SaveManager.Instance.Slots);
            OnSlotSelected?.Invoke(SaveManager.Instance.Current);
        }

        public void SelectSlot(int index)
        {
            if (SaveManager.Instance == null) return;
            SaveManager.Instance.SelectSlot(index);
            OnSlotsUpdated?.Invoke(SaveManager.Instance.Slots);
            OnSlotSelected?.Invoke(SaveManager.Instance.Current);
        }

        public void CreateDriverSlot(string slotName)
        {
            if (SaveManager.Instance == null) return;
            SaveManager.Instance.CreateSlot(slotName, CareerType.Driver);
            OnSlotsUpdated?.Invoke(SaveManager.Instance.Slots);
            OnSlotSelected?.Invoke(SaveManager.Instance.Current);
        }

        public void CreateManagerSlot(string slotName)
        {
            if (SaveManager.Instance == null) return;
            SaveManager.Instance.CreateSlot(slotName, CareerType.Manager);
            OnSlotsUpdated?.Invoke(SaveManager.Instance.Slots);
            OnSlotSelected?.Invoke(SaveManager.Instance.Current);
        }

        public void DeleteSlot(int index)
        {
            if (SaveManager.Instance == null) return;
            if (index < 0 || index >= SaveManager.Instance.Slots.Count) return;

            SaveManager.Instance.Slots.RemoveAt(index);
            int newIndex = Mathf.Clamp(SaveManager.Instance.SelectedSlotIndex, 0, SaveManager.Instance.Slots.Count - 1);
            if (SaveManager.Instance.Slots.Count > 0)
                SaveManager.Instance.SelectSlot(newIndex);

            SaveManager.Instance.SaveSlots();
            OnSlotsUpdated?.Invoke(SaveManager.Instance.Slots);
            OnSlotSelected?.Invoke(SaveManager.Instance.Current);
        }
    }
}
