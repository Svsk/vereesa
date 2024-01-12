using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Vereesa.Data.Interfaces;
using Vereesa.Data.Models.Reminders;
using Vereesa.Core.Infrastructure;
using Vereesa.Core.Extensions;
using NodaTime;

namespace Vereesa.Core.Services
{
    public class RemindMeService : IBotService
    {
        private readonly IMessagingClient _messaging;
        private readonly IRepository<Reminder> _reminderRepository;
        private const string _commandWord = "!remindme";

        public RemindMeService(
            IMessagingClient messaging,
            IJobScheduler scheduler,
            IRepository<Reminder> reminderRepository
        )
        {
            _messaging = messaging;
            _reminderRepository = reminderRepository;
            scheduler.EveryTenSeconds += AnnounceElapsedIntervalsAsync;
        }

        [OnCommand(_commandWord)]
        public async Task HandleMessageReceived(IMessage receivedMessage)
        {
            if (TryParseReminder(receivedMessage, out var reminder))
            {
                await AddReminderAsync(reminder);
                await AnnounceReminderAddedAsync(receivedMessage.Channel.Id, receivedMessage.Author.Id);
            }
        }

        private IMessageChannel GetChannel(ulong channelId)
        {
            var channel = _messaging.GetChannelById(channelId);
            return (IMessageChannel)channel;
        }

        private async Task AnnounceReminderAddedAsync(ulong channelId, ulong reminderUser)
        {
            var channel = GetChannel(channelId);
            await channel.SendMessageAsync($"OK, <@{reminderUser}>! I'll remind you! :sparkles:");
        }

        private async Task AddReminderAsync(Reminder reminder)
        {
            await _reminderRepository.AddAsync(reminder);
        }

        private bool TryParseReminder(IMessage message, out Reminder reminder)
        {
            reminder = null;
            var msgContent = message.Content;

            if (msgContent.StartsWith(_commandWord))
            {
                msgContent = msgContent.Replace("“", "\"").Replace("”", "\"").Replace("«", "\"").Replace("»", "\"");
            }

            if (msgContent.StartsWith(_commandWord) && msgContent.Contains("\""))
            {
                string reminderMessage = ExtractReminderMessage(msgContent);
                string reminderTime = ExtractReminderTime(msgContent, reminderMessage);
                bool reminderTimeParsed = TryParseReminderTime(reminderTime, out long remindUnixTimestamp);

                reminder = new Reminder();
                reminder.Message = reminderMessage;
                reminder.RemindTime = remindUnixTimestamp;
                reminder.UserId = message.Author.Id;
                reminder.ChannelId = message.Channel.Id;
            }

            return reminder != null;
        }

        private bool TryParseReminderTime(string reminderTime, out long remindUnixTimestamp)
        {
            remindUnixTimestamp = 0;

            try
            {
                var now = DateTimeOffset.UtcNow;
                var remindTime = now;

                if (!TryParseDuration(reminderTime, out var duration))
                {
                    throw new InvalidCastException("Couldn't cast string to duration.");
                }

                remindUnixTimestamp = SystemClock.Instance.GetCurrentInstant().Plus(duration).ToUnixTimeSeconds();
            }
            catch
            {
                return false;
            }

            return true;
        }

        private bool TryParseDuration(string reminderTime, out Duration result)
        {
            var split = reminderTime.Split(" ");
            var number = int.Parse(split[0]);
            var unit = split[1].ToLowerInvariant();

            if (unit.Contains("second"))
            {
                result = Duration.FromSeconds(number);
            }
            else if (unit.Contains("minute"))
            {
                result = Duration.FromMinutes(number);
            }
            else if (unit.Contains("hour"))
            {
                result = Duration.FromHours(number);
            }
            else if (unit.Contains("day"))
            {
                result = Duration.FromDays(number);
            }
            else
            {
                result = Duration.MaxValue;
                return false;
            }

            return true;
        }

        private string ExtractReminderMessage(string msgContent)
        {
            return msgContent.Split("\"").Skip(1).FirstOrDefault();
        }

        private string ExtractReminderTime(string msgContent, string reminderMessage)
        {
            return msgContent
                .Replace($"{_commandWord} ", string.Empty)
                .Replace($"\"{reminderMessage}\"", string.Empty)
                .ToLowerInvariant();
        }

        private async Task AnnounceElapsedIntervalsAsync()
        {
            var overdueReminders = await GetOverdueRemindersAsync();

            foreach (var reminder in overdueReminders)
            {
                if (await TryAnnounceReminderAsync(reminder))
                {
                    if (reminder.IsPeriodic)
                    {
                        reminder.RemindTime = Instant
                            .FromUnixTimeSeconds(reminder.RemindTime)
                            .Plus(Duration.FromSeconds(reminder.Interval))
                            .ToUnixTimeSeconds();

                        await UpdateReminderAsync(reminder);
                    }
                    else
                    {
                        await RemoveReminderAsync(reminder);
                    }
                }
            }
        }

