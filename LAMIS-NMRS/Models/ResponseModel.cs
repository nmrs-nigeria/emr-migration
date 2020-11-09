using System;
using System.Collections.Generic;
using System.Text;

namespace LAMIS_NMRS.Models
{
    public class ResponseModel
    {
        public string Message { get; set; }
        public int Code { get; set; }
        public int Patients { get; set; }
        public int Labs { get; set; }
        public int Pharmacy { get; set; }
        public int CareCard { get; set; }
        public int TotalObservations { get; set; }
        public int SuccessfulObservations { get; set; }
        public int FailedObservations { get; set; }
        public List<Failure> Failures { get; set; }
    }

    public class Failure
    {
        public string Error { get; set; }
        public string Identifier { get; set; }
        public int EncounterType { get; set; }
        public int FormType { get; set; }
    }
}
