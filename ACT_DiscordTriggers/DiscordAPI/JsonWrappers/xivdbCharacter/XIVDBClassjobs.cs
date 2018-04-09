using System.Collections.Generic;
using Newtonsoft.Json;

namespace XivDB {
    [JsonObject("classjobs")]
    public class XIVDBClassjobs {
        public List<XIVDBClassJob> class_jobs { get; set; }
    }
}

