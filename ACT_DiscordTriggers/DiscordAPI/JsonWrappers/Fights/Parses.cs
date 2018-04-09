using System.Collections.Generic;

namespace DiscordAPI.JsonWrappers
{
    public class Parses {
        public int Difficulty { get; set; }
        public int Size { get; set; }
        public int Kill { get; set; }
        public string Name { get; set; }
        public List<Spec> Specs { get; set; }
        public bool Variable { get; set; }
        public int Partition { get; set; }
    }
}