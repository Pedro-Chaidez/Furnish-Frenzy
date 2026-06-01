// HouseInventoryUI.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class HouseInventoryUI : MonoBehaviour
{
    [Header("Panel")]
    public GameObject inventoryPanel;
    public KeyCode toggleKey = KeyCode.I;

    [Header("Slots")]
    public Image[] slotIcons = new Image[6];
    public TextMeshProUGUI[] slotLabels = new TextMeshProUGUI[6];

    [Header("Buttons")]
    public UnityEngine.UI.Button backToStoreButton;

    [Header("Scene Names")]
    public string storeSceneName = "Store";

    void Start()
    {
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);

        if (backToStoreButton != null)
            backToStoreButton.onClick.AddListener(OnBackToStore);

        RefreshUI();
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            if (inventoryPanel != null)
                inventoryPanel.SetActive(!inventoryPanel.activeSelf);
            if (inventoryPanel != null && inventoryPanel.activeSelf)
                RefreshUI();
        }
    }

    public void RefreshUI()
    {
        if (CartInventory.Instance == null) return;

        List<FurnitureItem> items = CartInventory.Instance.GetCartItems();

        for (int i = 0; i < 6; i++)
        {
            bool hasItem = i < items.Count;

            if (slotIcons != null && i < slotIcons.Length && slotIcons[i] != null)
            {
                slotIcons[i].sprite  = hasItem ? items[i].icon : null;
                slotIcons[i].color   = hasItem ? Color.white : new Color(1, 1, 1, 0.15f);
                slotIcons[i].enabled = hasItem;
            }

            if (slotLabels != null && i < slotLabels.Length && slotLabels[i] != null)
                slotLabels[i].text = hasItem ? items[i].itemName : "";
        }
    }

    public void OnBackToStore()
    {
        SceneManager.LoadScene(storeSceneName);
    }
}