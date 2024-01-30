using BepInEx;
using HarmonyLib;
using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BannedRich
{
    [BepInPlugin("Aidanamite.BannedRich", "BannedRich", "1.0.1")]
    public class Main : BaseUnityPlugin
    {
        internal static Assembly modAssembly = Assembly.GetExecutingAssembly();
        internal static string modName = $"{modAssembly.GetName().Name}";
        internal static string modDir = $"{Environment.CurrentDirectory}\\BepInEx\\{modName}";
        public static List<ShowcaseSlot> banned = new List<ShowcaseSlot>();
        public static Sprite MarkSprite;

        void Awake()
        {
            MarkSprite = LoadImage("ban_mark.png", 32, 32).CreateSprite();
            new Harmony($"com.Aidanamite.{modName}").PatchAll(modAssembly);
            Logger.LogInfo($"{modName} has loaded");
        }
        void Update()
        {
            if (GUIManager.Instance && GUIManager.Instance.input != null && GUIManager.Instance.CurrentInventory && GUIManager.Instance.CurrentInventory is ShowcaseInventoryPanel)
            {
                var panel = GUIManager.Instance.CurrentInventory as ShowcaseInventoryPanel;
                var gui = panel._currentSlotWithLabelSelected ? panel._currentSlotWithLabelSelected : InventoryPanel.currentSelectedSlot ? InventoryPanel.currentSelectedSlot.GetComponent<ShowcaseSlotGUI>() : null;
                if (gui && GUIManager.Instance.input.LeftTrigger.WasPressed)
                {
                    var slot = panel.openendShowCase.itemPositions[gui.slotIndex];
                    if (!banned.Remove(slot))
                        banned.Add(slot);
                    gui.UpdateMark();
                }
            }
        }

        public static Texture2D LoadImage(string filename, int width, int height)
        {
            var spriteData = modAssembly.GetManifestResourceStream(modName + "." + filename);
            var rawData = new byte[spriteData.Length];
            spriteData.Read(rawData, 0, rawData.Length);
            var tex = new Texture2D(width, height);
            tex.LoadImage(rawData);
            tex.filterMode = FilterMode.Point;
            return tex;
        }
    }

    public static class ExtentionMethods
    {
        public static Sprite CreateSprite(this Texture2D texture) => Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 1);
        public static void UpdateMark(this ShowcaseSlotGUI gui)
        {
            var mark = gui.transform.Find("BanMark");
            if (!mark)
                mark = gui.AddBanMark();
            var slot = gui.ownerShowcase.itemPositions[gui.slotIndex];
            mark.gameObject.SetActive(Main.banned.Contains(slot));
        }

        public static Transform AddBanMark(this ShowcaseSlotGUI gui)
        {
            var i = GameObject.Instantiate(gui.imageItem, gui.transform, false);
            gui.imageSelector.transform.SetAsLastSibling();
            i.name = "BanMark";
            i.sprite = Main.MarkSprite;
            i.enabled = true;
            i.overrideSprite = null;
            i.material = null;
            return i.transform;
        }
    }

    [HarmonyPatch(typeof(CommonVisitorMind), "IsShowcaseInteresting")]
    class Patch_Visitor_ValidShowcase
    {
        static void Postfix(CommonVisitorMind __instance, ShowcaseSlot slot, ref bool __result) => __result = __result && (!(__instance.hasHighBudget || ((__instance as VisitorMind)?.isIndecisive??false)) || !Main.banned.Contains(slot));
    }

    [HarmonyPatch(typeof(InventorySlotGUI), "SetItem")]
    class Patch_SlotGUI_SetItem
    {
        static void Postfix(InventorySlotGUI __instance)
        {
            if (__instance is ShowcaseSlotGUI) (__instance as ShowcaseSlotGUI).UpdateMark();
        }
    }

    [HarmonyPatch(typeof(ShopManager))]
    class Patch_ShopManager
    {
        [HarmonyPatch("GameSlot_OnSaveToDungeon")]
        [HarmonyPostfix]
        static void GameSlot_OnSaveToDungeon(ShopManager __instance, GameSlot gameSlot)
        {
            for (int i = 0; i < __instance.itemPoints.Count; i++)
                if (Main.banned.Contains(__instance.itemPoints[i]))
                    gameSlot.shopSaveInfo.shopShowcases[i].curseName = "banned";
        }

        [HarmonyPatch("LoadShopFromSave")]
        [HarmonyPostfix]
        static void LoadShopFromSave(ShopManager __instance, GameSlot savedInfo)
        {
            Main.banned.Clear();
            int c = Mathf.Min(savedInfo.shopSaveInfo.shopShowcases.Count, __instance.itemPoints.Count);
            for (int i = 0; i < c; i++)
                if (savedInfo.shopSaveInfo.shopShowcases[i].curseName == "banned")
                    Main.banned.Add(__instance.itemPoints[i]);
        }
    }
}