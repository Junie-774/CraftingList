﻿using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Channels;
using CraftingList.Utility;
using Dalamud.Game;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.UI;
using Lumina.Excel.GeneratedSheets;

namespace CraftingList.Utility;

public enum UiColor
{
    /// <summary>
    /// Orange.
    /// </summary>
    Orange = 500,

    /// <summary>
    /// Blue.
    /// </summary>
    Blue = 502,

    /// <summary>
    /// Green.
    /// </summary>
    Green = 504,

    /// <summary>
    /// Yellow.
    /// </summary>
    Yellow = 506,

    /// <summary>
    /// Red.
    /// </summary>
    Red = 508,
}

/// <summary>
/// Manager that handles displaying output in the chat box.
/// </summary>
internal class ChatManager : IDisposable
{
    private readonly Channel<string> chatBoxMessages = Channel.CreateUnbounded<string>();

    [Signature("48 89 5C 24 ?? 57 48 83 EC 20 48 8B FA 48 8B D9 45 84 C9")]
    private readonly ProcessChatBoxDelegate processChatBox = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatManager"/> class.
    /// </summary>
    public ChatManager()
    {
        SignatureHelper.Initialise(this);
        DalamudApi.Framework.Update += this.FrameworkUpdate;
    }

    private unsafe delegate void ProcessChatBoxDelegate(UIModule* uiModule, IntPtr message, IntPtr unused, byte a4);

    /// <inheritdoc/>
    public void Dispose()
    {
        DalamudApi.Framework.Update -= this.FrameworkUpdate;

        this.chatBoxMessages.Writer.Complete();
    }

    /// <summary>
    /// Print a normal message.
    /// </summary>
    /// <param name="message">The message to print.</param>
    public void PrintMessage(string message)
        => DalamudApi.ChatGui.PrintChat(new XivChatEntry()
        {
            Message = $"[SND] {message}",
        });

    /// <summary>
    /// Print a happy message.
    /// </summary>
    /// <param name="message">The message to print.</param>
    /// <param name="color">UiColor value.</param>
    public void PrintColor(string message, UiColor color)
        => DalamudApi.ChatGui.PrintChat(
            new XivChatEntry()
            {
                Message = new SeString(
                    new UIForegroundPayload((ushort)color),
                    new TextPayload($"[SND] {message}"),
                    UIForegroundPayload.UIForegroundOff),
            });

    /// <summary>
    /// Print an error message.
    /// </summary>
    /// <param name="message">The message to print.</param>
    public void PrintError(string message)
        => DalamudApi.ChatGui.PrintChat(new XivChatEntry()
        {
            Message = $"[CraftingList] {message}",
        });

    /// <summary>
    /// Process a command through the chat box.
    /// </summary>
    /// <param name="message">Message to send.</param>
    public async void SendMessage(string message)
    {
        await this.chatBoxMessages.Writer.WriteAsync(message);
    }

    /// <summary>
    /// Clear the queue of messages to send to the chatbox.
    /// </summary>
    public void Clear()
    {
        var reader = this.chatBoxMessages.Reader;
        while (reader.Count > 0 && reader.TryRead(out var _))
            continue;
    }

    private void FrameworkUpdate(Framework framework)
    {
        if (this.chatBoxMessages.Reader.TryRead(out var message))
        {
            this.SendMessageInternal(message);
        }
    }

    private unsafe void SendMessageInternal(string message)
    {
        var framework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance();
        var uiModule = framework->GetUiModule();

        using var payload = new ChatPayload(message);
        var payloadPtr = Marshal.AllocHGlobal(400);
        Marshal.StructureToPtr(payload, payloadPtr, false);

        this.processChatBox(uiModule, payloadPtr, IntPtr.Zero, 0);

        Marshal.FreeHGlobal(payloadPtr);
    }

    [StructLayout(LayoutKind.Explicit)]
    private readonly struct ChatPayload : IDisposable
    {
        [FieldOffset(0)]
        private readonly IntPtr textPtr;

        [FieldOffset(16)]
        private readonly ulong textLen;

        [FieldOffset(8)]
        private readonly ulong unk1;

        [FieldOffset(24)]
        private readonly ulong unk2;

        internal ChatPayload(string text)
        {
            var stringBytes = Encoding.UTF8.GetBytes(text);
            this.textPtr = Marshal.AllocHGlobal(stringBytes.Length + 30);

            Marshal.Copy(stringBytes, 0, this.textPtr, stringBytes.Length);
            Marshal.WriteByte(this.textPtr + stringBytes.Length, 0);

            this.textLen = (ulong)(stringBytes.Length + 1);

            this.unk1 = 64;
            this.unk2 = 0;
        }

        public void Dispose()
        {
            Marshal.FreeHGlobal(this.textPtr);
        }
    }
}