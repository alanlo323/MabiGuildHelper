using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using DiscordBot.ButtonHandler;
using DiscordBot.Configuration;
using DiscordBot.DataObject;
using DiscordBot.Db;
using DiscordBot.Db.Entity;
using DiscordBot.Extension;
using DiscordBot.Helper;
using DiscordBot.SchedulerJob;
using DiscordBot.SemanticKernel;
using DiscordBot.SemanticKernel.Core;
using DiscordBot.SemanticKernel.Plugins.KernelMemory;
using DiscordBot.SemanticKernel.QueneService;
using DiscordBot.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.KernelMemory;
using Microsoft.SemanticKernel.Plugins.Web;
using Microsoft.VisualBasic.FileIO;
using NetTopologySuite.Utilities;
using static DiscordBot.Commands.IBaseCommand;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace DiscordBot.Commands.SlashCommand
{
    public class AiCommand(ILogger<AiCommand> logger, MabinogiKernelMemoryFactory mabiKMFactory) : IBaseSlashCommand
    {
        public string Name { get; set; } = "ai";
        public string Description { get; set; } = "AI相關功能";
        public CommandAvailability Availability { get; set; } = CommandAvailability.AdminServerOnly;

        public ApplicationCommandProperties GetCommandProperties()
        {
            var command = new SlashCommandBuilder()
                .WithName(Name)
                .WithDescription(Description)
                .WithDefaultMemberPermissions(GuildPermission.Administrator)
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("rag")
                    .WithDescription("檢索增強生成（Retrieval-Augmented Generation, RAG）")
                    .WithType(ApplicationCommandOptionType.SubCommandGroup)
                    .AddOption(new SlashCommandOptionBuilder()
                        .WithName("importwebsite")
                        .WithDescription("匯入網頁")
                        .WithType(ApplicationCommandOptionType.SubCommand)
                        .AddOption("url", ApplicationCommandOptionType.String, "網址", isRequired: true, minLength: 1)
                    )
                )
                ;
            return command.Build();
        }

        public async Task Excute(SocketSlashCommand command)
        {
            foreach (SocketSlashCommandDataOption option in command.Data.Options)
            {
                switch (option.Name)
                {
                    case "rag":
                        await HandleRagCommand(command, option);
                        break;
                    default:
                        break;
                }
            }
        }

        private async Task HandleRagCommand(SocketSlashCommand command, SocketSlashCommandDataOption option)
        {
            foreach (SocketSlashCommandDataOption subOption in option.Options)
            {
                switch (subOption.Name)
                {
                    case "importwebsite":
                        await HandleImportWevsiteCommand(command, subOption);
                        break;
                    default:
                        break;
                }
            }
        }

        private async Task HandleImportWevsiteCommand(SocketSlashCommand command, SocketSlashCommandDataOption option)
        {
            await command.DeferAsync();

            RestFollowupMessage? restFollowupMessage = null;
            string url = option.Options.Single(x => x.Name == "url").Value as string;
            // Remove URL fragment if it exists
            UriBuilder uriBuilder = new(url)
            {
                Fragment = string.Empty,
            };
            string cleanUrl = uriBuilder.Uri.ToString();
            // skip if it is not html
            if (!await MiscUtil.IsHtml(cleanUrl))
            {
                FollowUpOrEditMessage(command, "只支援html類網址", ref restFollowupMessage);
                return;
            }


            string documentId = MiscUtil.GetValidFileName(cleanUrl);
            IKernelMemory memory = await mabiKMFactory.GetMabinogiKernelMemory();

            if (await memory.IsDocumentReadyAsync(documentId))
            {
                FollowUpOrEditMessage(command, $"{cleanUrl.ToHighLight()}{Environment.NewLine}已經在資料庫中, 正在重新匯入...", ref restFollowupMessage);
            }
            else
            {
                FollowUpOrEditMessage(command, $"{cleanUrl.ToHighLight()}{Environment.NewLine}正在匯入...", ref restFollowupMessage);
            }

            documentId = await memory.ImportWebPageAsync(cleanUrl, documentId: documentId);
            FollowUpOrEditMessage(command, $"{cleanUrl.ToHighLight()}{Environment.NewLine}匯入完成 (documentId: {documentId})", ref restFollowupMessage);
        }

        private void FollowUpOrEditMessage(SocketInteraction socketInteraction, string content, ref RestFollowupMessage? restFollowupMessage, FileInfo? fileInfo = null, MessageComponent? components = null, Embed? embed = null)
        {
            if (restFollowupMessage == null)
            {
                restFollowupMessage = fileInfo == default
                    ? socketInteraction.FollowupAsync(content, components: components, embed: embed).GetAwaiter().GetResult()
                    : socketInteraction.FollowupWithFileAsync(filePath: fileInfo.FullName, fileName: fileInfo.Name, text: content, components: components, embed: embed).GetAwaiter().GetResult();
            }
            else
            {
                restFollowupMessage.ModifyAsync(x =>
                {
                    x.Content = content;
                    x.Components = components;
                    x.Embed = embed;
                    if (fileInfo != default) x.Attachments = new List<FileAttachment>() { new(path: fileInfo.FullName, fileName: fileInfo.Name) };
                }).GetAwaiter().GetResult();
            }
        }
    }
}