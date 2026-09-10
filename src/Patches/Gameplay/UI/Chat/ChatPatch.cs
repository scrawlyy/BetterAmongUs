using BetterAmongUs.Data;
using BetterAmongUs.Data.Config;
using BetterAmongUs.Generated;
using BetterAmongUs.Modules;
using BetterAmongUs.Modules.Support;
using BetterAmongUs.MonoScripts.Extended;
using BetterAmongUs.Structs;
using BetterAmongUs.Utilities;
using BetterAmongUs.Utilities.Extension;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace BetterAmongUs.Patches.Gameplay.UI.Chat;

[HarmonyPatch]
internal static class ChatPatch
{
    internal static List<string> ChatHistory = [];
    internal static int CurrentHistorySelection = -1;

    internal const string COMMAND_POSTFIX_ID = "<size=0%>IsCommand</size>";

    internal static bool IsChatVisible
    {
        get
        {
            if (BAUConfigs.ChatInGameplay.Value)
            {
                return true;
            }

            if (!GameState.IsInGamePlay)
            {
                return true;
            }

            return !PlayerControl.LocalPlayer.IsAlive() || GameState.IsMeeting || GameState.IsExilling;
        }
    }

    /// <summary>
    /// Removes all chat bubbles from the chat.
    /// </summary>
    internal static void ClearChat()
    {
        if (!HudManager.InstanceExists)
            return;

        // Clear all chat bubbles
        HudManager.Instance.Chat.chatBubblePool.ReclaimAll();
    }

    /// <summary>
    /// Removes all player chat bubbles from the chat, excluding command bubbles.
    /// </summary>
    internal static void ClearPlayerChats()
    {
        if (!HudManager.InstanceExists)
            return;

        // Clear only player chat bubbles (keep command bubbles)
        foreach (var obj in HudManager.Instance.Chat.chatBubblePool.activeChildren.ToArray())
        {
            var chatBubble = obj.GetComponent<ChatBubble>();
            if (chatBubble != null)
            {
                if (chatBubble.NameText.text.EndsWith(COMMAND_POSTFIX_ID)) continue;
                HudManager.Instance.Chat.chatBubblePool.Reclaim(chatBubble);
            }
        }
        HudManager.Instance.Chat.AlignAllBubbles();
    }

    /// <summary>
    /// Removes all command related chat bubbles from the chat, preserving player chat bubbles.
    /// </summary>
    internal static void ClearCommands()
    {
        if (!HudManager.InstanceExists)
            return;

        // Clear only command chat bubbles (keep player chat)
        foreach (var obj in HudManager.Instance.Chat.chatBubblePool.activeChildren.ToArray())
        {
            var chatBubble = obj.GetComponent<ChatBubble>();
            if (chatBubble != null)
            {
                if (!chatBubble.NameText.text.EndsWith(COMMAND_POSTFIX_ID)) continue;
                HudManager.Instance.Chat.chatBubblePool.Reclaim(chatBubble);
            }
        }
        HudManager.Instance.Chat.AlignAllBubbles();
    }

