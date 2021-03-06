﻿using LAMIS_NMRS.Models;
using LAMIS_NMRS.Utils;
using Microsoft.AspNetCore.Hosting.Internal;
using Npgsql;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Common
{
    public class DataBuilder_Read_From_DB
    {
        List<Regimen> regimens;
        List<NmrsConcept> nmsConcepts;
        List<Drug> drugs;
        List<Lab> labs;
        MigrationOption _migOption;
        MigrationReport migrationReport;
        string rootDir;
        int itemsPerPage = 100, pageNumber = 0;        
        string pgconn;
        bool migrationChecked = false;
        bool migrationHappend = false;

        //string mysqlconn;
        public DataBuilder_Read_From_DB(MigrationOption migOption)
        {
            _migOption = migOption;        
            migrationReport = new MigrationReport();
            rootDir = Directory.GetCurrentDirectory();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            //Console.ForegroundColor = ConsoleColor.White;
            //Console.WriteLine(Environment.NewLine);
            //Console.WriteLine("::: Starting Migration. Please don't close this window :::" + Environment.NewLine);
            //BuildPatientInfo();
        }
        public async Task BuildPatientInfo()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(Environment.NewLine);
            Console.WriteLine("::: Starting Migration. Please don't close this window :::" + Environment.NewLine);

            try
            {
                var startDate = DateTime.Now;
                regimens = GetRegimen();
                nmsConcepts = new Utilities().GetConcepts();
                drugs = GetDrugs();
                labs = GetLabs();                

                if (!regimens.Any() || !nmsConcepts.Any() || !drugs.Any() || !labs.Any())
                {
                    Console.WriteLine("ERROR: Regimen/Drugs or NMRS Concepts data List could not be retrieved. Command Aborted");
                    return;
                }

                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Retrieving patients...{0}", Environment.NewLine);

            retrievePatients: var patients = new List<Patient>();

                pgconn = "Host=localhost;Username=" + _migOption.LamisUsername + ";Password=" + _migOption.LamisPassword + ";Database=" + _migOption.LamisDatabaseName + ";";

                using (NpgsqlConnection connection = new NpgsqlConnection(pgconn))
                {
                    connection.Open();

                    pageNumber += 1;

                    var q = "SELECT * FROM patient where facility_id = " + _migOption .Facility + " order by patient_id offset  " + ((pageNumber - 1) * itemsPerPage) + " rows fetch next " + itemsPerPage + " rows only;";                                       

                    using (NpgsqlCommand cmd = new NpgsqlCommand(q, connection))
                    {
                        using (NpgsqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {                               

                                while (reader.Read())
                                {
                                    var patient = new Patient 
                                    { 
                                        identifiers = new List<Identifiers>(), 
                                        person = new PatientDemography(),
                                        Encounters = new List<Encounter>()
                                    };

                                    var pd = new PatientDemography();

                                    DateTime dateOfBirth;
                                    var enrolmentIdStr = reader["hospital_num"];
                                    var patient_id = reader["patient_id"].ToString();

                                    if (!long.TryParse(patient_id, out long patientId))
                                        continue;

                                    if (enrolmentIdStr != null)
                                    {
                                        var enrolmentId = enrolmentIdStr.ToString();

                                        if (!string.IsNullOrEmpty(enrolmentId))
                                        {
                                            var identifier = new Identifiers
                                            {
                                                identifier = enrolmentId,
                                                identifierType = "c82916e4-168c-495f-8ed0-b1b286c30a05", //Pepfar uuid
                                                location = "b1a8b05e-3542-4037-bbd3-998ee9c40574", //in-patient ward uuid
                                                preferred = true
                                            };
                                            patient.identifiers.Add(identifier);

                                            var dobStr = reader["date_birth"];
                                            if (dobStr != null)
                                            {
                                                var dob = dobStr.ToString().Trim();
                                                if (!string.IsNullOrEmpty(dob))
                                                {
                                                    if (DateTime.TryParse(dob.Trim(), out dateOfBirth))
                                                    {
                                                        pd.birthdate = dateOfBirth.ToString("yyyy-MM-dd");
                                                    }
                                                }
                                            }

                                            //Attributes
                                            var phoneStr = reader["phone"];
                                            if (phoneStr != null)
                                            {
                                                if (!string.IsNullOrEmpty(phoneStr.ToString()))
                                                {
                                                    var attribute = new PatientAttributes
                                                    {
                                                        attributeType = "14d4f066-15f5-102d-96e4-000c29c2a5d7", //Phone number uuid
                                                        value = Utilities.UnscrambleNumbers(phoneStr.ToString()),
                                                    };
                                                    patient.attributes = new List<PatientAttributes> { attribute };
                                                }
                                            }

                                            var givenName = reader["other_names"];
                                            var surname = reader["surname"];
                                            if(givenName != null && surname != null)
                                            {
                                                var otherNames = givenName.ToString();
                                                var familyName = surname.ToString();
                                                if (!string.IsNullOrEmpty(familyName) && !string.IsNullOrEmpty(otherNames))
                                                {
                                                    var name = new PersonName
                                                    {
                                                        preferred = true,
                                                        givenName = Utilities.UnscrambleCharacters(otherNames),
                                                        familyName = Utilities.UnscrambleCharacters(familyName)
                                                    };

                                                    pd.names = new List<PersonName> { name };
                                                }                                                
                                            }
                                            var ageStr = reader["age"];
                                            if (ageStr != null)
                                            {
                                                var ageX = ageStr.ToString();
                                                if (!string.IsNullOrEmpty(ageX))
                                                {
                                                    if(int.TryParse(ageX, out int age))
                                                    {
                                                        pd.age = age;
                                                    }                                                   
                                                }
                                            }

                                            var genderStr = reader["gender"];
                                            if (genderStr != null)
                                            {
                                                var gender = genderStr.ToString().Trim().Replace(" ", string.Empty);
                                                if (!string.IsNullOrEmpty(gender))
                                                {
                                                    pd.gender = gender;
                                                }
                                            }

                                            var adr = reader["address"];
                                            if(adr != null)
                                            {
                                                var addres1 = adr.ToString().Trim();
                                                if (!string.IsNullOrEmpty(addres1))
                                                {                                                
                                                    var address = new Personaddress
                                                    {
                                                        preferred = true,
                                                        address1 = Utilities.UnscrambleCharacters(addres1),
                                                        country = "Nigeria"
                                                    };

                                                    var lgaR = reader["lga"];
                                                    if(lgaR != null)
                                                    {
                                                        var lga = lgaR.ToString().Trim();
                                                        if (!string.IsNullOrEmpty(lga))
                                                        {
                                                            address.cityVillage = lga;
                                                        }                                                            
                                                    }

                                                    var stateR = reader["state"];
                                                    if (stateR != null)
                                                    {
                                                        var state = stateR.ToString().Trim();
                                                        if (!string.IsNullOrEmpty(state))
                                                        {
                                                            address.stateProvince = state;
                                                        }
                                                    }

                                                    pd.addresses = new List<Personaddress> { address };
                                                }
                                            }
                                            
                                            patient.person = pd;

                                            var artCommencement = BuiildArtCommencement(reader);
                                            if(!string.IsNullOrEmpty(artCommencement.encounterType))
                                            {
                                                patient.Encounters.Add(artCommencement);
                                            }
                                            var careCardAndVitals = BuildCareCardAndVitals(patientId, dobStr.ToString().Trim());
                                            if(careCardAndVitals.Any())
                                            {
                                                careCardAndVitals.ForEach(e =>
                                                {
                                                    if (!string.IsNullOrEmpty(e.encounterType))
                                                    {
                                                        patient.Encounters.Add(e);
                                                    }
                                                });
                                            }                                                                                    

                                            var careTermination = BuildCareTermination(reader);
                                            if (!string.IsNullOrEmpty(careTermination.encounterType))
                                            {
                                                patient.Encounters.Add(careTermination);
                                            }
                                            var hivEnrolment = BuildHIVEnrolment(reader, pd.gender);
                                            if (!string.IsNullOrEmpty(hivEnrolment.encounterType))
                                            {
                                                patient.Encounters.Add(hivEnrolment);

                                                var patientProgram = new PatientProgram
                                                {
                                                    dateEnrolled = hivEnrolment.encounterDatetime,
                                                    //dateCompleted = hivEnrolment.encounterDatetime,

                                                };

                                                patient.PatientProgram = patientProgram;
                                            }

                                            var pharmacies = BuildPharmacy(patientId, dobStr.ToString().Trim());
                                            if (pharmacies.Any())
                                            {
                                                pharmacies.ForEach(p =>
                                                {
                                                    if (!string.IsNullOrEmpty(p.encounterType))
                                                    {
                                                        patient.Encounters.Add(p);
                                                    }
                                                });
                                            }

                                            var labInfo = BuildLab(patientId);
                                            if (labInfo.Any())
                                            {
                                                labInfo.ForEach(l =>
                                                {
                                                    if (!string.IsNullOrEmpty(l.encounterType))
                                                    {
                                                        patient.Encounters.Add(l);
                                                    }
                                                });
                                            }

                                            patients.Add(patient);
                                        }
                                    }
                                                                        
                                }
                                                                
                            }
                        }
                    }
                }

                if (patients.Any())
                {
                    var mgP = await PushData(patients);

                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine($"Migrated {migrationReport.patients} patients so far." + Environment.NewLine);
                    Console.WriteLine("Retrieving more patients...{0}", Environment.NewLine);
                    goto retrievePatients;
                }
                else
                {
                    Console.WriteLine("Data migration completed" + Environment.NewLine);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(":::: MIGRATION SUMMARY ::::");
                    Console.WriteLine("Total Patients: " + migrationReport.patients.ToString());
                    Console.WriteLine("Total Visits: " + migrationReport.visit.ToString());
                    Console.WriteLine("Total Encounters: " + migrationReport.encounters.ToString());
                    Console.WriteLine("Total Obs: " + migrationReport.obs.ToString());
                    var d = (DateTime.Now - startDate).ToString(@"hh\:mm\:ss");
                    Console.WriteLine("Total Duration: {0}{1}{2}", d, Environment.NewLine, Environment.NewLine);                   
                }

                return;
            }
            catch (Exception ex)
            {
                var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(Environment.NewLine + message + Environment.NewLine);
                return;
            }
        }
        public Encounter BuiildArtCommencement(NpgsqlDataReader reader)
        {
            var artcmmtMap = GetARTCommencementMap();

            var artCommencement = new Encounter
            {
                encounterType = "21a8459c-8578-4649-931c-0cf565ee161b", //art commencement
                encounterDatetime = "",
                location = "b1a8b05e-3542-4037-bbd3-998ee9c40574", //using in-patient ward for now
                form = "38d688ed-a569-4868-b5b2-a2f204a2e572", //care card
                //provider = "1c3db49d-440a-11e6-a65c-00e04c680037", //super user
                obs = new List<Obs>()
            };

            try
            {
                //default functional status at start of ART
                var fStatusObs = new Obs
                {
                    concept = ((int)FunctionalStatus.concept).ToString(),
                    value = ((int)FunctionalStatus.working).ToString(),
                    groupMembers = new List<Obs>()
                };

                fStatusObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == fStatusObs.concept).UuId;
                fStatusObs.value = nmsConcepts.FirstOrDefault(c => c.ConceptId == fStatusObs.value).UuId;
                artCommencement.obs.Add(fStatusObs);

                var date_started = reader["date_started"];
                var date_registration = reader["date_registration"];

                DateTime artStartDate;
                if (date_started != null)
                {
                    var date_startedStr = date_started.ToString().Trim();

                    if (!string.IsNullOrEmpty(date_startedStr))
                    {
                        if (DateTime.TryParse(date_startedStr.Trim(), out artStartDate))
                        {
                            var artStrs = artcmmtMap.Where(f => f.VariableName == "date_started");
                            if (artStrs.Any())
                            {
                                var artS = artStrs.ElementAt(0);
                                var fartStartDateObs = new Obs
                                {
                                    concept =artS.OMRSConceptID,
                                    value = artStartDate.ToString("yyyy-MM-dd"),
                                    groupMembers = new List<Obs>()
                                };
                                fartStartDateObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == fartStartDateObs.concept).UuId;                               
                                artCommencement.obs.Add(fartStartDateObs);
                            }
                        }

                    }
                }

                if (date_registration != null)
                {
                    var date_registrationStr = date_registration.ToString().Trim();

                    if (!string.IsNullOrEmpty(date_registrationStr))
                    {
                        if (DateTime.TryParse(date_registrationStr.Trim(), out DateTime encDate))
                        {
                            artCommencement.encounterDatetime = encDate.ToString("yyyy-MM-dd");
                        }

                    }
                }

                var date_birthStr = reader["date_birth"];
                if (date_birthStr != null)
                {
                    var date_birth = date_birthStr.ToString().Trim();
                    if (!string.IsNullOrEmpty(date_started.ToString()) && !string.IsNullOrEmpty(date_birth))
                    {
                        var ageAtStart = Convert.ToDateTime(date_started).Year - Convert.ToDateTime(date_birth).Year;

                        var whoStageObs = new Obs //WHO Stage at start
                        {
                            concept =((int)WhoStage.concept).ToString(),
                            value = ageAtStart > 14 ? ((int)WhoStage.adultStage1).ToString() : ((int)WhoStage.paedStage1).ToString(),
                            groupMembers = new List<Obs>()
                        };
                        whoStageObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == whoStageObs.concept).UuId;
                        whoStageObs.value = nmsConcepts.FirstOrDefault(c => c.ConceptId == whoStageObs.value).UuId;
                        artCommencement.obs.Add(whoStageObs);
                    }
                }

                var regimenTypeStr = reader["regimentype"];
                var regimenStr = reader["regimen"];
                if (regimenTypeStr != null && regimenStr != null)
                {
                    var regimenType = regimenTypeStr.ToString();
                    var regimen = regimenStr.ToString();

                    if (!string.IsNullOrEmpty(regimenType) && !string.IsNullOrEmpty(regimen))
                    {
                        var rootWord = regimenType.ToLower().Contains("children") ? "Children" : "Adult";
                        var rgs = regimens.Where(r => r.Answers == regimen && r.Values.Contains(rootWord)).ToList();
                        if (rgs.Any())
                        {
                            var rg = rgs[0];

                            // Current regimen Line
                            var currentRegLineObs = new Obs
                            {
                                concept =((int)CurrentRegimenLine.concept).ToString(),
                                value = rg.NMRSQuestionConceptID.ToString(),
                                groupMembers = new List<Obs>()
                            };
                            currentRegLineObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == currentRegLineObs.concept).UuId;
                            currentRegLineObs.value = nmsConcepts.FirstOrDefault(c => c.ConceptId == currentRegLineObs.value).UuId;
                            artCommencement.obs.Add(currentRegLineObs);

                            // Regimen
                            var regimenObs = new Obs
                            {
                                concept =rg.NMRSQuestionConceptID.ToString(),
                                value = rg.NMRSAnswerConceptID.ToString(),
                                groupMembers = new List<Obs>()
                            };
                            regimenObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == regimenObs.concept).UuId;
                            regimenObs.value = nmsConcepts.FirstOrDefault(c => c.ConceptId == regimenObs.value).UuId;
                            artCommencement.obs.Add(regimenObs);

                        }
                    }
                }
                return artCommencement;
            }
            catch (Exception ex)
            {
                var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                Console.WriteLine(message);
                return new Encounter();
            }
        }                
        public Encounter BuildCareTermination(NpgsqlDataReader reader)
        {
            try
            {
                var careTermination = new Encounter
                {                    
                    obs = new List<Obs>()
                };

                var current_status = reader["current_status"];
                if (current_status != null)
                {
                    var currentStatus = current_status.ToString().Trim();

                    if (!string.IsNullOrEmpty(currentStatus))
                    {
                        if (currentStatus != "ART Start" && currentStatus != "HIV exposed status unknown" && currentStatus != "ART Transfer In" && currentStatus != "HIV+ non ART")
                        {
                            var exitMap = GetExitMap();

                            var date_current_status = reader["date_current_status"];
                            if (date_current_status != null)
                            {
                                var dateCurrentStatus = date_current_status.ToString().Trim();

                                if (!string.IsNullOrEmpty(dateCurrentStatus))
                                {
                                    if (DateTime.TryParse(dateCurrentStatus.Trim(), out DateTime dExit))
                                    {
                                        var dateExitX = exitMap.Where(f => f.VariableName == "date_current_status");
                                        if (dateExitX.Any())
                                        {
                                            var dateExit = dateExitX.ElementAt(0);
                                            var dateExitObs = new Obs
                                            {
                                                concept =dateExit.OMRSConceptID,
                                                value = dExit.ToString("yyyy-MM-dd"),
                                                groupMembers = new List<Obs>()
                                            };
                                            dateExitObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == dateExitObs.concept).UuId;
                                            careTermination.obs.Add(dateExitObs);
                                            careTermination.encounterDatetime = dExit.ToString("yyyy-MM-dd");

                                            //Reason for tracking
                                            var trackingObs = new Obs
                                            {
                                                concept =((int)TrackingReason.concept).ToString(),
                                                value = ((int)TrackingReason.MissedPharmacyRefill).ToString(), //default
                                                groupMembers = new List<Obs>()
                                            };
                                            trackingObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == trackingObs.concept).UuId;
                                            trackingObs.value = nmsConcepts.FirstOrDefault(c => c.ConceptId == trackingObs.value).UuId;
                                            careTermination.obs.Add(trackingObs);
                                        }

                                        var date_tracked = reader["date_tracked"];
                                        if (date_tracked != null)
                                        {
                                            var dateTracked = date_tracked.ToString().Trim();

                                            if (!string.IsNullOrEmpty(dateTracked))
                                            {
                                                if (DateTime.TryParse(dateTracked.Trim(), out DateTime dTract))
                                                {
                                                    var dTractX = exitMap.Where(f => f.VariableName == "date_tracked");
                                                    if (dTractX.Any())
                                                    {
                                                        var dateTrackedX = dateExitX.ElementAt(0);
                                                        var dateTrackedObs = new Obs
                                                        {
                                                            concept =dateTrackedX.OMRSConceptID,
                                                            value = dTract.ToString("yyyy-MM-dd"),
                                                            groupMembers = new List<Obs>()
                                                        };
                                                        dateTrackedObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == dateTrackedObs.concept).UuId;
                                                        careTermination.obs.Add(dateTrackedObs);

                                                    }
                                                }

                                            }
                                        }

                                        var agreed_date = reader["agreed_date"];
                                        if (agreed_date != null)
                                        {
                                            var agreedDate = agreed_date.ToString().Trim();

                                            if (!string.IsNullOrEmpty(agreedDate))
                                            {
                                                if (DateTime.TryParse(agreedDate.Trim(), out DateTime dMissed))
                                                {
                                                    var dateMissedObs = new Obs
                                                    {
                                                        concept =((int)Concepts.DateMissedAppointment).ToString(),
                                                        value = dMissed.ToString("yyyy-MM-dd"),
                                                        groupMembers = new List<Obs>()
                                                    };
                                                    dateMissedObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == dateMissedObs.concept).UuId;
                                                    careTermination.obs.Add(dateMissedObs);
                                                }

                                            }
                                        }

                                        //LTFU
                                        if (currentStatus == "Lost to Follow Up")
                                        {
                                            var ltfuObs = new Obs
                                            {
                                                concept = ((int)LTFU.concept).ToString(),
                                                value = "true",
                                                groupMembers = new List<Obs>()
                                            };
                                            ltfuObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == ltfuObs.concept).UuId;
                                            careTermination.obs.Add(ltfuObs);

                                            var reasonLtfuObs = new Obs
                                            {
                                                concept = ((int)ReasonForLTFU.concept).ToString(),
                                                value = ((int)ReasonForLTFU.Tracked_Not_Located).ToString(),
                                                groupMembers = new List<Obs>()
                                            };
                                            reasonLtfuObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == reasonLtfuObs.concept).UuId;
                                            reasonLtfuObs.value = nmsConcepts.FirstOrDefault(c => c.ConceptId == reasonLtfuObs.value).UuId;
                                            careTermination.obs.Add(reasonLtfuObs);

                                            var dateLtfuObs = new Obs
                                            {
                                                concept = ((int)Concepts.DateLTFU).ToString(),
                                                value = dExit.ToString("yyyy-MM-dd"),
                                                groupMembers = new List<Obs>()
                                            };
                                            dateLtfuObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == dateLtfuObs.concept).UuId;
                                            careTermination.obs.Add(dateLtfuObs);
                                        }

                                        //terminated
                                        if (currentStatus == "ART Transfer Out")
                                        {
                                            var trnsObs = new Obs
                                            {
                                                concept = ((int)PrevExposed.concept).ToString(),
                                                value = ((int)PrevExposed.Yes).ToString(),
                                                groupMembers = new List<Obs>()
                                            };
                                            trnsObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == trnsObs.concept).UuId;
                                            trnsObs.value = nmsConcepts.FirstOrDefault(c => c.ConceptId == trnsObs.value).UuId;
                                            careTermination.obs.Add(trnsObs);

                                            var reasonForTerminationObs = new Obs
                                            {
                                                concept = ((int)ReasonForTermination.concept).ToString(),
                                                value = ((int)ReasonForTermination.Transferred_Out_To_Another_Facility).ToString(),
                                                groupMembers = new List<Obs>()
                                            };
                                            reasonForTerminationObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == reasonForTerminationObs.concept).UuId;
                                            reasonForTerminationObs.value = nmsConcepts.FirstOrDefault(c => c.ConceptId == reasonForTerminationObs.value).UuId;
                                            careTermination.obs.Add(reasonForTerminationObs);

                                            var dateTerminatedObs = new Obs
                                            {
                                                concept = ((int)Concepts.DateCareTerminated).ToString(),
                                                value = careTermination.encounterDatetime,
                                                groupMembers = new List<Obs>()
                                            };
                                            dateTerminatedObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == dateTerminatedObs.concept).UuId;
                                            careTermination.obs.Add(dateTerminatedObs);
                                        }
                                        if (currentStatus == "Known Death")
                                        {
                                            var trnsObs = new Obs
                                            {
                                                concept = ((int)PrevExposed.concept).ToString(),
                                                value = ((int)PrevExposed.Yes).ToString(),
                                                groupMembers = new List<Obs>()
                                            };
                                            trnsObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == trnsObs.concept).UuId;
                                            trnsObs.value = nmsConcepts.FirstOrDefault(c => c.ConceptId == trnsObs.value).UuId;
                                            careTermination.obs.Add(trnsObs);

                                            var reasonForTerminationObs = new Obs
                                            {
                                                concept = ((int)ReasonForTermination.concept).ToString(),
                                                value = ((int)ReasonForTermination.Death).ToString(),
                                                groupMembers = new List<Obs>()
                                            };
                                            reasonForTerminationObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == reasonForTerminationObs.concept).UuId;
                                            reasonForTerminationObs.value = nmsConcepts.FirstOrDefault(c => c.ConceptId == reasonForTerminationObs.value).UuId;
                                            careTermination.obs.Add(reasonForTerminationObs);

                                            var cause_death = reader["cause_death"];
                                            if (cause_death != null)
                                            {
                                                var causeDeath = cause_death.ToString().Trim();

                                                if (!string.IsNullOrEmpty(causeDeath))
                                                {
                                                    int conceptId;
                                                    switch (causeDeath)
                                                    {
                                                        case "HIV disesase resulting in TB":
                                                            conceptId = (int)ReasonForDeath.Suspected_Opportunistic_Infection;
                                                            break;
                                                        case "HIV disesase resulting in Cancer":
                                                            conceptId = (int)ReasonForDeath.Suspected_Opportunistic_Infection;
                                                            break;
                                                        case "HIV disesase resulting in other infectious and parasitic diseases":
                                                            conceptId = (int)ReasonForDeath.Suspected_Opportunistic_Infection;
                                                            break;
                                                        case "Other HIV disease resulting in other disease or conditions leading to death":
                                                            conceptId = (int)ReasonForDeath.Suspected_Opportunistic_Infection;
                                                            break;
                                                        case "other natural causes":
                                                            conceptId = (int)ReasonForDeath.Suspected_Opportunistic_Infection;
                                                            break;
                                                        case "non-natural causes":
                                                            conceptId = (int)ReasonForDeath.Suspected_ARV_Side_effect;
                                                            break;
                                                        case "unknown cause":
                                                            conceptId = (int)ReasonForDeath.Unknown;
                                                            break;
                                                        default:
                                                            conceptId = (int)ReasonForDeath.Unknown;
                                                            break;
                                                    }
                                                    var causeOfDesthObs = new Obs
                                                    {
                                                        concept = ((int)ReasonForDeath.concept).ToString(),
                                                        value = conceptId.ToString(),
                                                        groupMembers = new List<Obs>()
                                                    };
                                                    causeOfDesthObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == causeOfDesthObs.concept).UuId;
                                                    causeOfDesthObs.value = nmsConcepts.FirstOrDefault(c => c.ConceptId == causeOfDesthObs.value).UuId;
                                                    careTermination.obs.Add(causeOfDesthObs);
                                                }
                                            }

                                            var dateTerminatedObs = new Obs
                                            {
                                                concept = ((int)Concepts.DateCareTerminated).ToString(),
                                                value = dExit.ToString("yyyy-MM-dd"),
                                                groupMembers = new List<Obs>()
                                            };
                                            dateTerminatedObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == dateTerminatedObs.concept).UuId;
                                            careTermination.obs.Add(dateTerminatedObs);
                                        }
                                        if (currentStatus == "Stopped Treatment")
                                        {
                                            var trnsObs = new Obs
                                            {
                                                concept = ((int)PrevExposed.concept).ToString(),
                                                value = ((int)PrevExposed.Yes).ToString(),
                                                groupMembers = new List<Obs>()
                                            };
                                            trnsObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == trnsObs.concept).UuId;
                                            trnsObs.value = nmsConcepts.FirstOrDefault(c => c.ConceptId == trnsObs.value).UuId;
                                            careTermination.obs.Add(trnsObs);

                                            var reasonForTerminationObs = new Obs
                                            {
                                                concept = ((int)ReasonForTermination.concept).ToString(),
                                                value = ((int)ReasonForTermination.Discontinued_Care).ToString(),
                                                groupMembers = new List<Obs>()
                                            };
                                            reasonForTerminationObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == reasonForTerminationObs.concept).UuId;
                                            reasonForTerminationObs.value = nmsConcepts.FirstOrDefault(c => c.ConceptId == reasonForTerminationObs.value).UuId;
                                            careTermination.obs.Add(reasonForTerminationObs);

                                            var reasonDiscontinuedCareObs = new Obs
                                            {
                                                concept = ((int)ReasonDiscontinuedCare.concept).ToString(),
                                                value = ((int)ReasonDiscontinuedCare.MovedOutOfArea).ToString(),
                                                groupMembers = new List<Obs>()
                                            };
                                            reasonDiscontinuedCareObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == reasonDiscontinuedCareObs.concept).UuId;
                                            reasonDiscontinuedCareObs.value = nmsConcepts.FirstOrDefault(c => c.ConceptId == reasonDiscontinuedCareObs.value).UuId;
                                            careTermination.obs.Add(reasonDiscontinuedCareObs);

                                            var dateTerminatedObs = new Obs
                                            {
                                                concept = ((int)Concepts.DateCareTerminated).ToString(),
                                                value = careTermination.encounterDatetime,
                                                groupMembers = new List<Obs>()
                                            };
                                            dateTerminatedObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == dateTerminatedObs.concept).UuId;
                                            careTermination.obs.Add(dateTerminatedObs);
                                        }

                                        //terminated == no
                                        if (currentStatus == "ART Restart")
                                        {
                                            var trnsObs = new Obs
                                            {
                                                concept = ((int)PrevExposed.concept).ToString(),
                                                value = ((int)PrevExposed.No).ToString(),
                                                groupMembers = new List<Obs>()
                                            };
                                            trnsObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == trnsObs.concept).UuId;
                                            trnsObs.value = nmsConcepts.FirstOrDefault(c => c.ConceptId == trnsObs.value).UuId;
                                            careTermination.obs.Add(trnsObs);

                                            var referredForObs = new Obs
                                            {
                                                concept = ((int)ReferredFor.concept).ToString(),
                                                value = ((int)ReferredFor.ADHERENCE_COUNSELING).ToString(),
                                                groupMembers = new List<Obs>()
                                            };
                                            referredForObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == referredForObs.concept).UuId;
                                            referredForObs.value = nmsConcepts.FirstOrDefault(c => c.ConceptId == referredForObs.value).UuId;
                                            careTermination.obs.Add(referredForObs);

                                            var dateTerminatedObs = new Obs
                                            {
                                                concept = ((int)Concepts.DateCareTerminated).ToString(),
                                                value = dExit.ToString("yyyy-MM-dd"),
                                                groupMembers = new List<Obs>()
                                            };
                                            dateTerminatedObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == dateTerminatedObs.concept).UuId;
                                            careTermination.obs.Add(dateTerminatedObs);
                                        }

                                        if (careTermination.obs.Any())
                                        {
                                            careTermination.encounterType = "ba6fb85e-8dc3-43e3-a269-59842cd2d364"; //tracking and termination
                                            careTermination.location = "b1a8b05e-3542-4037-bbd3-998ee9c40574"; //using in-patient ward for now
                                            careTermination.form = "5fbc99be-9aeb-4f94-85b0-b2fae88a0ced"; //tracking and termination
                                                                                                           //careTermination.provider = "f9badd80-ab76-11e2-9e96-0800200c9a66"; //super user
                                        }
                                    }

                                }
                            }
                        }
                    }
                }
                return !string.IsNullOrEmpty(careTermination.encounterType)? careTermination : new Encounter();
            }
            catch (Exception ex)
            {
                var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                Console.WriteLine(message);
                return new Encounter();
            }
        }
        public Encounter BuildHIVEnrolment(NpgsqlDataReader reader, string gender)
        {
            var artcmmtMap = GetARTCommencementMap();

            var hivEnrolment = new Encounter
            {
                encounterType = "11eb2647-91b4-482a-9cb3-08573e0d219b", //HIV Enrolment
                encounterDatetime = "",
                location = "6351fcf4-e311-4a19-90f9-35667d99a8af", //Registration Desk
                form = "c2df5a7d-05ac-4ae3-bcd0-39969f17dbab", //HIV Enrolment
                //provider = "1c3db49d-440a-11e6-a65c-00e04c680037", //super user
                obs = new List<Obs>()
            };

            try
            {                                               
                var date_started = reader["date_started"];
                DateTime artStartDate;
                if (date_started != null)
                {
                    var date_startedStr = date_started.ToString().Trim();

                    if (!string.IsNullOrEmpty(date_startedStr))
                    {
                        if (DateTime.TryParse(date_startedStr.Trim(), out artStartDate))
                        {
                            ;

                            var artStrs = artcmmtMap.Where(f => f.VariableName == "date_started");
                            if (artStrs.Any())
                            {
                                var artS = artStrs.ElementAt(0);
                                var fartStartDateObs = new Obs
                                {
                                    concept =artS.OMRSConceptID,
                                    value = artStartDate.ToString("yyyy-MM-dd"),
                                    groupMembers = new List<Obs>()
                                };
                                fartStartDateObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == fartStartDateObs.concept).UuId;
                                hivEnrolment.obs.Add(fartStartDateObs);
                            }
                        }

                    }
                }

                var date_registration = reader["date_registration"];

                DateTime encDate = new DateTime(1990, 1, 1);//default date

                if (date_registration != null)
                {
                    var date_registrationStr = date_registration.ToString().Trim();

                    if (!string.IsNullOrEmpty(date_registrationStr))
                    {
                        if (DateTime.TryParse(date_registrationStr.Trim(), out encDate))
                        {
                            hivEnrolment.encounterDatetime = encDate.ToString("yyyy-MM-dd");

                            var enrolmentDateObs = new Obs
                            {
                                concept =((int)Concepts.HivEnrolmentDate).ToString(),
                                value = encDate.ToString("yyyy-MM-dd"),
                                groupMembers = new List<Obs>()
                            };
                            enrolmentDateObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == enrolmentDateObs.concept).UuId;
                            hivEnrolment.obs.Add(enrolmentDateObs);
                        }
                    }
                }
                
                var date_confirmed_hiv = reader["date_confirmed_hiv"];
                if (date_confirmed_hiv != null)
                {
                    var date_confirmed_hivStr = date_confirmed_hiv.ToString().Trim();

                    if (!string.IsNullOrEmpty(date_confirmed_hivStr))
                    {
                        if (DateTime.TryParse(date_confirmed_hivStr.Trim(), out DateTime confirmDate))
                        {
                            var hivConfirmDateObs = new Obs
                            {
                                concept =((int)Concepts.HivConfirmationDate).ToString(),
                                value = confirmDate.ToString("yyyy-MM-dd"),
                                groupMembers = new List<Obs>()
                            };
                            hivConfirmDateObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == hivConfirmDateObs.concept).UuId;
                            hivEnrolment.obs.Add(hivConfirmDateObs);
                        }

                    }
                }
                else
                {
                    var hivConfirmDateObs = new Obs
                    {
                        concept =((int)Concepts.HivConfirmationDate).ToString(),
                        value = encDate.ToString("yyyy-MM-dd"),
                        groupMembers = new List<Obs>()
                    };
                    hivConfirmDateObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == hivConfirmDateObs.concept).UuId;
                    hivEnrolment.obs.Add(hivConfirmDateObs);
                }

                var entryPoint = reader["entry_point"];
                if (entryPoint != null)
                {
                    var careEntryPoint = entryPoint.ToString().Trim();

                    if (!string.IsNullOrEmpty(careEntryPoint))
                    {
                        int conceptId;
                        switch (careEntryPoint)
                        {
                            case "HCT":
                                conceptId = (int)CareEntryPoint.VCT;
                                break;
                            case "Transfer-in":
                                conceptId = (int)CareEntryPoint.TransferIn;
                                break;
                            case "OPD":
                                conceptId = (int)CareEntryPoint.OPD;
                                break;
                            case "In patients":
                                conceptId = (int)CareEntryPoint.Inpatient;
                                break;
                            case "In-patient":
                                conceptId = (int)CareEntryPoint.Inpatient;
                                break;
                            case "Outreach":
                                conceptId = (int)CareEntryPoint.Outreaches;
                                break;
                            case "PMTCT":
                                conceptId = (int)CareEntryPoint.ANC_PMTCT;
                                break;
                            case "Others":
                                conceptId = (int)CareEntryPoint.Others;
                                break;
                            case "TB DOTS":
                                conceptId = (int)CareEntryPoint.TB_DOT;
                                break;
                            default:
                                conceptId = (int)CareEntryPoint.VCT;
                                break;
                        }

                        var careEntryPointObs = new Obs
                        {
                            concept =((int)CareEntryPoint.concept).ToString(),
                            value = conceptId.ToString(),
                            groupMembers = new List<Obs>()
                        };
                        careEntryPointObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == careEntryPointObs.concept).UuId;
                        careEntryPointObs.value = nmsConcepts.FirstOrDefault(c => c.ConceptId == careEntryPointObs.value).UuId;
                        hivEnrolment.obs.Add(careEntryPointObs);
                    }
                }

                var marital_status = reader["marital_status"];
                if (marital_status != null)
                {
                    var maritalStatus = marital_status.ToString().Trim();

                    if (!string.IsNullOrEmpty(maritalStatus))
                    {
                        int conceptId;
                        switch (maritalStatus)
                        {
                            case "Windowed":
                                conceptId = (int)MaritalStatus.Widow_er;
                                break;
                            case "Widowed":
                                conceptId = (int)MaritalStatus.Widow_er;
                                break;
                            case "Married":
                                conceptId = (int)MaritalStatus.Married;
                                break;
                            case "Separated":
                                conceptId = (int)MaritalStatus.Separated;
                                break;
                            case "Divorced":
                                conceptId = (int)MaritalStatus.Divorced;
                                break;
                            case "Single":
                                conceptId = (int)MaritalStatus.Never_Married;
                                break;
                            default:
                                conceptId = (int)MaritalStatus.Never_Married;
                                break;
                        }

                        var maritalStatusObs = new Obs
                        {
                            concept =((int)MaritalStatus.concept).ToString(),
                            value = conceptId.ToString(),
                            groupMembers = new List<Obs>()
                        };
                        maritalStatusObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == maritalStatusObs.concept).UuId;
                        maritalStatusObs.value = nmsConcepts.FirstOrDefault(c => c.ConceptId == maritalStatusObs.value).UuId;
                        hivEnrolment.obs.Add(maritalStatusObs);
                    }
                }

                var education_status = reader["education"];
                if (education_status != null)
                {
                    var education = education_status.ToString().Trim();

                    if (!string.IsNullOrEmpty(education))
                    {
                        int conceptId;
                        switch (education)
                        {
                            case "Primary":
                                conceptId = (int)EducationLevel.Primary_Education;
                                break;
                            case "Junior Secondary":
                                conceptId = (int)EducationLevel.Secondary_Education;
                                break;
                            case "Senior Secondary":
                                conceptId = (int)EducationLevel.Secondary_Education;
                                break;
                            case "Quranic":
                                conceptId = (int)EducationLevel.Other;
                                break;
                            case "Post Secondary":
                                conceptId = (int)EducationLevel.Tertiary_Education;
                                break;
                            case "None":
                                conceptId = (int)EducationLevel.No_Education;
                                break;
                            default:
                                conceptId = (int)EducationLevel.No_Education;
                                break;
                        }

                        var educationObs = new Obs
                        {
                            concept =((int)EducationLevel.concept).ToString(),
                            value = conceptId.ToString(),
                            groupMembers = new List<Obs>()
                        };
                        educationObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == educationObs.concept).UuId;
                        educationObs.value = nmsConcepts.FirstOrDefault(c => c.ConceptId == educationObs.value).UuId;
                        hivEnrolment.obs.Add(educationObs);
                    }
                }

                var occupation_status = reader["occupation"];
                if (education_status != null)
                {
                    var occupation = occupation_status.ToString().Trim();

                    if (!string.IsNullOrEmpty(occupation))
                    {
                        int conceptId;
                        switch (occupation)
                        {
                            case "Student":
                                conceptId = (int)Occupation.Student;
                                break;
                            case "Employed":
                                conceptId = (int)Occupation.Employed;
                                break;
                            case "Unemployed":
                                conceptId = (int)Occupation.Unemployed;
                                break;
                            case "Retired":
                                conceptId = (int)Occupation.Retired;
                                break;                           
                            default:
                                conceptId = (int)Occupation.Unemployed;
                                break;
                        }

                        var occupationObs = new Obs
                        {
                            concept =((int)Occupation.concept).ToString(),
                            value = conceptId.ToString(),
                            groupMembers = new List<Obs>()
                        };
                        occupationObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == occupationObs.concept).UuId;
                        occupationObs.value = nmsConcepts.FirstOrDefault(c => c.ConceptId == occupationObs.value).UuId;
                        hivEnrolment.obs.Add(occupationObs);
                    }
                }

                var next_kin = reader["next_kin"];
                if (next_kin != null)
                {
                    var nextKin = next_kin.ToString().Trim();

                    if (!string.IsNullOrEmpty(nextKin))
                    {
                        var nextKinObs = new Obs
                        {
                            concept =((int)Concepts.NextOfKinName).ToString(),
                            value = Utilities.UnscrambleCharacters(nextKin),
                            groupMembers = new List<Obs>()
                        };
                        nextKinObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == nextKinObs.concept).UuId;
                        hivEnrolment.obs.Add(nextKinObs);
                    }
                }

                var phone_kin = reader["phone_kin"];
                if (phone_kin != null)
                {
                    var phoneKin = phone_kin.ToString().Trim();

                    if (!string.IsNullOrEmpty(phoneKin))
                    {
                        var phoneKinObs = new Obs
                        {
                            concept =((int)Concepts.NextOfKinPhone).ToString(),
                            value = Utilities.UnscrambleNumbers(phoneKin),
                            groupMembers = new List<Obs>()
                        };
                        phoneKinObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == phoneKinObs.concept).UuId;
                        hivEnrolment.obs.Add(phoneKinObs);
                    }
                }

                var relation_kin = reader["relation_kin"];
                if (relation_kin != null)
                {
                    var relationKin = relation_kin.ToString().Trim();

                    if (!string.IsNullOrEmpty(relationKin))
                    {
                        int conceptId;
                        switch (relationKin)
                        {
                            case "Mother":
                                conceptId = (int)NextOfKinRelationship.Mother;
                                break;
                            case "Sister":
                                conceptId = (int)NextOfKinRelationship.Sister;
                                break;
                            case "Aunt":
                                conceptId = (int)NextOfKinRelationship.Aunt;
                                break;
                            case "Father":
                                conceptId = (int)NextOfKinRelationship.Father;
                                break;

                            case "Treatment Supporter":
                                conceptId = (int)NextOfKinRelationship.Other;
                                break;
                            case "Treatment supporter":
                                conceptId = (int)NextOfKinRelationship.Other;
                                break;
                            case "Daughter":
                                conceptId = (int)NextOfKinRelationship.Daughter;
                                break;
                            case "Uncle":
                                conceptId = (int)NextOfKinRelationship.Uncle;
                                break;
                            case "Cousin":
                                conceptId = (int)NextOfKinRelationship.Other;
                                break;
                            case "Friend":
                                conceptId = (int)NextOfKinRelationship.Friend;
                                break;
                            case "Son":
                                conceptId = (int)NextOfKinRelationship.Son;
                                break;
                            case "Spouse":
                                var spouseConcept = gender == "Female" ? (int)NextOfKinRelationship.Husband : (int)NextOfKinRelationship.Wife;
                                conceptId = spouseConcept;
                                break;
                            case "Brother":
                                conceptId = (int)NextOfKinRelationship.Brother;
                                break;

                            default:
                                conceptId = (int)NextOfKinRelationship.Other;
                                break;
                        }

                        var relationKinObs = new Obs
                        {
                            concept =((int)NextOfKinRelationship.concept).ToString(),
                            value = conceptId.ToString(),
                            groupMembers = new List<Obs>()
                        };
                        relationKinObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == relationKinObs.concept).UuId;
                        relationKinObs.value = nmsConcepts.FirstOrDefault(c => c.ConceptId == relationKinObs.value).UuId;
                        hivEnrolment.obs.Add(relationKinObs);
                    }
                }

                var pregnant_status = reader["pregnant"];
                if (pregnant_status != null)
                {
                    var pregnant = pregnant_status.ToString().Trim();

                    if (!string.IsNullOrEmpty(pregnant))
                    {
                        string valueBoolean;
                        switch (pregnant)
                        {
                            case "0":
                                valueBoolean = "false";
                                break;
                            case "1":
                                valueBoolean = "true";
                                break;
                            default:
                                valueBoolean = "false";
                                break;
                        }

                        var pregnantObs = new Obs
                        {
                            concept = ((int)CurrentlyPregnant.concept).ToString(),
                            value = valueBoolean,
                            groupMembers = new List<Obs>()
                        };
                        pregnantObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == pregnantObs.concept).UuId;
                        hivEnrolment.obs.Add(pregnantObs);
                    }
                }

                var breastfeeding_status = reader["breastfeeding"];
                if (breastfeeding_status != null)
                {
                    var breastfeeding = breastfeeding_status.ToString().Trim();

                    if (!string.IsNullOrEmpty(breastfeeding))
                    {
                        int conceptId;
                        switch (breastfeeding)
                        {
                            case "0":
                                conceptId = (int)ChildBreastFeeding.No;
                                break;
                            case "1":
                                conceptId = (int)ChildBreastFeeding.Yes;
                                break;
                            default:
                                conceptId = (int)ChildBreastFeeding.No;
                                break;
                        }

                        var breastfeedingObs = new Obs
                        {
                            concept =((int)ChildBreastFeeding.concept).ToString(),
                            value = conceptId.ToString(),
                            groupMembers = new List<Obs>()
                        };
                        breastfeedingObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == breastfeedingObs.concept).UuId;
                        breastfeedingObs.value = nmsConcepts.FirstOrDefault(c => c.ConceptId == breastfeedingObs.value).UuId;
                        hivEnrolment.obs.Add(breastfeedingObs);
                    }
                }

                return hivEnrolment;
            }
            catch (Exception ex)
            {
                var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                Console.WriteLine(message);
                return new Encounter();
            }
        }
        public List<Encounter> BuildCareCardAndVitals(long patientId, string dateOfBirth)
        {            

            try
            {
                var vitalCare = new List<Encounter>();

                using (NpgsqlConnection connection = new NpgsqlConnection(pgconn))
                {
                    connection.Open();
                    var q = "SELECT * FROM clinic where patient_id =" + patientId + " and facility_id = " + _migOption.Facility;

                    using (NpgsqlCommand cmd = new NpgsqlCommand(q, connection))
                    {
                        using (NpgsqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {             
                                while (reader.Read())
                                {
                                    var obs = new List<Obs>();

                                    var careCard = new Encounter
                                    {
                                        encounterType = "0b8d256c-e5df-4801-9653-b6ae5b6e906b", //care card
                                        encounterDatetime = "",
                                        location = "b1a8b05e-3542-4037-bbd3-998ee9c40574", //using in-patient ward for now
                                        form = "5d522e63-463e-4a9f-a2c1-7ebbe4069a49", //care card
                                        //provider = "1c3db49d-440a-11e6-a65c-00e04c680037", //super user
                                        obs = new List<Obs>()
                                    };

                                    var vitals = new Encounter
                                    {
                                        encounterType = "",
                                        encounterDatetime = "",
                                        location = "b1a8b05e-3542-4037-bbd3-998ee9c40574", //using in-patient ward for now
                                        form = "a000cb34-9ec1-4344-a1c8-f692232f6edd", //vitals
                                        //provider = "1c3db49d-440a-11e6-a65c-00e04c680037", //super user
                                        obs = new List<Obs>()
                                    };

                                    var visitDateX = reader["date_visit"];     
                                    if(visitDateX != null)
                                    {
                                        var visitDateStr = visitDateX.ToString();
                                        if (DateTime.TryParse(visitDateStr.Trim(), out DateTime visitDate))
                                        {
                                            var visitDateObs = new Obs
                                            {
                                                concept =((int)Concepts.VisitDate).ToString(),
                                                value = visitDate.ToString("yyyy-MM-dd"),
                                                groupMembers = new List<Obs>()
                                            };
                                            visitDateObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == visitDateObs.concept).UuId;
                                            obs.Add(visitDateObs);

                                            careCard.encounterDatetime = visitDate.ToString("yyyy-MM-dd");
                                            vitals.encounterDatetime = visitDate.ToString("yyyy-MM-dd");

                                            var bpStr = reader["bp"];
                                            if(bpStr != null)
                                            {
                                                var bp = bpStr.ToString().Trim().Replace(" ", string.Empty);
                                                if (!string.IsNullOrEmpty(bp))
                                                {
                                                    if (bp != "/")
                                                    {
                                                        //Example: "110/70"
                                                        var bps = bp.Split('/');
                                                        if (bps.Length > 1)
                                                        {
                                                            var systolicObs = new Obs
                                                            {
                                                                concept = ((int)BloodPressure.Systolic).ToString(),
                                                                value = !string.IsNullOrEmpty(bps[0]) ? int.Parse(bps[0]) > 250 ? "250" : bps[0] : "110",
                                                                groupMembers = new List<Obs>()
                                                            };
                                                            systolicObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == systolicObs.concept).UuId;
                                                            obs.Add(systolicObs);

                                                            var outDiastObs = new Obs
                                                            {
                                                                concept = ((int)BloodPressure.Diastolic).ToString(),
                                                                value = !string.IsNullOrEmpty(bps[1]) ? int.Parse(bps[1]) > 150 ? "150" : bps[1] : "70",
                                                                groupMembers = new List<Obs>()
                                                            };
                                                            outDiastObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == outDiastObs.concept).UuId;
                                                            obs.Add(outDiastObs);
                                                        }
                                                    }
                                                }
                                            }
                                           
                                            var weightStr = reader["body_weight"];
                                            if(weightStr != null)
                                            {
                                                var weight = weightStr.ToString();
                                                float wOut = 0;
                                                if (!string.IsNullOrEmpty(weight))
                                                {
                                                    var wt = Regex.Match(weight, "\\d+");
                                                    var sts = float.TryParse(wt.ToString(), out wOut);

                                                    if (wOut > 0 && sts)
                                                    {
                                                        var weightObs = new Obs
                                                        {
                                                            concept =((int)Concepts.Weight).ToString(),
                                                            value = wOut > 250? "250" : wOut.ToString(),
                                                            groupMembers = new List<Obs>()
                                                        };
                                                        weightObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == weightObs.concept).UuId;
                                                        obs.Add(weightObs);
                                                        vitals.obs.Add(weightObs);
                                                        vitals.encounterType = "67a71486-1a54-468f-ac3e-7091a9a79584";
                                                    }
                                                }
                                            }
                                           
                                            var whoStageStr = reader["clinic_stage"];
                                            if (whoStageStr != null)
                                            {
                                                if (!string.IsNullOrEmpty(visitDateStr) && !string.IsNullOrEmpty(dateOfBirth))
                                                {
                                                    var ageAtVist = visitDate.Year - Convert.ToDateTime(dateOfBirth).Year;

                                                    var whoStage = whoStageStr.ToString();

                                                    if (!string.IsNullOrEmpty(whoStage))
                                                    {
                                                        int conceptId;

                                                        switch (whoStage)
                                                        {
                                                            case "Stage I":
                                                                conceptId = ageAtVist > 14 ? (int)WhoStage.adultStage1 : (int)WhoStage.paedStage1;
                                                                break;
                                                            case "Stage II":
                                                                conceptId = ageAtVist > 14 ? (int)WhoStage.adultStage2 : (int)WhoStage.paedStage2;
                                                                break;
                                                            case "Stage III":
                                                                conceptId = ageAtVist > 14 ? (int)WhoStage.adultStage3 : (int)WhoStage.paedStage3;
                                                                break;
                                                            case "Stage IV":
                                                                conceptId = ageAtVist > 14 ? (int)WhoStage.adultStage4 : (int)WhoStage.paedStage4;
                                                                break;
                                                            default:
                                                                conceptId = ageAtVist > 14 ? (int)WhoStage.adultStage1 : (int)WhoStage.paedStage1; //default: adult/paed stage 1
                                                                break;
                                                        }
                                                        var whoStageObs = new Obs
                                                        {
                                                            concept =((int)WhoStage.concept).ToString(),
                                                            value = conceptId.ToString(),
                                                            groupMembers = new List<Obs>()
                                                        };
                                                        whoStageObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == whoStageObs.concept).UuId;
                                                        whoStageObs.value = nmsConcepts.FirstOrDefault(c => c.ConceptId == whoStageObs.value).UuId;
                                                        obs.Add(whoStageObs);
                                                    }
                                                }
                                            }

                                            //pregnant
                                            var pregnStr = reader["pregnant"]; 
                                            if (pregnStr != null)
                                            {
                                                var pregn = pregnStr.ToString();
                                                if (!string.IsNullOrEmpty(pregn))
                                                {
                                                    int pregConceptId;

                                                    switch (pregn)
                                                    {
                                                        case "1":
                                                            pregConceptId = (int)PregnancyStatus.pregnant;
                                                            break;
                                                        case "0":
                                                            pregConceptId = (int)PregnancyStatus.notPregnant;
                                                            break;
                                                        default:
                                                            pregConceptId = (int)PregnancyStatus.notPregnant;
                                                            break;
                                                    }

                                                    var pregnObs = new Obs
                                                    {
                                                        concept =((int)PregnancyStatus.concept).ToString(),
                                                        value = pregConceptId.ToString(),
                                                        groupMembers = new List<Obs>()
                                                    };
                                                    pregnObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == pregnObs.concept).UuId;
                                                    pregnObs.value = nmsConcepts.FirstOrDefault(c => c.ConceptId == pregnObs.value).UuId;
                                                    obs.Add(pregnObs);
                                                }
                                            }
                                            
                                            //breastfeeding
                                            var breastfeedingStr = reader["breastfeeding"];
                                            if(breastfeedingStr != null)
                                            {
                                                var breastfeeding = breastfeedingStr.ToString();

                                                if (!string.IsNullOrEmpty(breastfeeding))
                                                {
                                                    if (breastfeeding.ToLower() == "1")
                                                    {
                                                        var pregnObs = new Obs
                                                        {
                                                            concept =((int)PregnancyStatus.concept).ToString(),
                                                            value = ((int)PregnancyStatus.breastFeeding).ToString(),
                                                            groupMembers = new List<Obs>()
                                                        };
                                                        pregnObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == pregnObs.concept).UuId;
                                                        pregnObs.value = nmsConcepts.FirstOrDefault(c => c.ConceptId == pregnObs.value).UuId;
                                                        obs.Add(pregnObs);
                                                    }
                                                }
                                            }
                                           
                                            var nextApptDateStr = reader["next_appointment"];
                                            if(nextApptDateStr != null)
                                            {
                                                var nextApptDate = nextApptDateStr.ToString();
                                                if (!string.IsNullOrEmpty(nextApptDate))
                                                {
                                                    if (DateTime.TryParse(nextApptDate.Trim(), out DateTime nextAppointment))
                                                    {
                                                        var nextAptDateObs = new Obs
                                                        {
                                                            concept = ((int)Concepts.nextAppointmentDate).ToString(),
                                                            value = nextAppointment.ToString("yyyy-MM-dd"),
                                                            groupMembers = new List<Obs>()
                                                        };
                                                        nextAptDateObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == nextAptDateObs.concept).UuId;
                                                        obs.Add(nextAptDateObs);
                                                    }                                                    
                                                }
                                            }
                                            
                                            var fStatus = reader["func_status"];
                                            if (fStatus != null)
                                            {
                                                var functionalStatus = fStatus.ToString().Trim().Replace(" ", string.Empty);

                                                if (!string.IsNullOrEmpty(functionalStatus))
                                                {
                                                    int conceptId;
                                                    switch (functionalStatus.ToLower())
                                                    {
                                                        case "working":
                                                            conceptId = (int)FunctionalStatus.working;
                                                            break;
                                                        case "bedridden":
                                                            conceptId = (int)FunctionalStatus.bedRidden;
                                                            break;
                                                        case "bed-ridden":
                                                            conceptId = (int)FunctionalStatus.bedRidden;
                                                            break;
                                                        case "ambulatory":
                                                            conceptId = (int)FunctionalStatus.ambulatory;
                                                            break;
                                                        default:
                                                            conceptId = (int)FunctionalStatus.working;
                                                            break;
                                                    }

                                                    var fStatusObs = new Obs
                                                    {
                                                        concept =((int)FunctionalStatus.concept).ToString(),
                                                        value = conceptId.ToString(),
                                                        groupMembers = new List<Obs>()
                                                    };
                                                    fStatusObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == fStatusObs.concept).UuId;
                                                    fStatusObs.value = nmsConcepts.FirstOrDefault(c => c.ConceptId == fStatusObs.value).UuId;
                                                    obs.Add(fStatusObs);
                                                }
                                            }

                                            var heightStr = reader["height"];
                                            if(heightStr != null)
                                            {
                                                var height  = heightStr.ToString();
                                                if (!string.IsNullOrEmpty(height))
                                                {
                                                    if(float.TryParse(height, out float ht))
                                                    {
                                                        var hts = "";
                                                        if (ht > 0)
                                                        {
                                                            var heghtX = (ht < 10) ? (ht * 10) : ht > 272 ? 272 : ht;
                                                            if (heghtX < 10)
                                                                heghtX = heghtX + 10;

                                                            hts = heghtX.ToString();
                                                        }
                                                        else
                                                        {
                                                            hts = "10";
                                                        }

                                                        var heightObs = new Obs
                                                        {
                                                            concept = ((int)Concepts.height).ToString(),
                                                            value = hts,
                                                            groupMembers = new List<Obs>()
                                                        };
                                                        heightObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == heightObs.concept).UuId;
                                                        obs.Add(heightObs);
                                                        vitals.obs.Add(heightObs);                                                       
                                                    }                                                     
                                                }
                                            }
                                            
                                            var tbStatus = reader["tb_status"];
                                            if (tbStatus != null)
                                            {
                                                var tbStatusStr = tbStatus.ToString().Trim();

                                                if (!string.IsNullOrEmpty(tbStatusStr))
                                                {
                                                    int conceptId;
                                                    switch (tbStatusStr.ToLower())
                                                    {
                                                        case "no sign or symptoms of tb":
                                                            conceptId = (int)TBStatus.noTbSigns;
                                                            break;
                                                        case "currently on tb treatment":
                                                            conceptId = (int)TBStatus.tbTreatment;
                                                            break;
                                                        case "tb suspected and referred for evaluation":
                                                            conceptId = (int)TBStatus.presumptiveTB;
                                                            break;
                                                        case "currently on inh prophylaxis":
                                                            conceptId = (int)TBStatus.inhProphylaxis;
                                                            break;
                                                        default:
                                                            conceptId = (int)TBStatus.noTbSigns;
                                                            break;
                                                    }

                                                    var tbStatusObs = new Obs
                                                    {
                                                        concept =((int)TBStatus.concept).ToString(),
                                                        value = conceptId.ToString(),
                                                        groupMembers = new List<Obs>()
                                                    };
                                                    tbStatusObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == tbStatusObs.concept).UuId;
                                                    tbStatusObs.value = nmsConcepts.FirstOrDefault(c => c.ConceptId == tbStatusObs.value).UuId;
                                                    obs.Add(tbStatusObs);
                                                }
                                            }

                                            var regimenTypeStr = reader["regimentype"];
                                            var regimenStr = reader["regimen"];
                                            
                                            if(regimenTypeStr != null && regimenStr != null)
                                            {
                                                var regimenType = regimenTypeStr.ToString().Trim();
                                                var regimen = regimenStr.ToString().Trim();
                                                if (!string.IsNullOrEmpty(regimenType) && !string.IsNullOrEmpty(regimen))
                                                {
                                                    var rootWord = regimenType.ToLower().Contains("children") ? "Children" : "Adult";
                                                    var rgs = regimens.Where(r => r.Answers == regimen && r.Values.Contains(rootWord)).ToList();
                                                    if (rgs.Any())
                                                    {
                                                        var rg = rgs[0];

                                                        // Current regimen Line
                                                        var currentRegLineObs = new Obs
                                                        {
                                                            concept =((int)CurrentRegimenLine.concept).ToString(),
                                                            value = rg.NMRSQuestionConceptID.ToString(),
                                                            groupMembers = new List<Obs>()
                                                        };
                                                        currentRegLineObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == currentRegLineObs.concept).UuId;
                                                        currentRegLineObs.value = nmsConcepts.FirstOrDefault(c => c.ConceptId == currentRegLineObs.value).UuId;
                                                        obs.Add(currentRegLineObs);

                                                        // Regimen
                                                        var regimenObs = new Obs
                                                        {
                                                            concept =rg.NMRSQuestionConceptID.ToString(),
                                                            value = rg.NMRSAnswerConceptID.ToString(),
                                                            groupMembers = new List<Obs>()
                                                        };
                                                        regimenObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == regimenObs.concept).UuId;
                                                        regimenObs.value = nmsConcepts.FirstOrDefault(c => c.ConceptId == regimenObs.value).UuId;
                                                        obs.Add(regimenObs);
                                                    }

                                                }
                                            }
                                            
                                            var adherence = reader["adherence_level"];
                                            if (adherence != null)
                                            {
                                                var drugAdherence = adherence.ToString().Trim();

                                                if (!string.IsNullOrEmpty(drugAdherence))
                                                {
                                                    int conceptId;
                                                    switch (drugAdherence.ToLower())
                                                    {
                                                        case "fair":
                                                            conceptId = (int)DrugAdhenrence.Fair;
                                                            break;
                                                        case "good":
                                                            conceptId = (int)DrugAdhenrence.Good;
                                                            break;
                                                        case "poor":
                                                            conceptId = (int)DrugAdhenrence.Poor;
                                                            break;
                                                        default:
                                                            conceptId = (int)DrugAdhenrence.Good;
                                                            break;
                                                    }

                                                    var drugAdherenceObs = new Obs
                                                    {
                                                        concept =((int)DrugAdhenrence.concept).ToString(),
                                                        value = conceptId.ToString(),
                                                        groupMembers = new List<Obs>()
                                                    };
                                                    drugAdherenceObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == drugAdherenceObs.concept).UuId;
                                                    drugAdherenceObs.value = nmsConcepts.FirstOrDefault(c => c.ConceptId == drugAdherenceObs.value).UuId;
                                                    obs.Add(drugAdherenceObs);
                                                }
                                            }

                                            var oiIds = reader["oi_ids"];
                                            if (oiIds != null)
                                            {
                                                var oisX = oiIds.ToString();
                                                if(!string.IsNullOrEmpty(oisX))
                                                {
                                                    var ois = oisX.ToString().Trim().Split(',').ToList();
                                                    ois.ForEach(o =>
                                                    {
                                                        int conceptId;
                                                        switch (o)
                                                        {
                                                            case "1":
                                                                conceptId = (int)OIs.Herpes_Zoster;
                                                                break;
                                                            case "2":
                                                                conceptId = (int)OIs.Pneumonia;
                                                                break;
                                                            case "3":
                                                                conceptId = (int)OIs.Dementia_Encephalitis;
                                                                break;
                                                            case "4":
                                                                conceptId = (int)OIs.Candidaisis_Oral_Virginal;
                                                                break;
                                                            case "5":
                                                                conceptId = (int)OIs.Fever;
                                                                break;
                                                            case "6":
                                                                conceptId = (int)OIs.Cough;
                                                                break;
                                                            case "7":
                                                                conceptId = (int)OIs.SkinInfection;
                                                                break;
                                                            default:
                                                                conceptId = (int)OIs.Fever;
                                                                break;
                                                        }

                                                        var oiObs = new Obs
                                                        {
                                                            concept =((int)OIs.concept).ToString(),
                                                            value = conceptId.ToString(),
                                                            groupMembers = new List<Obs>()
                                                        };
                                                        oiObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == oiObs.concept).UuId;
                                                        oiObs.value = nmsConcepts.FirstOrDefault(c => c.ConceptId == oiObs.value).UuId;
                                                        obs.Add(oiObs);
                                                    });
                                                }                                                
                                            }

                                            careCard.obs = obs;

                                            vitalCare.Add(careCard);
                                            if(!string.IsNullOrEmpty(vitals.encounterType))
                                                vitalCare.Add(vitals);
                                        }
                                    }                                 
                                    
                                }
                            }
                        }
                    }
                }

                return vitalCare;
            }
            catch (Exception ex)
            {
                var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                Console.WriteLine(message);
                return new List<Encounter>();
            }
        }
        public List<Encounter> BuildPharmacy(long patientId, string date_of_birth)
        {            
            var pharmacies = new List<Encounter>();
            try
            {
                using (NpgsqlConnection connection = new NpgsqlConnection(pgconn))
                {
                    connection.Open();
                    
                    var q = "select pharmacy_id,patient_id,facility_id,date_visit, r.description as regimen, rt.description as regimentype,duration,morning,afternoon,evening,adherence,next_appointment,time_stamp from (select * from pharmacy where patient_id = " + patientId + " and facility_id = " + _migOption.Facility + ") as p join (select * from regimen) r on p.regimen_id = r.regimen_id join (select * from regimentype) rt on p.regimentype_id = rt.regimentype_id";

                    using (NpgsqlCommand cmd = new NpgsqlCommand(q, connection))
                    {
                        using (NpgsqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    var visitDateX = reader["date_visit"];
                                    if (visitDateX != null)
                                    {
                                        var visitDateStr = visitDateX.ToString();
                                        if (DateTime.TryParse(visitDateStr.Trim(), out DateTime visitDate))
                                        {                     

                                            var pharmacy = new Encounter
                                            {
                                                encounterType = "a1fa6aa3-59e1-4833-a28c-bb62f2fb07df", //pharmacy
                                                encounterDatetime = "",
                                                location = "7f65d926-57d6-4402-ae10-a5b3bcbf7986", //pharmacy
                                                form = "4a238dc4-a76b-4c0f-a100-229d98fd5758", //pharmacy form
                                                                                               //provider = "1c3db49d-440a-11e6-a65c-00e04c680037", //super user
                                                obs = new List<Obs>()
                                            };

                                            pharmacy.encounterDatetime = visitDate.ToString("yyyy-MM-dd");

                                            var exs = pharmacies.Where(d => d.encounterDatetime == pharmacy.encounterDatetime).ToList();
                                            if (exs.Any())
                                            {
                                                pharmacy = exs[0];
                                            }

                                            var visitDateObs = new Obs
                                            {
                                                concept = ((int)Concepts.VisitDate).ToString(),
                                                value = visitDate.ToString("yyyy-MM-dd"),
                                                groupMembers = new List<Obs>()
                                            };
                                            visitDateObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == visitDateObs.concept).UuId;

                                            if (!pharmacy.obs.Any(t => t.concept == visitDateObs.concept))
                                                pharmacy.obs.Add(visitDateObs);
                                                                          
                                            // ----------- Treatment Age Category
                                            if (!string.IsNullOrEmpty(visitDateStr) && !string.IsNullOrEmpty(date_of_birth))
                                            {
                                                var ageAtVist = visitDate.Year - Convert.ToDateTime(date_of_birth).Year;
                                                
                                                var trxAgeObs = new Obs
                                                {
                                                    concept =((int)TreatmentAge.concept).ToString(),
                                                    value = ageAtVist > 14? ((int)TreatmentAge.Adult).ToString() : ((int)TreatmentAge.Child).ToString(),
                                                    groupMembers = new List<Obs>()
                                                };
                                                trxAgeObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == trxAgeObs.concept).UuId;
                                                trxAgeObs.value = nmsConcepts.FirstOrDefault(c => c.ConceptId == trxAgeObs.value).UuId;

                                                if (!pharmacy.obs.Any(t => t.concept == trxAgeObs.concept))
                                                    pharmacy.obs.Add(trxAgeObs);
                                            }                                           

                                            var regimenTypeStr = reader["regimentype"];
                                            var regimenStr = reader["regimen"];

                                            if (regimenTypeStr != null && regimenStr != null)
                                            {
                                                var regimenType = regimenTypeStr.ToString().Trim();
                                                var regimen = regimenStr.ToString().Trim();
                                                if (!string.IsNullOrEmpty(regimenType) && !string.IsNullOrEmpty(regimen))
                                                {
                                                    var rootWord = regimenType.ToLower().Contains("children") ? "Children" : "Adult";
                                                    var rgs = regimens.Where(r => r.Answers == regimen && r.Values.Contains(rootWord)).ToList();
                                                    if (rgs.Any())
                                                    {
                                                        var rg = rgs[0];

                                                        // Human immunodeficiency virus treatment regimen
                                                        var grObs = new Obs
                                                        {
                                                            concept = ((int)ARVRegimen.concept).ToString(),
                                                            groupMembers = new List<Obs>()
                                                        };
                                                        grObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == grObs.concept).UuId;


                                                        // Current regimen Line
                                                        var currentRegLineObs = new Obs
                                                        {
                                                            concept =((int)CurrentRegimenLine.concept).ToString(),
                                                            value = rg.NMRSQuestionConceptID.ToString(),
                                                            groupMembers = new List<Obs>()
                                                        };
                                                        currentRegLineObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == currentRegLineObs.concept).UuId;
                                                        currentRegLineObs.value = nmsConcepts.FirstOrDefault(c => c.ConceptId == currentRegLineObs.value).UuId;

                                                        if (!pharmacy.obs.Any(t => t.concept == currentRegLineObs.concept))
                                                            pharmacy.obs.Add(currentRegLineObs);

                                                        // Regimen
                                                        var regimenObs = new Obs
                                                        {
                                                            concept =rg.NMRSQuestionConceptID.ToString(),
                                                            value = rg.NMRSAnswerConceptID.ToString(),
                                                            groupMembers = new List<Obs>()
                                                        };
                                                        regimenObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == regimenObs.concept).UuId;
                                                        regimenObs.value = nmsConcepts.FirstOrDefault(c => c.ConceptId == regimenObs.value).UuId;

                                                        if (!pharmacy.obs.Any(t => t.concept == regimenObs.concept))
                                                            pharmacy.obs.Add(regimenObs);

                                                        //Treatment type
                                                        var treatmentTyeObs = new Obs
                                                        {
                                                            concept = ((int)TreatmentType.concept).ToString(),
                                                            value = ((int)TreatmentType.AntiretroviralTherapy).ToString(),
                                                            groupMembers = new List<Obs>()
                                                        };
                                                        treatmentTyeObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == treatmentTyeObs.concept).UuId;
                                                        treatmentTyeObs.value = nmsConcepts.FirstOrDefault(c => c.ConceptId == treatmentTyeObs.value).UuId;

                                                        if (!pharmacy.obs.Any(t => t.concept == treatmentTyeObs.concept))
                                                            pharmacy.obs.Add(treatmentTyeObs);

                                                        var durationStr = reader["duration"];
                                                        if (durationStr != null)
                                                        {
                                                            var duration = durationStr.ToString();
                                                            int durationOut = 0;
                                                            if (!string.IsNullOrEmpty(duration))
                                                            {
                                                                var wt = Regex.Match(duration, "\\d+");
                                                                var sts = int.TryParse(duration, out durationOut);

                                                                if (durationOut > 0 && sts)
                                                                {
                                                                    var durationX = durationOut == 0 ? "30" : durationOut > 180 ? "180" : durationOut.ToString();

                                                                    var durationObs = new Obs
                                                                    {
                                                                        concept = ((int)Concepts.MedicationDuration).ToString(),
                                                                        value = durationX,
                                                                        groupMembers = new List<Obs>()
                                                                    };
                                                                    durationObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == durationObs.concept).UuId;

                                                                    if (!grObs.groupMembers.Any(t => t.concept == durationObs.concept))
                                                                        grObs.groupMembers.Add(durationObs);

                                                                    // ------ Quantity of medication prescribed per dose
                                                                    var prescribedObs = new Obs
                                                                    {
                                                                        concept = ((int)Concepts.QuantityPrescribed).ToString(),
                                                                        value = durationX,
                                                                        groupMembers = new List<Obs>()
                                                                    };
                                                                    prescribedObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == prescribedObs.concept).UuId;

                                                                    if (!grObs.groupMembers.Any(t => t.concept == prescribedObs.concept))
                                                                        grObs.groupMembers.Add(prescribedObs);

                                                                    // ------ Medication dispensed
                                                                    var dispensedObs = new Obs
                                                                    {
                                                                        concept = ((int)Concepts.QuantityDispensed).ToString(),
                                                                        value = durationX,
                                                                        groupMembers = new List<Obs>()
                                                                    };
                                                                    dispensedObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == dispensedObs.concept).UuId;

                                                                    if (!grObs.groupMembers.Any(t => t.concept == dispensedObs.concept))
                                                                        grObs.groupMembers.Add(dispensedObs);
                                                                }
                                                            }
                                                        }


                                                        if (!pharmacy.obs.Any(t => t.concept == grObs.concept))
                                                            pharmacy.obs.Add(grObs);
                                                    }
                                                    else
                                                    {
                                                        var drgs = drugs.Where(r => (r.NAME.ToLower() + r.STRENGTH) == regimen.ToLower().Replace(" ", string.Empty)).ToList();
                                                        if (drgs.Any())
                                                        {
                                                            var drg = drgs[0];                                                       
                                                            var dName = drg.NAME.ToLower();

                                                            var oiGrpObs = new Obs
                                                            {
                                                                concept = drg.GROUPINGCONCEPT.ToString(),
                                                                groupMembers = new List<Obs>()
                                                            };
                                                            oiGrpObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == oiGrpObs.concept).UuId;

                                                            var oiDrugObs = new Obs
                                                            {
                                                                concept = drg.OPENMRSQUESTIONCONCEPT.ToString(),
                                                                value = drg.OPENMRSDRUGCONCEPTID.ToString(),
                                                                groupMembers = new List<Obs>()
                                                            };
                                                            oiDrugObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == oiDrugObs.concept).UuId;
                                                            oiDrugObs.value = nmsConcepts.FirstOrDefault(c => c.ConceptId == oiDrugObs.value).UuId;

                                                            if (!oiGrpObs.groupMembers.Any(t => t.concept == oiDrugObs.concept))
                                                                oiGrpObs.groupMembers.Add(oiDrugObs);

                                                            if (!pharmacy.obs.Any(t => t.concept == oiGrpObs.concept))
                                                                pharmacy.obs.Add(oiGrpObs);                                                            

                                                            //Treatment type
                                                            var treatmentTyeObs = new Obs
                                                            {
                                                                concept = ((int)TreatmentType.concept).ToString(),
                                                                value = ((int)TreatmentType.Non_ART).ToString(),
                                                                groupMembers = new List<Obs>()
                                                            };
                                                            treatmentTyeObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == treatmentTyeObs.concept).UuId;
                                                            treatmentTyeObs.value = nmsConcepts.FirstOrDefault(c => c.ConceptId == treatmentTyeObs.value).UuId;

                                                            if (!pharmacy.obs.Any(t => t.concept == treatmentTyeObs.concept))
                                                                pharmacy.obs.Add(treatmentTyeObs);

                                                            var durationStr = reader["duration"];
                                                            if (durationStr != null)
                                                            {
                                                                var duration = durationStr.ToString();
                                                                int durationOut = 0;
                                                                if (!string.IsNullOrEmpty(duration))
                                                                {
                                                                    var wt = Regex.Match(duration, "\\d+");
                                                                    var sts = int.TryParse(duration, out durationOut);

                                                                    if (durationOut > 0 && sts)
                                                                    {
                                                                        var durationX = durationOut == 0 ? "30" : durationOut > 180 ? "180" : durationOut.ToString();

                                                                        var durationObs = new Obs
                                                                        {
                                                                            concept = ((int)Concepts.OIDuration).ToString(),
                                                                            value = durationX,
                                                                            groupMembers = new List<Obs>()
                                                                        };
                                                                        durationObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == durationObs.concept).UuId;

                                                                        if (!oiGrpObs.groupMembers.Any(t => t.concept == durationObs.concept))
                                                                            oiGrpObs.groupMembers.Add(durationObs);

                                                                        // ------ Quantity of medication prescribed per dose
                                                                        var prescribedObs = new Obs
                                                                        {
                                                                            concept = ((int)Concepts.OIQtyPrescribed).ToString(),
                                                                            value = durationX,
                                                                            groupMembers = new List<Obs>()
                                                                        };
                                                                        prescribedObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == prescribedObs.concept).UuId;

                                                                        if (!oiGrpObs.groupMembers.Any(t => t.concept == prescribedObs.concept))
                                                                            oiGrpObs.groupMembers.Add(prescribedObs);

                                                                        // ------ Medication dispensed
                                                                        var dispensedObs = new Obs
                                                                        {
                                                                            concept = ((int)Concepts.OIQtyDispensed).ToString(),
                                                                            value = durationX,
                                                                            groupMembers = new List<Obs>()
                                                                        };
                                                                        dispensedObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == dispensedObs.concept).UuId;

                                                                        if (!oiGrpObs.groupMembers.Any(t => t.concept == dispensedObs.concept))
                                                                            oiGrpObs.groupMembers.Add(dispensedObs);
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }

                                                }
                                            }

                                            //---- Adherence counselling
                                            var adherence = reader["adherence"];
                                            if (adherence != null)
                                            {
                                                var drugAdherence = adherence.ToString().Trim();

                                                if (!string.IsNullOrEmpty(drugAdherence))
                                                {
                                                    int conceptId;
                                                    switch (drugAdherence.ToLower())
                                                    {
                                                        case "1":
                                                            conceptId = (int)AdherenceCounselling.Yes;
                                                            break;
                                                        case "0":
                                                        default:
                                                            conceptId = (int)AdherenceCounselling.No;
                                                            break;
                                                    }

                                                    var drugAdherenceCounsellingObs = new Obs
                                                    {
                                                        concept =((int)AdherenceCounselling.concept).ToString(),
                                                        value = conceptId.ToString(),
                                                        groupMembers = new List<Obs>()
                                                    };
                                                    drugAdherenceCounsellingObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == drugAdherenceCounsellingObs.concept).UuId;
                                                    drugAdherenceCounsellingObs.value = nmsConcepts.FirstOrDefault(c => c.ConceptId == drugAdherenceCounsellingObs.value).UuId;

                                                    if (!pharmacy.obs.Any(t => t.concept == drugAdherenceCounsellingObs.concept))
                                                        pharmacy.obs.Add(drugAdherenceCounsellingObs);
                                                }
                                            }

                                            // ---- Pick up Reason                                            
                                            var pickUpReasonObs = new Obs
                                            {
                                                concept =((int)PickUpReason.concept).ToString(),
                                                value = ((int)PickUpReason.Refill).ToString(),
                                                groupMembers = new List<Obs>()
                                            };
                                            pickUpReasonObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == pickUpReasonObs.concept).UuId;
                                            pickUpReasonObs.value = nmsConcepts.FirstOrDefault(c => c.ConceptId == pickUpReasonObs.value).UuId;

                                            if (!pharmacy.obs.Any(t => t.concept == pickUpReasonObs.concept))
                                                pharmacy.obs.Add(pickUpReasonObs);

                                            //------ Visit Type                                          
                                            var visitTypeObs = new Obs
                                            {
                                                concept =((int)VisitType.concept).ToString(),
                                                value = ((int)VisitType.ReturnVisitType).ToString(),
                                                groupMembers = new List<Obs>()
                                            };
                                            visitTypeObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == visitTypeObs.concept).UuId;
                                            visitTypeObs.value = nmsConcepts.FirstOrDefault(c => c.ConceptId == visitTypeObs.value).UuId;

                                            if (!pharmacy.obs.Any(t => t.concept == visitTypeObs.concept))
                                                pharmacy.obs.Add(visitTypeObs);

                                            //-------- Date ordered                                            
                                            var dateOrderedObs = new Obs
                                            {
                                                concept =((int)Concepts.DateOrdered).ToString(),
                                                value = visitDate.ToString("yyyy-MM-dd"),
                                                groupMembers = new List<Obs>()
                                            };
                                            dateOrderedObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == dateOrderedObs.concept).UuId;
                                            
                                            if (!pharmacy.obs.Any(t => t.concept == dateOrderedObs.concept))
                                                pharmacy.obs.Add(dateOrderedObs);

                                            //-------- Date dispensed                                            
                                            var dateDispensedObs = new Obs
                                            {
                                                concept =((int)Concepts.DateDispensed).ToString(),
                                                value = visitDate.ToString("yyyy-MM-dd"),
                                                groupMembers = new List<Obs>()
                                            };
                                            dateDispensedObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == dateDispensedObs.concept).UuId;

                                            if (!pharmacy.obs.Any(t => t.concept == dateDispensedObs.concept))
                                            {
                                                pharmacy.obs.Add(dateDispensedObs);
                                            }
                                            else
                                            {
                                                if(pharmacy.obs.Count(t => t.concept == dateDispensedObs.concept) < 2)
                                                    pharmacy.obs.Add(dateDispensedObs);
                                            }
                                                                                                                                    
                                            if (!pharmacies.Any(d => d.encounterDatetime == pharmacy.encounterDatetime))
                                            {
                                                pharmacies.Add(pharmacy);
                                            }                                            
                                        }
                                    }

                                }
                            }
                        }
                    }
                }
                return pharmacies;
            }
            catch (Exception ex)
            {
                var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                Console.WriteLine(message);
                return new List<Encounter>();
            }
        }
        public List<Encounter> BuildLab(long patientId)
        {
            try
            {
                var labTets = new List<Encounter>();
                                               
                var labDataList = GetLabData(patientId);
                if (labDataList.Any())
                {
                    labDataList.ForEach(lab =>
                    {
                        var visitDateX = lab.date_collected;
                        if (visitDateX != null)
                        {
                            var visitDateStr = visitDateX.ToString();
                            if (DateTime.TryParse(visitDateStr.Trim(), out DateTime visitDate))
                            {
                                var l = new Encounter
                                {                                    
                                    encounterDatetime = "",
                                    location = "7f65d926-57d6-4402-ae10-a5b3bcbf7986", //pharmacy
                                    form = "889ce948-f1ee-4656-91af-147a9e760309", //lab order and result form
                                    obs = new List<Obs>()
                                };

                                var visitDateObs = new Obs
                                {
                                    concept = ((int)Concepts.VisitDate).ToString(),
                                    value = visitDate.ToString("yyyy-MM-dd"),
                                    groupMembers = new List<Obs>()
                                };
                                visitDateObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == visitDateObs.concept).UuId;
                                l.obs.Add(visitDateObs);

                                l.encounterDatetime = visitDate.ToString("yyyy-MM-dd");

                                // Date collected
                                var date_c = new Obs
                                {
                                    concept = ((int)Concepts.DateCollected).ToString(),
                                    value = visitDate.ToString("yyyy-MM-dd"),
                                    groupMembers = new List<Obs>()
                                };
                                date_c.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == date_c.concept).UuId;
                                l.obs.Add(date_c);

                                // Date reported
                                if (!string.IsNullOrEmpty(lab.date_reported))
                                {
                                    if (DateTime.TryParse(lab.date_reported.Trim(), out DateTime date_reported))
                                    {
                                        var date_r = new Obs
                                        {
                                            concept = ((int)Concepts.DateReported).ToString(),
                                            value = date_reported.ToString("yyyy-MM-dd"),
                                            groupMembers = new List<Obs>()
                                        };
                                        date_r.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == date_r.concept).UuId;
                                        l.obs.Add(date_r);
                                    }
                                }

                                // lab_no
                                if (!string.IsNullOrEmpty(lab.labno))
                                {
                                    var lab_no = new Obs
                                    {
                                        concept = ((int)Concepts.LabNumber).ToString(),
                                        value = lab.labno,
                                        groupMembers = new List<Obs>()
                                    };
                                    lab_no.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == lab_no.concept).UuId;
                                    l.obs.Add(lab_no);
                                }

                                //Tests made and results
                                //check for result   
                                if (!string.IsNullOrEmpty(lab.resultab))
                                {
                                    var testType = labs.FirstOrDefault(m => m.Labtest_Id == lab.labtest_id);
                                    if(testType != null)
                                    {
                                        ////if(!string.IsNullOrEmpty(testType.ConceptBoolean))
                                        ////{
                                        ////    var labTestTypeObs = new Obs
                                        ////    {
                                        ////        concept = testType.ConceptBoolean,
                                        ////        value = "true",
                                        ////        groupMembers = new List<Obs>()
                                        ////    };
                                        ////    labTestTypeObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == labTestTypeObs.concept).UuId;
                                        ////    l.obs.Add(labTestTypeObs);
                                        ////}

                                        if (!string.IsNullOrEmpty(testType.Datatype))
                                        {
                                            if(testType.Datatype.ToLower() == "coded")
                                            {
                                                var outcome = lab.resultab == "0" ? testType.Negative : testType.Negative;
                                                var labTestTypeObs = new Obs
                                                {
                                                    concept = testType.Openmrsabsoluteconceptid,
                                                    value = outcome,
                                                    groupMembers = new List<Obs>()
                                                };
                                                labTestTypeObs.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == labTestTypeObs.concept).UuId;
                                                labTestTypeObs.value = nmsConcepts.FirstOrDefault(c => c.ConceptId == labTestTypeObs.value).UuId;
                                                l.obs.Add(labTestTypeObs);
                                            }       
                                            
                                            if(testType.Datatype.ToLower() == "numeric" && !string.IsNullOrEmpty(lab.resultab))
                                            {
                                                var outcome = 0;
                                                var maxValue = 0;
                                                var minValue = 0;

                                                if (!string.IsNullOrEmpty(testType.MaximumValue))
                                                {
                                                    if(int.TryParse(testType.MaximumValue, out int max_Value))
                                                    {
                                                        maxValue = max_Value;
                                                    }
                                                }

                                                if (!string.IsNullOrEmpty(testType.MinimumValue))
                                                {
                                                    if (int.TryParse(testType.MinimumValue, out int min_Value))
                                                    {
                                                        minValue = min_Value;
                                                    }
                                                }

                                                if (int.TryParse(lab.resultab, out int out_come))
                                                {
                                                    outcome = out_come;
                                                }

                                                var result = maxValue > 0 && outcome > maxValue ? maxValue : !string.IsNullOrEmpty(testType.MinimumValue) && outcome < minValue ? minValue : outcome;

                                                var lab_test = new Obs
                                                {
                                                    concept = testType.Openmrsabsoluteconceptid,
                                                    value = result.ToString(),
                                                    groupMembers = new List<Obs>()
                                                };
                                                lab_test.concept = nmsConcepts.FirstOrDefault(c => c.ConceptId == lab_test.concept).UuId;
                                                l.obs.Add(lab_test);
                                            }
                                        }

                                        l.encounterType = "7ccf3847-7bc3-42e5-8b7e-4125712660ea"; //lab
                                    }
                                    
                                }
                                labTets.Add(l);
                            }
                        }

                    });
                }

                return labTets;
            }
            catch(Exception ex)
            {
                var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                Console.WriteLine(message);
                return new List<Encounter>();
            }
        }
        public string SanitiseDrug(string regimen)
        {
            if(regimen.Contains('(') || regimen.Contains(')'))
            {
                regimen = Regex.Replace(regimen, @" ?\(.*?\)", string.Empty); //remove drug strengths and brackets
                SanitiseDrug(regimen);
            }
            if(!regimen.Contains("NRT"))
            {
                regimen = regimen.Replace('+', '-').Replace('/', '-').Replace("-r", "/r");
            }
            
            return regimen;
        }
        public List<ARTModel> GetARTCommencementMap()
        {
            try
            {
                var path = Path.Combine(rootDir, @"Templates", @"ARTCommencement.xlsx");
                FileInfo fileInfo = new FileInfo(path);
                var artcommencementMaps = new List<ARTModel>();

                using (var package = new ExcelPackage(fileInfo))
                {
                    var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                    // get number of rows and columns in the sheets
                    int rows = worksheet.Dimension.Rows;
                    

                    // loop through the worksheet rows and columns
                    for (int i = 2; i <= rows; i++)
                    {
                        var variableName = worksheet.Cells["A" + i].Value;
                        var variablePosition = worksheet.Cells["B" + i].Value;
                        var dataType = worksheet.Cells["C" + i].Value;
                        var lamisAnswer = worksheet.Cells["D" + i].Value;
                        var oMRSConceptID = worksheet.Cells["E" + i].Value;
                        var oMRSAnswerID = worksheet.Cells["F" + i].Value;

                        artcommencementMaps.Add(new ARTModel
                        {
                            VariableName = variableName != null? variableName.ToString().ToLower() : "",
                            VariablePosition = variablePosition != null? variablePosition.ToString(): "",
                            DataType = dataType!= null? dataType.ToString() : "",
                            LamisAnswer = lamisAnswer != null? lamisAnswer.ToString() : "",
                            OMRSConceptID = oMRSConceptID != null? oMRSConceptID.ToString() : "",
                            OMRSAnswerID = oMRSAnswerID != null? oMRSAnswerID.ToString() : ""
                        });
                    }

                }

                return artcommencementMaps;
            }
            catch (Exception ex)
            {
                var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                Console.WriteLine(message);
                return new List<ARTModel>();
            }
        }
        public List<Regimen> GetRegimen()
        {
            var regimens = new List<Regimen>();
            try
            {                
                var path = Path.Combine(rootDir, @"Templates", @"NMRSRegimen.xlsx");
                FileInfo fileInfo = new FileInfo(path);
                using (var package = new ExcelPackage(fileInfo))
                {
                    var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                    // get number of rows and columns in the sheets
                    int rows = worksheet.Dimension.Rows;
                    

                    // loop through the worksheet rows and columns
                    for (int i = 2; i <= rows; i++)
                    {
                        var variable = worksheet.Cells["A" + i].Value;
                        var values = worksheet.Cells["B" + i].Value;
                        var answers = worksheet.Cells["C" + i].Value;
                        var answerCode = worksheet.Cells["D" + i].Value;
                        var questionID = worksheet.Cells["E" + i].Value;
                        var nmrsQuestionConceptID = worksheet.Cells["F" + i].Value;
                        var nmrsAnswerConceptID = worksheet.Cells["G" + i].Value;

                        regimens.Add(new Regimen
                        {
                            Variable = variable != null ? variable.ToString().ToLower() : "",
                            Values = values != null ? values.ToString() : "",
                            Answers = answers != null ? answers.ToString() : "",
                            AnswerCode = answerCode != null ? answerCode.ToString() : "",
                            QuestionID = questionID != null ? questionID.ToString() : "",
                            NMRSQuestionConceptID = nmrsQuestionConceptID != null ? nmrsQuestionConceptID.ToString() : "",
                            NMRSAnswerConceptID = nmrsAnswerConceptID != null ? nmrsAnswerConceptID.ToString() : ""
                        });                        
                    }

                }
                return regimens;
            }
            catch (Exception ex)
            {
                var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                Console.WriteLine(message);
                return regimens;
            }
        }
        public List<Drug> GetDrugs()
        {
            var drugs = new List<Drug>();
            try
            {
                var path = Path.Combine(rootDir, @"Templates", @"drugcoding.xlsx");
                FileInfo fileInfo = new FileInfo(path);
                using (var package = new ExcelPackage(fileInfo))
                {
                    var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                    // get number of rows and columns in the sheets
                    int rows = worksheet.Dimension.Rows;
                    

                    // loop through the worksheet rows and columns
                    for (int i = 2; i <= rows; i++)
                    {
                        var abbrev = worksheet.Cells["A" + i].Value;
                        var name = worksheet.Cells["B" + i].Value;
                        var strenght = worksheet.Cells["C" + i].Value;
                        var morning = worksheet.Cells["D" + i].Value;
                        var afternoon = worksheet.Cells["E" + i].Value;
                        var evening = worksheet.Cells["F" + i].Value;
                        var openmrsQuestionConcept = worksheet.Cells["G" + i].Value;
                        var openmrsDDrugConceptId = worksheet.Cells["H" + i].Value;
                        var strengthConceptId = worksheet.Cells["I" + i].Value;
                        var groupingConcept = worksheet.Cells["J" + i].Value;

                        drugs.Add(new Drug
                        {
                            ABBREV = abbrev != null ? abbrev.ToString().ToLower() : "",
                            NAME = name != null ? name.ToString().ToLower() : "",
                            STRENGTH = strenght != null ? strenght.ToString().ToLower() : "",
                            MORNING = morning != null ? morning.ToString().ToLower() : "",
                            AFTERNOON = afternoon != null ? afternoon.ToString().ToLower() : "",
                            EVENING = evening != null ? evening.ToString().ToLower() : "",
                            OPENMRSQUESTIONCONCEPT = openmrsQuestionConcept != null ? openmrsQuestionConcept.ToString().ToLower() : "",
                            OPENMRSDRUGCONCEPTID = openmrsDDrugConceptId != null ? openmrsDDrugConceptId.ToString().ToLower() : "",
                            STRENGTHCONCEPTID = strengthConceptId != null ? strengthConceptId.ToString().ToLower() : "",
                            GROUPINGCONCEPT = groupingConcept != null ? groupingConcept.ToString().ToLower() : "",
                        });
                    }

                }
                return drugs;
            }
            catch (Exception ex)
            {
                var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                Console.WriteLine(message);
                return drugs;
            }
        }
        public List<ARTModel> GetExitMap()
        {
            try
            {
                var path = Path.Combine(rootDir, @"Templates", @"Exit.xlsx");
                FileInfo fileInfo = new FileInfo(path);
                var exitMaps = new List<ARTModel>();

                using (var package = new ExcelPackage(fileInfo))
                {
                    var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                    // get number of rows and columns in the sheets
                    int rows = worksheet.Dimension.Rows;
                    

                    // loop through the worksheet rows and columns
                    for (int i = 2; i <= rows; i++)
                    {
                        var variableName = worksheet.Cells["A" + i].Value;
                        var variablePosition = worksheet.Cells["B" + i].Value;
                        var dataType = worksheet.Cells["C" + i].Value;
                        var lamisAnswer = worksheet.Cells["D" + i].Value;
                        var oMRSConceptID = worksheet.Cells["E" + i].Value;
                        var oMRSAnswerID = worksheet.Cells["F" + i].Value;

                        exitMaps.Add(new ARTModel
                        {
                            VariableName = variableName != null ? variableName.ToString().ToLower() : "",
                            VariablePosition = variablePosition != null ? variablePosition.ToString() : "",
                            DataType = dataType != null ? dataType.ToString() : "",
                            LamisAnswer = lamisAnswer != null ? lamisAnswer.ToString() : "",
                            OMRSConceptID = oMRSConceptID != null ? oMRSConceptID.ToString() : "",
                            OMRSAnswerID = oMRSAnswerID != null ? oMRSAnswerID.ToString() : ""
                        });
                    }

                }

                return exitMaps;
            }
            catch (Exception ex)
            {
                var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                Console.WriteLine(message);
                return new List<ARTModel>();
            }
        }
        public List<Lab> GetLabs()
        {
            var labs = new List<Lab>();
            try
            {
                var path = Path.Combine(rootDir, @"Templates", @"LabTests.xlsx");
                FileInfo fileInfo = new FileInfo(path);
                using (var package = new ExcelPackage(fileInfo))
                {
                    var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                    // get number of rows and columns in the sheets
                    int rows = worksheet.Dimension.Rows;
                    

                    // loop through the worksheet rows and columns
                    for (int i = 2; i <= rows; i++)
                    {
                        var labtest_id = worksheet.Cells["A" + i].Value;
                        var lab_Category_id = worksheet.Cells["B" + i].Value;
                        var description = worksheet.Cells["C" + i].Value;
                        var measureab = worksheet.Cells["D" + i].Value;
                        var measurepc = worksheet.Cells["E" + i].Value;
                        var absolute_concept = worksheet.Cells["F" + i].Value;
                        var conceptID = worksheet.Cells["G" + i].Value;
                        var positive = worksheet.Cells["H" + i].Value;
                        var negative = worksheet.Cells["I" + i].Value;
                        var datatype = worksheet.Cells["J" + i].Value;
                        var conceptBoolean = worksheet.Cells["K" + i].Value;
                        var minimumValue = worksheet.Cells["L" + i].Value;
                        var maximumValue = worksheet.Cells["M" + i].Value;

                        labs.Add(new Lab
                        {
                            Labtest_Id = labtest_id != null ? labtest_id.ToString() : "",
                            Labtestcategory_Id = lab_Category_id != null ? lab_Category_id.ToString() : "",
                            Description = description != null ? description.ToString() : "",
                            Measureab = measureab != null ? measureab.ToString() : "",
                            Measurepc = measurepc != null ? measurepc.ToString() : "",
                            Openmrsabsoluteconceptid = absolute_concept != null ? absolute_concept.ToString() : "",
                            Openmrspcconceptid = conceptID != null ? conceptID.ToString() : "",
                            Positive = positive != null ? positive.ToString() : "",
                            Negative = negative != null ? negative.ToString() : "",
                            Datatype = datatype != null ? datatype.ToString() : "",
                            ConceptBoolean = conceptBoolean != null ? conceptBoolean.ToString() : "",
                            MinimumValue = minimumValue != null ? minimumValue.ToString() : "",
                            MaximumValue = maximumValue != null ? maximumValue.ToString() : ""
                        });
                    }

                }
                return labs;
            }
            catch (Exception ex)
            {
                var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                Console.WriteLine(message);
                return new List<Lab>();
            }
        }
        public List<LabData> GetLabData(long patientId)
        {
            try
            {
                var labDs = new List<LabData>();

                using (NpgsqlConnection connection = new NpgsqlConnection(pgconn))
                {
                    connection.Open();
                    var q = "SELECT * FROM laboratory where patient_id =" + patientId + " and facility_id = " + _migOption.Facility;

                    using (NpgsqlCommand cmd = new NpgsqlCommand(q, connection))
                    {
                        using (NpgsqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    var date_collected = reader["date_collected"];
                                    if (date_collected != null)
                                    {
                                        var visitDateStr = date_collected.ToString();
                                        if (DateTime.TryParse(visitDateStr.Trim(), out DateTime dateCollected))
                                        {
                                            var laboratory_id = reader["laboratory_id"];
                                            var patient_id = reader["patient_id"];
                                            var facility_id = reader["facility_id"];
                                            var date_reported = reader["date_reported"];                                            
                                            var labno = reader["labno"];
                                            var resultab = reader["resultab"];
                                            var resultpc = reader["resultpc"];
                                            var comment = reader["comment"];
                                            var labtest_id = reader["labtest_id"];
                                            var time_stamp = reader["time_stamp"];
                                            var uploaded = reader["uploaded"];
                                            var time_uploaded = reader["time_uploaded"];
                                            var user_id = reader["user_id"];
                                            var id_uuid = reader["id_uuid"];
                                            var uuid = reader["uuid"];
                                            //var archived = reader["archived"];

                                            labDs.Add(new LabData
                                            {
                                                laboratory_id = laboratory_id != null ? laboratory_id.ToString() : "",
                                                patient_id = patient_id != null ? long.Parse(patient_id.ToString()) : 0,
                                                facility_id = facility_id != null ? long.Parse(facility_id.ToString()) : 0,
                                                date_reported = date_reported != null ? date_reported.ToString() : "",
                                                date_collected = dateCollected.ToString("yyyy-MM-dd"),
                                                labno = labno != null ? labno.ToString() : "",
                                                resultab = resultab != null ? resultab.ToString() : "",
                                                resultpc = resultpc != null ? resultpc.ToString() : "",
                                                comment = comment != null ? comment.ToString() : "",
                                                labtest_id = labtest_id != null ? labtest_id.ToString() : "",
                                                time_stamp = time_stamp != null ? time_stamp.ToString() : "",
                                                uploaded = uploaded != null ? uploaded.ToString() : "",
                                                time_uploaded = time_uploaded != null ? time_uploaded.ToString() : "",
                                                user_id = user_id != null ? user_id.ToString() : "",
                                                id_uuid = id_uuid != null ? id_uuid.ToString() : "",
                                                uuid = uuid != null ? uuid.ToString() : "",
                                                //archived = archived != null ? archived.ToString() : ""
                                            });
                                        }
                                    }

                                }
                            }
                        }
                    }
                }

                return labDs;
            }
            catch (Exception ex)
            {
                var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                Console.WriteLine(message);
                return new List<LabData>();
            }
        }
        public async Task<MigrationReport> PushData(List<Patient> patients)
        {            
            if(!migrationChecked)
            {
                //Check if migration has been done before to determine if fresh or update migration needs to be conducted at this time
                //only do this with the first 5 identifiers in this list
                var identifiers = new List<string> ();
                var cnt = 0;
                patients.ForEach(id => 
                {
                    if (cnt < 5)
                    {
                        identifiers.Add(id.identifiers[0].identifier);
                        cnt += 1;
                    }
                });

                var chkMgg = new MigrateData(_migOption).CheckExistingMigration(identifiers);

                if (chkMgg.Any())
                {
                    migrationHappend = true;
                }
                migrationChecked = true;
            }

            var migratedDataReport = migrationHappend? await new MigrateData(_migOption).UpdateMigration(patients) : await new MigrateData(_migOption).Migrate(patients);
            if(migratedDataReport.patients > 0)
            {
                migrationReport.patients += migratedDataReport.patients;
                migrationReport.encounters += migratedDataReport.encounters;
                migrationReport.visit += migratedDataReport.visit;
                migrationReport.obs += migratedDataReport.obs;                
            }
            return migratedDataReport;
        }
    }
}
