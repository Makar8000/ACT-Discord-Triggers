using Newtonsoft.Json;

namespace XivDB {
    public class XIVDBCharacter {
        public int Lodestone_id { get; set; }
        public string Name { get; set; }
        public string Server { get; set; }
        public string Avatar { get; set; }
        public string Added { get; set; }
        public string Last_updated { get; set; }
        public string Last_synced { get; set; }
        public string Data_last_changed { get; set; }
        public string Data_hash { get; set; }
        public double Update_count { get; set; }
        public string Achievements_last_updated { get; set; }
        public string Achievements_last_changed { get; set; }
        public double Achievements_public { get; set; }
        public double Achievements_score_reborn { get; set; }
        public double Achievements_score_legacy { get; set; }
        public double Achievements_score_reborn_total { get; set; }
        public double Achievements_score_legacy_total { get; set; }
        public string Deleted { get; set; }
        public int Priority { get; set; }
        public int Patch { get; set; }
        [JsonProperty("data")]
        public data Data { get; set; }
        public Grand_Companies Grand_companies { get; set; }
        public string Portrait { get; set; }
        public string Last_active { get; set; }
        public string Url { get; set; }
        public string Url_api { get; set; }
        public string Url_xivdb { get; set; }
        public string Url_lodestone { get; set; }
        public string Url_type { get; set; }
        public double Achievements_score_reborn_percent { get; set; }
        public double Achievements_score_legacy_percent { get; set; }
        public Extras Extras { get; set; }
    }
}


