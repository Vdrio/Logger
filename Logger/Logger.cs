using SQLite;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using SDebug = System.Diagnostics.Debug;

namespace Vdrio.Diagnostics
{
    public static class Logger
    {
        private static bool Initialized = false;
        private static object logMonitor = new object();
        public static SQLiteConnection Database { get; private set; }


        /// <summary>
        /// The main Initialize method. Instantiates the SQLite connection where LogData is stored
        /// </summary>
        /// <remarks>
        /// <para> Must be called before any logging is done.</para>
        /// </remarks>
        public static void Initialize()
        {
            try
            {
                if (!Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VdrioLogger")))
                {
                    Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VdrioLogger"));
                }
                Initialize(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VdrioLogger", "log.db"));
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
        /// </remarks>
        public static void Initialize(string dbPath)
        {
            try
            {
                Database = new SQLiteConnection(dbPath);
                Database.CreateTable(typeof(LogData));
                Initialized = true;
            }
            catch(Exception ex)
            {
                SDebug.WriteLine("Failed to initialize logger for path: " + dbPath + ":\n" + ex);
                throw ex;
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
                        data.Id = Database.CreateUniqueId();
                        Database.Insert(data);
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
                        data.Id = Database.CreateUniqueId();
                        Database.Insert(data);
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
                    IsBackground = true
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
                        data.Id = Database.CreateUniqueId();
                        Database.Insert(data);
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
                    IsBackground = true
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
                        data.Id = Database.CreateUniqueId();
                        Database.Insert(data);
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
                    IsBackground = true
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
                        data.Id = Database.CreateUniqueId();
                        Database.Insert(data);
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
                    IsBackground = true
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
                        data.Id = Database.CreateUniqueId();
                        Database.Insert(data);
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
                    IsBackground = true
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
                if (!db.TableMappings.Contains(new TableMapping(typeof(LogData))))
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

    }




    public static class Logger<T> where T : BaseLogData, new()
    {

        private static bool Initialized = false;
        private static object logMonitor = new object();

        public static SQLiteConnection Database { get; private set; }
        public static void Initialize(string dbPath)
        {
            try
            {
                Database = new SQLiteConnection(dbPath);
                Database.CreateTable(typeof(T));
                Initialized = true;
            }
            catch (Exception ex)
            {
                SDebug.WriteLine("Failed to initialize logger for path: " + dbPath + ":\n" + ex);
                throw ex;
            }
        }
        public static void Initialize()
        {
            try
            {
                Initialize(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "log.db"));
            }
            catch (Exception ex)
            {
                SDebug.WriteLine("Failed to initialize logger for path: " + Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "log.db") + ":\n" + ex);
                throw ex;
            }
        }

    }

   


}
