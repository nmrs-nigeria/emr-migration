using System;
using System.Collections.Generic;
using System.Text;

namespace LAMIS_NMRS.Utils
{
    public enum Concepts
    {
        VisitDate = 165785,        
        Weight = 5089,
        height = 5090, 
        nextAppointmentDate = 5096,
        HivConfirmationDate = 160554,
        HivEnrolmentDate = 160555,
        NextOfKinName = 162729,
        NextOfKinPhone = 159635,
        DateTracked = 165461,
        DateMissedAppointment = 165778,
        DateLTFU = 166152,
        DateCareTerminated = 165469,
        MedicationDuration = 159368,
        QuantityPrescribed = 160856,
        QuantityDispensed = 1443,
        DateOrdered = 164989,
        DateDispensed = 164989
    }

    public enum ReferredFor
    {
        concept = 165776,
        ADHERENCE_COUNSELING = 5488,
        Other = 5622
    }

    public enum TreatmentAge
    {
        concept = 165720,
        Child  = 1528,
        Adult = 165709
    }

    public enum VisitType
    {
        concept = 164181,
        NewVisit = 164180,
        ReturnVisitType = 160530
    }

    public enum PickUpReason
    {
        concept = 165774,
        Refill = 165662,
        New = 165773,
        Switch = 165772,
        Substitution = 165665
    }

    public enum ServiceDeliveryModel
    {
        concept = 164181,
        NotDifferentiated = 166153,
        CommunityPharmacy = 166134,
        CommunityART = 166135,
        RefillFastTrack = 166151,
        MultimonthScripting = 166149,
        FamilyDrugPickup  = 166150
    }

    public enum ARVRegimen
    {
        concept = 162240,
        ARVMedication = 165724,
        ARVDrugStrength = 165725,
        DrugFrequency = 165723,
        MedicationDuration = 159368,
        QuantityPrescribed = 160856,
        MedicationDispensed = 1443,
        ARVMedicationDosage = 166120
    }
 
    public enum TreatmentType
    {
        concept = 165945,
        AntiretroviralTherapy = 165303,
        Non_ART = 165941,
        OccupPEP = 165942,
        Non_Occup_PEP = 165943,
        HEI = 165658,
        PrEP = 165944,
        PMTCT = 165685
    }


    public enum ReasonForDeath
    {
        concept = 165889,
        Other_Cause_of_death = 165888,
        Suspected_Opportunistic_Infection = 165887,
        Suspected_ARV_Side_effect = 165886,
        Unknown = 1067
    }

    public enum ReasonDiscontinuedCare
    {
        concept = 165916,
        SelfDiscontinuation = 165890,
        ForcedDiscontinuation = 165891,
        MovedOutOfArea = 165892,
        Other = 5622
    }
    public enum ReasonForTermination
    {
        concept = 165470,
        Transferred_Out_To_Another_Facility = 159492,
        Death = 165889,
        Discontinued_Care = 165916
    }

    public enum LTFU
    {
        concept = 5240,
        Yes = 1065,
        No = 1066
    }

    public enum PrevExposed
    {
        concept = 165586,
        Yes = 1065,
        No = 1066
    }

    public enum ReasonForLTFU
    {
        concept = 166157,
        Tracked_Not_Located = 166154,
        Did_not_Track = 166155
    }

    public enum NextOfKinRelationship
    {
        concept = 164943,
        Father = 971,
        Mother = 970,
        Daughter = 160728,
        Sister = 160730,
        Brother = 160729,
        Son = 160727,
        Uncle = 974,
        Aunt = 975,
        Wife = 164944,
        Husband = 164945,
        Friend = 5618,
        Parent = 1527,
        Other = 5622
    }

    public enum TrackingReason
    {
        concept = 165460,
        CoupleTesting = 165789,
        MissedAppointment = 165462,
        MissedPharmacyRefill = 165473,
        Other = 5622
    }

    public enum ChildBreastFeeding
    {
        concept = 165876,
        Yes = 1065,
        No = 1066
    }

    public enum CareEntryPoint
    {
        concept = 160540,
        OPD = 160542,
        Inpatient = 160536,
        VCT = 160539,
        TB_DOT = 160541,
        STI_Clinic = 160546,
        ANC_PMTCT = 160538,
        TransferIn = 160563,
        Outreaches = 160545,
        IndexTesting = 165794,
        Others = 5622
    }

    public enum MaritalStatus
    {
        concept = 1054,
        Single = 5555,
        Married = 160536,
        Divorced = 1058,
        Separated = 1056,
        Cohabiting = 1060,
        Widow_er = 1059
    }

    public enum EducationLevel
    {
        concept = 1712,
        No_Education = 1107,
        Primary_Education = 1713,
        Secondary_Education = 1714,
        Tertiary_Education = 160292,
        Other = 5622
    }

    public enum Occupation
    {
        concept = 1542,
        Unemployed = 123801,
        Employed = 1540,
        Student = 159465,
        Retired = 159461,
        NotApplicable = 1175,
        Unknown = 1067
    }

    public enum CurrentlyPregnant
    {
        concept = 1434,
        Yes = 1065,
        No = 1066
    }

    public enum PregnancyStatus
    {
        concept = 165050,
        notPregnant = 165047,
        pregnant = 165048,
        breastFeeding = 165049
    }

    public enum BloodPressure
    {
        Systolic = 5085,
        Diastolic = 5086,
    }

    public enum WhoStage
    {
        concept = 5356,
        adultStage1 = 1204,
        adultStage2 = 1205,
        adultStage3 = 1206,
        adultStage4 = 1207,
        paedStage1 = 1220,
        paedStage2 = 1221,
        paedStage3 = 1222,
        paedStage4 = 1223
    }

    public enum FunctionalStatus
    {
        concept = 165039,
        working = 159468,
        ambulatory = 160026,
        bedRidden = 162752
    }

    public enum TBStatus
    {
        concept = 1659,
        noTbSigns = 1660,
        tbTreatment = 1662,
        presumptiveTB = 142177,
        inhProphylaxis = 166042,
        tbConfirmed = 1661
    }

    public enum CurrentRegimenLine
    {
        concept = 165708,
        AdultFirstLine = 164506,
        AdultSecondLine = 164513,
        AdultThirdLine = 165702,
        PaediatricFirstLine = 164507,
        PaediatricSecondLine = 164514,
        PaediatricThirdLine = 165703
    }

    public enum DrugAdhenrence
    {
        concept = 165290,
        Poor = 165289,
        Fair = 165288,
        Good = 165287
    }

    public enum AdherenceCounselling
    {
        concept = 165832,
        No = 1066,
        Yes = 1065
    }

    public enum OIs
    {
        concept = 160170,
        Herpes_Zoster = 117543,
        Pneumonia = 114100,
        Dementia_Encephalitis = 119566,
        Candidaisis_Oral_Virginal = 5334,
        Diarrhea = 5018,
        Fever = 140238,
        Cough = 143264,
        SkinInfection = 160161
    }
}



