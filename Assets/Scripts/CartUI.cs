using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CartUI : MonoBehaviour
{
    public static CartUI Instance { get; private set; }

    [Header("Panel")]
    public GameObject cartPanel;
    public KeyCode toggleKey = KeyCode.Tab;

    [Header("Cart Slots")]
    public Image[] slotIcons = new Image[6];
    public TextMeshProUGUI[] slotLabels = new TextMeshProUGUI[6];
    public TextMeshProUGUI titleText;

    [Header("Hand Items Display")]
    public TextMeshProUGUI handItemsText;

    [Header("Category Icons")]
    public Sprite bathroom;
    public Sprite bed;
    public Sprite chair;
    public Sprite closet;
    public Sprite drawer;
    public Sprite kitchen;
    public Sprite pillow;
    public Sprite table;
    public Sprite sofa;

    [Header("Fallback")]
    public Sprite defaultIcon;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    private void OnEnable()  => CartInventory.InventoryChanged += RefreshUI;
    private void OnDisable() => CartInventory.InventoryChanged -= RefreshUI;

    private void Start()
    {
        if (cartPanel != null)
            cartPanel.SetActive(false);

        // Force-clear any default TMP placeholder text
        foreach (var label in GetComponentsInChildren<TMPro.TextMeshProUGUI>(true))
            label.text = "";

        RefreshUI();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            if (cartPanel != null)
                cartPanel.SetActive(!cartPanel.activeSelf);
            RefreshUI();
        }
    }

    public void RefreshUI()
    {
        if (CartInventory.Instance == null) return;

        List<FurnitureItem> cartItems = CartInventory.Instance.GetCartItems();
        FurnitureItem bigItem = CartInventory.Instance.GetBigItem();
        int occupancy = CartInventory.Instance.GetVisualOccupancy();

        if (titleText != null)
            titleText.text = $"INVENTORY  {occupancy}/6";

        ClearSlots();

        if (bigItem != null)
        {
            // Fill slots 0-4 with the big item icon to show it takes up 5 slots
            for (int i = 0; i < 5; i++)
                SetSlot(i, bigItem);

            // Slot 5 gets the small item if one exists, otherwise stays empty
            if (cartItems.Count > 0)
                SetSlot(5, cartItems[0]);
        }
        else
        {
            // All 6 slots free for small items
            for (int i = 0; i < cartItems.Count && i < 6; i++)
                SetSlot(i, cartItems[i]);
        }

        if (handItemsText != null)
        {
            handItemsText.text = bigItem != null
                ? $"Carrying: {ShortenName(bigItem.itemName)}"
                : "Carrying: None";
        }
    }

    private void ClearSlots()
    {
        for (int i = 0; i < 6; i++)
        {
            if (slotIcons != null && i < slotIcons.Length && slotIcons[i] != null)
            {
                slotIcons[i].sprite = null;
                slotIcons[i].color = new Color(1f, 1f, 1f, 0.18f);
                slotIcons[i].preserveAspect = true;
            }

            if (slotLabels != null && i < slotLabels.Length && slotLabels[i] != null)
                slotLabels[i].text = "";
        }
    }

    private void SetSlot(int index, FurnitureItem item)
    {
        if (item == null) return;

        if (slotIcons != null && index < slotIcons.Length && slotIcons[index] != null)
        {
            slotIcons[index].sprite = GetCategorySprite(item);
            slotIcons[index].color = Color.white;
            slotIcons[index].preserveAspect = true;
        }

        if (slotLabels != null && index < slotLabels.Length && slotLabels[index] != null)
            slotLabels[index].text = ShortenName(item.itemName);
    }

    private Sprite GetCategorySprite(FurnitureItem item)
    {
        if (item == null) return defaultIcon;

        string name = item.itemName.ToLower();

        if (name.Contains("bathroom") || name.Contains("toilet")) return bathroom;
        if (name.Contains("bed"))                                  return bed;
        if (name.Contains("chair"))                                return chair;
        if (name.Contains("closet") || name.Contains("wardrobe")) return closet;
        if (name.Contains("drawer") || name.Contains("dresser"))  return drawer;
        if (name.Contains("kitchen"))                              return kitchen;
        if (name.Contains("pillow") || name.Contains("cushion"))  return pillow;
        if (name.Contains("table"))                                return table;
        if (name.Contains("sofa")   || name.Contains("couch"))    return sofa;

        return defaultIcon;
    }

    private string ShortenName(string itemName)
    {
        if (string.IsNullOrEmpty(itemName)) return "";

        string name = itemName.ToLower();

        if (name.Contains("bathroom") || name.Contains("toilet")) return "Bathroom";
        if (name.Contains("bed"))                                  return "Bed";
        if (name.Contains("chair"))                                return "Chair";
        if (name.Contains("closet") || name.Contains("wardrobe")) return "Closet";
        if (name.Contains("drawer") || name.Contains("dresser"))  return "Drawer";
        if (name.Contains("kitchen"))                              return "Kitchen";
        if (name.Contains("pillow") || name.Contains("cushion"))  return "Pillow";
        if (name.Contains("table"))                                return "Table";
        if (name.Contains("sofa")   || name.Contains("couch"))    return "Sofa";

        return itemName;
    }
}