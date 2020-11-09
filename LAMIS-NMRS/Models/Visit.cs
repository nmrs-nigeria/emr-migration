using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace LAMIS_NMRS.Models
{
  
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class Visit
    {
        public string patient { get; set; }
        public string visitType = "7b0f5697-27e3-40c4-8bae-f4049abfb4ed";//only Hospital Visit available in table
        public string startDatetime { get; set; }
        public string location { get; set; }       
        public string stopDatetime { get; set; }
        public List<string> encounters { get; set; }
    }

    public class VisitQueryModel
    {
        public string uuid { get; set; }
        public string visitdate { get; set; }
        public string identifier { get; set; }

    }
}
