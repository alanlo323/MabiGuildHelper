﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace DiscordBot.Commands
{
    public interface IBaseCommand
    {
        public enum CommandAvailability
        {
            Global,
            AdminServerOnly
        }

        public string Name { get; set; }
        public string Description { get; set; }

        public CommandAvailability Availability { get; set; }

        public ApplicationCommandProperties GetCommandProperties();
    }
}
