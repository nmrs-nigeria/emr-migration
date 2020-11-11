using Common;
using LAMIS_NMRS.Models;
using LAMIS_NMRS.Utils;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LAMIS_NMRS
{
    class Program
    {             
        static void Main(string[] args)
        {
            var apiUrl = Utilities.GetAppConfigItem("rest_api");
            APIHelper apiHelper = new APIHelper(apiUrl);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Starting Migration. Please don't close this window...." + Environment.NewLine);

            MigrateData(apiHelper, 0, 10);
        }
        static DateTime startDate = DateTime.Now;
        static int patientsMigrated = 0;
        static int encounterMigrated = 0;
        static int obsMigrated = 0;
        static int visitMigrated = 0;

        public static void MigrateData(APIHelper apiHelper, int page, int pageSize)
        {
            try
            {               
                page += 1;
                var patients = new DataBuilder().BuildPatientInfo(pageSize, page);
                if (patients.Any())
                {
                    patients.ForEach(p =>
                    {
                        //migrate person, identifiers, address                        
                        var personUuid = apiHelper.PostMessageWithData<CommonResponse, PatientDemography>(URLConstants.person, p.person).Result.uuid;
                        if (!string.IsNullOrEmpty(personUuid))
                        {
                            var patientInfo = new PatientInfo
                            {
                                person = personUuid,
                                identifiers = p.identifiers
                            };
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.WriteLine("Migrating patient with ID {0} ...", p.identifiers[0].identifier);
                            var patientUuid = apiHelper.PostMessageWithData<CommonResponse, PatientInfo>(URLConstants.patient, patientInfo).Result.uuid;

                            foreach (var e in p.Encounters)
                            {
                                e.patient = patientUuid;
                            }
                            patientsMigrated += 1;
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

                                var visitUuid = apiHelper.PostMessageWithData<CommonResponse, Visit>(URLConstants.visit, visit).Result.uuid;
                                if (string.IsNullOrEmpty(visitUuid))
                                {
                                    var error = "Clinical Visit failed to migrate. Visit Date: " + g.Key;
                                    Console.ForegroundColor = ConsoleColor.White;
                                    Console.WriteLine("Visit migration for patient with ID: {0} {1} {2} at " + visit.startDatetime + " failed with the following message",p.identifiers[0],Environment.NewLine,Environment.NewLine);

                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine(error);
                                    continue;
                                }
                                visitMigrated += 1;

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

                                    var encounterUuid = apiHelper.PostMessageWithData<CommonResponse, Encounter>(URLConstants.encounter, enc).Result.uuid;
                                    if (!string.IsNullOrEmpty(encounterUuid))
                                    {
                                        encounterMigrated += 1;
                                        obsMigrated += enc.obs.Count();
                                        enc.obs.ForEach(l => 
                                        { 
                                        
                                            if(l.groupMembers.Any())
                                                obsMigrated += l.groupMembers.Count();
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
                            var summarry = "Patients: " + patientsMigrated.ToString() +
                            "; Encounters: " + encounterMigrated.ToString() +
                            "; Visits: " + visitMigrated.ToString()+
                            "; Obs: " + obsMigrated.ToString();
                            
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("Total Data Migrated: {0}{1}{2}Total Duration: {3} seconds{4}{5}", Environment.NewLine, summarry, Environment.NewLine, (DateTime.Now - startDate).ToString("hh:mm:ss"), Environment.NewLine, Environment.NewLine);                            
                        }

                    });

                    MigrateData(apiHelper, page, pageSize);
                }               
                            
            }
            catch(Exception ex)
            {
                var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(Environment.NewLine + Environment.NewLine);
                Console.WriteLine("An error was encountered with the following message:" + Environment.NewLine);
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(message + Environment.NewLine);                
            }
        }       
    }
}
