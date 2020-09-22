# .NET Standard Logger
> Simplify your logging for all .NET projects with this library. Creates .txt and .db logs. SQL database can be helpful if you want to easily query your logs!

## Table of contents
* [General info](#general-info)
* [Setup](#setup)
* [Code Examples](#code-examples)
* [Status](#status)
* [Contact](#contact)

## General info
Logging is something that must be done on all projects. This makes logging painless with just a quick reference to Vdrio.Diagnostics. This package only relies on .NET Standard, so this works on all .NET platforms.


## Setup
For simplest setup, just reference Vdrio.Diagnostics and call Logger. Exception, Debug, Trace, Error, Warn, or UserInput and pass in the necessary parameters.
For more advanced setups, you can make your own BaseLogData or LogData object and pass it in to Logger<T> and do all the same functions you can do with the non-generic logger.

## Code Examples
Initialization Example:
```csharp
using Vdrio.Diagnostics;

//Initialize with no parameters to store in {LocalAppData}/VdrioLogger
Logger.Initialize();

//Initialize with type, must inherit abstract class BaseLogData or class LogData
Logger<T>.Initialize();

//Initialize with optional parameters
//string path, bool saveToSQLDatabase, TimeSpan createNewLogFileInterval, string archiveFolderPath, TimeSpan archiveInterval, TimeSpan deleteAfter
//path is where you will save your logs. saveToSQLDatabase is true by default, SQL Database takes up more space than text log, so you may want to disable
//createNewLogFileInterval is the interval at which a new log file will be made to split up your logs into files organized by time
//archiveFolderPath is where you want to put logs that are old, but you still want to keep, archiveInterval is the age that the file must be for archiving
//deleteAfter is infinite by default, but if you want to delete log files of a certain age, you can specify that time here
//This will initialize logging that splits up files in 12 hour windows and sends to archive folder at 30 days and delete after 90 days

Logger.Initialize(logPath, true, TimeSpan.FromHours(12), archivePath, TimeSpan.FromDays(30), TimeSpan.FromDays(90));


//Do the same generically

Logger<T>.Initialize(logPath, true, TimeSpan.FromHours(12), archivePath, TimeSpan.FromDays(30), TimeSpan.FromDays(90));

```



Logging examples (all can be done generically with Logger<T> instead of Logger):
```csharp

    //Log TraceType Start
    Logger.Trace(TraceType.Start, "Starting testing logger");

    //Log Warn with Warning Level
    Logger.Warn(WarningLevel.Severe, "Severe Warning Test");

    //Log Error with Error Level
    Logger.Error(ErrorLevel.Critical, "Critical Error Test");

    //Log Debug for plain message
    Logger.Debug("Just a debug message");

    //Log UserInput with UserInputType
    Logger.UserInput(UserInputType.Navigation, "User navigation test");

    //Call .Log() from exception to log an exception
    Exception ex = new Exception();
    ex.Log("Logged exception through extension!");

    //Above is same as:
    Logger.Exception(ex, "Logged exception normally!");

    //Extension for generic is called like this:
    ex.Log<T>("Logged generic type through extension!");

    //Log TraceType Complete
    Logger.Trace(TraceType.Complete, "Finished logging example");
```

Example Log Output:
```
[09/20/2020 12:34:31][Exception]:

Object reference not set to an instance of an object.
   at LoggerExample.Program.CauseInnerException() in C:\Users\lucas\source\repos\Logger\LoggerExample\Program.cs:line 61
   at LoggerExample.Program.CauseException() in C:\Users\lucas\source\repos\Logger\LoggerExample\Program.cs:line 45
======================================================================================================================================
[09/20/2020 12:34:31][Trace][InProgress]:
Testing loop caused exception
======================================================================================================================================
[09/20/2020 12:34:31][Error][Critical]:
Critical Error Test
======================================================================================================================================
[09/20/2020 12:34:31][Debug]:
Just a debug message
======================================================================================================================================
[09/20/2020 12:34:31][UserInput][Navigation]:
User navigation test
======================================================================================================================================
[09/20/2020 12:34:31][Exception]:
a8212a2e-f59b-4206-9413-908da24c5d4a
Exception of type 'System.Exception' was thrown.

======================================================================================================================================
[09/20/2020 12:34:31][Trace][Complete]:
Finished testing loop
======================================================================================================================================
[09/20/2020 12:34:32][Trace][Start]:
Started testing loop, iteration 70
======================================================================================================================================
```


Example BaseLogData implementation

```csharp
    public class MyCustomLogData : BaseLogData
    {
        public MyCustomLogData():base()
        {

        }
        public override void CreateDebugLogData(string message)
        {
            LogType = LogDataType.Debug;
            LongLogMessage = message + "\n===============================================================================\n";
        }

        public override void CreateErrorLogData(ErrorLevel level, string message)
        {
            LogType = LogDataType.Error;

            LongLogMessage = message + "\n===============================================================================\n";
        }

        public override void CreateExceptionLogData(Exception ex, string message)
        {
            LogType = LogDataType.Exception;

            LongLogMessage = message + "\n===============================================================================\n";
        }

        public override void CreateTraceLogData(TraceType type, string message)
        {
            LogType = LogDataType.Trace;

            LongLogMessage = message + "\n===============================================================================\n";
        }

        public override void CreateUserInputLogData(UserInputType type, string message)
        {
            LogType = LogDataType.UserInput;

            LongLogMessage = message + "\n===============================================================================\n";
        }

        public override void CreateWarnLogData(WarningLevel level, string message)
        {
            LogType = LogDataType.Warn;

            LongLogMessage = message + "\n===============================================================================\n";
        }
    }
    
```


This outputs a log something like:
```


Severe Warning Test
===============================================================================

Critical Error Test
===============================================================================

Just a debug message
===============================================================================

User navigation test
===============================================================================
```


## Status
Project is: _in progress_

## Contact
Created by [@Vdrio](lucasdglass@outlook.com) - feel free to contact me!
