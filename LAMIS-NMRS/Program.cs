using Common;
using LAMIS_NMRS.Models;
using LAMIS_NMRS.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
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
                if(patients.Any())
                {                    
                    patients.ForEach(p => 
                    {
                        //migrate person, identifiers, address
                        var personUuid = apiHelper.PostMessageWithData<CommonResponse, PatientDemography>(URLConstants.person, p.person).Result.uuid;
                        if(!string.IsNullOrEmpty(personUuid))
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

                                foreach (var enc in encounters)
                                {
                                    var encounterUuid = apiHelper.PostMessageWithData<CommonResponse, Encounter>(URLConstants.encounter, enc).Result.uuid;
                                    if(!string.IsNullOrEmpty(encounterUuid))
                                    {
                                        encounterUuids.Add(encounterUuid);
                                        encounterMigrated += 1;
                                        obsMigrated += enc.obs.Count();
                                        enc.obs.ForEach(o => 
                                        {
                                            if(o.groupMembers.Any())
                                                obsMigrated += o.groupMembers.Count();
                                        });
                                    }
                                }

                                var visit = new Visit
                                {
                                    startDatetime = g.Key,
                                    stopDatetime = g.Key,
                                    encounters = encounterUuids,
                                    location = encounters[0].location,
                                    patient = patientUuid
                                };

                                var visitUuid = apiHelper.PostMessageWithData<CommonResponse, Visit>(URLConstants.visit, visit).Result.uuid;
                                if (!string.IsNullOrEmpty(visitUuid))
                                {
                                    var error = "Clinical Visit failed to migrate. Visit Date: " + g.Key;
                                    Console.WriteLine(error);
                                }
                                visitMigrated += 1;
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
