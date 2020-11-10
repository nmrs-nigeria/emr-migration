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
            MigrateData(apiHelper, 0, 10);
        }

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
                            var patientUuid = apiHelper.PostMessageWithData<CommonResponse, PatientInfo>(URLConstants.patient, patientInfo).Result.uuid;

                            foreach (var e in p.Encounters)
                            {
                                e.patient = patientUuid;
                            }
                            patientsMigrated += 1;
                            var encounterGroups = p.Encounters.GroupBy(g => g.encounterDatetime).ToList();

                            //Migrate Encounters
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
                                    Console.WriteLine(error);
                                    continue;
                                }
                                visitMigrated += 1;

                                foreach (var enc in encounters)
                                {
                                    //obsMigrated += enc.obs.Count();

                                    enc.visit = visitUuid;

                                    //var obsList = enc.obs;
                                    //enc.obs = null;
                                                              
                                    enc.obs.ForEach(ol =>
                                    {
                                        //ol.encounter = encounterUuid;
                                        ol.location = enc.location;
                                        ol.obsDatetime = DateTime.Now.ToString("yyyy-MM-dd");
                                        ol.person = personUuid;                                                                                      
                                           
                                        if (ol.groupMembers.Any())
                                        {
                                            var groupMembers = new List<string>();

                                            ol.groupMembers.ForEach(og =>
                                            {
                                                //og.encounter = encounterUuid;
                                                og.location = enc.location;
                                                og.obsDatetime = DateTime.Now.ToString("yyyy-MM-dd");
                                                og.person = personUuid;

                                                //var cIds2 = nmsConcepts.Where(c => c.ConceptId == og.concept).ToList();
                                                //if (cIds2.Any())
                                                //{
                                                //    og.concept = cIds2[0].UuId;

                                                    //var obsUuid = apiHelper.PostMessageWithData<CommonResponse, Obs>(URLConstants.obs, og).Result.uuid;
                                                    //if (!string.IsNullOrEmpty(obsUuid))
                                                    //{
                                                    //    groupMembers.Add(obsUuid);
                                                    //}
                                                    //else
                                                    //{
                                                    //    Console.WriteLine("Encounter Obs failed to be migrated");
                                                    //}
                                                //}
                                            });

                                            //var obsUuid2 = apiHelper.PostMessageWithData<CommonResponse, Obs>(URLConstants.obs, ol).Result.uuid;
                                            //if (!string.IsNullOrEmpty(obsUuid2))
                                            //{

                                            //}
                                        }
                                    });                                        

                                    var encounterUuid = apiHelper.PostMessageWithData<CommonResponse, Encounter>(URLConstants.encounter, enc).Result.uuid;
                                    if (!string.IsNullOrEmpty(encounterUuid))
                                    {
                                        encounterMigrated += 1;
                                        //encounterUuids.Add(encounterUuid);
                                    }
                                    else
                                    {
                                        Console.WriteLine("Error Migrating Encounter {0}: {1}", Environment.NewLine, Newtonsoft.Json.JsonConvert.SerializeObject(enc));
                                    }
                                }
                            }
                            var summarry = "Patients Migrated: " + patientsMigrated.ToString() + Environment.NewLine +
                            "Encounters Migrated: " + encounterMigrated.ToString() + Environment.NewLine +
                            "Visit Migrated: " + visitMigrated.ToString() + Environment.NewLine +
                            "Obs Migrated: " + obsMigrated.ToString();
                            Console.WriteLine(summarry);
                        }

                    });

                    page += 1;
                    MigrateData(apiHelper, page, pageSize);
                }               
                            
            }
            catch(Exception ex)
            {
                var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                Console.WriteLine(message);
            
            }
        }       
    }
}