        private async Task RemoveReminderAsync(Reminder reminder)
        {
            await _reminderRepository.DeleteAsync(reminder);
        }

        private async Task UpdateReminderAsync(Reminder reminder)
        {
            await _reminderRepository.AddOrEditAsync(reminder);
        }

        private async Task<bool> TryAnnounceReminderAsync(Reminder reminder)
        {
            try
            {
                var channel = GetChannel(reminder.ChannelId);

                if (channel == null)
                {
                    // could not find channel - deleted?
                    return true;
                }

                if (!reminder.IsPeriodic)
                {
                    await channel.SendMessageAsync($"<@{reminder.UserId}> Remember! {reminder.Message}");
                }
                else
                {
                    // Periodic reminders have the target in the message.
                    await channel.SendMessageAsync($"{reminder.Message}");
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        private async Task<List<Reminder>> GetOverdueRemindersAsync()
        {
            return (await _reminderRepository.GetAllAsync())
                .Where(r => r.RemindTime < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                .ToList();
        }

        [AsyncHandler]
        [OnCommand("!reminder delete")]
        public async Task DeleteReminder(IMessage message)
        {
            var reminders = (await _reminderRepository.GetAllAsync())
                .Where(r => r.UserId == message.Author.Id)
                .ToList();

            if (!reminders.Any())
            {
                await message.Channel.SendMessageAsync("You have no active reminders! ✨");
                return;
            }

            var reminderSelction =
                $"Which reminder would you like me to delete?\n"
                + string.Join("\n", reminders.Select((item, index) => $"[{index + 1}] {item.Message}"))
                + "\n"
                + "Select by responding with the number corresponding to the item you want to remove. Like this `1`.";

            var response = await _messaging.Prompt(message.Author, reminderSelction, message.Channel, 30000);

            if (
                !int.TryParse(response.Content, out var selectedItemNumber)
                || reminders.Count < selectedItemNumber
                || selectedItemNumber < 1
            )
            {
                await message.Channel.SendMessageAsync("I don't understand which reminder you mean...");
                return;
            }

            var itemToDelete = reminders[selectedItemNumber - 1];
            await RemoveReminderAsync(itemToDelete);

            await message.Channel.SendMessageAsync("OK, I deleted that reminder! ✨");
        }

        [AsyncHandler]
        [OnCommand("!reminder create periodic")]
        [Authorize("Officer")]
        public async Task CreatePeriodicReminder(IMessage message)
        {
            var abort = new Func<string, Task>(
                async (reason) =>
                {
                    await message.Channel.SendMessageAsync($"❌ {reason} Please try again.");
                }
            );

            var prompt = new Func<string, int, Task<IMessage>>(
                async (promptMessage, timeout) =>
                {
                    return await _messaging.Prompt(message.Author, $"🎗 {promptMessage}", message.Channel, timeout);
                }
            );

            var intervalLength = await prompt("How long between each reminder?", 15000);
            if (!TryParseDuration(intervalLength.Content, out var duration))
            {
                await abort("I wasn't able to figure out the interval. Please try again.");
                return;
            }

            var startTime = await prompt(
                "When should the first reminder be? I run on Karazhan realm time. Format it like this `2021-07-09 22:19:01`.",
                60000
            );
            ZonedDateTime dateTime;
            try
            {
                dateTime = startTime.Content.ParseToZonedDateTime("yyyy-MM-dd HH:mm:ss", "Europe/Paris");
            }
            catch
            {
                await abort("I wasn't able to figure out the start time.");
                return;
            }

            var reminderTarget = await prompt("Who should I remind?", 15000);
            if (reminderTarget == null)
            {
                await abort("I wasn't able to figure out who to remind.");
                return;
            }

            var reminderText = await prompt("What should the reminder say?", 60000);
            if (reminderText == null)
            {
                await abort("I wasn't able to figure out what the reminder should say.");
                return;
            }

            var channel = await prompt("What channel should the reminder be sent to?", 30000);
            if (
                channel == null
                || !ulong.TryParse(channel.Content.Replace("<#", "").Replace(">", "").Trim(), out var channelId)
            )
            {
                await abort("I wasn't able to figure out what channel to send the reminder to. " + channel?.Content);
                return;
            }

            var reminder = new Reminder();
            reminder.IsPeriodic = true;
            reminder.Interval = duration.TotalSeconds;
            reminder.RemindTime = dateTime.ToInstant().ToUnixTimeSeconds();
            reminder.UserId = message.Author.Id;
            reminder.ChannelId = channelId;
            reminder.Message = $"{reminderTarget.Content}: {reminderText.Content}";
            await AddReminderAsync(reminder);

            await message.Channel.SendMessageAsync(
                $"OK, I'll remind {reminderTarget.Content} that {reminderText.Content} every {duration} starting on {dateTime}."
            );
        }
    }
}
