using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RemoteVSCSPlugin
{
    public class VSCSCommand
    {
        [JsonProperty("CommandName")]
        public string Name { get; set; }
        public object Value { get; set; }
    }
}
