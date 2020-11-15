using Common;
using LAMIS_NMRS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LAMIS_NMRS.Utils
{
    public class MigrateData
    {
        APIHelper apiHelper;
        MigrationReport migrationReport;

        public MigrateData()
        {
            var apiUrl = Utilities.GetAppConfigItem("rest_api");
            apiHelper = new APIHelper(apiUrl);
            migrationReport = new MigrationReport();
        }
       
        public MigrationReport Migrate(List<Patient> patients)
        {
            try
            {
                if (patients.Any())
                {
                    patients.ForEach(p =>
                    {
                        //migrate person, identifiers, address   
                        var startD = DateTime.Now;
                        var patientMigrated = 1;
                        var encountersMigrated = 0;
                        var visitsMigrated = 0;
                        var obsCount = 0;

                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine("Migrating patient with ID {0} ...", p.identifiers[0].identifier);
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
                            if(!string.IsNullOrEmpty(patientUuid))
                            {
                                foreach (var e in p.Encounters)
                                {
                                    e.patient = patientUuid;
                                }
                                migrationReport.patients += 1;
                                var encounterGroups = p.Encounters.GroupBy(g => g.encounterDatetime).ToList();

                                //Migrate Encounters
                                Console.WriteLine("Migrating encounters...{0}", Environment.NewLine);
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
                                        var error = "Clinical Visit failed to migrate. Visit Date: " + g.Key;
                                        Console.ForegroundColor = ConsoleColor.White;
                                        Console.WriteLine("Visit migration for patient with ID: {0} {1} {2} at " + visit.startDatetime + " failed with the following message", p.identifiers[0], Environment.NewLine, Environment.NewLine);

                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.WriteLine(error);
                                        continue;
                                    }

                                    migrationReport.visit += 1;
                                    visitsMigrated += 1;

                                    foreach (var enc in encounters)
                                    {
                                        enc.visit = visitUuid;

                                        enc.obs.ForEach(ol =>
                                        {
                                            ol.location = enc.location;
                                            ol.obsDatetime = DateTime.Now.ToString("yyyy-MM-dd");
                                            ol.person = personUuid;

                                            if (ol.groupMembers.Any())
                                            {
                                                var groupMembers = new List<string>();

                                                ol.groupMembers.ForEach(og =>
                                                {
                                                    og.location = enc.location;
                                                    og.obsDatetime = DateTime.Now.ToString("yyyy-MM-dd");
                                                    og.person = personUuid;
                                                });
                                            }
                                        });

                                        var encounterUuid = apiHelper.SendData<ApiResponse, Encounter>(URLConstants.encounter, enc).Result.uuid;
                                        if (!string.IsNullOrEmpty(encounterUuid))
                                        {
                                            migrationReport.encounters += 1;
                                            encountersMigrated += 1;
                                            migrationReport.obs += enc.obs.Count();
                                            obsCount += 1;

                                            enc.obs.ForEach(l =>
                                            {

                                                if (l.groupMembers.Any())
                                                {
                                                    migrationReport.obs += l.groupMembers.Count();
                                                    obsCount += 1;
                                                }
                                                    
                                            });
                                        }
                                        else
                                        {
                                            Console.ForegroundColor = ConsoleColor.Red;
                                            Console.WriteLine("Error Migrating Encounter:{0}", Environment.NewLine);
                                            Console.ForegroundColor = ConsoleColor.White;
                                            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(enc) + Environment.NewLine + Environment.NewLine);
                                        }
                                    }
                                }

                                var summarry = "Patient " + p.identifiers[0].identifier + " successfully migrated with: " + Environment.NewLine +
                                visitsMigrated.ToString() +" Visits, " + encountersMigrated.ToString() + " Encounters and " + obsCount.ToString() + " Obs";

                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine(summarry);
                                var d = (DateTime.Now - startD).ToString(@"hh\:mm\:ss");
                                Console.WriteLine("Duration: {0}{1}{2}", d, Environment.NewLine, Environment.NewLine);
                            }
                            else
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine(Environment.NewLine + Environment.NewLine);
                                Console.WriteLine("Patient " + p.identifiers[0].identifier + " could not be migrated. Please check the error message and make the necessary corrections" + Environment.NewLine);
                            }
                        }

                    });
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
        public MigrationReport UpdateMigrate(List<Patient> patients)
        {
            try
            {
                if (patients.Any())
                {
                    patients.ForEach(p =>
                    {
                        //migrate person, identifiers, address   
                        var startD = DateTime.Now;
                        var encountersMigrated = 0;
                        var visitsMigrated = 0;
                        var obsCount = 0;

                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine("Migrating patient with ID {0} ...", p.identifiers[0].identifier);
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
                        var dy = apiHelper.GetData("patient?v=default&identifier=" + p.identifiers[0].identifier).Result;
                        if(dy != null)
                        {
                            var ll = dy.results.ToList();
                            if(ll.Any())
                            {
                                personUuid = ll[0].uuid;
                                patientExists = true;
                            }
                        }

                        if(string.IsNullOrEmpty(personUuid))
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
                            
                            if (!string.IsNullOrEmpty(patientUuid))
                            {
                                foreach (var e in p.Encounters)
                                {
                                    e.patient = patientUuid;
                                }
                                migrationReport.patients += 1;
                                var encounterGroups = p.Encounters.GroupBy(g => g.encounterDatetime).ToList();

                                //Migrate Encounters
                                Console.WriteLine("Migrating encounters...{0}", Environment.NewLine);
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
                                        var error = "Clinical Visit failed to migrate. Visit Date: " + g.Key;
                                        Console.ForegroundColor = ConsoleColor.White;
                                        Console.WriteLine("Visit migration for patient with ID: {0} {1} {2} at " + visit.startDatetime + " failed with the following message", p.identifiers[0], Environment.NewLine, Environment.NewLine);

                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.WriteLine(error);
                                        continue;
                                    }

                                    migrationReport.visit += 1;
                                    visitsMigrated += 1;

                                    foreach (var enc in encounters)
                                    {
                                        enc.visit = visitUuid;

                                        enc.obs.ForEach(ol =>
                                        {
                                            ol.location = enc.location;
                                            ol.obsDatetime = DateTime.Now.ToString("yyyy-MM-dd");
                                            ol.person = personUuid;

                                            if (ol.groupMembers.Any())
                                            {
                                                var groupMembers = new List<string>();

                                                ol.groupMembers.ForEach(og =>
                                                {
                                                    og.location = enc.location;
                                                    og.obsDatetime = DateTime.Now.ToString("yyyy-MM-dd");
                                                    og.person = personUuid;
                                                });
                                            }
                                        });

                                        var encounterUuid = "";

                                        var dy2 = apiHelper.GetData("encounter?v=default&todate=" + enc.encounterDatetime + "&patient=" + patientUuid + "&encounterType=" + enc.encounterType + "&fromdate=" + enc.encounterDatetime).Result;

                                        if (dy2 != null)
                                        {
                                            var excEnc = dy2.results.ToList();
                                            if (excEnc.Any())
                                            {
                                                encounterUuid = excEnc[0].uuid;
                                            }
                                        }

                                        if(string.IsNullOrEmpty(encounterUuid))
                                        {
                                            encounterUuid = apiHelper.SendData<ApiResponse, Encounter>(URLConstants.encounter, enc).Result.uuid;
                                        }
                                                                                
                                        if (!string.IsNullOrEmpty(encounterUuid))
                                        {
                                            migrationReport.encounters += 1;
                                            encountersMigrated += 1;
                                            migrationReport.obs += enc.obs.Count();
                                            obsCount += 1;

                                            enc.obs.ForEach(l =>
                                            {
                                                if (l.groupMembers.Any())
                                                {
                                                    migrationReport.obs += l.groupMembers.Count();
                                                    obsCount += 1;
                                                }

                                            });
                                        }
                                        else
                                        {
                                            Console.ForegroundColor = ConsoleColor.Red;
                                            Console.WriteLine("Error Migrating Encounter:{0}", Environment.NewLine);
                                            Console.ForegroundColor = ConsoleColor.White;
                                            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(enc) + Environment.NewLine + Environment.NewLine);
                                        }
                                    }
                                }

                                var summarry = "Patient " + p.identifiers[0].identifier + " successfully migrated with: " + Environment.NewLine +
                                visitsMigrated.ToString() + " Visits, " + encountersMigrated.ToString() + " Encounters and " + obsCount.ToString() + " Obs";

                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine(summarry);
                                var d = (DateTime.Now - startD).ToString(@"hh\:mm\:ss");
                                Console.WriteLine("Duration: {0}{1}{2}", d, Environment.NewLine, Environment.NewLine);
                            }
                            else
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine(Environment.NewLine + Environment.NewLine);
                                Console.WriteLine("Patient " + p.identifiers[0].identifier + " could not be migrated. Please check the error message and make the necessary corrections" + Environment.NewLine);
                            }
                        }

                    });

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
    }
}
