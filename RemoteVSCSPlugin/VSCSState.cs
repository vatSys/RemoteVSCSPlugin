using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using vatsys;

namespace RemoteVSCSPlugin
{
    public class VSCSState
    {
        public bool Connected { get; set; }
        public bool Transmitting { get; set; }
        public bool Group { get; set; }
        public bool AllToSpeaker { get; set; }
        public bool TonesToSpeaker { get; set; }

        public IList<Line> Lines { get; set; }
        public IList<Frequency> Frequencies { get; set; }   

        public string Serialize()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        public static VSCSState Deserialize(string json)
        {
            return JsonConvert.DeserializeObject<VSCSState>(json);
        }
    }

    public class Line
    {
        public int Id { get; set; }
        public string Callsign { get; set; }
        public string Name { get;set; }
        public string FullName { get; set; }
        public VSCSLineTypes Type { get; set; }
        public VSCSLineStates State { get; set; }
        public bool External { get; set; }
        
        [JsonIgnore]
        public VSCSLine VSCSLine { get; set; }

        public Line()
        {

        }

        public Line(VSCSLine vscsLine)
        {
            Id = vscsLine.Name.GetHashCode() + vscsLine.Type.GetHashCode();
            Callsign = vscsLine.Name;
            Name = vscsLine.Sector?.Name;
            FullName = vscsLine.Sector?.FullName;
            Type = vscsLine.Type;
            State = vscsLine.State;
            External = vscsLine.External;
            VSCSLine = vscsLine;
        }
    }

    public class Frequency
    {
        public int Id { get; set;  }
        public string Name { get; set; }
        public string FriendlyName { get; set; }
        public uint Hertz { get; set; }
        public uint AliasHertz { get; set; }
        public IList<string> TextMessages { get; set; }
        public bool Receive { get; set; }
        public bool Receiving { get; set; }
        public bool Transmit { get; set; }

        [JsonIgnore]
        public VSCSFrequency VSCSFrequency { get; set; }

        public Frequency() { }

        public Frequency(VSCSFrequency vscsFreq)
        {
            Id = vscsFreq.Name.GetHashCode() + vscsFreq.GetFSDFrequency();
            Name = vscsFreq.Name;
            FriendlyName = vscsFreq.FriendlyName;
            Hertz = vscsFreq.Frequency;
            AliasHertz = vscsFreq.AliasFrequency;
            TextMessages = vscsFreq.TextMessages.ToArray();
            Receive = vscsFreq.Receive;
            Receiving = vscsFreq.Receiving;
            Transmit = vscsFreq.Transmit;
            VSCSFrequency = vscsFreq;
        }
    }
}
