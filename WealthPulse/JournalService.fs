﻿namespace WealthPulse

open Journal
open Journal.Types
open Journal.SymbolPrices
open WealthPulse.Utility
open System
open System.IO
open System.Threading

module JournalService =

    /// Interface for Nancy Dependency Injection
    type IJournalService =
        abstract member Journal : Journal
        abstract member OutstandingPayees : (string * decimal) list
        abstract member JournalLastModified : DateTime
        abstract member GetAndClearException : string option
        abstract member SymbolPriceDB : SymbolPriceDB

    /// Implementation of IJournalService for Nancy Dependency Injection
    type JournalService() =
        //let ledgerFilePath = Environment.GetEnvironmentVariable("LEDGER_FILE")
        let ledgerFilePath = @"C:\Users\Mark\Nexus\Documents\finances\ledger\test_investments_penny.dat"
        let configFilePath = Environment.GetEnvironmentVariable("WEALTH_PULSE_CONFIG_FILE")
        let pricesFilePath = Environment.GetEnvironmentVariable("WEALTH_PULSE_PRICES_FILE")
        let rwlock = new ReaderWriterLock()

        let mutable journal = Journal.create List.empty
        let mutable outstandingPayees = List.empty
        let mutable journalLastModified = DateTime.MinValue
        let mutable exceptionMessage = None
        let configEnabled = configFilePath <> null && File.Exists(configFilePath)
        let mutable symbolConfig = Map.empty : SymbolConfigs //loadSymbolConfig configFilePath
        let mutable configLastModified = DateTime.MinValue
        let pricesEnabled = pricesFilePath <> null
        let mutable symbolPriceDB = Map.empty : SymbolPriceDB //loadSymbolPriceDB pricesFilePath
        let mutable symbolPricesLastFetched = DateTime.Now.AddDays(-1.0).AddSeconds(20.0)//DateTime.Now.AddDays(-1.0).AddMinutes(10.0) // delay first fetch by 10 minutes
        

        let loadJournal () =
            let lastModified = File.GetLastWriteTime(ledgerFilePath)
            do printfn "Parsing ledger file: %s" ledgerFilePath
            try
                let (entries, parseTime) = time <| fun () -> Parser.parseJournalFile ledgerFilePath Text.Encoding.ASCII
                do printfn "Parsed ledger file in %A seconds." parseTime.TotalSeconds
                do printfn "Transactions parsed: %d" <| List.length entries
                do printfn "Ledger last modified: %s" <| lastModified.ToString()
                rwlock.AcquireWriterLock(Timeout.Infinite)
                try
                    journal <- Journal.create entries
                    outstandingPayees <- Query.outstandingPayees journal
                    journalLastModified <- lastModified
                    exceptionMessage <- None
                finally
                    rwlock.ReleaseWriterLock()
            with
                ex -> 
                    do printfn "Error parsing ledger: %s" ex.Message
                    rwlock.AcquireWriterLock(Timeout.Infinite)
                    try
                        journalLastModified <- lastModified
                        exceptionMessage <- Some ex.Message
                    finally
                        rwlock.ReleaseWriterLock()


        let loadConfig () =
            if configEnabled then
                let lastModified = File.GetLastWriteTime(configFilePath)
                do printfn "Parsing config file: %s" configFilePath
                try
                    let (config, parseTime) = time <| fun () -> loadSymbolConfig configFilePath
                    do printfn "Parsed config file in %A seconds." parseTime.TotalSeconds
                    do printfn "Config last modified: %s" <| lastModified.ToString()
                    rwlock.AcquireWriterLock(Timeout.Infinite)
                    try
                        symbolConfig <- config
                        configLastModified <- lastModified
                    finally
                        rwlock.ReleaseWriterLock()
                with
                    ex -> 
                        do printfn "Error parsing ledger: %s" ex.Message
                        rwlock.AcquireWriterLock(Timeout.Infinite)
                        try
                            configLastModified <- lastModified
                        finally
                            rwlock.ReleaseWriterLock()


        let loadSymbolPriceDB () =
            if pricesEnabled then
                do printfn "Parsing prices file: %s" pricesFilePath
                try
                    let (priceDB, parseTime) = time <| fun () -> loadSymbolPriceDB pricesFilePath
                    do printfn "Parsed prices file in %A seconds." parseTime.TotalSeconds
                    rwlock.AcquireWriterLock(Timeout.Infinite)
                    try
                        symbolPriceDB <- priceDB
                    finally
                        rwlock.ReleaseWriterLock()
                with
                    ex -> 
                        do printfn "Error parsing prices: %s" ex.Message

        
        let fetchSymbolPrices () =
            if pricesEnabled then
                do printfn "Fetching new symbol prices..."
                try
                    let symbolUsage = Query.identifySymbolUsage journal
                    let priceDB = updateSymbolPriceDB symbolUsage symbolConfig symbolPriceDB
                    do printfn "Storing prices to: %s" pricesFilePath
                    do saveSymbolPriceDB pricesFilePath priceDB
                    rwlock.AcquireWriterLock(Timeout.Infinite)
                    try
                        symbolPriceDB <- priceDB
                        symbolPricesLastFetched <- DateTime.Now
                    finally
                        rwlock.ReleaseWriterLock()
                with
                    ex -> 
                        do printfn "Error fetching new symbol prices: %s" ex.Message
                        rwlock.AcquireWriterLock(Timeout.Infinite)
                        try
                            symbolPricesLastFetched <- DateTime.Now
                        finally
                            rwlock.ReleaseWriterLock()
        

        let backgroundTasks () =
            while true do
                // reload journal and config files when modified
                // fetch symbol prices once a day
                if File.GetLastWriteTime(ledgerFilePath) > journalLastModified then loadJournal ()
                if configEnabled && File.GetLastWriteTime(configFilePath) > configLastModified then loadConfig ()
                if pricesEnabled && (System.DateTime.Now - symbolPricesLastFetched).TotalDays >= 1.0 then fetchSymbolPrices ()
                do Thread.Sleep(5000)


        // Initialization

        do loadJournal ()
        do loadConfig ()
        do loadSymbolPriceDB ()
        let watchThread = new Thread(ThreadStart(backgroundTasks))
        do watchThread.Start()

        

        interface IJournalService with
            member this.Journal = 
                rwlock.AcquireReaderLock(Timeout.Infinite)
                try
                    journal
                finally
                    rwlock.ReleaseReaderLock()

            member this.OutstandingPayees = 
                rwlock.AcquireReaderLock(Timeout.Infinite)
                try
                    outstandingPayees
                finally
                    rwlock.ReleaseReaderLock()

            member this.JournalLastModified = 
                rwlock.AcquireReaderLock(Timeout.Infinite)
                try
                    journalLastModified
                finally
                    rwlock.ReleaseReaderLock()

            member this.GetAndClearException =
                rwlock.AcquireWriterLock(Timeout.Infinite)
                try
                    let msg = exceptionMessage
                    exceptionMessage <- None
                    msg
                finally
                    rwlock.ReleaseWriterLock()

            member this.SymbolPriceDB = 
                rwlock.AcquireReaderLock(Timeout.Infinite)
                try
                    symbolPriceDB
                finally
                    rwlock.ReleaseReaderLock()