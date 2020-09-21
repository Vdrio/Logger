using SQLite;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using SDebug = System.Diagnostics.Debug;
using System.Globalization;

namespace Vdrio.Diagnostics
{
    public static class Logger
    {
        public static bool Initialized { get; private set; } = false;
        private static object logMonitor = new object();
        public static SQLiteConnection Database { get; private set; }

        public static StreamWriter TextLog { get; private set; }

        private static bool SaveToSQLDatabase { get; set; }

        public static string CurrentTextLogPath { get; private set; }
        public static string CurrentBaseLogPath { get; private set; }
        public static string CurrentArchiveFolderPath { get; private set; }

        private static Timer CreateNewLogFileTimer { get; set; }
        private static Timer DeleteAfterTimer { get; set; }

        private static TimeSpan CreateNewLogFileInterval { get; set; }
        private static TimeSpan ArchiveInterval { get; set; }
        private static TimeSpan DeleteAfter { get; set; }

        public static event LoggerEventArgs AnyLogAdded;
        public static event LoggerEventArgs ExceptionLogAdded;
        public static event LoggerEventArgs DebugLogAdded;
        public static event LoggerEventArgs WarnLogAdded;
        public static event LoggerEventArgs ErrorLogAdded;
        public static event LoggerEventArgs UserInputLogAdded;
        public static event LoggerEventArgs TraceLogAdded;

        public static event LogFileCompletedEventArgs LogFileCompleted;
        public static event LogFileArchivedEventArgs LogFileArchived;
        


        /// <summary>
        /// The main Initialize method. Instantiates the SQLite connection where LogData is stored
        /// </summary>
        /// <remarks>
        /// <para> Must be called before any logging is done.</para>
        /// <para> Default values for TimeSpans are 12 hours, 30 days, Infinite respectively</para>
        /// </remarks>
        
