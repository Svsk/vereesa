using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;

namespace Vereesa.Core.Tests.Mocks 
{
    public class ChannelMock : IChannel, IMessageChannel
    {
        public List<IMessage> Messages { get; private set; } = new List<IMessage>();



        public string Name => throw new NotImplementedException();

        public DateTimeOffset CreatedAt => throw new NotImplementedException();

        public ulong Id => throw new NotImplementedException();

        public Task DeleteMessageAsync(ulong messageId, RequestOptions options = null)
        {
            throw new NotImplementedException();
        }

        public Task DeleteMessageAsync(IMessage message, RequestOptions options = null)
        {
            throw new NotImplementedException();
        }

        public IDisposable EnterTypingState(RequestOptions options = null)
        {
            throw new NotImplementedException();
        }

        public async Task<IMessage> GetMessageAsync(ulong id, CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null)
        {
            return Messages.FirstOrDefault();
        }

        public IAsyncEnumerable<IReadOnlyCollection<IMessage>> GetMessagesAsync(int limit = 100, CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null)
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<IReadOnlyCollection<IMessage>> GetMessagesAsync(ulong fromMessageId, Direction dir, int limit = 100, CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null)
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<IReadOnlyCollection<IMessage>> GetMessagesAsync(IMessage fromMessage, Direction dir, int limit = 100, CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyCollection<IMessage>> GetPinnedMessagesAsync(RequestOptions options = null)
        {
            throw new NotImplementedException();
        }

        public Task<IUser> GetUserAsync(ulong id, CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null)
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<IReadOnlyCollection<IUser>> GetUsersAsync(CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null)
        {
            throw new NotImplementedException();
        }

        public Task<IUserMessage> SendFileAsync(string filePath, string text = null, bool isTTS = false, RequestOptions options = null)
        {
            throw new NotImplementedException();
        }

        public Task<IUserMessage> SendFileAsync(Stream stream, string filename, string text = null, bool isTTS = false, RequestOptions options = null)
        {
            throw new NotImplementedException();
        }

        public Task<IUserMessage> SendFileAsync(string filePath, string text = null, bool isTTS = false, Embed embed = null, RequestOptions options = null, bool isSpoiler = false)
        {
            throw new NotImplementedException();
        }

        public Task<IUserMessage> SendFileAsync(Stream stream, string filename, string text = null, bool isTTS = false, Embed embed = null, RequestOptions options = null, bool isSpoiler = false)
        {
            throw new NotImplementedException();
        }

        public async Task<IUserMessage> SendMessageAsync(string text, bool isTTS = false, Embed embed = null, RequestOptions options = null)
        {
            var message = new MessageMock(embed);
            message.Content = text;
            this.Messages.Add(message);
            return message;
        }

        public Task TriggerTypingAsync(RequestOptions options = null)
        {
            throw new NotImplementedException();
        }
    }
}