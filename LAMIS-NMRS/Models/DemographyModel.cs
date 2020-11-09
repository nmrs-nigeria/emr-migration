using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace LAMIS_NMRS.Models
{
   
    public class PatientDemography
    {
        public List<PersonName> names { get; set; }
        public string gender { get; set; }
        public double age { get; set; }
        public string birthdate { get; set; }
        public bool birthdateEstimated { get; set; }
        [JsonIgnore]
        public string identifier { get; set; }
        [JsonIgnore]
        public string HospitalNo { get; set; }
        [JsonIgnore]
        public string dateEnrolled { get; set; }
        [JsonIgnore]
        public Guid UniqueId { get; set; }
        public bool dead { get; set; }
        public string deathDate { get; set; }
        public string causeOfDeath { get; set; }
        public bool deathdateEstimated { get; set; }
        public string birthtime { get; set; }        
        public List<Personaddress> addresses { get; set; }
        //public List<PatientAttributes> attributes { get; set; }
    }

    public class PatientAttributes
    {
        public string attributeType { get; set; }
        public string value { get; set; }
        public string hydratedObject { get; set; }
    }
    
    public class Patient
    {
        public PatientDemography person { get; set; }
        public List<Identifiers> identifiers { get; set; }
        public List<Encounter> Encounters { get; set; }
    }

    public class PatientInfo
    {
        public string person { get; set; } //UUID
        public List<Identifiers> identifiers { get; set; }
    }


    public class Identifiers
    {
        public string identifier { get; set; }
        public string identifierType { get; set; } // uuid;
        public bool preferred { get; set; } //= bool;
        public string location { get; set; }  //= uuid;
    }

    public class PatientProgram
    {
        public string patient { get; set; } //uuid
        public string program = "9083deaa-f37f-44b3-9046-b87b134711a1"; //uuid
        public string dateEnrolled { get; set; }
        public string location { get; set; } //= "7f65d926-57d6-4402-ae10-a5b3bcbf7986";
        public bool voided = false;
    }

    public class PersonName
    {
        public bool preferred = true;
        public string givenName { get; set; }
        public string middleName { get; set; }
        public string familyName { get; set; }
        public string familyName2 { get; set; }
        public string prefix { get; set; }
        public string familyNamePrefix { get; set; }
        public string familyNameSuffix { get; set; }
        public string degree { get; set; }
    }

    public class Personaddress
    {
        public bool preferred { get; set; }
        public string address1 { get; set; }
        public string address2 { get; set; }
        public string cityVillage { get; set; }
        public string stateProvince { get; set; }
        public string country { get; set; }
        public string postalCode { get; set; }
        public string countyDistrict { get; set; }
        public string address3 { get; set; }
        public string address4 { get; set; }
        public string address5 { get; set; }
        public string address6 { get; set; }
        public string startDate { get; set; }
        public string endDate { get; set; }
        public string latitude { get; set; }
        public string longitude { get; set; }
    }
}

