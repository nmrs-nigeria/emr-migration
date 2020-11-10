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
        public string person { get; set; }
        public string obsDatetime { get; set; }
        public string concept { get; set; }
        public string location { get; set; }
        public string order { get; set; }
        public string encounter { get; set; }
        public string accessionNumber { get; set; }
        public List<Obs> groupMembers { get; set; }
        public string valueCodedNam { get; set; }
        public string comment { get; set; }
        public string voided { get; set; }
        public string value { get; set; }
        public string valueModifier { get; set; }
        public string formFieldPath { get; set; }
        public string formFieldNamespace { get; set; }
    }

    public class ConceptUUID
    {
        public string concept_id { get; set; }
        public string UUID { get; set; }
    }
}