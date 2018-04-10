using System.Collections.Generic;

namespace DiscordAPI.JsonWrappers
{
    public class Paging {
        public int Page { get; set; }
        public int Total { get; set; }
        public List<int> Pages { get; set; }
        public int Next { get; set; }
        public int Prev { get; set; }
    }

}