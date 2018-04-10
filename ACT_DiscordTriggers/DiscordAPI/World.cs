using System.Collections.Generic;


namespace DiscordAPI
{
    public class World
    {
        public string Name;
        public string DC;
        public string Region;

        public World(string _name, string _dc, string _region)
        {
            Name = _name;
            DC = _dc;
            Region = _region;
        }
    }

    public class Worlds
    {
        public static List<World> GetWorlds()
        {
            var worlds = new List<World>
            {

                // -------------
                // | Primal 11 |
                // -------------
                new World("Behemoth", "Primal", "NA"),
                new World("Brynhildr", "Primal", "NA"),
                new World("Diabolos", "Primal", "NA"),
                new World("Excalibur", "Primal", "NA"),
                new World("Exodus", "Primal", "NA"),
                new World("Famfrit", "Primal", "NA"),
                new World("Hyperion", "Primal", "NA"),
                new World("Lamia", "Primal", "NA"),
                new World("Leviathan", "Primal", "NA"),
                new World("Malboro", "Primal", "NA"),
                new World("Ultros", "Primal", "NA"),

                // ----------------
                // | Elemental 10 | 21
                // ----------------
                new World("Aegis", "Elemental", "JP"),
                new World("Atomos", "Elemental", "JP"),
                new World("Carbuncle", "Elemental", "JP"),
                new World("Garuda", "Elemental", "JP"),
                new World("Gungnir", "Elemental", "JP"),
                new World("Kujata", "Elemental", "JP"),
                new World("Ramuh", "Elemental", "JP"),
                new World("Tonberry", "Elemental", "JP"),
                new World("Typhon", "Elemental", "JP"),
                new World("Unicorn", "Elemental", "JP"),

                // ------------
                // | Chaos 10 | 31
                // ------------
                new World("Cerberus", "Chaos", "EU"),
                new World("Lich", "Chaos", "EU"),
                new World("Louisoix", "Chaos", "EU"),
                new World("Moogle", "Chaos", "EU"),
                new World("Odin", "Chaos", "EU"),
                new World("Omega", "Chaos", "EU"),
                new World("Phoenix", "Chaos", "EU"),
                new World("Ragnarok", "Chaos", "EU"),
                new World("Shiva", "Chaos", "EU"),
                new World("Zodiark", "Chaos", "EU"),

                // -----------
                // | Gaia 11 | 42
                // -----------
                new World("Alexander", "Gaia", "JP"),
                new World("Bahamut", "Gaia", "JP"),
                new World("Durandal", "Gaia", "JP"),
                new World("Fenrir", "Gaia", "JP"),
                new World("Ifrit", "Gaia", "JP"),
                new World("Ridill", "Gaia", "JP"),
                new World("Tiamat", "Gaia", "JP"),
                new World("Ultima", "Gaia", "JP"),
                new World("Valefor", "Gaia", "JP"),
                new World("Yojimbo", "Gaia", "JP"),
                new World("Zeromus", "Gaia", "JP"),

                // -----------
                // | Mana 11 | 53
                // -----------
                new World("Anima", "Mana", "JP"),
                new World("Asura", "Mana", "JP"),
                new World("Belias", "Mana", "JP"),
                new World("Chocobo", "Mana", "JP"),
                new World("Hades", "Mana", "JP"),
                new World("Ixion", "Mana", "JP"),
                new World("Mandragora", "Mana", "JP"),
                new World("Masamune", "Mana", "JP"),
                new World("Pandaemonium", "Mana", "JP"),
                new World("Shinryu", "Mana", "JP"),
                new World("Titan", "Mana", "JP"),

                // -------------
                // | Aether 13 | 66
                // -------------
                new World("Adamantoise", "Aether", "NA"),
                new World("Balmung", "Aether", "NA"),
                new World("Cactuar", "Aether", "NA"),
                new World("Coeurl", "Aether", "NA"),
                new World("Faerie", "Aether", "NA"),
                new World("Gilgamesh", "Aether", "NA"),
                new World("Goblin", "Aether", "NA"),
                new World("Jenova", "Aether", "NA"),
                new World("Mateus", "Aether", "NA"),
                new World("Midgardsormr", "Aether", "NA"),
                new World("Sargatanas", "Aether", "NA"),
                new World("Siren", "Aether", "NA"),
                new World("Zalera", "Aether", "NA")
            };

            return worlds;
        }
    }
}