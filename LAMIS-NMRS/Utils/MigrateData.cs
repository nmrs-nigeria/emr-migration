using Common;
using LAMIS_NMRS.Models;
using MySql.Data.MySqlClient;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LAMIS_NMRS.Utils
{
    public class MigrateData
    {
        APIHelper apiHelper;
        MigrationReport migrationReport;
        MigrationOption _migOption;
        string mysqlconn = "";

        public MigrateData(MigrationOption migOption)
        {
            _migOption = migOption;
            var apiUrl = "http://localhost:"
                + (_migOption.NmrsServerPort ?? "8080")
                + (_migOption.NmrsUrlBasePath ?? "/openmrs")
                + "/ws/rest/v1/";
            _migOption.BaseUrl = apiUrl;
            mysqlconn = "Server=localhost;Database=" + _migOption.NmrsDatabaseName + ";Uid=" + _migOption.NmrsWebUsername + ";Pwd=" + _migOption.NmrsWebPassword + ";";
            apiHelper = new APIHelper(_migOption);
            migrationReport = new MigrationReport();
        }

        public dynamic CheckMigration()
        {
            var dxc = new { patients = 0, encounters = 0 };
            try
            {
                using (var connection = new MySqlConnection(mysqlconn))
                {
                    connection.Open();
                    var q = "select * from"
                            + "(select count(*) as patients  from patient) as p"
                            + "cross join"
                            + "(select count(*) as encounters from encounter) as e";

                    using (MySqlCommand cmd = new MySqlCommand(q, connection))
                    {
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    var patients = reader["patients"];
                                    var encounters = reader["encounters"];
                                    if (patients != null && encounters != null)
                                    {
                                        dxc = new { patients = int.Parse(patients.ToString()), encounters = int.Parse(encounters.ToString()) };
                                    }

                                }
                            }
                        }
                    }
                }

                return dxc;
            }
            catch (Exception ex)
            {
                var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(Environment.NewLine + Environment.NewLine);
                Console.WriteLine("An error was encountered trying to check for previous migration data." + Environment.NewLine);
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Pleasse ensure that the correct values for the following variables were provided in the AppSettings.json file:" + Environment.NewLine);
                Console.WriteLine("nmrs_Database_Name, nmrs_Server_Username, nmrs_Server_Password, nmrs_Server_Port");
                Console.WriteLine(message + Environment.NewLine);
                return dxc;
            }
        }
        public List<string> CheckExistingMigration(List<string> identifiers)
        {
            var dxc = new List<string>();
            try
            {
                identifiers.ForEach(identifier =>
                {
                    var exts = apiHelper.GetData("patient?identifier=" + identifier).Result.results;
                    if (exts.Any())
                    {
                        dxc.Add(exts[0].uuid);
                    }
                });

                return dxc;
            }
            catch (Exception ex)
            {
                var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(Environment.NewLine + Environment.NewLine);
                Console.WriteLine("An error was encountered trying to check for previous migration data." + Environment.NewLine);
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Please ensure that the correct values for the following variables were provided in the AppSettings.json file:" + Environment.NewLine);
                Console.WriteLine("nmrs_Web_Username, nmrs_Web_Password, nmrs_Server_Port");
                Console.WriteLine(message + Environment.NewLine);
                return dxc;
            }
        }
        public async Task<MigrationReport> Migrate(List<Patient> patients)
        {
            try
            {
                if (patients.Any())
                {
                    await Task.Run(() =>
                    {
                        Parallel.ForEach(patients, p =>
                        {
                            //migrate person, identifiers, address   
                            var startD = DateTime.Now;
                            var encountersMigrated = 0;
                            var visitsMigrated = 0;
                            var obsCount = 0;

                            //Console.ForegroundColor = ConsoleColor.White;
                            //Console.WriteLine("Migrating patient with ID {0} ...", p.identifiers[0].identifier);
                            if (p.person.addresses == null)
                            {
                                p.person.addresses = new List<Personaddress>();
                            }

                            if (!p.person.addresses.Any())
                            {
                                p.person.addresses.Add(new Personaddress
                                {
                                    preferred = true,
                                    address1 = " ",
                                    country = "Nigeria",
                                    cityVillage = " ",
                                    stateProvince = " "
                                });
                            }

                            var personUuid = apiHelper.SendData<ApiResponse, PatientDemography>(URLConstants.person, p.person).Result.uuid;
                            if (!string.IsNullOrEmpty(personUuid))
                            {
                                var patientInfo = new PatientInfo
                                {
                                    person = personUuid,
                                    identifiers = p.identifiers
                                };
                                var patientUuid = apiHelper.SendData<ApiResponse, PatientInfo>(URLConstants.patient, patientInfo).Result.uuid;
                                if (!string.IsNullOrEmpty(patientUuid))
                                {

                                    //migrate patient program
                                    if (p.PatientProgram != null)
                                    {
                                        if (!string.IsNullOrEmpty(p.PatientProgram.dateEnrolled))
                                        {
                                            p.PatientProgram.patient = patientUuid;
                                            var ppEnrolment = apiHelper.SendData<ApiResponse, PatientProgram>(URLConstants.programenrollment, p.PatientProgram).Result.uuid;
                                        }
                                    }

                                    //migrate patient attribute
                                    if (p.attributes != null)
                                    {
                                        if (p.attributes.Any())
                                        {
                                            var attribute = p.attributes[0];
                                            var ppAttribute = apiHelper.SendData<ApiResponse, PatientAttributes>("/person/" + patientUuid + "/attribute", attribute).Result.uuid;
                                        }
                                    }

                                    foreach (var e in p.Encounters)
                                    {
                                        e.patient = patientUuid;
                                    }

                                    //migrationReport.patients += 1;
                                    var encounterGroups = p.Encounters.GroupBy(g => g.encounterDatetime).ToList();

                                    //Migrate Encounters
                                    //Console.WriteLine("Migrating encounters...{0}", Environment.NewLine);
                                    foreach (var g in encounterGroups)
                                    {
                                        var encounterUuids = new List<string>();
                                        var encounters = g.ToList();

                                        var visit = new Visit
                                        {
                                            startDatetime = g.Key,
                                            stopDatetime = g.Key,
                                            location = encounters[0].location,
                                            patient = patientUuid
                                        };

                                        var visitUuid = apiHelper.SendData<ApiResponse, Visit>(URLConstants.visit, visit).Result.uuid;
                                        if (string.IsNullOrEmpty(visitUuid))
                                        {
                                            lock (migrationReport)
                                            {
                                                var error = "Clinical Visit failed to migrate. Visit Date: " + g.Key;
                                                Console.ForegroundColor = ConsoleColor.White;
                                                Console.WriteLine("Visit migration for patient with ID: {0} {1} {2} at " + visit.startDatetime + " failed with the following message", p.identifiers[0], Environment.NewLine, Environment.NewLine);

                                                Console.ForegroundColor = ConsoleColor.Red;
                                                Console.WriteLine(error);
                                            }
                                            continue;
                                        }

                                        //migrationReport.visit += 1;
                                        visitsMigrated += 1;

                                        foreach (var enc in encounters)
                                        {
                                            enc.visit = visitUuid;
                                            enc.encounterDatetime = g.Key;

                                            enc.obs.ForEach(ol =>
                                            {
                                                ol.location = enc.location;
                                                ol.obsDatetime = enc.encounterDatetime;
                                                ol.person = personUuid;

                                                if (ol.groupMembers.Any())
                                                {
                                                    var groupMembers = new List<string>();

                                                    ol.groupMembers.ForEach(og =>
                                                    {
                                                        og.location = enc.location;
                                                        og.obsDatetime = enc.encounterDatetime;
                                                        og.person = personUuid;
                                                    });
                                                }
                                            });

                                            var encounterUuid = apiHelper.SendData<ApiResponse, Encounter>(URLConstants.encounter, enc).Result.uuid;
                                            if (!string.IsNullOrEmpty(encounterUuid))
                                            {
                                                //migrationReport.encounters += 1;
                                                encountersMigrated += 1;
                                                //migrationReport.obs += enc.obs.Count();
                                                obsCount += 1;

                                                enc.obs.ForEach(l =>
                                                {
                                                    if (l.groupMembers.Any())
                                                    {
                                                        //migrationReport.obs += l.groupMembers.Count();
                                                        obsCount += 1;
                                                    }

                                                });
                                            }
                                            else
                                            {
                                                lock (migrationReport)
                                                {
                                                    Console.ForegroundColor = ConsoleColor.Red;
                                                    Console.WriteLine("Error Migrating Encounter:{0}", Environment.NewLine);
                                                    Console.ForegroundColor = ConsoleColor.White;
                                                    Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(enc) + Environment.NewLine + Environment.NewLine);
                                                }
                                            }
                                        }
                                    }

                                    lock (migrationReport)
                                    {
                                        var summarry = "Patient " + p.identifiers[0].identifier + " successfully migrated with: " + Environment.NewLine +
                                        visitsMigrated.ToString() + " Visits, " + encountersMigrated.ToString() + " Encounters and " + obsCount.ToString() + " Obs";

                                        Console.ForegroundColor = ConsoleColor.Green;
                                        Console.WriteLine(summarry);
                                        var d = (DateTime.Now - startD).ToString(@"hh\:mm\:ss");
                                        Console.WriteLine("Duration: {0}{1}{2}", d, Environment.NewLine, Environment.NewLine);
                                    }
                                }
                                else
                                {
                                    lock (migrationReport)
                                    {
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.WriteLine(Environment.NewLine + Environment.NewLine);
                                        Console.WriteLine("Patient " + p.identifiers[0].identifier + " could not be migrated. Please check the error message and make the necessary corrections" + Environment.NewLine);
                                    }
                                }
                            }

                            lock (migrationReport)
                            {
                                migrationReport.patients += 1;
                                migrationReport.visit += visitsMigrated;
                                migrationReport.encounters += encountersMigrated;
                                migrationReport.obs += obsCount;
                            }
                        });
                    });

                    //patients.ForEach(p =>
                    //{
                    //    //migrate person, identifiers, address   
                    //    var startD = DateTime.Now;
                    //    var encountersMigrated = 0;
                    //    var visitsMigrated = 0;
                    //    var obsCount = 0;

                    //    Console.ForegroundColor = ConsoleColor.White;
                    //    Console.WriteLine("Migrating patient with ID {0} ...", p.identifiers[0].identifier);
                    //    if (p.person.addresses == null)
                    //    {
                    //        p.person.addresses = new List<Personaddress>();
                    //    }

                    //    if (!p.person.addresses.Any())
                    //    {
                    //        p.person.addresses.Add(new Personaddress
                    //        {
                    //            preferred = true,
                    //            address1 = " ",
                    //            country = "Nigeria",
                    //            cityVillage = " ",
                    //            stateProvince = " "
                    //        });
                    //    }

                    //    var personUuid = apiHelper.SendData<ApiResponse, PatientDemography>(URLConstants.person, p.person).Result.uuid;
                    //    if (!string.IsNullOrEmpty(personUuid))
                    //    {
                    //        var patientInfo = new PatientInfo
                    //        {
                    //            person = personUuid,
                    //            identifiers = p.identifiers
                    //        };
                    //        var patientUuid = apiHelper.SendData<ApiResponse, PatientInfo>(URLConstants.patient, patientInfo).Result.uuid;
                    //        if(!string.IsNullOrEmpty(patientUuid))
                    //        {

                    //            //migrate patient program
                    //            if (p.PatientProgram != null)
                    //            {
                    //                if (!string.IsNullOrEmpty(p.PatientProgram.dateEnrolled))
                    //                {
                    //                    p.PatientProgram.patient = patientUuid;
                    //                    var ppEnrolment = apiHelper.SendData<ApiResponse, PatientProgram>(URLConstants.programenrollment, p.PatientProgram).Result.uuid;
                    //                }
                    //            }

                    //            //migrate patient attribute
                    //            if (p.attributes != null)
                    //            {
                    //                if (p.attributes.Any())
                    //                {
                    //                    var attribute = p.attributes[0];
                    //                    var ppAttribute = apiHelper.SendData<ApiResponse, PatientAttributes>("/person/" + patientUuid + "/attribute", attribute).Result.uuid;
                    //                }
                    //            }

                    //            foreach (var e in p.Encounters)
                    //            {
                    //                e.patient = patientUuid;
                    //            }
                    //            migrationReport.patients += 1;
                    //            var encounterGroups = p.Encounters.GroupBy(g => g.encounterDatetime).ToList();

                    //            //Migrate Encounters
                    //            Console.WriteLine("Migrating encounters...{0}", Environment.NewLine);
                    //            foreach (var g in encounterGroups)
                    //            {
                    //                var encounterUuids = new List<string>();
                    //                var encounters = g.ToList();

                    //                var visit = new Visit
                    //                {
                    //                    startDatetime = g.Key,
                    //                    stopDatetime = g.Key,
                    //                    location = encounters[0].location,
                    //                    patient = patientUuid
                    //                };

                    //                var visitUuid = apiHelper.SendData<ApiResponse, Visit>(URLConstants.visit, visit).Result.uuid;
                    //                if (string.IsNullOrEmpty(visitUuid))
                    //                {
                    //                    var error = "Clinical Visit failed to migrate. Visit Date: " + g.Key;
                    //                    Console.ForegroundColor = ConsoleColor.White;
                    //                    Console.WriteLine("Visit migration for patient with ID: {0} {1} {2} at " + visit.startDatetime + " failed with the following message", p.identifiers[0], Environment.NewLine, Environment.NewLine);

                    //                    Console.ForegroundColor = ConsoleColor.Red;
                    //                    Console.WriteLine(error);
                    //                    continue;
                    //                }

                    //                migrationReport.visit += 1;
                    //                visitsMigrated += 1;

                    //                foreach (var enc in encounters)
                    //                {
                    //                    enc.visit = visitUuid;
                    //                    enc.encounterDatetime = g.Key;

                    //                    enc.obs.ForEach(ol =>
                    //                    {
                    //                        ol.location = enc.location;
                    //                        ol.obsDatetime = enc.encounterDatetime;
                    //                        ol.person = personUuid;

                    //                        if (ol.groupMembers.Any())
                    //                        {
                    //                            var groupMembers = new List<string>();

                    //                            ol.groupMembers.ForEach(og =>
                    //                            {
                    //                                og.location = enc.location;
                    //                                og.obsDatetime = enc.encounterDatetime;
                    //                                og.person = personUuid;
                    //                            });
                    //                        }
                    //                    });

                    //                    var encounterUuid = apiHelper.SendData<ApiResponse, Encounter>(URLConstants.encounter, enc).Result.uuid;
                    //                    if (!string.IsNullOrEmpty(encounterUuid))
                    //                    {
                    //                        migrationReport.encounters += 1;
                    //                        encountersMigrated += 1;
                    //                        migrationReport.obs += enc.obs.Count();
                    //                        obsCount += 1;

                    //                        enc.obs.ForEach(l =>
                    //                        {

                    //                            if (l.groupMembers.Any())
                    //                            {
                    //                                migrationReport.obs += l.groupMembers.Count();
                    //                                obsCount += 1;
                    //                            }

                    //                        });
                    //                    }
                    //                    else
                    //                    {
                    //                        Console.ForegroundColor = ConsoleColor.Red;
                    //                        Console.WriteLine("Error Migrating Encounter:{0}", Environment.NewLine);
                    //                        Console.ForegroundColor = ConsoleColor.White;
                    //                        Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(enc) + Environment.NewLine + Environment.NewLine);
                    //                    }
                    //                }
                    //            }

                    //            var summarry = "Patient " + p.identifiers[0].identifier + " successfully migrated with: " + Environment.NewLine +
                    //            visitsMigrated.ToString() +" Visits, " + encountersMigrated.ToString() + " Encounters and " + obsCount.ToString() + " Obs";

                    //            Console.ForegroundColor = ConsoleColor.Green;
                    //            Console.WriteLine(summarry);
                    //            var d = (DateTime.Now - startD).ToString(@"hh\:mm\:ss");
                    //            Console.WriteLine("Duration: {0}{1}{2}", d, Environment.NewLine, Environment.NewLine);
                    //        }
                    //        else
                    //        {
                    //            Console.ForegroundColor = ConsoleColor.Red;
                    //            Console.WriteLine(Environment.NewLine + Environment.NewLine);
                    //            Console.WriteLine("Patient " + p.identifiers[0].identifier + " could not be migrated. Please check the error message and make the necessary corrections" + Environment.NewLine);
                    //        }
                    //    }

                    //});
                    return migrationReport;
                }
                return new MigrationReport();
            }
            catch (Exception ex)
            {
                var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(Environment.NewLine + Environment.NewLine);
                Console.WriteLine("An error was encountered with the following message:" + Environment.NewLine);
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(message + Environment.NewLine);
                return migrationReport;
            }
        }
        public async Task<MigrationReport> UpdateMigration(List<Patient> patients)
        {
            try
            {
                if (patients.Any())
                {
                    await Task.Run(() =>
                    {
                        Parallel.ForEach(patients, p =>
                        {
                            //migrate person, identifiers, address   
                            var startD = DateTime.Now;
                            var encountersMigrated = 0;
                            var visitsMigrated = 0;
                            var obsCount = 0;

                            //Console.ForegroundColor = ConsoleColor.White;
                            //Console.WriteLine("Migrating patient with ID {0} ...", p.identifiers[0].identifier);
                            if (p.person.addresses == null)
                            {
                                p.person.addresses = new List<Personaddress>();
                            }

                            if (!p.person.addresses.Any())
                            {
                                p.person.addresses.Add(new Personaddress
                                {
                                    preferred = true,
                                    address1 = " ",
                                    country = "Nigeria",
                                    cityVillage = " ",
                                    stateProvince = " "
                                });
                            }

                            var personUuid = "";
                            var patientExists = false;
                            var uuids = apiHelper.GetData("patient?identifier=" + p.identifiers[0].identifier).Result.results;
                            if (uuids.Any())
                            {
                                personUuid = uuids[0].uuid;
                                patientExists = true;
                            }

                            if (string.IsNullOrEmpty(personUuid))
                            {
                                personUuid = apiHelper.SendData<ApiResponse, PatientDemography>(URLConstants.person, p.person).Result.uuid;
                            }
                            if (!string.IsNullOrEmpty(personUuid))
                            {
                                var patientInfo = new PatientInfo
                                {
                                    person = personUuid,
                                    identifiers = p.identifiers
                                };

                                var patientUuid = "";

                                if (!patientExists)
                                {
                                    patientUuid = apiHelper.SendData<ApiResponse, PatientInfo>(URLConstants.patient, patientInfo).Result.uuid;
                                }
                                else
                                {
                                    patientUuid = personUuid;
                                }

                                if (!string.IsNullOrEmpty(patientUuid))
                                {
                                    //migrate patient program
                                    if (p.PatientProgram != null)
                                    {
                                        if (!string.IsNullOrEmpty(p.PatientProgram.dateEnrolled))
                                        {
                                            //check if patient is already enrolled in program
                                            var pPrograms = apiHelper.GetData(URLConstants.programenrollment + "?patient=" + patientUuid).Result.results;
                                            if (!pPrograms.Any())
                                            {
                                                p.PatientProgram.patient = patientUuid;
                                                var ppEnrolment = apiHelper.SendData<ApiResponse, PatientProgram>(URLConstants.programenrollment, p.PatientProgram).Result.uuid;
                                            }

                                        }
                                    }

                                    //migrate patient attribute
                                    if (p.attributes != null)
                                    {
                                        if (p.attributes.Any())
                                        {
                                            //check if patient already has this attribute
                                            var pAttributes = apiHelper.GetData("/person/" + patientUuid + "/attribute").Result.results;
                                            if (!pAttributes.Any())
                                            {
                                                var attribute = p.attributes[0];
                                                var ppAttribute = apiHelper.SendData<ApiResponse, PatientAttributes>("/person/" + patientUuid + "/attribute", attribute).Result.uuid;
                                            }
                                        }
                                    }

                                    foreach (var e in p.Encounters)
                                    {
                                        e.patient = patientUuid;
                                    }
                                    //migrationReport.patients += 1;
                                    var encounterGroups = p.Encounters.GroupBy(g => g.encounterDatetime).ToList();

                                    //Migrate Encounters
                                    //Console.WriteLine("Migrating encounters...{0}", Environment.NewLine);
                                    foreach (var g in encounterGroups)
                                    {
                                        var encounterUuids = new List<string>();
                                        var encounters = g.ToList();

                                        var visit = new Visit
                                        {
                                            startDatetime = g.Key,
                                            stopDatetime = g.Key,
                                            location = encounters[0].location,
                                            patient = patientUuid
                                        };

                                        var dv = encounters[0];
                                        var visitUuid = "";

                                        var vV = apiHelper.GetData("visit?limit=1&startIndex=0&todate=" + g.Key + "&patient=" + patientUuid + "&fromdate=" + g.Key).Result.results;
                                        if (vV.Any())
                                        {
                                            visitUuid = vV[0].uuid;
                                        }
                                        else
                                        {
                                            visitUuid = apiHelper.SendData<ApiResponse, Visit>(URLConstants.visit, visit).Result.uuid;
                                        }

                                        if (string.IsNullOrEmpty(visitUuid))
                                        {
                                            Console.ForegroundColor = ConsoleColor.Red;
                                            Console.WriteLine(Environment.NewLine + "The clinical Visit on " + visit.startDatetime + " for patient " + p.identifiers[0].identifier + " failed to be migrated" + Environment.NewLine + Environment.NewLine);
                                            Console.ForegroundColor = ConsoleColor.White;
                                            continue;
                                        }

                                        //migrationReport.visit += 1;
                                        visitsMigrated += 1;

                                        foreach (var enc in encounters)
                                        {
                                            enc.visit = visitUuid;
                                            enc.encounterDatetime = g.Key;

                                            enc.obs.ForEach(ol =>
                                            {
                                                ol.location = enc.location;
                                                ol.obsDatetime = enc.encounterDatetime;
                                                ol.person = personUuid;

                                                if (ol.groupMembers.Any())
                                                {
                                                    var groupMembers = new List<string>();

                                                    ol.groupMembers.ForEach(og =>
                                                    {
                                                        og.location = enc.location;
                                                        og.obsDatetime = enc.encounterDatetime;
                                                        og.person = personUuid;
                                                    });
                                                }
                                            });

                                            var encounterUuid = "";
                                            var encuuids = apiHelper.GetData("encounter?todate=" + enc.encounterDatetime + "&patient=" + patientUuid + "&encounterType=" + enc.encounterType + "&fromdate=" + enc.encounterDatetime).Result.results;
                                            if (encuuids.Any())
                                            {
                                                encounterUuid = encuuids[0].uuid;
                                            }

                                            if (string.IsNullOrEmpty(encounterUuid))
                                            {
                                                encounterUuid = apiHelper.SendData<ApiResponse, Encounter>(URLConstants.encounter, enc).Result.uuid;
                                            }

                                            if (!string.IsNullOrEmpty(encounterUuid))
                                            {
                                                //migrationReport.encounters += 1;
                                                encountersMigrated += 1;
                                                //migrationReport.obs += enc.obs.Count();
                                                obsCount += 1;

                                                enc.obs.ForEach(l =>
                                                {
                                                    if (l.groupMembers.Any())
                                                    {
                                                        //migrationReport.obs += l.groupMembers.Count();
                                                        obsCount += 1;
                                                    }

                                                });
                                            }
                                            else
                                            {
                                                lock (migrationReport)
                                                {
                                                    Console.ForegroundColor = ConsoleColor.Red;
                                                    Console.WriteLine("Error Migrating Encounter:{0}", Environment.NewLine);
                                                    Console.ForegroundColor = ConsoleColor.White;
                                                    Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(enc) + Environment.NewLine + Environment.NewLine);
                                                }
                                            }
                                        }
                                    }

                                    lock (migrationReport)
                                    {
                                        var summarry = "Patient " + p.identifiers[0].identifier + " successfully migrated with: " + Environment.NewLine +
                                        visitsMigrated.ToString() + " Visits, " + encountersMigrated.ToString() + " Encounters and " + obsCount.ToString() + " Obs";

                                        Console.ForegroundColor = ConsoleColor.Green;
                                        Console.WriteLine(summarry);
                                        var d = (DateTime.Now - startD).ToString(@"hh\:mm\:ss");
                                        Console.WriteLine("Duration: {0}{1}{2}", d, Environment.NewLine, Environment.NewLine);
                                    }
                                }
                                else
                                {
                                    lock (migrationReport)
                                    {
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.WriteLine(Environment.NewLine + Environment.NewLine);
                                        Console.WriteLine("Patient " + p.identifiers[0].identifier + " could not be migrated. Please check the error message and make the necessary corrections" + Environment.NewLine);
                                    }
                                }
                            }

                            lock (migrationReport)
                            {
                                migrationReport.patients += 1;
                                migrationReport.visit += visitsMigrated;
                                migrationReport.encounters += encountersMigrated;
                                migrationReport.obs += obsCount;
                            }
                        });
                    });

                    //patients.ForEach(p =>
                    //{
                    //    //migrate person, identifiers, address   
                    //    var startD = DateTime.Now;
                    //    var encountersMigrated = 0;
                    //    var visitsMigrated = 0;
                    //    var obsCount = 0;

                    //    Console.ForegroundColor = ConsoleColor.White;
                    //    Console.WriteLine("Migrating patient with ID {0} ...", p.identifiers[0].identifier);
                    //    if (p.person.addresses == null)
                    //    {
                    //        p.person.addresses = new List<Personaddress>();
                    //    }

                    //    if (!p.person.addresses.Any())
                    //    {
                    //        p.person.addresses.Add(new Personaddress
                    //        {
                    //            preferred = true,
                    //            address1 = " ",
                    //            country = "Nigeria",
                    //            cityVillage = " ",
                    //            stateProvince = " "
                    //        });
                    //    }

                    //    var personUuid = "";
                    //    var patientExists = false;
                    //    var uuids = apiHelper.GetData("patient?identifier=" + p.identifiers[0].identifier).Result.results;
                    //    if (uuids.Any())
                    //    {
                    //        personUuid = uuids[0].uuid;
                    //        patientExists = true;
                    //    }

                    //    if(string.IsNullOrEmpty(personUuid))
                    //    {
                    //        personUuid = apiHelper.SendData<ApiResponse, PatientDemography>(URLConstants.person, p.person).Result.uuid;
                    //    }                        
                    //    if (!string.IsNullOrEmpty(personUuid))
                    //    {
                    //        var patientInfo = new PatientInfo
                    //        {
                    //            person = personUuid,
                    //            identifiers = p.identifiers
                    //        };

                    //        var patientUuid = "";

                    //        if (!patientExists)
                    //        {
                    //            patientUuid = apiHelper.SendData<ApiResponse, PatientInfo>(URLConstants.patient, patientInfo).Result.uuid;
                    //        }
                    //        else
                    //        {
                    //            patientUuid = personUuid;
                    //        }

                    //        if (!string.IsNullOrEmpty(patientUuid))
                    //        {
                    //            //migrate patient program
                    //            if (p.PatientProgram != null)
                    //            {
                    //                if (!string.IsNullOrEmpty(p.PatientProgram.dateEnrolled))
                    //                {
                    //                    //check if patient is already enrolled in program
                    //                    var pPrograms = apiHelper.GetData(URLConstants.programenrollment + "?patient=" + patientUuid).Result.results;
                    //                    if (!pPrograms.Any())
                    //                    {
                    //                        p.PatientProgram.patient = patientUuid;
                    //                        var ppEnrolment = apiHelper.SendData<ApiResponse, PatientProgram>(URLConstants.programenrollment, p.PatientProgram).Result.uuid;
                    //                    }

                    //                }
                    //            }

                    //            //migrate patient attribute
                    //            if (p.attributes != null)
                    //            {
                    //                if (p.attributes.Any())
                    //                {
                    //                    //check if patient already has this attribute
                    //                    var pAttributes = apiHelper.GetData("/person/" + patientUuid + "/attribute").Result.results;
                    //                    if (!pAttributes.Any())
                    //                    {
                    //                        var attribute = p.attributes[0];
                    //                        var ppAttribute = apiHelper.SendData<ApiResponse, PatientAttributes>("/person/" + patientUuid + "/attribute", attribute).Result.uuid;
                    //                    }
                    //                }
                    //            }

                    //            foreach (var e in p.Encounters)
                    //            {
                    //                e.patient = patientUuid;
                    //            }
                    //            migrationReport.patients += 1;
                    //            var encounterGroups = p.Encounters.GroupBy(g => g.encounterDatetime).ToList();

                    //            //Migrate Encounters
                    //            Console.WriteLine("Migrating encounters...{0}", Environment.NewLine);
                    //            foreach (var g in encounterGroups)
                    //            {
                    //                var encounterUuids = new List<string>();
                    //                var encounters = g.ToList();

                    //                var visit = new Visit
                    //                {
                    //                    startDatetime = g.Key,
                    //                    stopDatetime = g.Key,
                    //                    location = encounters[0].location,
                    //                    patient = patientUuid
                    //                };

                    //                var dv = encounters[0];
                    //                var visitUuid = "";

                    //                var vV = apiHelper.GetData("visit?limit=1&startIndex=0&todate=" + g.Key + "&patient=" + patientUuid + "&fromdate=" + g.Key).Result.results;
                    //                if (vV.Any())
                    //                {
                    //                    visitUuid = vV[0].uuid;
                    //                }
                    //                else
                    //                {
                    //                    visitUuid = apiHelper.SendData<ApiResponse, Visit>(URLConstants.visit, visit).Result.uuid;
                    //                }

                    //                if (string.IsNullOrEmpty(visitUuid))
                    //                {                                        
                    //                    Console.ForegroundColor = ConsoleColor.Red;
                    //                    Console.WriteLine(Environment.NewLine + "The clinical Visit on " + visit.startDatetime + " for patient " + p.identifiers[0].identifier + " failed to be migrated" + Environment.NewLine + Environment.NewLine);
                    //                    Console.ForegroundColor = ConsoleColor.White;
                    //                    continue;
                    //                }

                    //                migrationReport.visit += 1;
                    //                visitsMigrated += 1;

                    //                foreach (var enc in encounters)
                    //                {
                    //                    enc.visit = visitUuid;
                    //                    enc.encounterDatetime = g.Key;

                    //                    enc.obs.ForEach(ol =>
                    //                    {
                    //                        ol.location = enc.location;
                    //                        ol.obsDatetime = enc.encounterDatetime;
                    //                        ol.person = personUuid;

                    //                        if (ol.groupMembers.Any())
                    //                        {
                    //                            var groupMembers = new List<string>();

                    //                            ol.groupMembers.ForEach(og =>
                    //                            {
                    //                                og.location = enc.location;
                    //                                og.obsDatetime = enc.encounterDatetime;
                    //                                og.person = personUuid;
                    //                            });
                    //                        }
                    //                    });

                    //                    var encounterUuid = "";
                    //                    var encuuids = apiHelper.GetData("encounter?todate=" + enc.encounterDatetime + "&patient=" + patientUuid + "&encounterType=" + enc.encounterType + "&fromdate=" + enc.encounterDatetime).Result.results;
                    //                    if (encuuids.Any())
                    //                    {
                    //                        encounterUuid = encuuids[0].uuid;
                    //                    }

                    //                    if(string.IsNullOrEmpty(encounterUuid))
                    //                    {
                    //                        encounterUuid = apiHelper.SendData<ApiResponse, Encounter>(URLConstants.encounter, enc).Result.uuid;
                    //                    }

                    //                    if (!string.IsNullOrEmpty(encounterUuid))
                    //                    {
                    //                        migrationReport.encounters += 1;
                    //                        encountersMigrated += 1;
                    //                        migrationReport.obs += enc.obs.Count();
                    //                        obsCount += 1;

                    //                        enc.obs.ForEach(l =>
                    //                        {
                    //                            if (l.groupMembers.Any())
                    //                            {
                    //                                migrationReport.obs += l.groupMembers.Count();
                    //                                obsCount += 1;
                    //                            }

                    //                        });
                    //                    }
                    //                    else
                    //                    {
                    //                        Console.ForegroundColor = ConsoleColor.Red;
                    //                        Console.WriteLine("Error Migrating Encounter:{0}", Environment.NewLine);
                    //                        Console.ForegroundColor = ConsoleColor.White;
                    //                        Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(enc) + Environment.NewLine + Environment.NewLine);
                    //                    }
                    //                }
                    //            }

                    //            var summarry = "Patient " + p.identifiers[0].identifier + " successfully migrated with: " + Environment.NewLine +
                    //            visitsMigrated.ToString() + " Visits, " + encountersMigrated.ToString() + " Encounters and " + obsCount.ToString() + " Obs";

                    //            Console.ForegroundColor = ConsoleColor.Green;
                    //            Console.WriteLine(summarry);
                    //            var d = (DateTime.Now - startD).ToString(@"hh\:mm\:ss");
                    //            Console.WriteLine("Duration: {0}{1}{2}", d, Environment.NewLine, Environment.NewLine);
                    //        }
                    //        else
                    //        {
                    //            Console.ForegroundColor = ConsoleColor.Red;
                    //            Console.WriteLine(Environment.NewLine + Environment.NewLine);
                    //            Console.WriteLine("Patient " + p.identifiers[0].identifier + " could not be migrated. Please check the error message and make the necessary corrections" + Environment.NewLine);
                    //        }
                    //    }

                    //});

                    return migrationReport;
                }
                return new MigrationReport();
            }
            catch (Exception ex)
            {
                var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(Environment.NewLine + Environment.NewLine);
                Console.WriteLine("An error was encountered with the following message:" + Environment.NewLine);
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(message + Environment.NewLine);
                return migrationReport;
            }
        }
        public ApiResponse UpdateFacility<T>(string urlPart, T data)
        {
            var res = apiHelper.SendData<ApiResponse, T>(urlPart, data).Result;
            return res;
        }
    }
}
