using ACT_DiscordTriggers.JsonWrappers;
using Discord;
using Discord.Commands;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using XivDB;

namespace ACT_DiscordTriggers
{
    public class DiscordTriggers : ModuleBase
    {
        private static DiscordPlugin Bot { get; set; }

        public static void Init(DiscordPlugin bot)
        {
            if(Bot == null)
                Bot = bot;
        }

        private static HttpClient NewClient()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return client;
        }

        private static string GetClass(string name)
        {
            switch (name)
            {
                case "WhiteMage":
                    return "<:whm:343479909160583168>";
                case "Astrologian":
                    return "<:ast:343479455005540353>";
                case "Scholar":
                    return "<:sch:343479909236080640>";
                case "Paladin":
                    return "<:pld:343479908854661121>";
                case "Warrior":
                    return "<:war:343479908879826945>";
                case "DarkKnight":
                    return "<:drk:343479908980359168>";
                case "Bard":
                    return "<:brd:343479908757929995>";
                case "Machinist":
                    return "<:mch:343479908879826955>";
                case "BlackMage":
                    return "<:blm:343479267394322433>";
                case "RedMage":
                    return "<:rdm:343479908741283852>";
                case "Summoner":
                    return "<:smn:343479908900667394>";
                case "Dragoon":
                    return "<:drg:343479908632231937>";
                case "Monk":
                    return "<:mnk:343479908980228106>";
                case "Ninja":
                    return "<:nin:343479908850466818>";
                case "Samurai":
                    return "<:sam:343479909043273730>";
                default:
                    return ":poop:";
            }
        }

        [Command("fflogs")]
        [Summary("Drellis speciality")]
        public async Task Fflogs(string server, [Remainder] string name)
        {
            await Fflog(server, name);
        }

        [Command("fflog")]
        [Summary("Drellis speciality")]
        public async Task Fflog(string server, [Remainder] string name)
        {
            string character = string.Empty;

            foreach (string part in name.Split(' '))
                character += (string.IsNullOrEmpty(character) ? "" : " ") + part.First().ToString().ToUpper() + part.Substring(1);


            var worlds = Worlds.GetWorlds();
            var worldResult = from wrld in worlds
                              where wrld.Name.ToLower().Contains(server.ToLower())
                              select wrld;
            var world = worldResult.First();

            var url = new Uri($"https://api.xivdb.com/search?one=characters&string={character}&pretty=1");
            HttpClient client = NewClient();

            string responseBody = await client.GetStringAsync(url);
            var xivdbCharacters = JsonConvert.DeserializeObject<CharacterSearch>(responseBody);

            var results = from charResult in xivdbCharacters.Characters.Results
                          where charResult.Name.ToLower() == character.ToLower() && charResult.Server == server
                          select charResult;

            string playerLink = "";
            string playerDB = "";
            XIVDBCharacter xivdbCharacter = null;
            if (results.Count() > 0)
            {
                playerLink = results.First().Url_api;
                playerDB = results.First().Url_xivdb;

                url = new Uri(playerLink);
                responseBody = await client.GetStringAsync(url);
                xivdbCharacter = JsonConvert.DeserializeObject<XIVDBCharacter>(responseBody);
            }



            url = new Uri($"https://www.fflogs.com/v1/parses/character/{character}/{world.Name}/{world.Region}/?api_key={Bot.txtFFLogsToken.Text}");
            try
            {
                responseBody = await client.GetStringAsync(url);
            }
            catch (Exception)
            {
                await Context.Channel.SendMessageAsync($"No parses found for {character}.");
                return;
            }
            List<Parses> parses = new List<Parses>();

            try
            {
                parses = JsonConvert.DeserializeObject<List<Parses>>(responseBody);
            }
            catch
            {
                await Context.Channel.SendMessageAsync($"Hidden parses detected, {character} is a scrub.");
                return;
            }
            string id = "0";
            if (parses.Count > 0)
            {
                if (parses[0].Specs.Count > 0)
                {
                    foreach (DatumFight data in parses[0].Specs[0].Data)
                    {
                        if (data.character_name == character)
                            id = data.character_id.ToString();
                    }
                }
            }

            StringBuilder des = new StringBuilder();

            if (parses.Count == 0)
                des.AppendLine("No encounters found.");

            foreach (Parses parse in parses)
            {
                des.AppendLine($"__**{parse.Name}**__");
                foreach (var spec in parse.Specs)
                {
                    des.AppendLine($"{GetClass(spec.spec)} DPS <{string.Format("{0:0.#}", spec.Best_Persecondamount)}> Top <{string.Format("{0:0.#}", spec.Best_Historical_Percent)}%> Avg <{string.Format("{0:0.#}", spec.Historical_Median)}%> Kills <{spec.Data.Count}>");
                }
            }

            var embed = new EmbedBuilder()
            .WithTitle($"Click Here - FFLogs Info")
            .WithUrl($"https://www.fflogs.com/character/id/{id}")
            .WithThumbnailUrl(xivdbCharacter == null ? "" : xivdbCharacter.Avatar)
            //.WithImageUrl(xivdbCharacter == null ? "" : xivdbCharacter.portrait)
            .WithFooter(new EmbedFooterBuilder()
            .WithText($"{character} - {world.Name} - {world.Region} | {(xivdbCharacter == null ? "Unknown" : xivdbCharacter.Data.race)}"))
            .WithColor(new Color(102, 255, 222))
            .WithDescription(des.ToString())
            .Build();

            await Context.Channel.SendMessageAsync("", embed: embed);
        }

        [Command("status")]
        [Summary("Drellis speciality")]
        public async Task Status([Remainder] string text)
        {
            Bot.SetGameAsync(text);
            await Context.Channel.SendMessageAsync($"Status updated to - Playing {text}");
        }


    }
}
