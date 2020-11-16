using System;
using System.Collections.Generic;
using System.Text;

namespace LAMIS_NMRS.Models
{
    public class Regimen
    {
        public string Variable { get; set; }
        public string Values { get; set; }
        public string Answers { get; set; }
        public string AnswerCode { get; set; }
        public string QuestionID { get; set; }
        public string NMRSQuestionConceptID { get; set; }
        public string NMRSAnswerConceptID { get; set; }
    }

    public class Drug
    {
        public string ABBREV { get; set; }
        public string NAME { get; set; }
        public string STRENGTH { get; set; }
        public string MORNING { get; set; }
        public string AFTERNOON { get; set; }
        public string EVENING { get; set; }
        public string OPENMRSQUESTIONCONCEPT { get; set; }
        public string OPENMRSDRUGCONCEPTID { get; set; }
        public string STRENGTHCONCEPTID { get; set; }
        public string GROUPINGCONCEPT { get; set; }
    }

    public class NmrsConcept
    {
        public string ConceptId { get; set; }
        public string UuId { get; set; }
    }

    public class MigrationUpdate
    {
        public string LastPatient { get; set; }
        public string LastDate { get; set; }
        public string LastEncounterType { get; set; }
        public string LastVisit { get; set; }
        public string LastPage { get; set; }
    }

    public class ARTModel
    {
        public string VariableName { get; set; }
        public string VariablePosition { get; set; }
        public string DataType { get; set; }
        public string LamisAnswer { get; set; }
        public string OMRSConceptID { get; set; }
        public string OMRSAnswerID { get; set; }
    }

    public class Lab
    {
        public string Labtest_Id { get; set; }
        public string Labtestcategory_Id { get; set; }
        public string Description { get; set; }
        public string Measureab { get; set; }
        public string Measurepc { get; set; }
        public string Openmrsabsoluteconceptid { get; set; }
        public string Openmrspcconceptid { get; set; }
        public string Positive { get; set; }
        public string Negative { get; set; }
        public string Datatype { get; set; }
        public string ConceptBoolean { get; set; }
        public string MinimumValue { get; set; }
        public string MaximumValue { get; set; }
    }
    
    public class LabData
    {
        public string laboratory_id { get; set; }
        public long patient_id { get; set; }
        public string facility_id { get; set; }
        public string date_reported { get; set; }
        public string date_collected { get; set; }
        public string labno { get; set; }
        public string resultab { get; set; }
        public string resultpc { get; set; }
        public string comment { get; set; }
        public string labtest_id { get; set; }
        public string time_stamp { get; set; }
        public string uploaded { get; set; }
        public string time_uploaded { get; set; }
        public string user_id { get; set; }
        public string id_uuid { get; set; }
        public string uuid { get; set; }
        public string archived { get; set; }
    }

    public class ClinicData
    {
        public string clinic_id { get; set; }
        public long patient_id { get; set; }
        public string facility_id { get; set; }
        public string date_visit { get; set; }
        public string clinic_stage { get; set; }
        public string func_status { get; set; }
        public string tb_status { get; set; }
        public string viral_load { get; set; }
        public string cd4 { get; set; }
        public string cd4p { get; set; }
        public string regimentype { get; set; }
        public string regimen { get; set; }
        public string body_weight { get; set; }
        public string height { get; set; }
        public string waist { get; set; }
        public string bp { get; set; }
        public string pregnant { get; set; }
        public string lmp { get; set; }
        public string breastfeeding { get; set; }
        public string oi_screened { get; set; }
        public string sti_ids { get; set; }
        public string sti_treated { get; set; }
        public string oi_ids { get; set; }
        public string adr_screened { get; set; }
        public string adr_ids { get; set; }
        public string adherence_level { get; set; }
        public string adhere_ids { get; set; }
        public string commence { get; set; }
        public string next_appointment { get; set; }
        public string notes { get; set; }
        public string time_stamp { get; set; }
        public string uploaded { get; set; }
        public string time_uploaded { get; set; }
        public string user_id { get; set; }
        public string gestational_age { get; set; }
        public string maternal_status_art { get; set; }
        public string id_uuid { get; set; }
        public string uuid { get; set; }
        public string deviceconfig_id { get; set; }
        public string archived { get; set; }
    }

    public class PharmacyData
    {
        public string pharmacy_id { get; set; }
        public long patient_id { get; set; }
        public string facility_id { get; set; }
        public string date_visit { get; set; }
        public string duration { get; set; }
        public string morning { get; set; }
        public string afternoon { get; set; }
        public string evening { get; set; }
        public string adr_screened { get; set; }
        public string adr_ids { get; set; }
        public string prescrip_error { get; set; }
        public string adherence { get; set; }
        public string next_appointment { get; set; }
        public string regimentype { get; set; }
        public string regimen { get; set; }
        public string regimentype_id { get; set; }
        public string regimen_id { get; set; }
        public string regimendrug_id { get; set; }
        public string time_stamp { get; set; }
        public string uploaded { get; set; }
        public string time_uploaded { get; set; }
        public string user_id { get; set; }
        public string id_uuid { get; set; }
        public string dmoc_type { get; set; }
        public string uuid { get; set; }
        public string archived { get; set; }   
    }

    public class MigrationOption
    {
        public int Option { get; set; }
        public int Facility { get; set; }
        public string LamisDatabaseName { get; set; }
        public string LamisUsername { get; set; }
        public string LamisPassword { get; set; }
        public string NmrsDatabaseName { get; set; }
        public string PatientsFilePath { get; set; }
        public string ClinicalsFilePath { get; set; }
        public string LabDataFilePath { get; set; }
        public string PharmacyDataFilePath { get; set; }
        public string FaciltyName { get; set; }
        public string FacilityDatim_code { get; set; }
        public string NmrsWebUsername { get; set; }
        public string NmrsWebPassword { get; set; }
        public string NmrsServerPort { get; set; }
        public string BaseUrl { get; set; }
    }

    public class MigrationReport
    {
        public int patients { get; set; }
        public int encounters { get; set; }
        public int obs { get; set; }
        public int visit { get; set; }
    }

    public class ApiGetResponse
    {
        public List<Result> results { get; set; }
    }

    public class Result
    {
        public string uuid { get; set; }
        public string display { get; set; }
        public List<Link> links { get; set; }
    }

    public class Link
    {
        public string rel { get; set; }
        public string uri { get; set; }
    }

}
