namespace ACT_DiscordTriggers.JsonWrappers
{

    public class DatumFight {
        public int character_id { get; set; }
        public string character_name { get; set; }
        public double persecondamount { get; set; }
        public string ilvl { get; set; }
        public int duration { get; set; }
        public object start_time { get; set; }
        public string report_code { get; set; }
        public int report_fight { get; set; }
        public int ranking_id { get; set; }
        public string guild { get; set; }
        public int total { get; set; }
        public string rank { get; set; }
        public double percent { get; set; }
        public int exploit { get; set; }
        public bool banned { get; set; }
        public double historical_count { get; set; }
        public double historical_percent { get; set; }
    }
}