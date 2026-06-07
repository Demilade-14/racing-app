using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using RacingGame.Save;

namespace RacingGame.UI
{
    public class CareerSlotRow : MonoBehaviour
    {
        public TextMeshProUGUI slotNameText;
        public TextMeshProUGUI slotTypeText;
        public TextMeshProUGUI seasonText;
        public Button selectButton;
        public Button deleteButton;

        int _slotIndex;

        public void Bind(SaveSlot slot, int index)
        {
            _slotIndex = index;
            slotNameText.text = slot.slotName;
            slotTypeText.text = slot.slotType.ToString();
            seasonText.text = slot.career != null
                ? $"Season {slot.career.season}"
                : "Season 1";

            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() => Select());
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(() => Delete());
        }

        void Select() => CareerSlotUI.Instance?.SelectSlot(_slotIndex);
        void Delete() => CareerSlotUI.Instance?.DeleteSlot(_slotIndex);
    }
}
