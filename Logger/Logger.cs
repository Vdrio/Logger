using SQLite;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace Vdrio.Diagnostics
{
    public static class Logger
    {
        private static bool Initialized = false;
        private static object logMonitor = new object();
        public static SQLiteConnection Database { get; private set; }
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
                Debug.WriteLine("Failed to initialize logger for path: " + Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VdrioLogger", "log.db") + ":\n" + ex);
                throw ex;
            }
        }
        
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
                Debug.WriteLine("Failed to initialize logger for path: " + dbPath + ":\n" + ex);
                throw ex;
            }
        }


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
                        Debug.WriteLine(ex.Message);
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
                Debug.WriteLine(ex.Message);
            }
        }


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
                Debug.WriteLine(ex.Message);
            }
        }

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
                        Debug.WriteLine(ex.Message);
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
                Debug.WriteLine(x.Message);
            }
        }
        
        public static void Exception(Exception ex, string formatString, params string[] parameters)
        {
            try
            {
                Exception(ex, string.Format(formatString, parameters));
            }
            catch (Exception x)
            {
                Debug.WriteLine(x.Message);
            }
        }

        public static void Log(this Exception ex, string message = "")
        {
            try
            {
                Exception(ex, message);
            }
            catch(Exception x)
            {
                Debug.WriteLine(x.Message);
            }
        }
        public static void Log(this Exception ex, string formatString, params string[] parameters)
        {
            try
            {
                Exception(ex, string.Format(formatString, parameters));
            }
            catch(Exception x)
            {
                Debug.WriteLine(x.Message);
            }
        }

        public static string CreateUniqueId(this SQLiteConnection db)
        {
            try
            {
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
                Debug.WriteLine(ex.Message);
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
                Debug.WriteLine("Failed to initialize logger for path: " + dbPath + ":\n" + ex);
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
                Debug.WriteLine("Failed to initialize logger for path: " + Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "log.db") + ":\n" + ex);
                throw ex;
            }
        }

    }

   


}
