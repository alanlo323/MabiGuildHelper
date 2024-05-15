// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace DiscordBot.SemanticKernel.Plugins.KernelMemory.Extensions.Discord;

public class DiscordDbMessage : DiscordMessage
{
    public string Id
    {
        get
        {
            return this.MessageId;
        }
        set
        {
            this.MessageId = value;
        }
    }
}
