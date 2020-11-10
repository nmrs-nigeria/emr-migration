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

    public class NmrsConcept
    {
        public string ConceptId { get; set; }
        public string UuId { get; set; }
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
    }

    public class LabData
    {
        public string laboratory_id { get; set; }
        public string patient_id { get; set; }
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
}