        public static void Initialize(bool saveToSQLDatabase = true, TimeSpan? createNewLogFileInterval = null, string archiveFolderPath = null, TimeSpan? archiveInterval = null, TimeSpan? deleteAfter = null)
        {
            try
            {
                //Assign default values that were unable to be compile time constants
                CreateNewLogFileInterval = createNewLogFileInterval ?? TimeSpan.FromHours(12);
                DeleteAfter = deleteAfter ?? Timeout.InfiniteTimeSpan;
                ArchiveInterval = archiveInterval ?? TimeSpan.FromDays(30);
                CurrentArchiveFolderPath = archiveFolderPath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VdrioLogger", "Archive");
                SaveToSQLDatabase = saveToSQLDatabase;


                if (!Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VdrioLogger")))
                {
                    Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VdrioLogger"));
                }
                Initialize(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VdrioLogger", "log.db"), saveToSQLDatabase, CreateNewLogFileInterval, CurrentArchiveFolderPath , archiveInterval, DeleteAfter);
            }
            catch(Exception ex)
            {
                SDebug.WriteLine("Failed to initialize logger for path: " + Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VdrioLogger", "log.db") + ":\n" + ex);
                throw ex;
            }
        }


        /// <summary>
        /// The Initialize method that accepts an alternate path for the database. Instantiates the SQLite connection where LogData is stored in the specified path
        /// </summary>
        /// <remarks>
        /// <para> Must be called before any logging is done.</para>
        /// <para> Default values for TimeSpans are 12 hours, 30 days, Infinite respectively</para>
        /// <para> File path will include start date time appended at end of file, format: {logFileName}MMddyyyy_HHMM.db</para>
        /// <para> Path will ignore file type, ie .txt, .db. This will make a .txt file and optional .db file for log storage</para>
        /// </remarks>
        public static void Initialize(string path, bool saveToSQLDatabase = true, TimeSpan? createNewLogFileInterval = null, string archiveFolderPath = null, TimeSpan? archiveInterval = null, TimeSpan? deleteAfter = null)
        {
            try
            {
                //Initialize could get called multiple times, want to make sure this doesn't get added more than once
                AnyLogAdded -= Logger_AnyLogAdded;
                AnyLogAdded += Logger_AnyLogAdded;

                //Assign default values that were unable to be compile time constants
                CreateNewLogFileInterval = createNewLogFileInterval ?? TimeSpan.FromHours(12);
                DeleteAfter = deleteAfter ?? Timeout.InfiniteTimeSpan;
                ArchiveInterval = archiveInterval ?? TimeSpan.FromDays(30);
                CurrentArchiveFolderPath = archiveFolderPath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VdrioLogger", "Archive");

                if (ArchiveInterval <= CreateNewLogFileInterval || (DeleteAfter != Timeout.InfiniteTimeSpan && DeleteAfter <= ArchiveInterval))
                {
                    throw new InvalidOperationException("Archive interval must be greater than CreateNewLogFileInterval and DeleteAfter must be greater than archive interval");
                }

                SaveToSQLDatabase = saveToSQLDatabase;

                string[] pathSplit = path.Split('.');
                TimeSpan timeOfDay = DateTime.Now - DateTime.Today;
                DateTime startTime = DateTime.Today;
                if (pathSplit?.Length == 2 || !path.Contains("."))
                {
                    if (timeOfDay > createNewLogFileInterval)
                    {
                        int currentIntervalCount = (int)(timeOfDay.Ticks / ((TimeSpan)createNewLogFileInterval).Ticks);
                        startTime += TimeSpan.FromTicks(((TimeSpan)createNewLogFileInterval).Ticks * currentIntervalCount);
                    }
                    if (!path.Contains("."))
                    {
                        CurrentBaseLogPath = path;
                        path += startTime.ToString("MMddyyyy_HHmmss") + ".db";
                    }
                    else
                    {
                        CurrentBaseLogPath = pathSplit[0];
                        pathSplit[0] += startTime.ToString("MMddyyyy_HHmmss") + ".db";
                        path = pathSplit[0];
                    }
                }
                else
                {
                    throw new InvalidOperationException("Provided path is invalid");
                }
                CurrentTextLogPath = path.Replace(".db", ".txt");
                if (!File.Exists(CurrentTextLogPath))
                {
                    TextLog = File.CreateText(CurrentTextLogPath);
                    TextLog.Close();
                    TextLog.Dispose();
                }
                if (!Directory.Exists(CurrentArchiveFolderPath))
                {
                    try
                    {
                        Directory.CreateDirectory(CurrentArchiveFolderPath);
                    }
                    catch(Exception x)
                    {
                        SDebug.WriteLine(x);
                        throw x;
                    }
                }
                CreateNewLogFileTimer?.Dispose();
                CreateNewLogFileTimer = new Timer(new TimerCallback(NewLogFileIntervalHit), path, (TimeSpan)(startTime + CreateNewLogFileInterval - DateTime.Now), (TimeSpan)CreateNewLogFileInterval);

                if (SaveToSQLDatabase)
                {
                    Database?.Dispose();
                    Database = new SQLiteConnection(path);
                    Database.CreateTable(typeof(LogData));
                }
                Initialized = true;
            }
            catch(Exception ex)
            {
                SDebug.WriteLine("Failed to initialize logger for path: " + path + ":\n" + ex);
                throw ex;
            }
        }

        public static void NewLogFileIntervalHit(object state)
        {
            string path = CurrentBaseLogPath;
            string[] pathSplit = path.Split('.');
            TimeSpan timeOfDay = DateTime.Now - DateTime.Today;
            DateTime startTime = DateTime.Today;
            if (pathSplit?.Length == 2 || !path.Contains("."))
            {
                if (timeOfDay > CreateNewLogFileInterval)
                {
                    int currentIntervalCount = (int)(timeOfDay.Ticks / ((TimeSpan)CreateNewLogFileInterval).Ticks);
                    startTime += TimeSpan.FromTicks(((TimeSpan)CreateNewLogFileInterval).Ticks * currentIntervalCount);
                }
                if (!path.Contains("."))
                {
                    path += startTime.ToString("MMddyyyy_HHmmss") + ".db";
                }
                else
                {
                    pathSplit[0] += startTime.ToString("MMddyyyy_HHmmss") + ".db";
                    path = pathSplit[0];
                }
                string oldFilePath = CurrentTextLogPath;
                CurrentTextLogPath = path.Replace(".db", ".txt");
                if (!File.Exists(CurrentTextLogPath))
                {
                    TextLog = File.CreateText(CurrentTextLogPath);
                    TextLog.Close();
                    TextLog.Dispose();
                }
                if (SaveToSQLDatabase)
                {
                    Database?.Dispose();
                    Database = new SQLiteConnection(path);
                    Database.CreateTable(typeof(LogData));
                }
                DeleteOldFiles();
                ArchiveOldFiles();
                LogFileCompleted?.Invoke(oldFilePath);
            }
            else
            {
                throw new InvalidOperationException("Provided path is invalid");
            }
        }

        public static void DeleteOldFiles()
        {
            string path = CurrentArchiveFolderPath;
            foreach(var file in Directory.GetFiles(path, "*.*"))
            {
                string dateTimeString = file.Split('\\')?.Last().Replace(CurrentBaseLogPath.Split('\\')?.Last(), "").Split('.').First();

                if (DateTime.TryParseExact(dateTimeString, "MMddyyyy_HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out DateTime time))
                {
                    if (DateTime.Now - time - CreateNewLogFileInterval >= DeleteAfter)
                    {
                        try
                        {
                            Monitor.Enter(logMonitor);
                            File.Delete(file);
                        }
                        catch(Exception x)
                        {
                            SDebug.WriteLine(x);
                        }
                        finally
                        {
                            Monitor.Exit(logMonitor);
                        }
                    }
                }
            }
            
        }
        
        public static void ArchiveOldFiles()
        {
            string[] path = CurrentBaseLogPath.Split('\\');
            path[path.Length - 1] = "";
            foreach(var file in Directory.GetFiles(string.Join("\\", path), "*.*"))
            {
                string dateTimeString = file.Split('\\')?.Last().Replace(CurrentBaseLogPath.Split('\\')?.Last(), "").Split('.').First();

                if (DateTime.TryParseExact(dateTimeString, "MMddyyyy_HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out DateTime time))
                {
                    if (DateTime.Now - time - CreateNewLogFileInterval >= ArchiveInterval)
                    {
                        string fileName = file.Split('\\').Last();
                        try
                        {
                            Monitor.Enter(logMonitor);
                            File.Move(file, Path.Combine(CurrentArchiveFolderPath, fileName));
                        }
                        catch (Exception x)
                        {
                            SDebug.WriteLine(x);
                        }
                        finally
                        {
                            Monitor.Exit(logMonitor);
                        }
                        LogFileArchived?.Invoke(Path.Combine(CurrentArchiveFolderPath, fileName));
                    }
                }
            }
            
        }

        private static void WriteToTextFile(LogData data)
        {
            using (TextLog = new StreamWriter(CurrentTextLogPath, true))
            {
                TextLog.AutoFlush = true;
                TextLog.WriteLine(data.LongLogMessage);
                TextLog.Close();
            }
        }

        private static void Logger_AnyLogAdded(BaseLogData logData)
        {
            switch (logData.LogType)
            {
                case LogDataType.Error: 
                    ErrorLogAdded?.Invoke(logData);
                    break;
                case LogDataType.Exception: 
                    ExceptionLogAdded?.Invoke(logData);
                    break;
                case LogDataType.Debug: 
                    DebugLogAdded?.Invoke(logData);
                    break;
                case LogDataType.Warn: 
                    WarnLogAdded?.Invoke(logData);
                    break;
                case LogDataType.Trace:
                    TraceLogAdded?.Invoke(logData);
                    break;
                case LogDataType.UserInput:
                    UserInputLogAdded?.Invoke(logData);
                    break;
            }
        }


        /// <summary>
        /// The main Trace method. Creates a log of type Trace and inserts it into the database
        /// </summary>
        /// <remarks>
        /// <para> Forces the logging to happen on a background thread holding the logMonitor object.</para>
        /// </remarks>
        public static void Trace(TraceType type, string message)
        {
            try
            {
                if (!Initialized)
                {
                    Initialize();
                }
                Thread t = new Thread(new ThreadStart(delegate
                {
                    Monitor.Enter(logMonitor);
                    try
                    {
                        LogData data = new LogData(LogDataType.Trace, type, message);

                        WriteToTextFile(data);

                        AnyLogAdded?.Invoke(data);
                        if (SaveToSQLDatabase)
                        {
                            data.Id = Database.CreateUniqueId();
                            Database.Insert(data);
                        }
                    }
                    catch (Exception ex)
                    {
                        SDebug.WriteLine(ex.Message);
                    }
                    finally
                    {
                        Monitor.Exit(logMonitor);
                    }
                }))
                {
                    IsBackground = true, Priority= ThreadPriority.Lowest
                };
                t.Start();
            }
            catch (Exception ex)
            {
                SDebug.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// The alternate Trace method that allows the user to pass in a format string and parameters. Formats the string and calls the main Trace method
        /// </summary>
        public static void Trace(TraceType type, string formatString, params string[] parameters)
        {
            if (!Initialized)
            {
                Initialize();
            }
            try
            {
                string message = string.Format(formatString, parameters);
                Trace(type, message);
            }
            catch (Exception ex)
            {
                SDebug.WriteLine(ex.Message);
            }
        }


        /// <summary>
        /// The main Exception method. Creates a log of type Exception and inserts it into the database
        /// </summary>
        /// <remarks>
        /// <para> Forces the logging to happen on a background thread holding the logMonitor object.</para>
        /// </remarks>
        public static void Exception(Exception ex, string message)
        {
            try
            {
                if (!Initialized)
                {
                    Initialize();
                }
                Thread t = new Thread(new ThreadStart(delegate
                {
                    Monitor.Enter(logMonitor);
                    try
                    {
                        LogData data = new LogData(LogDataType.Exception, ex, message);

                        WriteToTextFile(data);
                        AnyLogAdded?.Invoke(data);
                        if (SaveToSQLDatabase)
                        {
                            data.Id = Database.CreateUniqueId();
                            Database.Insert(data);
                        }
                    }
                    catch (Exception x)
                    {
                        SDebug.WriteLine(x.Message);
                    }
                    finally
                    {
                        Monitor.Exit(logMonitor);
                    }
                }))
                {
                    IsBackground = true,
                    Priority = ThreadPriority.Lowest
                };
                t.Start();
            }
            catch (Exception x)
            {
                SDebug.WriteLine(x.Message);
            }
        }

        /// <summary>
        /// The alternate Exception method that allows the user to pass in a format string and parameters. Formats the string and calls the main Exception method
        /// </summary>
        public static void Exception(Exception ex, string formatString, params string[] parameters)
        {
            try
            {
                Exception(ex, string.Format(formatString, parameters));
            }
            catch (Exception x)
            {
                SDebug.WriteLine(x.Message);
            }
        }

        /// <summary>
        /// The main Debug method. Creates a log of type Debug and inserts it into the database
        /// </summary>
        /// <remarks>
        /// <para> Forces the logging to happen on a background thread holding the logMonitor object.</para>
        /// </remarks>
        public static void Debug(string message)
        {
            try
            {
                if (!Initialized)
                {
                    Initialize();
                }
                Thread t = new Thread(new ThreadStart(delegate
                {
                    Monitor.Enter(logMonitor);
                    try
                    {
                        LogData data = new LogData(LogDataType.Debug, message);

                        WriteToTextFile(data);
                        AnyLogAdded?.Invoke(data);
                        if (SaveToSQLDatabase)
                        {
                            data.Id = Database.CreateUniqueId();
                            Database.Insert(data);
                        }
                    }
                    catch (Exception x)
                    {
                        SDebug.WriteLine(x.Message);
                    }
                    finally
                    {
                        Monitor.Exit(logMonitor);
                    }
                }))
                {
                    IsBackground = true,
                    Priority = ThreadPriority.Lowest
                };
                t.Start();
            }
            catch (Exception x)
            {
                SDebug.WriteLine(x.Message);
            }
        }

        /// <summary>
        /// The alternate Debug method that allows the user to pass in a format string and parameters. Formats the string and calls the main Exception method
        /// </summary>
        public static void Debug(string formatString, params string[] parameters)
        {
            try
            {
                Debug(string.Format(formatString, parameters));
            }
            catch (Exception x)
            {
                SDebug.WriteLine(x.Message);
            }
        }

        /// <summary>
        /// The main Warn method. Creates a log of type Warn and inserts it into the database
        /// </summary>
        /// <remarks>
        /// <para> Forces the logging to happen on a background thread holding the logMonitor object.</para>
        /// </remarks>
        public static void Warn(WarningLevel level, string message)
        {
            try
            {
                if (!Initialized)
                {
                    Initialize();
                }
                Thread t = new Thread(new ThreadStart(delegate
                {
                    Monitor.Enter(logMonitor);
                    try
                    {
                        LogData data = new LogData(LogDataType.Warn, level, message);

                        WriteToTextFile(data);
                        AnyLogAdded?.Invoke(data);
                        if (SaveToSQLDatabase)
                        {
                            data.Id = Database.CreateUniqueId();
                            Database.Insert(data);
                        }
                    }
                    catch (Exception x)
                    {
                        SDebug.WriteLine(x.Message);
                    }
                    finally
                    {
                        Monitor.Exit(logMonitor);
                    }
                }))
                {
                    IsBackground = true,
                    Priority = ThreadPriority.Lowest
                };
                t.Start();
            }
            catch (Exception x)
            {
                SDebug.WriteLine(x.Message);
            }
        }

        /// <summary>
        /// The alternate Warn method that allows the user to pass in a format string and parameters. Formats the string and calls the main Exception method
        /// </summary>
        public static void Warn(WarningLevel level, string formatString, params string[] parameters)
        {
            try
            {
                Warn(level, string.Format(formatString, parameters));
            }
            catch (Exception x)
            {
                SDebug.WriteLine(x.Message);
            }
        }

        /// <summary>
        /// The main Error method. Creates a log of type Error and inserts it into the database
        /// </summary>
        /// <remarks>
        /// <para> Forces the logging to happen on a background thread holding the logMonitor object.</para>
        /// </remarks>
        public static void Error(ErrorLevel level, string message)
        {
            try
            {
                if (!Initialized)
                {
                    Initialize();
                }
                Thread t = new Thread(new ThreadStart(delegate
                {
                    Monitor.Enter(logMonitor);
                    try
                    {
                        LogData data = new LogData(LogDataType.Error, level, message);

                        WriteToTextFile(data);

                        AnyLogAdded?.Invoke(data);
                        if (SaveToSQLDatabase)
                        {
                            data.Id = Database.CreateUniqueId();
                            Database.Insert(data);
                        }
                    }
                    catch (Exception x)
                    {
                        SDebug.WriteLine(x.Message);
                    }
                    finally
                    {
                        Monitor.Exit(logMonitor);
                    }
                }))
                {
                    IsBackground = true,
                    Priority = ThreadPriority.Lowest
                };
                t.Start();
            }
            catch (Exception x)
            {
                SDebug.WriteLine(x.Message);
            }
        }

        /// <summary>
        /// The alternate Error method that allows the user to pass in a format string and parameters. Formats the string and calls the main Exception method
        /// </summary>
        public static void Error(ErrorLevel level, string formatString, params string[] parameters)
        {
            try
            {
                Error(level, string.Format(formatString, parameters));
            }
            catch (Exception x)
            {
                SDebug.WriteLine(x.Message);
            }
        }

        /// <summary>
        /// The main UserInput method. Creates a log of type UserInput and inserts it into the database
        /// </summary>
        /// <remarks>
        /// <para> Forces the logging to happen on a background thread holding the logMonitor object.</para>
        /// </remarks>
        public static void UserInput(UserInputType type, string message)
        {
            try
            {
                if (!Initialized)
                {
                    Initialize();
                }
                Thread t = new Thread(new ThreadStart(delegate
                {
                    Monitor.Enter(logMonitor);
                    try
                    {
                        LogData data = new LogData(LogDataType.UserInput, type, message);

                        WriteToTextFile(data);
                        AnyLogAdded?.Invoke(data);
                        if (SaveToSQLDatabase)
                        {
                            data.Id = Database.CreateUniqueId();
                            Database.Insert(data);
                        }
                    }
                    catch (Exception x)
                    {
                        SDebug.WriteLine(x.Message);
                    }
                    finally
                    {
                        Monitor.Exit(logMonitor);
                    }
                }))
                {
                    IsBackground = true,
                    Priority = ThreadPriority.Lowest
                };
                t.Start();
            }
            catch (Exception x)
            {
                SDebug.WriteLine(x.Message);
            }
        }

        /// <summary>
        /// The alternate UserInput method that allows the user to pass in a format string and parameters. Formats the string and calls the main Exception method
        /// </summary>
        public static void UserInput(UserInputType type, string formatString, params string[] parameters)
        {
            try
            {
                UserInput(type, string.Format(formatString, parameters));
            }
            catch (Exception x)
            {
                SDebug.WriteLine(x.Message);
            }
        }

        /// <summary>
        /// Extension method that allows users to log by calling [Exception].Log(), optional message parameter. Calls the main Exception method
        /// </summary>
        public static void Log(this Exception ex, string message = "")
        {
            try
            {
                Exception(ex, message);
            }
            catch(Exception x)
            {
                SDebug.WriteLine(x.Message);
            }
        }

        /// <summary>
        /// Extension method that allows users to log by calling [Exception].Log(formatString, parameters). Calls the main Exception method
        /// </summary>
        public static void Log(this Exception ex, string formatString, params string[] parameters)
        {
            try
            {

                Exception(ex, string.Format(formatString, parameters));
            }
            catch(Exception x)
            {
                SDebug.WriteLine(x.Message);
            }
        }


        /// <summary>
        /// Extension method that allows users to log by calling [Exception].Log(), optional message parameter. Calls the main Exception method
        /// </summary>
        public static void Log<T>(this Exception ex, string message = "") where T : BaseLogData,new()
        {
            try
            {
                if (!Logger<T>.Initialized)
                {
                    Logger<T>.Initialize();
                }
                Logger<T>.Exception(ex, message);
            }
            catch (Exception x)
            {
                SDebug.WriteLine(x.Message);
            }
        }

        /// <summary>
        /// Extension method that allows users to log by calling [Exception].Log(formatString, parameters). Calls the main Exception method
        /// </summary>
        public static void Log<T>(this Exception ex, string formatString, params string[] parameters) where T : BaseLogData, new()
        {
            try
            {
                if (!Logger<T>.Initialized)
                {
                    Logger<T>.Initialize();
                }
                Logger<T>.Exception(ex, string.Format(formatString, parameters));
            }
            catch (Exception x)
            {
                SDebug.WriteLine(x.Message);
            }
        }

        /// <summary>
        /// Extension method on SQLLite connection that will create a new guid and make sure it is unique in the database
        /// </summary>
        /// <remarks>
        /// <para> Database must have a table of LogData</para>
        /// </remarks>
        internal static string CreateUniqueId(this SQLiteConnection db)
        {
            try
            {
                if (!Initialized)
                {
                    Initialize();
                }
                if (db.TableMappings.FirstOrDefault(x=>x.MappedType == typeof(LogData)) == null)
                {
                    throw new NotImplementedException("Database must have a table of type LogData");
                }
                bool isUnique = false;
                while (!isUnique)
                {
                    string guid = Guid.NewGuid().ToString();
                    if (db.Table<LogData>().FirstOrDefault(x => x.Id == guid) == null)
                    {
                        return guid;
                    }
                }
            }
            catch (Exception ex)
            {
                SDebug.WriteLine(ex.Message);
                throw new NotImplementedException("A class that implements BaseLogData must also include a parameterless constructor");
            }
            return null;
        }

        internal static string CreateUniqueId<T>(this SQLiteConnection db) where T : BaseLogData, new()
        {
            try
            {
                if (!Logger<T>.Initialized)
                {
                    Logger<T>.Initialize();
                }
                TableMapping myTableMapping = Logger<T>.Database.TableMappings.FirstOrDefault(x => x.MappedType == typeof(T));
                if (Logger<T>.Database.TableMappings.FirstOrDefault(x => x.MappedType == typeof(T)) == null)
                {
                    throw new NotImplementedException("Database must have a table of type " + typeof(T).ToString());
                }
                bool isUnique = false;
                while (!isUnique)
                {
                    string guid = Guid.NewGuid().ToString();
                    if (db.Table<T>().FirstOrDefault(x => x.Id == guid) == null)
                    {
                        isUnique = true;
                        return guid;
                    }
                }
            }
            catch (Exception ex)
            {
                SDebug.WriteLine(ex.Message);
                throw new NotImplementedException("A class that implements BaseLogData must also include a parameterless constructor");
            }
            return null;
        }
    }


    public static class Logger<T> where T : BaseLogData, new()
    {

        public static bool Initialized { get; private set; } = false;
        private static object logMonitor = new object();
        public static SQLiteConnection Database { get; private set; }

        public static StreamWriter TextLog { get; private set; }

        private static bool SaveToSQLDatabase { get; set; }

        public static string CurrentTextLogPath { get; private set; }
        public static string CurrentBaseLogPath { get; private set; }
        public static string CurrentArchiveFolderPath { get; private set; }

        private static Timer CreateNewLogFileTimer { get; set; }
        private static Timer DeleteAfterTimer { get; set; }

        private static TimeSpan CreateNewLogFileInterval { get; set; }
        private static TimeSpan ArchiveInterval { get; set; }
        private static TimeSpan DeleteAfter { get; set; }

        public static event LoggerEventArgs AnyLogAdded;
        public static event LoggerEventArgs ExceptionLogAdded;
        public static event LoggerEventArgs DebugLogAdded;
        public static event LoggerEventArgs WarnLogAdded;
        public static event LoggerEventArgs ErrorLogAdded;
        public static event LoggerEventArgs UserInputLogAdded;
        public static event LoggerEventArgs TraceLogAdded;

        public static event LogFileCompletedEventArgs LogFileCompleted;
        public static event LogFileArchivedEventArgs LogFileArchived;



        /// <summary>
        /// The main Initialize method. Instantiates the SQLite connection where LogData is stored
        /// </summary>
        /// <remarks>
        /// <para> Must be called before any logging is done.</para>
        /// <para> Default values for TimeSpans are 12 hours, 30 days, Infinite respectively</para>
        /// </remarks>

        public static void Initialize(bool saveToSQLDatabase = true, TimeSpan? createNewLogFileInterval = null, string archiveFolderPath = null, TimeSpan? archiveInterval = null, TimeSpan? deleteAfter = null)
        {
            try
            {
                //Assign default values that were unable to be compile time constants
                CreateNewLogFileInterval = createNewLogFileInterval ?? TimeSpan.FromHours(12);
                DeleteAfter = deleteAfter ?? Timeout.InfiniteTimeSpan;
                ArchiveInterval = archiveInterval ?? TimeSpan.FromDays(30);
                CurrentArchiveFolderPath = archiveFolderPath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VdrioLogger", "Archive");
                SaveToSQLDatabase = saveToSQLDatabase;


                if (!Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VdrioLogger")))
                {
                    Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VdrioLogger"));
                }
                Initialize(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VdrioLogger", "log.db"), saveToSQLDatabase, CreateNewLogFileInterval, CurrentArchiveFolderPath, archiveInterval, DeleteAfter);
            }
            catch (Exception ex)
            {
                SDebug.WriteLine("Failed to initialize logger for path: " + Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VdrioLogger", "log.db") + ":\n" + ex);
                throw ex;
            }
        }


        /// <summary>
        /// The Initialize method that accepts an alternate path for the database. Instantiates the SQLite connection where LogData is stored in the specified path
        /// </summary>
        /// <remarks>
        /// <para> Must be called before any logging is done.</para>
        /// <para> Default values for TimeSpans are 12 hours, 30 days, Infinite respectively</para>
        /// <para> File path will include start date time appended at end of file, format: {logFileName}MMddyyyy_HHMM.db</para>
        /// <para> Path will ignore file type, ie .txt, .db. This will make a .txt file and optional .db file for log storage</para>
        /// </remarks>
        public static void Initialize(string path, bool saveToSQLDatabase = true, TimeSpan? createNewLogFileInterval = null, string archiveFolderPath = null, TimeSpan? archiveInterval = null, TimeSpan? deleteAfter = null)
        {
            try
            {
                //Initialize could get called multiple times, want to make sure this doesn't get added more than once
                AnyLogAdded -= Logger_AnyLogAdded;
                AnyLogAdded += Logger_AnyLogAdded;

                //Assign default values that were unable to be compile time constants
                CreateNewLogFileInterval = createNewLogFileInterval ?? TimeSpan.FromHours(12);
                DeleteAfter = deleteAfter ?? Timeout.InfiniteTimeSpan;
                ArchiveInterval = archiveInterval ?? TimeSpan.FromDays(30);
                CurrentArchiveFolderPath = archiveFolderPath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VdrioLogger", "Archive");

                if (ArchiveInterval <= CreateNewLogFileInterval || (DeleteAfter != Timeout.InfiniteTimeSpan && DeleteAfter <= ArchiveInterval))
                {
                    throw new InvalidOperationException("Archive interval must be greater than CreateNewLogFileInterval and DeleteAfter must be greater than archive interval");
                }

                SaveToSQLDatabase = saveToSQLDatabase;

                string[] pathSplit = path.Split('.');
                TimeSpan timeOfDay = DateTime.Now - DateTime.Today;
                DateTime startTime = DateTime.Today;
                if (pathSplit?.Length == 2 || !path.Contains("."))
                {
                    if (timeOfDay > createNewLogFileInterval)
                    {
                        int currentIntervalCount = (int)(timeOfDay.Ticks / ((TimeSpan)createNewLogFileInterval).Ticks);
                        startTime += TimeSpan.FromTicks(((TimeSpan)createNewLogFileInterval).Ticks * currentIntervalCount);
                    }
                    if (!path.Contains("."))
                    {
                        CurrentBaseLogPath = path;
                        path += startTime.ToString("MMddyyyy_HHmmss") + ".db";
                    }
                    else
                    {
                        CurrentBaseLogPath = pathSplit[0];
                        pathSplit[0] += startTime.ToString("MMddyyyy_HHmmss") + ".db";
                        path = pathSplit[0];
                    }
                }
                else
                {
                    throw new InvalidOperationException("Provided path is invalid");
                }
                CurrentTextLogPath = path.Replace(".db", ".txt");
                if (!File.Exists(CurrentTextLogPath))
                {
                    TextLog = File.CreateText(CurrentTextLogPath);
                    TextLog.Close();
                    TextLog.Dispose();
                }
                if (!Directory.Exists(CurrentArchiveFolderPath))
                {
                    try
                    {
                        Directory.CreateDirectory(CurrentArchiveFolderPath);
                    }
                    catch (Exception x)
                    {
                        SDebug.WriteLine(x);
                        throw x;
                    }
                }
                CreateNewLogFileTimer?.Dispose();
                CreateNewLogFileTimer = new Timer(new TimerCallback(NewLogFileIntervalHit), path, (TimeSpan)(startTime + CreateNewLogFileInterval - DateTime.Now), (TimeSpan)CreateNewLogFileInterval);

                if (SaveToSQLDatabase)
                {
                    Database?.Dispose();
                    Database = new SQLiteConnection(path);
                    Database.CreateTable(typeof(T));
                }
                Initialized = true;
            }
            catch (Exception ex)
            {
                SDebug.WriteLine("Failed to initialize logger for path: " + path + ":\n" + ex);
                throw ex;
            }
        }

        public static void NewLogFileIntervalHit(object state)
        {
            string path = CurrentBaseLogPath;
            string[] pathSplit = path.Split('.');
            TimeSpan timeOfDay = DateTime.Now - DateTime.Today;
            DateTime startTime = DateTime.Today;
            if (pathSplit?.Length == 2 || !path.Contains("."))
            {
                if (timeOfDay > CreateNewLogFileInterval)
                {
                    int currentIntervalCount = (int)(timeOfDay.Ticks / ((TimeSpan)CreateNewLogFileInterval).Ticks);
                    startTime += TimeSpan.FromTicks(((TimeSpan)CreateNewLogFileInterval).Ticks * currentIntervalCount);
                }
                if (!path.Contains("."))
                {
                    path += startTime.ToString("MMddyyyy_HHmmss") + ".db";
                }
                else
                {
                    pathSplit[0] += startTime.ToString("MMddyyyy_HHmmss") + ".db";
                    path = pathSplit[0];
                }
                string oldFilePath = CurrentTextLogPath;
                CurrentTextLogPath = path.Replace(".db", ".txt");
                if (!File.Exists(CurrentTextLogPath))
                {
                    TextLog = File.CreateText(CurrentTextLogPath);
                    TextLog.Close();
                    TextLog.Dispose();
                }
                if (SaveToSQLDatabase)
                {
                    Database?.Dispose();
                    Database = new SQLiteConnection(path);
                    Database.CreateTable(typeof(T));
                }
                DeleteOldFiles();
                ArchiveOldFiles();
                LogFileCompleted?.Invoke(oldFilePath);
            }
            else
            {
                throw new InvalidOperationException("Provided path is invalid");
            }
        }

        public static void DeleteOldFiles()
        {
            string path = CurrentArchiveFolderPath;
            foreach (var file in Directory.GetFiles(path, "*.*"))
            {
                string dateTimeString = file.Split('\\')?.Last().Replace(CurrentBaseLogPath.Split('\\')?.Last(), "").Split('.').First();

                if (DateTime.TryParseExact(dateTimeString, "MMddyyyy_HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out DateTime time))
                {
                    if (DateTime.Now - time - CreateNewLogFileInterval >= DeleteAfter)
                    {
                        try
                        {
                            Monitor.Enter(logMonitor);
                            File.Delete(file);
                        }
                        catch (Exception x)
                        {
                            SDebug.WriteLine(x);
                        }
                        finally
                        {
                            Monitor.Exit(logMonitor);
                        }
                    }
                }
            }

        }

        public static void ArchiveOldFiles()
        {
            string[] path = CurrentBaseLogPath.Split('\\');
            path[path.Length - 1] = "";
            foreach (var file in Directory.GetFiles(string.Join("\\", path), "*.*"))
            {
                string dateTimeString = file.Split('\\')?.Last().Replace(CurrentBaseLogPath.Split('\\')?.Last(), "").Split('.').First();

                if (DateTime.TryParseExact(dateTimeString, "MMddyyyy_HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out DateTime time))
                {
                    if (DateTime.Now - time - CreateNewLogFileInterval >= ArchiveInterval)
                    {
                        string fileName = file.Split('\\').Last();
                        try
                        {
                            Monitor.Enter(logMonitor);
                            File.Move(file, Path.Combine(CurrentArchiveFolderPath, fileName));
                        }
                        catch (Exception x)
                        {
                            SDebug.WriteLine(x);
                        }
                        finally
                        {
                            Monitor.Exit(logMonitor);
                        }
                        LogFileArchived?.Invoke(Path.Combine(CurrentArchiveFolderPath, fileName));
                    }
                }
            }

        }

        private static void WriteToTextFile(T data)
        {
            using (TextLog = new StreamWriter(CurrentTextLogPath, true))
            {
                TextLog.AutoFlush = true;
                TextLog.WriteLine(data.LongLogMessage);
                TextLog.Close();
            }
        }

        private static void Logger_AnyLogAdded(BaseLogData logData)
        {
            switch (logData.LogType)
            {
                case LogDataType.Error:
                    ErrorLogAdded?.Invoke(logData);
                    break;
                case LogDataType.Exception:
                    ExceptionLogAdded?.Invoke(logData);
                    break;
                case LogDataType.Debug:
                    DebugLogAdded?.Invoke(logData);
                    break;
                case LogDataType.Warn:
                    WarnLogAdded?.Invoke(logData);
                    break;
                case LogDataType.Trace:
                    TraceLogAdded?.Invoke(logData);
                    break;
                case LogDataType.UserInput:
                    UserInputLogAdded?.Invoke(logData);
                    break;
            }
        }


        /// <summary>
        /// The main Trace method. Creates a log of type Trace and inserts it into the database
        /// </summary>
        /// <remarks>
        /// <para> Forces the logging to happen on a background thread holding the logMonitor object.</para>
        /// </remarks>
        public static void Trace(TraceType type, string message)
        {
            try
            {
                if (!Initialized)
                {
                    Initialize();
                }
                Thread t = new Thread(new ThreadStart(delegate
                {
                    Monitor.Enter(logMonitor);
                    try
                    {
                        T data = new T();
                        data.CreateTraceLogData(type, message);
                        WriteToTextFile(data);

                        AnyLogAdded?.Invoke(data);
                        if (SaveToSQLDatabase)
                        {
                            data.Id = Database.CreateUniqueId<T>();
                            Database.Insert(data);
                        }
                    }
                    catch (Exception ex)
                    {
                        SDebug.WriteLine(ex.Message);
                    }
                    finally
                    {
                        Monitor.Exit(logMonitor);
                    }
                }))
                {
                    IsBackground = true,
                    Priority = ThreadPriority.Lowest
                };
                t.Start();
            }
            catch (Exception ex)
            {
                SDebug.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// The alternate Trace method that allows the user to pass in a format string and parameters. Formats the string and calls the main Trace method
        /// </summary>
        public static void Trace(TraceType type, string formatString, params string[] parameters)
        {
            if (!Initialized)
            {
                Initialize();
            }
            try
            {
                string message = string.Format(formatString, parameters);
                Trace(type, message);
            }
            catch (Exception ex)
            {
                SDebug.WriteLine(ex.Message);
            }
        }


        /// <summary>
        /// The main Exception method. Creates a log of type Exception and inserts it into the database
        /// </summary>
        /// <remarks>
        /// <para> Forces the logging to happen on a background thread holding the logMonitor object.</para>
        /// </remarks>
        public static void Exception(Exception ex, string message)
        {
            try
            {
                if (!Initialized)
                {
                    Initialize();
                }
                Thread t = new Thread(new ThreadStart(delegate
                {
                    Monitor.Enter(logMonitor);
                    try
                    {
                        T data = new T();
                        data.CreateExceptionLogData(ex, message);

                        WriteToTextFile(data);
                        AnyLogAdded?.Invoke(data);
                        if (SaveToSQLDatabase)
                        {
                            data.Id = Database.CreateUniqueId<T>();
                            Database.Insert(data);
                        }
                    }
                    catch (Exception x)
                    {
                        SDebug.WriteLine(x.Message);
                    }
                    finally
                    {
                        Monitor.Exit(logMonitor);
                    }
                }))
                {
                    IsBackground = true,
                    Priority = ThreadPriority.Lowest
                };
                t.Start();
            }
            catch (Exception x)
            {
                SDebug.WriteLine(x.Message);
            }
        }

        /// <summary>
        /// The alternate Exception method that allows the user to pass in a format string and parameters. Formats the string and calls the main Exception method
        /// </summary>
        public static void Exception(Exception ex, string formatString, params string[] parameters)
        {
            try
            {
                Exception(ex, string.Format(formatString, parameters));
            }
            catch (Exception x)
            {
                SDebug.WriteLine(x.Message);
            }
        }

        /// <summary>
        /// The main Debug method. Creates a log of type Debug and inserts it into the database
        /// </summary>
        /// <remarks>
        /// <para> Forces the logging to happen on a background thread holding the logMonitor object.</para>
        /// </remarks>
        public static void Debug(string message)
        {
            try
            {
                if (!Initialized)
                {
                    Initialize();
                }
                Thread t = new Thread(new ThreadStart(delegate
                {
                    Monitor.Enter(logMonitor);
                    try
                    {
                        T data = new T();
                        data.CreateDebugLogData(message);

                        WriteToTextFile(data);
                        AnyLogAdded?.Invoke(data);
                        if (SaveToSQLDatabase)
                        {
                            data.Id = Database.CreateUniqueId<T>();
                            Database.Insert(data);
                        }
                    }
                    catch (Exception x)
                    {
                        SDebug.WriteLine(x.Message);
                    }
                    finally
                    {
                        Monitor.Exit(logMonitor);
                    }
                }))
                {
                    IsBackground = true,
                    Priority = ThreadPriority.Lowest
                };
                t.Start();
            }
            catch (Exception x)
            {
                SDebug.WriteLine(x.Message);
            }
        }

        /// <summary>
        /// The alternate Debug method that allows the user to pass in a format string and parameters. Formats the string and calls the main Exception method
        /// </summary>
        public static void Debug(string formatString, params string[] parameters)
        {
            try
            {
                Debug(string.Format(formatString, parameters));
            }
            catch (Exception x)
            {
                SDebug.WriteLine(x.Message);
            }
        }

        /// <summary>
        /// The main Warn method. Creates a log of type Warn and inserts it into the database
        /// </summary>
        /// <remarks>
        /// <para> Forces the logging to happen on a background thread holding the logMonitor object.</para>
        /// </remarks>
        public static void Warn(WarningLevel level, string message)
        {
            try
            {
                if (!Initialized)
                {
                    Initialize();
                }
                Thread t = new Thread(new ThreadStart(delegate
                {
                    Monitor.Enter(logMonitor);
                    try
                    {
                        T data = new T();
                        data.CreateWarnLogData(level, message);

                        WriteToTextFile(data);
                        AnyLogAdded?.Invoke(data);
                        if (SaveToSQLDatabase)
                        {
                            data.Id = Database.CreateUniqueId<T>();
                            Database.Insert(data);
                        }
                    }
                    catch (Exception x)
                    {
                        SDebug.WriteLine(x.Message);
                    }
                    finally
                    {
                        Monitor.Exit(logMonitor);
                    }
                }))
                {
                    IsBackground = true,
                    Priority = ThreadPriority.Lowest
                };
                t.Start();
            }
            catch (Exception x)
            {
                SDebug.WriteLine(x.Message);
            }
        }

        /// <summary>
        /// The alternate Warn method that allows the user to pass in a format string and parameters. Formats the string and calls the main Exception method
        /// </summary>
        public static void Warn(WarningLevel level, string formatString, params string[] parameters)
        {
            try
            {
                Warn(level, string.Format(formatString, parameters));
            }
            catch (Exception x)
            {
                SDebug.WriteLine(x.Message);
            }
        }

        /// <summary>
        /// The main Error method. Creates a log of type Error and inserts it into the database
        /// </summary>
        /// <remarks>
        /// <para> Forces the logging to happen on a background thread holding the logMonitor object.</para>
        /// </remarks>
        public static void Error(ErrorLevel level, string message)
        {
            try
            {
                if (!Initialized)
                {
                    Initialize();
                }
                Thread t = new Thread(new ThreadStart(delegate
                {
                    Monitor.Enter(logMonitor);
                    try
                    {
                        T data = new T();
                        data.CreateErrorLogData(level, message);
                        WriteToTextFile(data);

                        AnyLogAdded?.Invoke(data);
                        if (SaveToSQLDatabase)
                        {
                            data.Id = Database.CreateUniqueId<T>();
                            Database.Insert(data);
                        }
                    }
                    catch (Exception x)
                    {
                        SDebug.WriteLine(x.Message);
                    }
                    finally
                    {
                        Monitor.Exit(logMonitor);
                    }
                }))
                {
                    IsBackground = true,
                    Priority = ThreadPriority.Lowest
                };
                t.Start();
            }
            catch (Exception x)
            {
                SDebug.WriteLine(x.Message);
            }
        }

        /// <summary>
        /// The alternate Error method that allows the user to pass in a format string and parameters. Formats the string and calls the main Exception method
        /// </summary>
        public static void Error(ErrorLevel level, string formatString, params string[] parameters)
        {
            try
            {
                Error(level, string.Format(formatString, parameters));
            }
            catch (Exception x)
            {
                SDebug.WriteLine(x.Message);
            }
        }

        /// <summary>
        /// The main UserInput method. Creates a log of type UserInput and inserts it into the database
        /// </summary>
        /// <remarks>
        /// <para> Forces the logging to happen on a background thread holding the logMonitor object.</para>
        /// </remarks>
        public static void UserInput(UserInputType type, string message)
        {
            try
            {
                if (!Initialized)
                {
                    Initialize();
                }
                Thread t = new Thread(new ThreadStart(delegate
                {
                    Monitor.Enter(logMonitor);
                    try
                    {
                        T data = new T();
                        data.CreateUserInputLogData(type, message);
                        WriteToTextFile(data);
                        AnyLogAdded?.Invoke(data);
                        if (SaveToSQLDatabase)
                        {
                            data.Id = Database.CreateUniqueId<T>();
                            Database.Insert(data);
                        }
                    }
                    catch (Exception x)
                    {
                        SDebug.WriteLine(x.Message);
                    }
                    finally
                    {
                        Monitor.Exit(logMonitor);
                    }
                }))
                {
                    IsBackground = true,
                    Priority = ThreadPriority.Lowest
                };
                t.Start();
            }
            catch (Exception x)
            {
                SDebug.WriteLine(x.Message);
            }
        }

        /// <summary>
        /// The alternate UserInput method that allows the user to pass in a format string and parameters. Formats the string and calls the main Exception method
        /// </summary>
        public static void UserInput(UserInputType type, string formatString, params string[] parameters)
        {
            try
            {
                UserInput(type, string.Format(formatString, parameters));
            }
            catch (Exception x)
            {
                SDebug.WriteLine(x.Message);
            }
        }


        /// <summary>
        /// Extension method on SQLLite connection that will create a new guid and make sure it is unique in the database
        /// </summary>
        /// <remarks>
        /// <para> Database must have a table of LogData</para>
        /// </remarks>
        

    }

   


}
