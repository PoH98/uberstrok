using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace UberStrok.WebServices.AspNetCore.Core.Discord
{
    internal class CoreDiscord
    {
        private static DiscordSocketClient client;

        private static ulong lobbychannel = 0uL;

        private static ulong userloginchannel = 0uL;

        private static readonly List<ulong> allowedChannels = new List<ulong>();

        private static string token = null;

        private static ulong AltDentifierChannel;

        private static ulong LeaderboardChannel;

        private static ulong CommandChannel;

        public static string emptyline = Environment.NewLine + "_ _" + Environment.NewLine + "_ _" + Environment.NewLine;

        public static void Initialise()
        {
            UDPListener.BeginListen();
            Console.WriteLine("UDP Listener Started.\n");
            MainAsync();
        }

        public static async void MainAsync()
        {
            try
            {
                GetConfig();
                bool exit = false;
                if (string.IsNullOrEmpty(token))
                {
                    Console.WriteLine(Environment.NewLine + "Discord bot token missing/invalid. Skipping....." + Environment.NewLine);
                    exit = true;
                }
                if (!exit && (lobbychannel == 0L || userloginchannel == 0L))
                {
                    Console.WriteLine(Environment.NewLine + "Invalid Discord Channel ID. Couldnt parse data to ulong. Skipping....." + Environment.NewLine);
                    exit = true;
                }
                if (!exit)
                {
                    Console.WriteLine(token);
                    client = new DiscordSocketClient();
                    await client.LoginAsync(TokenType.Bot, token);
                    await client.StartAsync();
                    client.MessageReceived += MessageReceived;
                    Console.WriteLine(Environment.NewLine + "Discord bot initialised." + Environment.NewLine);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static void GetConfig()
        {
            try
            {
                string path = Path.Combine(Directory.GetCurrentDirectory(), "assets", "configs", "discord.config");
                if (File.Exists(path))
                {
                    string[] discord = File.ReadAllLines(path);
                    string[] array = discord;
                    foreach (string line in array)
                    {
                        string data = line.Trim();
                        if (data.StartsWith("lobbychannel:"))
                        {
                            lobbychannel = ulong.Parse(data.Replace("lobbychannel:", ""));
                        }
                        else if (data.StartsWith("userloginchannel:"))
                        {
                            userloginchannel = ulong.Parse(data.Replace("userloginchannel:", ""));
                        }
                        else if (data.StartsWith("token:"))
                        {
                            token = data.Replace("token:", "");
                        }
                        else if (data.StartsWith("prefix:"))
                        {
                            UberBeatDiscord.prefix = data.Replace("prefix:", "");
                        }
                        else if (data.StartsWith("AltDentifier:"))
                        {
                            AltDentifierChannel = ulong.Parse(data.Replace("AltDentifier:", ""));
                            allowedChannels.Add(AltDentifierChannel);
                        }
                        else if (data.StartsWith("Leaderboard:"))
                        {
                            LeaderboardChannel = ulong.Parse(data.Replace("Leaderboard:", ""));
                            allowedChannels.Add(LeaderboardChannel);
                        }
                        else if (data.StartsWith("CommandChannel:"))
                        {
                            CommandChannel = ulong.Parse(data.Replace("CommandChannel:", ""));
                            allowedChannels.Add(CommandChannel);
                        }
                        /*else if (data.Contains(":") && ulong.TryParse(data.Substring(data.IndexOf(":") + 1), out id))
						{
							allowedChannels.Add(id);
						}*/
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString() + "\n\nSkipping discord bot.");
            }
        }

        private static async Task MessageReceived(SocketMessage message)
        {
            if (!allowedChannels.Contains(message.Channel.Id))
            {
                return;
            }
            if (message.Channel.Id == AltDentifierChannel)
            {
                if (message.Content != "?verify")
                {
                    await message.DeleteAsync();
                    return;
                }
            }
            if (string.IsNullOrEmpty(UberBeatDiscord.prefix) || !message.Content.StartsWith(UberBeatDiscord.prefix))
            {
                return;
            }
            UberBeatDiscord Bot = new UberBeatDiscord();
            string reply = Bot.Reply(message.Content.Split(' '));
            if (string.IsNullOrEmpty(reply))
            {
                return;
            }
            if (!reply.Contains("|@@|@|"))
            {
                _ = await message.Channel.SendMessageAsync(emptyline + reply);
                return;
            }
            _ = await message.Channel.SendMessageAsync(emptyline);
            string[] replies = reply.Split(new string[1]
            {
                "|@@|@|"
            }, StringSplitOptions.None);
            string[] array = replies;
            foreach (string text in array)
            {
                _ = await message.Channel.SendMessageAsync("```" + text + "```");
            }
        }

        [Command("say")]
        public static async Task SendChannel([Remainder] string message)
        {
            SocketTextChannel channel = client.GetChannel(lobbychannel) as SocketTextChannel;
            _ = await channel.SendMessageAsync(emptyline + message);
        }

        [Command("say")]
        public static async Task SendLoginLog([Remainder] string message)
        {
            SocketTextChannel channel = client.GetChannel(userloginchannel) as SocketTextChannel;
            _ = await channel.SendMessageAsync(emptyline + message);
        }
    }
}
