using System;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord.WebSocket;
using Vereesa.Core.Extensions;

namespace Vereesa.Core.Services
{
    public class MovieSuggestionService
    {
        private DiscordSocketClient _discord;

        public MovieSuggestionService(DiscordSocketClient discord)
        {
            _discord = discord;
            discord.Ready += Initialize;
        }

        private async Task Initialize()
        {
            _discord.MessageReceived += HandleMessageReceived;
        }

        private async Task HandleMessageReceived(SocketMessage message)
        {
            string command = message.GetCommand().ToLowerInvariant();

            switch (command) 
            {
                case "!moviesuggest":
                case "!moviesuggestion":
                case "!movie":
                    (string title, string year) titleAndYear = await GetRandomMovieSuggestionAsync();
                    await message.Channel.SendMessageAsync($"Try \"{titleAndYear.title}\", {message.Author.Username}. It was made in {titleAndYear.year} and should be on Netflix.");
                    break;
            }
        }

        private async Task<(string title, string year)> GetRandomMovieSuggestionAsync()
        {
            using (var client = new HttpClient()) 
            {
                HttpResponseMessage result = await client.GetAsync("https://agoodmovietowatch.com/random?netflix=1");
                string response = await result.Content.ReadAsStringAsync();
                response = response.Replace(Environment.NewLine, string.Empty).Replace("\n", string.Empty);
                
                Regex movieTitlePattern = new Regex("<h1>(.+?)<a>");
                Match movieTitleMatch = movieTitlePattern.Match(response);
                Group movieTitleGroup = movieTitleMatch.Groups.Skip(1).FirstOrDefault();

                Regex movieYearPattern = new Regex(@"\((.+?)\)");
                Match movieYearMatch = movieYearPattern.Match(response);
                Group movieYearGroup = movieYearMatch.Groups.Skip(1).FirstOrDefault();
                
                string movieTitle = movieTitleGroup.Value;
                string movieYear = movieYearGroup.Value;

                return (movieTitle, movieYear);
            }
        }
    }
}