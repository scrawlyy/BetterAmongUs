using BetterAmongUs.Data.Config;
using HarmonyLib;
using UnityEngine;

namespace BetterAmongUs.Patches.Client;

[HarmonyPatch]
internal static class DarkModePatch
{
    private static readonly Color ChatBackground = new(0.2f, 0.2f, 0.2f);
    private static readonly Color ChatMask = new(0.1f, 0.1f, 0.1f);
    private static readonly Color MutedButton = new(0.5f, 0.5f, 0.5f);

    [HarmonyPatch(typeof(ChatBubble), nameof(ChatBubble.SetCosmetics))]
    [HarmonyPostfix]
    private static void ChatBubble_SetCosmetics_Postfix(ChatBubble __instance)
    {
        if (!BAUConfigs.DarkMode.Value || __instance == null)
            return;

        __instance.Background.color = ChatBackground;
        __instance.MaskArea.color = ChatMask;
        __instance.TextArea.color = Color.white;
        __instance.TextArea.outlineWidth = __instance.NameText.outlineWidth * 0.75f;
    }

    [HarmonyPatch(typeof(ChatController), nameof(ChatController.Update))]
    [HarmonyPostfix]
    private static void ChatController_Update_Postfix(ChatController __instance)
    {
        if (!BAUConfigs.DarkMode.Value || __instance == null)
            return;

        __instance.backgroundImage.color = ChatBackground;
        SetButtonColors(__instance.chatButton);
    }

    [HarmonyPatch(typeof(FreeChatInputField), nameof(FreeChatInputField.Awake))]
    [HarmonyPostfix]
    private static void FreeChatInputField_Awake_Postfix(FreeChatInputField __instance)
    {
        if (!BAUConfigs.DarkMode.Value || __instance == null)
            return;

        __instance.background.color = ChatBackground;
        var rollover = __instance.background.GetComponent<ButtonRolloverHandler>();
        if (rollover != null)
        {
            rollover.OutColor = new Color(0.15f, 0.15f, 0.15f);
            rollover.OverColor = new Color(0.25f, 0.25f, 0.25f);
            rollover.UnselectedColor = new Color(0.15f, 0.15f, 0.15f);
        }

        __instance.textArea.gameObject.GetComponent<TMPro.TextMeshPro>().color = Color.white;
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    private static void MeetingHud_Start_Postfix(MeetingHud __instance)
    {
        if (!BAUConfigs.DarkMode.Value || __instance == null)
            return;

        var phoneUi = __instance.meetingContents == null ? null : __instance.meetingContents.transform.Find("PhoneUI");
        var baseColorTransform = phoneUi == null ? null : phoneUi.Find("baseColor");
        var baseColor = baseColorTransform == null ? null : baseColorTransform.GetComponent<SpriteRenderer>();
        if (baseColor != null)
            baseColor.color = new Color(0.01f, 0.01f, 0.01f);

        if (__instance.Glass != null)
            __instance.Glass.color = new Color(0.7f, 0.7f, 0.7f, 0.3f);

        var skipVote = __instance.SkipVoteButton == null ? null : __instance.SkipVoteButton.GetComponent<SpriteRenderer>();
        if (skipVote != null)
            skipVote.color = new Color(0.4f, 0.4f, 0.4f);

        foreach (var playerMaterialColors in __instance.PlayerColoredParts)
        {
            if (playerMaterialColors == null)
                continue;

            playerMaterialColors.color = new Color(0.25f, 0.25f, 0.25f);
            PlayerMaterial.SetColors(7, playerMaterialColors);
        }
    }

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    private static void HudManager_Update_Postfix(HudManager __instance)
    {
        if (!BAUConfigs.DarkMode.Value || __instance == null)
            return;

        SetButtonColors(__instance.MapButton);
        if (__instance.SettingsButton != null)
            SetButtonColors(__instance.SettingsButton.GetComponent<PassiveButton>());
    }

    [HarmonyPatch(typeof(ProgressTracker), nameof(ProgressTracker.FixedUpdate))]
    [HarmonyPostfix]
    private static void ProgressTracker_FixedUpdate_Postfix(ProgressTracker __instance)
    {
        if (!BAUConfigs.DarkMode.Value || __instance == null)
            return;

        var backgroundTransform = __instance.transform.Find("Background");
        var background = backgroundTransform == null ? null : backgroundTransform.GetComponent<SpriteRenderer>();
        if (background != null)
            background.color = new Color(0.6f, 0.6f, 0.6f);
    }

    [HarmonyPatch(typeof(FriendsListButton), nameof(FriendsListButton.Awake))]
    [HarmonyPostfix]
    private static void FriendsListButton_Awake_Postfix(FriendsListButton __instance)
    {
        if (!BAUConfigs.DarkMode.Value || __instance == null || __instance.Button == null)
            return;

        var button = __instance.Button.transform;
        SetSpriteColor(button.Find("Inactive"), MutedButton);
        SetSpriteColor(button.Find("Active"), MutedButton);
        SetSpriteColor(button.Find("Selected"), MutedButton);
        SetSpriteColor(button.Find("background"), MutedButton);
        SetSpriteColor(button.Find("NotifCount"), new Color(0.8f, 0.8f, 0.8f));
    }

    [HarmonyPatch(typeof(ChatNotification), nameof(ChatNotification.Awake))]
    [HarmonyPostfix]
    private static void ChatNotification_Awake_Postfix(ChatNotification __instance)
    {
        if (!BAUConfigs.DarkMode.Value || __instance == null)
            return;

        __instance.background.color = ChatBackground;
        __instance.chatText.color = Color.white;
    }

    private static void SetButtonColors(PassiveButton? button)
    {
        if (button == null)
            return;

        SetSpriteColor(button.inactiveSprites, MutedButton);
        SetSpriteColor(button.activeSprites, MutedButton);
        SetSpriteColor(button.selectedSprites, MutedButton);
        SetSpriteColor(button.transform.Find("Background"), MutedButton);
    }

    private static void SetSpriteColor(Transform? transform, Color color)
    {
        var sprite = transform == null ? null : transform.GetComponent<SpriteRenderer>();
        if (sprite != null)
            sprite.color = color;
    }

    private static void SetSpriteColor(SpriteRenderer? sprite, Color color)
    {
        if (sprite != null)
            sprite.color = color;
    }

    private static void SetSpriteColor(GameObject? gameObject, Color color)
    {
        if (gameObject != null)
            SetSpriteColor(gameObject.GetComponent<SpriteRenderer>(), color);
    }
}