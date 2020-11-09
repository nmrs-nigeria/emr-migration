using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace LAMIS_NMRS.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class Encounter
    {
        public string patient { get; set; }
        public string encounterType { get; set; }
        public string encounterDatetime { get; set; }
        public string location { get; set; }  //"7f65d926-57d6-4402-ae10-a5b3bcbf7986";
        public string form { get; set; } //uuid
        //public string provider { get; set; }
        public List<Obs> obs { get; set; }
        public string visit { get; set; }
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class Obs
    {        
        public string concept { get; set; }      

      //  [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<Obs> groupMembers { get; set; }
        public string obsGroup { get; set; } //conceptId
        public string valueDatetime { get; set; }
        public string valueNumeric { get; set; }
        public string valueText { get; set; }
        public string valueCoded { get; set; } //conceptId       
        public string valueComplex { get; set; }
        public string valueBoolean { get; set; }
    }

    public class ConceptUUID
    {
        public string concept_id { get; set; }
        public string UUID { get; set; }
    }
}