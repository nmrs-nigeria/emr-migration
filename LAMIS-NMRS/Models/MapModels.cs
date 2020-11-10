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

    public class ARTModel
    {
        public string VariableName { get; set; }
        public string VariablePosition { get; set; }
        public string DataType { get; set; }
        public string LamisAnswer { get; set; }
        public string OMRSConceptID { get; set; }
        public string OMRSAnswerID { get; set; }
    }
 
}
