using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiscordBot.Helper;
using Microsoft.SemanticKernel.Plugins.Web;
using Microsoft.SemanticKernel;
using DiscordBot.SchedulerJob;
using Microsoft.Extensions.Logging;
using DiscordBot.Extension;
using Discord.WebSocket;
using Discord.Rest;
using DocumentFormat.OpenXml.Drawing;
using Discord;
using Amazon.Runtime.Internal.Endpoints.StandardLibrary;
using System.Collections;
using System.IO;

namespace DiscordBot.SemanticKernel.Plugins.Event
{
    /// <summary>
    /// Discord event manager  plugin.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="EventManagerPlugin"/> class.
    /// </remarks>
    public sealed class EventManagerPlugin(ILogger<EventManagerPlugin> logger, DiscordSocketClient client, DataScrapingHelper dataScrapingHelper)
    {
        /// <summary>
        /// Get events list from a discord server
        /// </summary>
        /// <param name="serverId">The id of the server.</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
        /// <returns>The return value contains the content as a string</returns>
        [KernelFunction, Description("Get existing events  from a discord server.")]
        public async Task<List<string>> GetCurrentEvents(
            Kernel kernel,
            [Description("The id of the server")] ulong serverId,
            CancellationToken cancellationToken = default)
        {
            List<string> events = [];
            try
            {
                SocketGuild guild = client.GetGuild(serverId);
                events = [.. (await guild.GetEventsAsync()).Select(x => $"""
                Id: {x.Id}
                Name: {x.Name}
                StartTime: {x.StartTime}
                EndTime: {x.EndTime}
                Description: {x.Description}
                Link: {x.Location}
                """)];
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw;
            }
            return events;
        }

        /// <summary>
        /// Create an event in a discord server
        /// </summary>
        /// <param name="serverId">The id of the server.</param>
        /// <param name="name">The name of the event.</param>
        /// <param name="startTime">The start time of the event.</param>
        /// <param name="description">The description of the event.</param>
        /// <param name="endTime">The end time of the event.</param>
        /// <param name="link">The location of the event; links are supported</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
        /// <returns>The return value contains the content as a string</returns>
        [KernelFunction, Description("Create an event in a discord server.")]
        public async Task<string> CreateEvent(
            Kernel kernel,
            [Description("The id of the server")] ulong serverId,
            [Description("The name of the event")] string name,
            [Description("The start time of the event")] DateTimeOffset startTime,
            [Description("The description of the event")] string description = null,
            [Description("The end time of the event")] DateTimeOffset? endTime = null,
            [Description("The link of the event")] string link = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                bool amendStartTime = false;
                DateTime now = DateTime.Now;
                if (startTime < DateTime.Now && endTime < DateTime.Now) return $"Do not need to create this event since it is in the pass.";
                if (startTime < DateTime.Now && endTime >= DateTime.Now) amendStartTime = true;

                SocketGuild guild = client.GetGuild(serverId);
                Image coverImage = default;

                var snapshot = await dataScrapingHelper.GetWebsiteSnapshot(link);
                using MemoryStream stream = new(snapshot.Item2 ?? []);
                if (snapshot != default) coverImage = new Image(stream);

                RestGuildEvent restGuildEvent = await guild.CreateEventAsync(name, amendStartTime ? DateTime.Now.AddSeconds(5) : startTime, GuildScheduledEventType.External, description: description, endTime: endTime, coverImage: coverImage, location: link);
                return $"Event: {restGuildEvent.Name} created.";
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw;
            }
        }

        /// <summary>
        /// Create an event in a discord server
        /// </summary>
        /// <param name="serverId">The id of the server.</param>
        /// <param name="eventId">The id of the event.</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
        /// <returns>The return value contains the content as a string</returns>
        [KernelFunction, Description("End an event in a discord server.")]
        public async Task<string> EndEvent(
            Kernel kernel,
            [Description("The id of the server")] ulong serverId,
            [Description("The id of the event")] ulong eventId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                SocketGuild guild = client.GetGuild(serverId);
                RestGuildEvent restGuildEvent = await guild.GetEventAsync(eventId);
                await restGuildEvent.EndAsync();

                return $"Event: {restGuildEvent.Name} ended.";
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw;
            }
        }

        /// <summary>
        /// Create an event in a discord server
        /// </summary>
        /// <param name="serverId">The id of the server.</param>
        /// <param name="eventId">The id of the event.</param>
        /// <param name="name">The name of the event.</param>
        /// <param name="startTime">The start time of the event.</param>
        /// <param name="description">The description of the event.</param>
        /// <param name="endTime">The end time of the event.</param>
        /// <param name="link">The location of the event; links are supported</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
        /// <returns>The return value contains the content as a string</returns>
        [KernelFunction, Description("Modify an event in a discord server.")]
        public async Task<string> ModifyEvent(
            Kernel kernel,
            [Description("The id of the server")] ulong serverId,
            [Description("The id of the event")] ulong eventId,
            [Description("The new name of the event")] string name,
            [Description("The new start time of the event")] DateTimeOffset startTime,
            [Description("The new description of the event")] string description = null,
            [Description("The new end time of the event")] DateTimeOffset? endTime = null,
            [Description("The new link of the event")] string link = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                bool amendStartTime = false;
                DateTime now = DateTime.Now;
                if (startTime < DateTime.Now && endTime < DateTime.Now) return $"Do not need to modify this event since it is in the pass.";
                if (startTime < DateTime.Now && endTime >= DateTime.Now) amendStartTime = true;

                SocketGuild guild = client.GetGuild(serverId);
                Image coverImage = default;

                var snapshot = await dataScrapingHelper.GetWebsiteSnapshot(link);
                using MemoryStream stream = new(snapshot.Item2 ?? []);
                if (snapshot != default) coverImage = new Image(stream);

                RestGuildEvent restGuildEvent = await guild.GetEventAsync(eventId);
                await restGuildEvent.ModifyAsync(x =>
                   {
                       x.Name = name;
                       if (restGuildEvent.StartTime > DateTime.Now) x.StartTime = amendStartTime ? DateTime.Now.AddSeconds(5) : startTime;
                       x.Description = description;
                       if (endTime.HasValue) x.EndTime = endTime.Value;
                       x.Location = link;
                       if (snapshot != default) x.CoverImage = coverImage;
                   });
                return $"Event: {restGuildEvent.Name} updated.";
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw;
            }
        }
    }
}
