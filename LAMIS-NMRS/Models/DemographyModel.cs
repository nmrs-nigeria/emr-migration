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
    }
    
    public class Patient
    {
        public PatientDemography person { get; set; }
        public List<Identifiers> identifiers { get; set; }
        public List<Encounter> Encounters { get; set; }
        public PatientProgram PatientProgram { get; set; }
        public List<PatientAttributes> attributes { get; set; }
    }

    public class PatientInfo
    {
        public string person { get; set; } //UUID
        public List<Identifiers> identifiers { get; set; }
    }

    public class LamisPatient
    {
        public long patient_id { get; set; }
        public long facility_id { get; set; }
        public string hospital_num { get; set; }
        public string unique_id { get; set; }
        public string surname { get; set; }
        public string other_names { get; set; }
        public string gender { get; set; }
        public string date_birth { get; set; }
        public string age { get; set; }        
        public string age_unit  { get; set; }
        public string marital_status { get; set; }
        public string education { get; set; }
        public string occupation { get; set; }
        public string address { get; set; }
        public string phone { get; set; }
        public string state { get; set; }
        public string lga { get; set; }
        public string next_kin { get; set; }
        public string address_kin { get; set; }
        public string phone_kin { get; set; }
        public string relation_kin { get; set; }
        public string entry_point { get; set; }
        public string target_group { get; set; }
        public string date_confirmed_hiv { get; set; }
        public string date_enrolled_pmtct { get; set; }
        public string source_referral { get; set; }
        public string time_hiv_diagnosis { get; set; }
        public string tb_status { get; set; }
        public string pregnant { get; set; }
        public string breastfeeding { get; set; }
        public string date_registration { get; set; }
        public string status_registration { get; set; }
        public string enrollment_setting { get; set; }
        public string casemanager_id  { get; set; }
        public string communitypharm_id  { get; set; }
        public string date_started  { get; set; }
        public string current_status { get; set; }
        public string date_current_status  { get; set; }
        public string regimentype  { get; set; }
        public string regimen  { get; set; }
        public string last_clinic_stage { get; set; }
        public string last_viral_load  { get; set; }
        public string last_cd4  { get; set; }
        public string last_cd4p  { get; set; }
        public string date_last_cd4 { get; set; }
        public string date_last_viral_load   { get; set; }
        public string viral_load_due_date { get; set; }
        public string viral_load_type { get; set; }
        public string date_last_refill { get; set; }
        public string date_next_refill { get; set; }
        public string last_refill_duration  { get; set; }
        public string last_refill_setting { get; set; }
        public string date_last_clinic  { get; set; }
        public string date_next_clinic { get; set; }
        public string date_tracked { get; set; }
        public string outcome  { get; set; }
        public string cause_death  { get; set; }
        public string agreed_date  { get; set; }
        public string send_message  { get; set; }
        public string time_stamp   { get; set; }
        public string uploaded  { get; set; }
        public string time_uploaded  { get; set; }
        public string user_id { get; set; }
        public string id_uuid  { get; set; }
        public string partnerinformation_id  { get; set; }
        public string hts_id   { get; set; }
        public string uuid { get; set; }
        public string archived { get; set; }
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
        public string program = "9083deaa-f37f-44b3-9046-b87b134711a1"; //HIV Treatment Services
        public string dateEnrolled { get; set; }
        public string dateCompleted { get; set; }
        public string location = "6351fcf4-e311-4a19-90f9-35667d99a8af"; //Registration Desk
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

