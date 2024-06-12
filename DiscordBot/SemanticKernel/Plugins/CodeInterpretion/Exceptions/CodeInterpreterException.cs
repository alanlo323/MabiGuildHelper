// Copyright (c) Kevin BEAUGRAND. All rights reserved.

namespace DiscordBot.SemanticKernel.Plugins.KernelMemory.CodeInterpretion.Exceptions;

internal class CodeInterpreterException : Exception
{
    internal CodeInterpreterException(string message, params string[] warnings)
        : base(message)
    {
        this.Warnings = warnings;
    }

    public string[] Warnings { get; }
}
