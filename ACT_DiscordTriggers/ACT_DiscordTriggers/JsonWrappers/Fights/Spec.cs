using System.Collections.Generic;

namespace ACT_DiscordTriggers.JsonWrappers
{
    public class Spec
{
    public string @Class { get; set; }
    public string spec { get; set; }
    public bool Combined { get; set; }
    public List<DatumFight> Data { get; set; }
    public double Best_Persecondamount { get; set; }
    public int Best_Duration { get; set; }
    public double Best_Historical_Percent { get; set; }
    public double Best_Allstar_Points { get; set; }
    public double Best_Combined_Allstar_Points { get; set; }
    public double Possible_Allstar_Points { get; set; }
    public double Historical_Total { get; set; }
    public double Historical_Median { get; set; }
    public double Historical_Avg { get; set; }
}
}