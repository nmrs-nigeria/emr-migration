using Common;
using LAMIS_NMRS.Models;
using LAMIS_NMRS.Utils;
using System;

namespace LAMIS_NMRS
{
    class Program
    {             
        static void Main(string[] args)
        {
            Console.WriteLine(Environment.NewLine);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Before we begin::" + Environment.NewLine);
            Console.WriteLine("Please ensure to first provide the correct values for the following variables:");
            Console.WriteLine("lamis_Database_Name");
            Console.WriteLine("lamis_Server_Username");
            Console.WriteLine("lamis_Server_Password");
            Console.WriteLine("facilty_name");
            Console.WriteLine("facility_datim_code");
            Console.WriteLine("nmrs_Database_Name");
            Console.WriteLine("nmrs_Server_Username");
            Console.WriteLine("nmrs_Server_Password");
            Console.WriteLine("nmrs_Server_Port" + Environment.NewLine);
            Console.WriteLine("This can be done in the AppSettings.json file in this application's root folder.");

            var migOption = new MigrationOption();

            // Prompt user to select to either migrate data from CSV or Database            
            // and read user's option

            Console.ForegroundColor = ConsoleColor.White;

        ChooseMigrationOption: var migrationOption = ChooseMigrationOption();

            if(string.IsNullOrEmpty(migrationOption))
            {
                goto ChooseMigrationOption;
            }

            var isValidOption = int.TryParse(migrationOption, out int option);
            if (!isValidOption || option < 1 || option > 2)
            {
                goto ChooseMigrationOption;
            }

            migOption.Option = option;

        getFacilityId: var facility_Id = EnterFacilityId();
            if (string.IsNullOrEmpty(facility_Id))
            {
                Console.WriteLine(Environment.NewLine + "Please enter a valid integer value:");
                goto getFacilityId;
            }

            var isValidEntry = int.TryParse(facility_Id, out int facilityId);
            if (!isValidEntry || facilityId < 1)
            {
                Console.WriteLine(Environment.NewLine + "Please enter LAMIS Facility ID:");
                goto getFacilityId;
            }

            migOption.Facility = facilityId;
                       
            migOption.FaciltyName = Utilities.GetAppConfigItem("facilty_name");
            migOption.FacilityDatim_code = Utilities.GetAppConfigItem("facility_datim_code");
            migOption.NmrsServerUsername = Utilities.GetAppConfigItem("nmrs_Server_Username");
            migOption.NmrsServerPassword = Utilities.GetAppConfigItem("nmrs_Server_Password");
            migOption.NmrsServerPort = Utilities.GetAppConfigItem("nmrs_Server_Port");
            migOption.NmrsDatabaseName = Utilities.GetAppConfigItem("nmrs_Database_Name");

            if (string.IsNullOrEmpty(migOption.FaciltyName) || string.IsNullOrEmpty(migOption.FacilityDatim_code) || string.IsNullOrEmpty(migOption.NmrsServerUsername) || string.IsNullOrEmpty(migOption.NmrsServerPassword) || string.IsNullOrEmpty(migOption.NmrsServerPort) || string.IsNullOrEmpty(migOption.NmrsDatabaseName))
            {
                Console.WriteLine(Environment.NewLine + "Some required variables were not provided. Please review the AppSettings.json file and ensure all variables are provided.");
                return;
            }

            if (option == 1) //migrate from Database
            {
                #region LAMIS Server Credentials
                migOption.LamisDatabaseName = Utilities.GetAppConfigItem("lamis_Database_Name");
                migOption.LamisUsername = Utilities.GetAppConfigItem("lamis_Server_Username");
                migOption.LamisPassword = Utilities.GetAppConfigItem("lamis_Server_Password");

                if(string.IsNullOrEmpty(migOption.LamisDatabaseName) || string.IsNullOrEmpty(migOption.LamisUsername) || string.IsNullOrEmpty(migOption.LamisPassword))
                {
                    Console.WriteLine(Environment.NewLine + "Some required variables were not provided. Please review the AppSettings.json file and ensure all variables are provided.");
                    return;
                }
                #endregion
            }
            else
            {
                if (option == 2) //Migrate from files
                {
                    //prompt user to enter PatientDemography File Path
                    Console.WriteLine(Environment.NewLine + "Please paste PatientDemography Data File Path:");
                    migOption.PatientsFilePath = Console.ReadLine();

                    //prompt user to enter Clinicals File Path
                    Console.WriteLine(Environment.NewLine + "Please paste Clinicals Data File Path:");
                    migOption.ClinicalsFilePath = Console.ReadLine();

                    //prompt user to enter Lab data File Path
                    Console.WriteLine(Environment.NewLine + "Please paste Lab data File Path:");
                    migOption.LabDataFilePath = Console.ReadLine();

                    //prompt user to enter Pharmacy Data File Path
                    Console.WriteLine(Environment.NewLine + "Please paste Pharmacy Data File Path:");
                    migOption.PharmacyDataFilePath = Console.ReadLine();
                }
            }            

            if(migOption.Option == 1)
            {
                new DataBuilder_Read_From_DB(migOption);
            }
            else
            {
                new DataBuilder(migOption);
            }
        }     
        
        static string ChooseMigrationOption()
        {
            // Prompt user to select to either migrate data from CSV or Database
            Console.WriteLine(string.Empty);
            Console.WriteLine("::: Migration Options :::");
            Console.ForegroundColor = ConsoleColor.Green;            
            Console.WriteLine("1. Migrate from a LAMIS Database");
            Console.WriteLine("2. Migrate Data from CSV/Excel files");            
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Which do you prefer? Enter 1 or 2");
            return Console.ReadLine();
        }

        static string EnterFacilityId()
        {
            // Force user to select either option 1 or 2
            Console.WriteLine(Environment.NewLine + "Please enter LAMIS Facility ID (This is the facility primary key from the LAMIS Database):");
            return Console.ReadLine();
        }
    }
}
