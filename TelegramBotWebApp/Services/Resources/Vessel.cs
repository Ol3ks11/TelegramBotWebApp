using Newtonsoft.Json;

public class Vessel
{
    public Vessel()
    {
        imoNumber = "blank";
        name = "blank";
    }

    [JsonProperty("vesselIMONumber")]
    public string imoNumber { get; set; }

    [JsonProperty("vesselLongName")]
    public string name { get; set; }
}