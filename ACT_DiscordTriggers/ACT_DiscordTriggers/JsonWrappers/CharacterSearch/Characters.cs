using System.Collections.Generic;

namespace ACT_DiscordTriggers.JsonWrappers
{

    public class Characters {
        public List<CharacterResult> Results { get; set; }
        public int Total { get; set; }
        public Paging Paging { get; set; }
    }
}