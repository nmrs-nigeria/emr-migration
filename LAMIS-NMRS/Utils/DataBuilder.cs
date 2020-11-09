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

namespace Common
{
    public class DataBuilder
    {
        //Most used locations:
        //[{pharmacy : 7f65d926-57d6-4402-ae10-a5b3bcbf7986}, 
        //{laboratory: 7fdfa2cb-bc95-405a-88c6-32b7673c0453},
        //{registration: 6351fcf4-e311-4a19-90f9-35667d99a8af}'
        //{inpatientWard: b1a8b05e-3542-4037-bbd3-998ee9c40574},
        //{outPatientClinic: 58c57d25-8d39-41ab-8422-108a0c277d98}]

        List<Regimen> regimens;
        string rootDir;

        public DataBuilder()
        {           
            rootDir = Directory.GetCurrentDirectory();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public List<Patient> BuildPatientInfo(int itemsPerPage, int pageNumber)
        {
            var patients = new List<Patient>();
       
            try
            {
                regimens = GetRegimen();
                if(!regimens.Any())
                {
                    Console.WriteLine("ERROR: Regimen data List could not be retrieved. Command Aborted");
                    return new List<Patient>();
                }
                using (NpgsqlConnection connection = new NpgsqlConnection(Utilities.GetConnectionString("pgconn")))
                {
                    connection.Open();
                    var q = "SELECT * FROM patient order by patient_id offset  " + ((pageNumber - 1) * itemsPerPage) + " rows fetch next " + itemsPerPage + " rows only;";
                                       
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
                                                        value = phoneStr.ToString(),
                                                    };
                                                    //pd.attributes = new List<PatientAttributes> { attribute };
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

                                            patients.Add(patient);
                                        }
                                    }
                                                                        
                                }
                            }
                        }
                    }
                }
                return patients;
            }
            catch (Exception ex)
            {
                var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                Console.WriteLine(message);
                return new List<Patient>();
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
                //provider = "f9badd80-ab76-11e2-9e96-0800200c9a66", //super user
                obs = new List<Obs>()
            };

            try
            {
                //default functional status at start of ART
                var fStatusObs = new Obs
                {
                    concept = ((int)FunctionalStatus.concept).ToString(),
                    valueCoded = ((int)FunctionalStatus.working).ToString()
                };
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
                                    concept = artS.OMRSConceptID,
                                    valueDatetime = artStartDate.ToString("yyyy-MM-dd")
                                };
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
                            concept = ((int)WhoStage.concept).ToString(),
                            valueCoded = ageAtStart > 14 ? ((int)WhoStage.adultStage1).ToString() : ((int)WhoStage.paedStage1).ToString()
                        };
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
                                concept = ((int)CurrentRegimenLine.concept).ToString(),
                                valueCoded = rg.NMRSQuestionConceptID.ToString()
                            };
                            artCommencement.obs.Add(currentRegLineObs);

                            // Regimen
                            var regimenObs = new Obs
                            {
                                concept = rg.NMRSQuestionConceptID.ToString(),
                                valueCoded = rg.NMRSAnswerConceptID.ToString()
                            };
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
                                                concept = dateExit.OMRSConceptID,
                                                valueDatetime = dExit.ToString("yyyy-MM-dd")
                                            };
                                            careTermination.obs.Add(dateExitObs);
                                            careTermination.encounterDatetime = dExit.ToString("yyyy-MM-dd");

                                            //Reason for tracking
                                            var trackingObs = new Obs
                                            {
                                                concept = ((int)TrackingReason.concept).ToString(),
                                                valueCoded = ((int)TrackingReason.MissedPharmacyRefill).ToString() //default
                                            };
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
                                                            concept = dateTrackedX.OMRSConceptID,
                                                            valueDatetime = dTract.ToString("yyyy-MM-dd")
                                                        };
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
                                                        concept = ((int)Concepts.DateMissedAppointment).ToString(),
                                                        valueDatetime = dMissed.ToString("yyyy-MM-dd")
                                                    };
                                                    careTermination.obs.Add(dateMissedObs);
                                                }

                                            }
                                        }
                                    }

                                }
                            }

                            //LTFU
                            if (currentStatus == "Lost to Follow Up")
                            {
                                var ltfuObs = new Obs
                                {
                                    concept = ((int)LTFU.concept).ToString(),
                                    valueCoded = ((int)LTFU.Yes).ToString()
                                };
                                careTermination.obs.Add(ltfuObs);

                                var reasonLtfuObs = new Obs
                                {
                                    concept = ((int)ReasonForLTFU.concept).ToString(),
                                    valueCoded = ((int)ReasonForLTFU.Tracked_Not_Located).ToString()
                                };
                                careTermination.obs.Add(reasonLtfuObs);

                                var dateLtfuObs = new Obs
                                {
                                    concept = ((int)Concepts.DateLTFU).ToString(),
                                    valueDatetime = careTermination.encounterDatetime
                                };
                                careTermination.obs.Add(dateLtfuObs);
                            }

                            //terminated
                            if (currentStatus == "ART Transfer Out")
                            {
                                var trnsObs = new Obs
                                {
                                    concept = ((int)PrevExposed.concept).ToString(),
                                    valueCoded = ((int)PrevExposed.Yes).ToString()
                                };
                                careTermination.obs.Add(trnsObs);

                                var reasonForTerminationObs = new Obs
                                {
                                    concept = ((int)ReasonForTermination.concept).ToString(),
                                    valueCoded = ((int)ReasonForTermination.Transferred_Out_To_Another_Facility).ToString()
                                };
                                careTermination.obs.Add(reasonForTerminationObs);

                                var dateTerminatedObs = new Obs
                                {
                                    concept = ((int)Concepts.DateCareTerminated).ToString(),
                                    valueDatetime = careTermination.encounterDatetime
                                };
                                careTermination.obs.Add(dateTerminatedObs);
                            }
                            if (currentStatus == "Known Death")
                            {
                                var trnsObs = new Obs
                                {
                                    concept = ((int)PrevExposed.concept).ToString(),
                                    valueCoded = ((int)PrevExposed.Yes).ToString()
                                };
                                careTermination.obs.Add(trnsObs);

                                var reasonForTerminationObs = new Obs
                                {
                                    concept = ((int)ReasonForTermination.concept).ToString(),
                                    valueCoded = ((int)ReasonForTermination.Death).ToString()
                                };
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
                                            valueCoded = conceptId.ToString()
                                        };
                                        careTermination.obs.Add(causeOfDesthObs);
                                    }
                                }

                                var dateTerminatedObs = new Obs
                                {
                                    concept = ((int)Concepts.DateCareTerminated).ToString(),
                                    valueDatetime = careTermination.encounterDatetime
                                };
                                careTermination.obs.Add(dateTerminatedObs);
                            }
                            if (currentStatus == "Stopped Treatment")
                            {
                                var trnsObs = new Obs
                                {
                                    concept = ((int)PrevExposed.concept).ToString(),
                                    valueCoded = ((int)PrevExposed.Yes).ToString()
                                };
                                careTermination.obs.Add(trnsObs);

                                var reasonForTerminationObs = new Obs
                                {
                                    concept = ((int)ReasonForTermination.concept).ToString(),
                                    valueCoded = ((int)ReasonForTermination.Discontinued_Care).ToString()
                                };
                                careTermination.obs.Add(reasonForTerminationObs);

                                var reasonDiscontinuedCareObs = new Obs
                                {
                                    concept = ((int)ReasonDiscontinuedCare.concept).ToString(),
                                    valueCoded = ((int)ReasonDiscontinuedCare.MovedOutOfArea).ToString()
                                };
                                careTermination.obs.Add(reasonDiscontinuedCareObs);

                                var dateTerminatedObs = new Obs
                                {
                                    concept = ((int)Concepts.DateCareTerminated).ToString(),
                                    valueDatetime = careTermination.encounterDatetime
                                };
                                careTermination.obs.Add(dateTerminatedObs);
                            }

                            //terminated == no
                            if (currentStatus == "ART Restart")
                            {
                                var trnsObs = new Obs
                                {
                                    concept = ((int)PrevExposed.concept).ToString(),
                                    valueCoded = ((int)PrevExposed.No).ToString()
                                };
                                careTermination.obs.Add(trnsObs);

                                var referredForObs = new Obs
                                {
                                    concept = ((int)ReferredFor.concept).ToString(),
                                    valueCoded = ((int)ReferredFor.ADHERENCE_COUNSELING).ToString()
                                };
                                careTermination.obs.Add(referredForObs);

                                var dateTerminatedObs = new Obs
                                {
                                    concept = ((int)Concepts.DateCareTerminated).ToString(),
                                    valueDatetime = careTermination.encounterDatetime
                                };
                                careTermination.obs.Add(dateTerminatedObs);
                            }

                            if(careTermination.obs.Any())
                            {
                                careTermination.encounterType = "ba6fb85e-8dc3-43e3-a269-59842cd2d364"; //tracking and termination
                                careTermination.location = "b1a8b05e-3542-4037-bbd3-998ee9c40574"; //using in-patient ward for now
                                careTermination.form = "5fbc99be-9aeb-4f94-85b0-b2fae88a0ced"; //tracking and termination
                                //careTermination.provider = "f9badd80-ab76-11e2-9e96-0800200c9a66"; //super user
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
                encounterType = "11eb2647-91b4-482a-9cb3-08573e0d219b", //art commencement
                encounterDatetime = "",
                location = "b1a8b05e-3542-4037-bbd3-998ee9c40574", //using in-patient ward for now
                form = "c2df5a7d-05ac-4ae3-bcd0-39969f17dbab", //HIV Enrolment
                //provider = "f9badd80-ab76-11e2-9e96-0800200c9a66", //super user
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
                                    concept = artS.OMRSConceptID,
                                    valueDatetime = artStartDate.ToString("yyyy-MM-dd")
                                };
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
                                concept = ((int)Concepts.HivEnrolmentDate).ToString(),
                                valueDatetime = encDate.ToString("yyyy-MM-dd")
                            };
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
                                concept = ((int)Concepts.HivConfirmationDate).ToString(),
                                valueDatetime = confirmDate.ToString("yyyy-MM-dd")
                            };
                            hivEnrolment.obs.Add(hivConfirmDateObs);
                        }

                    }
                }
                else
                {
                    var hivConfirmDateObs = new Obs
                    {
                        concept = ((int)Concepts.HivConfirmationDate).ToString(),
                        valueDatetime = encDate.ToString("yyyy-MM-dd")
                    };
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
                            concept = ((int)CareEntryPoint.concept).ToString(),
                            valueCoded = conceptId.ToString()
                        };
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
                                conceptId = (int)MaritalStatus.Single;
                                break;
                            default:
                                conceptId = (int)MaritalStatus.Single;
                                break;
                        }

                        var maritalStatusObs = new Obs
                        {
                            concept = ((int)MaritalStatus.concept).ToString(),
                            valueCoded = conceptId.ToString()
                        };
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
                            concept = ((int)EducationLevel.concept).ToString(),
                            valueCoded = conceptId.ToString()
                        };
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
                            concept = ((int)Occupation.concept).ToString(),
                            valueCoded = conceptId.ToString()
                        };
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
                            concept = ((int)Concepts.NextOfKinName).ToString(),
                            valueText = Utilities.UnscrambleCharacters(nextKin)
                        };
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
                            concept = ((int)Concepts.NextOfKinPhone).ToString(),
                            valueText = Utilities.UnscrambleNumbers(phoneKin)
                        };
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
                            concept = ((int)NextOfKinRelationship.concept).ToString(),
                            valueCoded = conceptId.ToString()
                        };
                        hivEnrolment.obs.Add(relationKinObs);
                    }
                }

                var pregnant_status = reader["pregnant"];
                if (pregnant_status != null)
                {
                    var pregnant = pregnant_status.ToString().Trim();

                    if (!string.IsNullOrEmpty(pregnant))
                    {
                        int conceptId;
                        switch (pregnant)
                        {
                            case "0":
                                conceptId = (int)CurrentlyPregnant.No;
                                break;
                            case "1":
                                conceptId = (int)CurrentlyPregnant.Yes;
                                break;
                            default:
                                conceptId = (int)CurrentlyPregnant.No;
                                break;
                        }

                        var pregnantObs = new Obs
                        {
                            concept = ((int)CurrentlyPregnant.concept).ToString(),
                            valueCoded = conceptId.ToString()
                        };
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
                            concept = ((int)ChildBreastFeeding.concept).ToString(),
                            valueCoded = conceptId.ToString()
                        };
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

                using (NpgsqlConnection connection = new NpgsqlConnection(Utilities.GetConnectionString("pgconn")))
                {
                    connection.Open();
                    var q = "SELECT * FROM clinic where patient_id =" + patientId;

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
                                        //provider = "f9badd80-ab76-11e2-9e96-0800200c9a66", //super user
                                        obs = new List<Obs>()
                                    };

                                    var vitals = new Encounter
                                    {
                                        encounterType = "67a71486-1a54-468f-ac3e-7091a9a79584", //vitals
                                        encounterDatetime = "",
                                        location = "b1a8b05e-3542-4037-bbd3-998ee9c40574", //using in-patient ward for now
                                        form = "a000cb34-9ec1-4344-a1c8-f692232f6edd", //vitals
                                        //provider = "f9badd80-ab76-11e2-9e96-0800200c9a66", //super user
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
                                                concept = ((int)Concepts.VisitDate).ToString(),
                                                valueDatetime = visitDateStr
                                            };
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
                                                        //"110/70"
                                                        var bps = bp.Split('/');
                                                        if (bps.Length > 1)
                                                        {
                                                            var systolicObs = new Obs
                                                            {
                                                                concept = ((int)BloodPressure.Systolic).ToString(),
                                                                valueNumeric = bps[0]
                                                            };
                                                            obs.Add(systolicObs);

                                                            var outDiastObs = new Obs
                                                            {
                                                                concept = ((int)BloodPressure.Diastolic).ToString(),
                                                                valueNumeric = bps[1]
                                                            };
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
                                                            concept = ((int)Concepts.Weight).ToString(),
                                                            valueNumeric = wOut.ToString()
                                                        };
                                                        obs.Add(weightObs);
                                                        vitals.obs.Add(weightObs);
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
                                                            concept = ((int)WhoStage.concept).ToString(),
                                                            valueCoded = conceptId.ToString()
                                                        };
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
                                                        concept = ((int)PregnancyStatus.concept).ToString(),
                                                        valueCoded = pregConceptId.ToString()
                                                    };
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
                                                            concept = ((int)PregnancyStatus.concept).ToString(),
                                                            valueCoded = ((int)PregnancyStatus.breastFeeding).ToString()
                                                        };
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
                                                    var nextAptDateObs = new Obs
                                                    {
                                                        concept = ((int)Concepts.nextAppointmentDate).ToString(),
                                                        valueDatetime = nextApptDate
                                                    };
                                                    obs.Add(nextAptDateObs);
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
                                                        concept = ((int)FunctionalStatus.concept).ToString(),
                                                        valueCoded = conceptId.ToString()
                                                    };
                                                    obs.Add(fStatusObs);
                                                }
                                            }

                                            var heightStr = reader["height"];
                                            if(heightStr != null)
                                            {
                                                var height  = heightStr.ToString();
                                                if (!string.IsNullOrEmpty(height))
                                                {
                                                    var heightObs = new Obs
                                                    {
                                                        concept = ((int)Concepts.height).ToString(),
                                                        valueNumeric = height
                                                    };
                                                    obs.Add(heightObs);
                                                    vitals.obs.Add(heightObs);
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
                                                        concept = ((int)TBStatus.concept).ToString(),
                                                        valueCoded = conceptId.ToString()
                                                    };
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
                                                            concept = ((int)CurrentRegimenLine.concept).ToString(),
                                                            valueCoded = rg.NMRSQuestionConceptID.ToString()
                                                        };
                                                        obs.Add(currentRegLineObs);

                                                        // Regimen
                                                        var regimenObs = new Obs
                                                        {
                                                            concept = rg.NMRSQuestionConceptID.ToString(),
                                                            valueCoded = rg.NMRSAnswerConceptID.ToString()
                                                        };
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
                                                        concept = ((int)DrugAdhenrence.concept).ToString(),
                                                        valueCoded = conceptId.ToString()
                                                    };
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
                                                            concept = ((int)OIs.concept).ToString(),
                                                            valueCoded = conceptId.ToString()
                                                        };
                                                        obs.Add(oiObs);
                                                    });
                                                }                                                
                                            }

                                            careCard.obs = obs;

                                            vitalCare.Add(careCard);
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
                using (NpgsqlConnection connection = new NpgsqlConnection(Utilities.GetConnectionString("pgconn")))
                {
                    connection.Open();
                    var q = "select pharmacy_id,patient_id,date_visit, r.description as regimen, rt.description as regimentype,duration,morning,afternoon,evening,adherence,next_appointment,time_stamp from (select * from pharmacy where patient_id = " + patientId + ") as p join (select * from regimen) r on p.regimen_id = r.regimen_id join (select * from regimentype) rt on p.regimentype_id = rt.regimentype_id";

                    using (NpgsqlCommand cmd = new NpgsqlCommand(q, connection))
                    {
                        using (NpgsqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                var obs = new List<Obs>();

                                while (reader.Read())
                                {
                                    var pharmacy = new Encounter
                                    {
                                        encounterType = "a1fa6aa3-59e1-4833-a28c-bb62f2fb07df", //pharmacy
                                        encounterDatetime = "",
                                        location = "7f65d926-57d6-4402-ae10-a5b3bcbf7986", //pharmacy
                                        form = "4a238dc4-a76b-4c0f-a100-229d98fd5758", //pharmacy form
                                        //provider = "f9badd80-ab76-11e2-9e96-0800200c9a66", //super user
                                        obs = new List<Obs>()
                                    };

                                    var visitDateX = reader["date_visit"];
                                    if (visitDateX != null)
                                    {
                                        var visitDateStr = visitDateX.ToString();
                                        if (DateTime.TryParse(visitDateStr.Trim(), out DateTime visitDate))
                                        {
                                            var visitDateObs = new Obs
                                            {
                                                concept = ((int)Concepts.VisitDate).ToString(),
                                                valueDatetime = visitDateStr
                                            };
                                            obs.Add(visitDateObs);

                                            pharmacy.encounterDatetime = visitDate.ToString("yyyy-MM-dd");

                                            // Human immunodeficiency virus treatment regimen
                                            var grObs = new Obs
                                            {
                                                concept = ((int)ARVRegimen.concept).ToString(),
                                                groupMembers = new List<Obs>()
                                            };

                                            // ----------- Treatment Age Category
                                            if (!string.IsNullOrEmpty(visitDateStr) && !string.IsNullOrEmpty(date_of_birth))
                                            {
                                                var ageAtVist = visitDate.Year - Convert.ToDateTime(date_of_birth).Year;
                                                
                                                var trxAgeObs = new Obs
                                                {
                                                    concept = ((int)TreatmentAge.concept).ToString(),
                                                    valueCoded = ageAtVist > 14? ((int)TreatmentAge.Adult).ToString() : ((int)TreatmentAge.Child).ToString()
                                                };
                                                grObs.groupMembers.Add(trxAgeObs);
                                            }

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
                                                        var durationObs = new Obs
                                                        {
                                                            concept = ((int)Concepts.MedicationDuration).ToString(),
                                                            valueNumeric = durationOut.ToString()
                                                        };
                                                        grObs.groupMembers.Add(durationObs);

                                                        // ------ Quantity of medication prescribed per dose
                                                        var prescribedObs = new Obs
                                                        {
                                                            concept = ((int)Concepts.QuantityPrescribed).ToString(),
                                                            valueNumeric = durationOut.ToString()
                                                        };
                                                        grObs.groupMembers.Add(prescribedObs);

                                                        // ------ Medication dispensed
                                                        var dispensedObs = new Obs
                                                        {
                                                            concept = ((int)Concepts.QuantityDispensed).ToString(),
                                                            valueNumeric = durationOut.ToString()
                                                        };
                                                        grObs.groupMembers.Add(dispensedObs);

                                                        if (grObs.groupMembers.Any())
                                                        {
                                                            pharmacy.obs.Add(grObs);
                                                        }
                                                    }
                                                }
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

                                                        // Current regimen Line
                                                        var currentRegLineObs = new Obs
                                                        {
                                                            concept = ((int)CurrentRegimenLine.concept).ToString(),
                                                            valueCoded = rg.NMRSQuestionConceptID.ToString()
                                                        };
                                                        grObs.groupMembers.Add(currentRegLineObs);

                                                        // Regimen
                                                        var regimenObs = new Obs
                                                        {
                                                            concept = rg.NMRSQuestionConceptID.ToString(),
                                                            valueCoded = rg.NMRSAnswerConceptID.ToString()
                                                        };
                                                        grObs.groupMembers.Add(regimenObs);
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
                                                        concept = ((int)AdherenceCounselling.concept).ToString(),
                                                        valueCoded = conceptId.ToString()
                                                    };
                                                    obs.Add(drugAdherenceCounsellingObs);
                                                }
                                            }

                                            // ---- Pick up Reason                                            
                                            var pickUpReasonObs = new Obs
                                            {
                                                concept = ((int)PickUpReason.concept).ToString(),
                                                valueCoded = ((int)PickUpReason.Refill).ToString()
                                            };
                                            obs.Add(pickUpReasonObs);

                                            //------ Visit Type                                          
                                            var visitTypeObs = new Obs
                                            {
                                                concept = ((int)VisitType.concept).ToString(),
                                                valueCoded = ((int)VisitType.ReturnVisitType).ToString()
                                            };
                                            obs.Add(visitTypeObs);

                                            //-------- Date ordered                                            
                                            var dateOrderedObs = new Obs
                                            {
                                                concept = ((int)Concepts.DateOrdered).ToString(),
                                                valueDatetime = visitDate.ToString("yyyy-MM-dd")
                                            };
                                            obs.Add(dateOrderedObs);

                                            //-------- Date dispensed                                            
                                            var dateDispensedObs = new Obs
                                            {
                                                concept = ((int)Concepts.DateDispensed).ToString(),
                                                valueDatetime = visitDate.ToString("yyyy-MM-dd")
                                            };
                                            obs.Add(dateDispensedObs);
                                            pharmacy.obs.AddRange(obs);
                                            pharmacy.obs.Add(grObs);
                                            pharmacies.Add(pharmacy);
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
                    int columns = worksheet.Dimension.Columns;

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
                    int columns = worksheet.Dimension.Columns;

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
                    int columns = worksheet.Dimension.Columns;

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
    }

}
