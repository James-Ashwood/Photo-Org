// Modules
using System;       // System
using System.IO;        // System input output
using System.Text;      // Working with text
using System.Threading;     // For use when delaying
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.Generic;       //  To be able to use lists
using MetadataExtractor;        // Metadata extractor - used when extracting EXIF data
using Newtonsoft.Json;      // Used when deseriealizing the holding pair files

namespace Photo_Organiser_Pro
{
    // Holding Pairs class - used when deserializing the JSON file
    class HoldingPairs {
        public List<List<string>> Main { get; set; }      // Stores the lists of holding pairs
        public string Comment { get; set; }     // Stores the additional comment
    }

    // Main Program
    class Program
    {
        static void Main(string[] args)
        {
            FileStream LogFileObject;

            string CurrentProcessName = Process.GetCurrentProcess().ProcessName;       // Gets the current process name

            int ProcessesLog = 0;
            foreach (Process IndividualProcess in Process.GetProcesses()) {
                if (IndividualProcess.ProcessName == CurrentProcessName)
                {
                    ProcessesLog++;
                }
            }
            if (ProcessesLog>1)
                return;

            // Constants to do with this instance of the running of the program
            string Version = "1.0.3";       // Version
            string CurrentDateTime = DateTime.Now.ToString("yyyy-MM-dd-h-mm-ss");       // Gets the current date time and typecasts it to a string
            List<string> LogInfo = new List<string>();      // Stores the information to log
            List<string> AllowedEndings = new List<string>() {"raf", "jpeg", "jpg", "mov", "mp4"};        // A list that stores the allowed file endings
            List<string> DeleteEndings = new List<string>() {"aae"};        // A list that stores the file endings where we should delete the file

            // Write these to STDOUT - used for debugging
            Console.WriteLine("Photo Organiser Pro - Version " + Version.ToString());       // Output the version
            Console.WriteLine("Current Date Time - " + CurrentDateTime);        // And the current run time

            // Write these to the log file as a record
            LogInfo.Add("");
            LogInfo.Add("[NEW START]");
            LogInfo.Add("[INFO]       Photo Organiser Pro - Version " + Version.ToString());
            LogInfo.Add("[INFO]       Current Date Time - " + CurrentDateTime);

            // Reading the holding pairs
            string HoldingPairsString = File.ReadAllText("/app/HoldingPairs.json");
            HoldingPairs HoldingPair = JsonConvert.DeserializeObject<HoldingPairs>(HoldingPairsString);

            List<string> ToReview = new List<string>() {};

            bool Empty = true;
            // Loops through each holding pair
            foreach (List<string> Pair in HoldingPair.Main) {
                // If the Directory exists
                if (System.IO.Directory.Exists(Pair[0]))
                {
                    // If its still empty
                    if (Empty)
                    {
                        // Get all files
                        string[] Files = System.IO.Directory.GetFiles(Pair[0], "*", SearchOption.AllDirectories);
                        // Search through all the files
                        foreach (string File in Files)
                        {
                            // If the file is not in the review folder, 
                            if (!File.Contains(Pair[2]))
                            {
                                Empty = false;
                            }
                        }
                    }
                }
            }
            // If its empty, log its empty and exit
            if (Empty)
            {
                Console.WriteLine("[EMPTY]      No Files To Process");
                LogInfo.Add("[EMPTY]      No Files To Process");
                
                // Write to the log file
                LogFileObject = new FileStream("/app/LogFile.log", FileMode.Append, FileAccess.Write);       // Open the log file in append and write mode
                using (StreamWriter FileWriter = new StreamWriter(LogFileObject)) {     // Write to it with the information
                    foreach (string Line in LogInfo) {      // Loops through each file to output
                        FileWriter.WriteLine(Line);     // Writes it to the file
                    }
                }

                return;     // Exit
            }

            // Loops through each holding pair
            foreach (List<string> Pair in HoldingPair.Main) {

                // Remove all the data from the ToReview list
                ToReview.Clear();
                
                // Record this pair
                Console.WriteLine("[PAIR]       New Holding Pair - " + Pair[0].ToString() + ": " + Pair[1].ToString());     // Outputs the pair to STDOUT
                LogInfo.Add("[PAIR]       New Holding Pair - " + Pair[0].ToString() + ": " + Pair[1].ToString());        // Outputs the pair to the log file

                // If the Directory exists
                if (System.IO.Directory.Exists(Pair[0]))
                {

                    // Get all the files (loops recursively, going into subfolders)
                    string[] Files = System.IO.Directory.GetFiles(Pair[0], "*", SearchOption.AllDirectories);

                    Thread.Sleep(5000);     // Waits for 5000 milliseconds (5 seconds), meaning all copying of files should be complete by then

                    // Loop through these files
                    foreach (string FilePath in Files)
                    {
                        if (!FilePath.Contains(Pair[2]))
                        {
                            // Output this file
                            Console.WriteLine("[FILE]       " + FilePath);
                            LogInfo.Add("[FILE]       " + FilePath);

                            // If we need to process the file
                            if (AllowedEndings.Contains(FilePath.Split(".")[FilePath.Split(".").Length-1].ToLower())) {
                                
                                // Note: The files involved in this if statement will all be required to be proccessed. We use the FilePath and the MetadataExtractor to do this.

                                Console.WriteLine("[PROC]       " + FilePath);      // Output the file path and confirm its is going to be processed
                                LogInfo.Add("[PROC]       " + FilePath);        // And log it


                                // Variables to store the data in
                                string Date = "";
                                string Make = "";
                                string Model = "";
                                string OriginalName = Path.GetFileName(FilePath);

                                // To store the checksums
                                string MD5Checksum = "MD5 Checksum Here";
                                string NewMD5Checksum = "New MD5 Checksum Here";

                                try {

                                    // Get the metadata information
                                    IEnumerable<MetadataExtractor.Directory> Directories = ImageMetadataReader.ReadMetadata(FilePath);

                                    // Loop through the information directories
                                    foreach (var Directory in Directories)
                                    {
                                        // Loop through each specific piece 
                                        foreach (var Tag in Directory.Tags)
                                        {
                                            if (Tag.Name == "Date/Time Original")
                                            {
                                                string[] TempDate = Tag.Description.Split(' ')[0].Split(":");       // Split the date into 3 seperate strings (year, month, date)
                                                if (TempDate[2].Length == 1)        // If the day is only one digit
                                                {
                                                    TempDate[2] = "0" + TempDate[0];        // Add a 0 before the digit
                                                }
                                                if (TempDate[1].Length == 1)        // If the month is only one digit
                                                {
                                                    TempDate[1] = "0" + TempDate[1];        // Add a 0 before the digit
                                                }
                                                Date = $@"{TempDate[0]}-{TempDate[1]}-{TempDate[2]}";       // Form the date from the correct year, month and date
                                            }
                                            if (Tag.Name == "Make")
                                            {
                                                Make = Tag.Description.Replace(" ", "-");
                                            }
                                            if (Tag.Name == "Model")
                                            {
                                                Model = Tag.Description.Replace(" ", "-");
                                            }
                                        }
                                    }

                                    // If we werent able to get a date, try to use the creation date instead.
                                    if (Date == "")
                                    {
                                        foreach (var Directory in Directories)
                                        {
                                            foreach (var Tag in Directory.Tags)
                                            {
                                                if (Tag.Name == "Creation Date")
                                                {
                                                    List<string> Months = new List<string>() { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

                                                    string Year = Tag.Description.Split(" ")[5];
                                                    string Month = (Months.IndexOf(Tag.Description.Split(" ")[1]) + 1).ToString();
                                                    string Day = Tag.Description.Split(" ")[2];
                                                    if (Month.Length == 1)
                                                    {
                                                        Month = "0" + Month;
                                                    }
                                                    if (Day.Length == 1)
                                                    {
                                                        Day = "0" + Day;
                                                    }
                                                    Date = $@"{Year}-{Month}-{Day}";
                                                }
                                            }
                                        }
                                    }
                                    // If we werent able to get a date, try to use the created date instead.
                                    if (Date == "")
                                    {
                                        foreach (var Directory in Directories)
                                        {
                                            foreach (var Tag in Directory.Tags)
                                            {
                                                if (Tag.Name == "Created")
                                                {
                                                    List<string> Months = new List<string>() { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

                                                    string Year = Tag.Description.Split(" ")[4];
                                                    string Month = (Months.IndexOf(Tag.Description.Split(" ")[1]) + 1).ToString();
                                                    string Day = Tag.Description.Split(" ")[2];
                                                    if (Month.Length == 1)
                                                    {
                                                        Month = "0" + Month;
                                                    }
                                                    if (Day.Length == 1)
                                                    {
                                                        Day = "0" + Day;
                                                    }
                                                    Date = $@"{Year}-{Month}-{Day}";
                                                }
                                            }
                                        }
                                    }
                                }
                                catch (IOException)
                                {
                                    Console.WriteLine("[ERROR]      Metadata extraction failed due to IOException");      // Output the old and new file paths
                                    LogInfo.Add("[ERROR]      Metadata extraction failed due to IOException");        // And log it
                                }

                                // If all the data can be found
                                if (Date != "") {

                                    string MakeModel = "";
                                    if (Make == "" && Model == "")
                                    {
                                        MakeModel = "Unknown-Device";
                                    }
                                    else if (Make == "")
                                    {
                                        MakeModel = Model.Replace(" ", "-");
                                    }
                                    else if (Model == "")
                                    {
                                        MakeModel = Make.Replace(" ", "-");
                                    }
                                    else
                                    {
                                        MakeModel = $@"{Make}_{Model}";
                                    }

                                    // Generate new file names
                                    string NewFileName = $@"{Date}_{MakeModel}_{OriginalName}";      // Generate the new file name based of the components we have collected
                                    string NewFilePath = $@"{Pair[1]}/{Date.Replace("-", "/")}/{NewFileName}";      // Using the name and date we can generate the new path
                                    
                                    Console.WriteLine("[NEW NAME]   " + FilePath + ": " + NewFilePath);      // Output the old and new file paths
                                    LogInfo.Add("[NEW NAME]   " + FilePath + ": " + NewFilePath);        // And log it

                                    // Make any required directories
                                    if (!System.IO.Directory.Exists($@"{Pair[1]}/{Date.Replace("-", "/")}/"))
                                    {
                                        System.IO.Directory.CreateDirectory($@"{Pair[1]}/{Date.Replace("-", "/")}/");
                                    }

                                    // Copying
                                    try     // Try copying the files
                                    {
                                        File.Copy(FilePath, NewFilePath, true);
                                    }
                                    catch (IOException)     // If there is an error, report it and move on
                                    {
                                        Console.WriteLine("[ERROR]      Copying Failed due to IOException");      // Output the old and new file paths
                                        LogInfo.Add("[ERROR]      Copying Failed due to IOException");        // And log it
                                    }

                                    // Calculate the new MD5 Checksum
                                    using (var md5 = System.Security.Cryptography.MD5.Create())
                                    {
                                        // Using old file
                                        using (var stream = System.IO.File.OpenRead(FilePath))
                                        {
                                            // Create a hash and return the checksum from the hash
                                            var hash = md5.ComputeHash(stream);
                                            MD5Checksum = BitConverter.ToString(hash).Replace("-", "");
                                        }
                                        // Using new file
                                        using (var stream = System.IO.File.OpenRead(NewFilePath))
                                        {
                                            // Create a hash and return the checksum from the hash
                                            var hash = md5.ComputeHash(stream);
                                            NewMD5Checksum = BitConverter.ToString(hash).Replace("-", "");
                                        }
                                    }

                                    //  If the file exists
                                    if (File.Exists(NewFilePath))
                                    {
                                        // If both checksums are the same
                                        if (MD5Checksum == NewMD5Checksum)
                                        {
                                            // Report a success
                                            Console.WriteLine("[SUCCESS]    Copying was successful. MD5 Checksums were: " + MD5Checksum + " and " + NewMD5Checksum);
                                            LogInfo.Add("[SUCCESS]    Copying was successful. MD5 Checksums were: " + MD5Checksum + " and " + NewMD5Checksum);
                                        
                                            // And delete the original file.
                                            File.Delete(FilePath);
                                            if (!System.IO.Directory.Exists(FilePath)) {     // Make sure the file is deleted
                                                Console.WriteLine("[DELETE OLD] " + FilePath);      // If so, output it
                                                LogInfo.Add("[DELETE OLD] " + FilePath);        // And log it 
                                            }
                                            else {
                                                Console.WriteLine("[ERROR]      Couldnt delete old. " + FilePath + " would not delete");      // Otherwise, output an error
                                                LogInfo.Add("[ERROR]      Couldnt delete old. " + FilePath + " would not delete");        // And log it                               
                                            }
                                        }
                                        // Otherwise
                                        else
                                        {
                                            // Report an error
                                            Console.WriteLine("[ERROR]      Copying Failed due to incorrect copy (MD5 Checksums do not match, they were " + MD5Checksum + " and " + NewMD5Checksum + ")");
                                            LogInfo.Add("[ERROR]      Copying Failed due to incorrect copy (MD5 Checksums do not match" + MD5Checksum + " and " + NewMD5Checksum + ")");
                                        }
                                    }
                                    // Otherwise
                                    else
                                    {
                                        // Report an error
                                        Console.WriteLine("[ERROR]      New file not found");
                                        LogInfo.Add("[ERROR]      New file not found");
                                    }
                                }
                                else        // If we cannot collect the correct data
                                {
                                        // Report an error
                                        Console.WriteLine("[ERROR]      Cannot find all the data\n[ERROR]      Date: " + Date + "\n[ERROR]      Make: " + Make + "\n[ERROR]      Model: " + Model);
                                        LogInfo.Add("[ERROR]      Cannot find all the data. The data found was: ");
                                        LogInfo.Add("[ERROR]      Date: " + Date);
                                        LogInfo.Add("[ERROR]      Make: " + Make);
                                        LogInfo.Add("[ERROR]      Model: " + Model);

                                        // Put it into review
                                        Console.WriteLine("[REVIEW]     " + FilePath);
                                        LogInfo.Add("[REVIEW]     " + FilePath);
                                        ToReview.Add(FilePath);
                                }
                            }

                            // If we need to delete the file
                            else if (DeleteEndings.Contains(FilePath.Split(".")[FilePath.Split(".").Length-1].ToLower())) {
                                File.Delete(FilePath);
                                if (!System.IO.Directory.Exists(FilePath)) {     // Make sure the file is deleted
                                    Console.WriteLine("[DELETED]    " + FilePath);      // If so, output it
                                    LogInfo.Add("[DELETED]    " + FilePath);        // And log it 
                                }
                                else {
                                    Console.WriteLine("[ERROR]      " + FilePath + " would not delete");      // Otherwise, output an error
                                    LogInfo.Add("[ERROR]      " + FilePath + " would not delete");        // And log it                               
                                }
                            }

                            // Otherwise, just move it to the review folder it
                            else {
                                Console.WriteLine("[REVIEW]     " + FilePath);
                                LogInfo.Add("[REVIEW]     " + FilePath);
                                ToReview.Add(FilePath);
                            }
                        }
                    }
                }

                // Adding the files to the review folder
                if (ToReview.Count > 0) {
                    
                    // Create this running's folder
                    if (!File.Exists((Pair[2] + "//" + CurrentDateTime.Replace("/", "-")).Replace("//", "/")))
                    {
                        System.IO.Directory.CreateDirectory((Pair[2] + "/" + CurrentDateTime.Replace("/", "-")).Replace("//", "/"));
                    }

                    // Create the new file path to store the new location
                    string NewFilePath;

                    // Loop through every file and move it
                    foreach (string FilePath in ToReview)
                    {
                        // To store the checksum
                        string MD5Checksum = "Original MD5 Checksum Here";
                        string NewMD5Checksum = "New MD5 Checksum Here";

                        // Calculate the new MD5 Checksum
                        using (var md5 = System.Security.Cryptography.MD5.Create())
                        {
                            // Using old file
                            using (var stream = System.IO.File.OpenRead(FilePath))
                            {
                                // Create a hash and return the checksum from the hash
                                var hash = md5.ComputeHash(stream);
                                MD5Checksum = BitConverter.ToString(hash).Replace("-", "");
                            }
                        }

                        // Calculate new file location
                        NewFilePath = Pair[2] + "/" + CurrentDateTime.Replace("/", "-") + "/" + Path.GetFileName(FilePath);
                        NewFilePath.Replace("//", "/");

                        // Moving the files
                        try     // Try moving the files
                        {
                            File.Copy(FilePath, NewFilePath, true);
                        }
                        catch (IOException)     // If there is an error, report it and move on
                        {
                            Console.WriteLine("[ERROR]      Moving Failed due to IOException");      // Output the old and new file paths
                            LogInfo.Add("[ERROR]      Moving Failed due to IOException");        // And log it
                        }

                        // Calculate the new MD5 Checksum
                        using (var md5 = System.Security.Cryptography.MD5.Create())
                        {
                            // Using new file
                            using (var stream = System.IO.File.OpenRead(NewFilePath))
                            {
                                // Create a hash and return the checksum from the hash
                                var hash = md5.ComputeHash(stream);
                                NewMD5Checksum = BitConverter.ToString(hash).Replace("-", "");
                            }
                        }
                        //  If the file exists
                        if (File.Exists(NewFilePath))
                        {
                            // If both checksums are the same
                            if (MD5Checksum == NewMD5Checksum)
                            {
                                // Report a success
                                Console.WriteLine("[SUCCESS]    Copying was successful. MD5 Checksums were: " + MD5Checksum + " and " + NewMD5Checksum);
                                LogInfo.Add("[SUCCESS]    Copying was successful. MD5 Checksums were: " + MD5Checksum + " and " + NewMD5Checksum);
                            
                                // And delete the original file.
                                File.Delete(FilePath);
                                if (!System.IO.Directory.Exists(FilePath)) {     // Make sure the file is deleted
                                    Console.WriteLine("[DELETE OLD] " + FilePath);      // If so, output it
                                    LogInfo.Add("[DELETE OLD] " + FilePath);        // And log it 
                                }
                                else {
                                    Console.WriteLine("[ERROR]      Couldnt delete old. " + FilePath + " would not delete");      // Otherwise, output an error
                                    LogInfo.Add("[ERROR]      Couldnt delete old. " + FilePath + " would not delete");        // And log it                               
                                }
                            }
                            // Otherwise
                            else
                            {
                                // Report an error
                                Console.WriteLine("[ERROR]      Copying Failed due to incorrect copy (MD5 Checksums do not match, they were " + MD5Checksum + " and " + NewMD5Checksum + ")");
                                LogInfo.Add("[ERROR]      Copying Failed due to incorrect copy (MD5 Checksums do not match" + MD5Checksum + " and " + NewMD5Checksum + ")");
                            }
                        }
                        // Otherwise
                        else
                        {
                            // Report an error
                            Console.WriteLine("[ERROR]      New file not found");
                            LogInfo.Add("[ERROR]      New file not found");
                        }
                    }
                }

                // Deletes empty directories
                string Extras = Pair[0] + " -type d -empty -delete -mindepth 1";
                Process.Start("find", Extras);
            }

            // Write to the log file
            LogFileObject = new FileStream("/app/LogFile.log", FileMode.Append, FileAccess.Write);       // Open the log file in append and write mode
            using (StreamWriter FileWriter = new StreamWriter(LogFileObject)) {     // Write to it with the information
                foreach (string Line in LogInfo) {      // Loops through each file to output
                    FileWriter.WriteLine(Line);     // Writes it to the file
                }
            }
        }
    }
}