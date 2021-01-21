using Common;
using LAMIS_NMRS.Models;
using LAMIS_NMRS.Utils;
using System;
using System.Threading.Tasks;

namespace LAMIS_NMRS
{
    class Program
    {
        [MTAThread]
        static async Task Main(string[] args)
        {
            try
            {
                Console.WriteLine(Environment.NewLine);
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(" Before we begin::" + Environment.NewLine);
                Console.WriteLine(" Please ensure to first provide the correct values for the following variables:" + Environment.NewLine);
                Console.WriteLine(" *  partner_short_name");
                Console.WriteLine(" *  partner_full_name");
                Console.WriteLine(" *  facilty_name");
                Console.WriteLine(" *  facility_datim_code");
                //Console.WriteLine(" *  nmrs_Database_Name");
                Console.WriteLine(" *  nmrs_Web_Username");
                Console.WriteLine(" *  nmrs_Web_Password");
                Console.WriteLine(" *  nmrs_Server_Port" + Environment.NewLine);
                Console.WriteLine(" This can be done in the AppSettings.json file in this application's root folder.");

                var migOption = new MigrationOption();

                // Prompt user to select to either migrate data from CSV or Database            
                // and read user's option

                Console.ForegroundColor = ConsoleColor.White;

            ChooseMigrationOption: var migrationOption = "3";// ChooseMigrationOption();

                if (string.IsNullOrEmpty(migrationOption))
                {
                    goto ChooseMigrationOption;
                }

                var isValidOption = int.TryParse(migrationOption, out int option);
                if (!isValidOption || option < 1 || option > 3)
                {
                    goto ChooseMigrationOption;
                }

                migOption.Option = option;

                if (option != 3)
                {
                getFacilityId: var facility_Id = EnterFacilityId();
                    if (string.IsNullOrEmpty(facility_Id))
                    {
                        Console.WriteLine(Environment.NewLine + " Please enter a valid integer value:");
                        goto getFacilityId;
                    }

                    var isValidEntry = int.TryParse(facility_Id, out int facilityId);
                    if (!isValidEntry || facilityId < 1)
                    {
                        Console.WriteLine(Environment.NewLine + " Please enter LAMIS Facility ID:");
                        goto getFacilityId;
                    }

                    migOption.Facility = facilityId;
                }

                migOption.FaciltyName = Utilities.GetAppConfigItem("facilty_name");
                migOption.FacilityDatim_code = Utilities.GetAppConfigItem("facility_datim_code");
                migOption.NmrsWebUsername = Utilities.GetAppConfigItem("nmrs_Web_Username");
                migOption.NmrsWebPassword = Utilities.GetAppConfigItem("nmrs_Web_Password");
                migOption.NmrsServerPort = Utilities.GetAppConfigItem("nmrs_Server_Port");
                //migOption.NmrsDatabaseName = Utilities.GetAppConfigItem("nmrs_Database_Name");
                migOption.NmrsUrlBasePath = Utilities.GetAppConfigItem("nmrs_Url_Base_Path");
                migOption.PartnerShortName = Utilities.GetAppConfigItem("partner_short_name");
                migOption.PartnerFullName = Utilities.GetAppConfigItem("partner_full_name");

                if (string.IsNullOrEmpty(migOption.FaciltyName) || string.IsNullOrEmpty(migOption.FacilityDatim_code) || string.IsNullOrEmpty(migOption.NmrsWebUsername) || string.IsNullOrEmpty(migOption.NmrsWebPassword) || string.IsNullOrEmpty(migOption.NmrsServerPort)) // || string.IsNullOrEmpty(migOption.NmrsDatabaseName)
                {
                    Console.WriteLine(Environment.NewLine + " Some required variables were not provided. Please review the AppSettings.json file and ensure all variables are provided.");
                    return;
                }


                if (option == 1) //migrate from Database
                {

                    Console.WriteLine(Environment.NewLine + " You have chosen to migrate data from a LAMIS database :::" + Environment.NewLine);
                    Console.WriteLine(" Please ensure that the correct values for the variables listed below are provided in the AppSettings.json file as well:" + Environment.NewLine);
                    Console.WriteLine(" *  lamis_Database_Name");
                    Console.WriteLine(" *  lamis_Server_Username");
                    Console.WriteLine(" *  lamis_Server_Password" + Environment.NewLine);

                    #region LAMIS Server Credentials
                    migOption.LamisDatabaseName = Utilities.GetAppConfigItem("lamis_Database_Name");
                    migOption.LamisUsername = Utilities.GetAppConfigItem("lamis_Server_Username");
                    migOption.LamisPassword = Utilities.GetAppConfigItem("lamis_Server_Password");

                    if (string.IsNullOrEmpty(migOption.LamisDatabaseName) || string.IsNullOrEmpty(migOption.LamisUsername) || string.IsNullOrEmpty(migOption.LamisPassword))
                    {
                        Console.WriteLine(Environment.NewLine + " Some required variables were not provided. Please review the AppSettings.json file and ensure all variables are provided.");
                        goto ChooseMigrationOption;
                    }
                    #endregion
                }
                else if (option == 2)
                {
                    //Migrate from files
                    Console.WriteLine(Environment.NewLine + " You have chosen to Migrate Data from Excel (.xlsx) files :::" + Environment.NewLine);
                    Console.WriteLine(" Please ensure that the correct and full file paths for the variables listed below are provided in the AppSettings.json file as well:" + Environment.NewLine);
                    Console.WriteLine(" *  patients_Data_File_Path");
                    Console.WriteLine(" *  clinic_Data_File_Path");
                    Console.WriteLine(" *  lab_Data_File_Path");
                    Console.WriteLine(" *  pharmacy_Data_File_Path" + Environment.NewLine);
                    migOption.PatientsFilePath = Utilities.GetAppConfigItem("patients_Data_File_Path");
                    migOption.ClinicalsFilePath = Utilities.GetAppConfigItem("clinic_Data_File_Path");
                    migOption.LabDataFilePath = Utilities.GetAppConfigItem("lab_Data_File_Path");
                    migOption.PharmacyDataFilePath = Utilities.GetAppConfigItem("pharmacy_Data_File_Path");
                    if (string.IsNullOrEmpty(migOption.PatientsFilePath) || string.IsNullOrEmpty(migOption.ClinicalsFilePath) || string.IsNullOrEmpty(migOption.LabDataFilePath) || string.IsNullOrEmpty(migOption.PharmacyDataFilePath))
                    {
                        Console.WriteLine(Environment.NewLine + " One or more required Data File Path(s) were not provided. Please review the AppSettings.json file and ensure all Data File Paths are provided.");
                        goto ChooseMigrationOption;
                    }
                }
                else if (option == 3)
                {
                    Console.WriteLine(Environment.NewLine + " You have chosen to migrate data from a NMRS database to NMRS PoC database :::" + Environment.NewLine);
                    Console.WriteLine(" Please ensure that the correct values for the variables listed below are provided in the AppSettings.json file as well:" + Environment.NewLine);
                    Console.WriteLine(" *  nmrs_Database_Name");
                    Console.WriteLine(" *  nmrs_Server_Username");
                    Console.WriteLine(" *  nmrs_Server_Password" + Environment.NewLine);

                    #region NMRS Server Credentials
                    migOption.LamisDatabaseName = Utilities.GetAppConfigItem("nmrs_Database_Name");
                    migOption.LamisUsername = Utilities.GetAppConfigItem("nmrs_Server_Username");
                    migOption.LamisPassword = Utilities.GetAppConfigItem("nmrs_Server_Password");

                    if (string.IsNullOrEmpty(migOption.LamisDatabaseName) || string.IsNullOrEmpty(migOption.LamisUsername) || string.IsNullOrEmpty(migOption.LamisPassword))
                    {
                        Console.WriteLine(Environment.NewLine + " Some required variables were not provided. Please review the AppSettings.json file and ensure all variables are provided.");
                        goto ChooseMigrationOption;
                    }
                    #endregion
                }
                var locatin = new Location
                {
                    //property = "aff27d58-a15c-49a6-9beb-d30dcfc0c66e", //location id = 8, uuid
                    name = migOption.FaciltyName
                };
                new MigrateData(migOption).UpdateFacility("location/aff27d58-a15c-49a6-9beb-d30dcfc0c66e", locatin);
                if (migOption.Option == 1)
                {
                    await new DataBuilder_Read_From_DB(migOption).BuildPatientInfo();
                }
                else if (migOption.Option == 3)
                {
                    await new DataBuilder_Read_From_NMRS_DB(migOption).BuildPatientInfo();
                }
                else
                {
                    await new DataBuilder(migOption).BuildPatientInfo();
                }


                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Configuring Facility Details..." + Environment.NewLine);
                Console.ForegroundColor = ConsoleColor.White;

                var migrtr = new MigrateData(migOption);

                //facility Name
                var facilityName = new SystemSetting
                {
                    value = migOption.FaciltyName
                };

                migrtr.UpdateFacility("systemsetting/db0a9be9-b88e-4daf-be8e-fc59887b866f", facilityName);

                //facility datim code
                var facilityDatimCode = new SystemSetting
                {
                    value = migOption.FacilityDatim_code
                };
                migrtr.UpdateFacility("systemsetting/b857adbc-587f-4791-b1a1-291780703ad1", facilityDatimCode);

                //default location
                var defaultLocation = new SystemSetting
                {
                    value = migOption.FaciltyName
                };
                migrtr.UpdateFacility("systemsetting/ff16a585-5ea7-4272-838c-ef9241359592", defaultLocation);

                //location
                var location = new Location
                {
                    //property = "aff27d58-a15c-49a6-9beb-d30dcfc0c66e", //location id = 8, uuid
                    name = migOption.FaciltyName
                };
                migrtr.UpdateFacility("location/aff27d58-a15c-49a6-9beb-d30dcfc0c66e", location);

                if (!string.IsNullOrEmpty(migOption.PartnerShortName))
                {
                    //partner short name
                    var partnerShortName = new SystemSetting
                    {
                        value = migOption.PartnerShortName
                    };
                    migrtr.UpdateFacility("systemsetting/6df044cf-e3f3-485d-af21-8f3a7c0e1b98", partnerShortName);
                }

                if (!string.IsNullOrEmpty(migOption.PartnerFullName))
                {
                    //partner full name
                    var partnerFullName = new SystemSetting
                    {
                        value = migOption.PartnerFullName
                    };
                    migrtr.UpdateFacility("systemsetting/bbd6d156-d908-4001-8374-92c4922d2d1d", partnerFullName);

                }

                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Completed Configuring Facility Details" + Environment.NewLine);
                Console.WriteLine(Environment.NewLine);
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine("** Please evaluate the migration messages and ensure all went well." + Environment.NewLine);
                Console.WriteLine("** You can as well re-initiate the migration process to make up for discrepancies observed or migrate corrections made on failed data." + Environment.NewLine);
                Console.ForegroundColor = ConsoleColor.White;
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.White;
                var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                Console.WriteLine(Environment.NewLine + message + Environment.NewLine);
                Console.ReadLine();
            }
        }

        static string ChooseMigrationOption()
        {
            // Prompt user to select to either migrate data from CSV or Database
            Console.WriteLine(string.Empty);
            Console.WriteLine("::: Migration Options :::");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("1. Migrate from a LAMIS Database");
            Console.WriteLine("2. Migrate Data from Excel (.xlsx) files");
            Console.WriteLine("3. Migrate from NMRS to NMRSPoC");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Which do you prefer? Enter 1, 2 or 3");
            return Console.ReadLine();
        }

        static string EnterFacilityId()
        {
            // Force user to select either option 1 or 2
            Console.WriteLine(Environment.NewLine + "Please enter the LAMIS ID for this Facility (This is the primary key from the Facility table in the LAMIS Database):");
            return Console.ReadLine();
        }
    }
}
