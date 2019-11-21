using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace net.bootscreen.fhem
{
    [Serializable]
    internal class FHEMcredentials
    {
        [JsonProperty(PropertyName = "fhem_ip")]
        public string FHEM_ip { get; set; }
        [JsonProperty(PropertyName = "fhem_port")]
        public int FHEM_port { get; set; }
        [JsonProperty(PropertyName = "fhem_username")]
        public string FHEM_user { get; set; }
        [JsonProperty(PropertyName = "fhem_password")]
        public string FHEM_pw { get; set; }
        [JsonProperty(PropertyName = "fhem_csrf")]
        public string FHEM_csrf { get; set; }
    }
}