    [HarmonyPatch(typeof(ChatController), nameof(ChatController.Update))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    private static void ChatController_Update_Prefix(ChatController __instance)
    {
        // Ctrl+x to cut text to clipboard
        if (__instance.IsOpenOrOpening)
        {
            if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.X))
            {
                ClipboardHelper.PutClipboardString(__instance.freeChatField.textArea.text);
                __instance.freeChatField.textArea.SetText("");
            }
            // Up arrow for chat history navigation
            if (Input.GetKeyDown(KeyCode.UpArrow) && ChatHistory.Any())
            {
                CurrentHistorySelection = Mathf.Clamp(--CurrentHistorySelection, 0, ChatHistory.Count - 1);
                __instance.freeChatField.textArea.SetText(ChatHistory[CurrentHistorySelection]);
            }
            // Down arrow for chat history navigation
            if (Input.GetKeyDown(KeyCode.DownArrow) && ChatHistory.Any())
            {
                CurrentHistorySelection++;
                if (CurrentHistorySelection < ChatHistory.Count)
                    __instance.freeChatField.textArea.SetText(ChatHistory[CurrentHistorySelection]);
                else __instance.freeChatField.textArea.SetText("");
            }
        }
    }
    // Log chat messages to console
    [HarmonyPatch(typeof(ChatController), nameof(ChatController.AddChat))]
    [HarmonyPostfix]
    private static void ChatController_AddChat_Postfix(ChatController __instance, PlayerControl sourcePlayer, string chatText)
    {
        // Log chat publicly if player is alive, privately if dead
        if (sourcePlayer.IsAlive() || !PlayerControl.LocalPlayer.IsAlive())
        {
            Logger_.Log($"{sourcePlayer.Data.PlayerName} -> {chatText}", "ChatLog");
        }
        else
        {
            Logger_.LogPrivate($"{sourcePlayer.Data.PlayerName} -> {chatText}", "ChatLog");
        }
    }

    [HarmonyPatch(typeof(ChatController), nameof(ChatController.SetChatBubbleName))]
    [HarmonyPostfix]
    private static void ChatController_SetChatBubbleName_Postfix(ChatBubble bubble, NetworkedPlayerInfo playerInfo, bool isDead, bool didVote)
    {
        if (BAUModdedSupportFlags.HasFlag(BAUModdedSupportFlags.Disable_ChatNameOverride))
            return;

        if (playerInfo == null)
            return;

        if (didVote)
            return;

        if (bubble == null)
            return;

        if (bubble.NameText == null)
            return;

        var sourcePlayer = playerInfo.Object;
        if (sourcePlayer == null)
            return;

        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer == null)
            return;

        SplitStringBuilder ssbTag = new(100, '-');

        string hashPuid = Utils.GetHashPuid(sourcePlayer);
        string friendCode = playerInfo.FriendCode;
        string playerName = playerInfo.ExtendedData()?.RealName ?? "???";

        // In lobby, show player tags instead of roles
        if (GameState.IsLobby && !GameState.IsFreePlay)
        {
            var betterData = sourcePlayer.ExtendedData();
            if (betterData == null)
                return;

            // Show BAU user tag
            if (sourcePlayer.IsLocalPlayer() || betterData.IsBetterUser)
            {
                ssbTag.AppendFormat("<color=#0dff00>{1}{0}</color>", TranslationStrings.Player_BetterUser.LocalizedString, betterData.IsVerifiedBetterUser || sourcePlayer.IsLocalPlayer() ? "✓ " : "");
            }

            // Show mod-specific tags based on player data
            if (BetterDataManager.Files.BetterDataFile.TryGetCheatInfo(sourcePlayer.Data, out var info))
            {
                ssbTag.Append(info.title.ToColor(info.hexColor));
            }
        }

        ssbTag.Append(sourcePlayer.GetRoleInfo(false).Size(75f));

        string infoText = ssbTag.ToString().Size(75f);

        // Position tags before local player name, after other players' names
        if (sourcePlayer.IsLocalPlayer())
            playerName = infoText + " " + playerName;
        else
            playerName += " " + infoText;

        bubble.NameText.SetText(playerName);
    }

    [HarmonyPatch(typeof(FreeChatInputField), nameof(FreeChatInputField.Awake))]
    [HarmonyPostfix]
    private static void FreeChatInputField_Awake_Postfix(FreeChatInputField __instance)
    {
        // Enable extended character support for chat
        __instance.textArea.allowAllCharacters = true;
        __instance.textArea.AllowSymbols = true;
        __instance.textArea.AllowPaste = true;
        __instance.textArea.AllowEmail = true;
        __instance.textArea.characterLimit = ModInfo.Constants.MAX_CHAT_TEXT;
        __instance.charCountText.text = "0/" + ModInfo.Constants.MAX_CHAT_TEXT;
    }

    [HarmonyPatch(typeof(FreeChatInputField), nameof(FreeChatInputField.UpdateCharCount))]
    [HarmonyPostfix]
    private static void FreeChatInputField_UpdateCharCount_Postfix(FreeChatInputField __instance)
    {
        // Update character counter with color coding
        int length = __instance.textArea.text.Length;
        __instance.charCountText.text = string.Format("{0}/" + ModInfo.Constants.MAX_CHAT_TEXT, length);
        __instance.charCountText.color = GetCharColor(length);
    }

    private static Color GetCharColor(int length)
    {
        // Color gradient: green -> yellow -> red as text length increases
        Color[] colorGradient = [Color.green, Color.yellow, Color.red];
        (float min, float max) lerpRange = (0f, ModInfo.Constants.MAX_CHAT_TEXT - 1);
        return colorGradient.LerpColor(lerpRange, length);
    }
}
