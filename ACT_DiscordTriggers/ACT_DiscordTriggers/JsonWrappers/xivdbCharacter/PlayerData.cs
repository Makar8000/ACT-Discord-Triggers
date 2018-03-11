using Newtonsoft.Json;

namespace XivDB {
    public class data {
        public string id { get; set; }
        public string name { get; set; }
        public string server { get; set; }
        public string title { get; set; }
        public string avatar { get; set; }
        public string portrait { get; set; }
        public string biography { get; set; }
        public string race { get; set; }
        public string clan { get; set; }
        public string gender { get; set; }
        public string nameday { get; set; }
        public Guardian guardian { get; set; }
        public City city { get; set; }
        public Grand_Company grand_company { get; set; }
        [JsonProperty("classjobs")]
        public XIVDBClassjobs classjobs { get; set; }
        public Mounts mounts { get; set; }
        public Minions minions { get; set; }
        public Active_Class active_class { get; set; }
    }
}
