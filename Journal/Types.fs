﻿module Journal.Types

open System

// Journal Types

/// A ledger account. e.g. "Assets:Accounts:Savings"
type Account = string

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Account =

    /// Calculate full account lineage for a particular account. This will return
    /// a list of all parent accounts and the account itself.
    /// e.g. given "a:b:c", returns ["a"; "a:b"; "a:b:c"]
    let getAccountLineage (account: string) =
        /// Use with fold to get all combinations.
        let combinator (s: string list) (t: string) =
            if not s.IsEmpty then (s.Head + ":" + t) :: s else t :: s
        account.Split ':'
        |> Array.fold combinator []
        |> List.rev


type SymbolValue = string

/// A commodity symbol. e.g. "$", "AAPL", "MSFT"
type Symbol = {
    Value: SymbolValue;
    Quoted: bool;
}


[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Symbol =

    let create quoted symbol =
        {Value=symbol; Quoted=quoted}

    let render (symbol : Symbol) =
        match symbol.Quoted with
        | true -> "\"" + symbol.Value + "\""
        | _    -> symbol.Value



/// An amount may be provided or inferred in a transaction
type AmountSource = 
    | Provided
    | Inferred

/// How an amount is formatted when rendered or in the source file
type AmountFormat =
    | SymbolLeftWithSpace
    | SymbolLeftNoSpace
    | SymbolRightWithSpace
    | SymbolRightNoSpace

/// An amount is a quantity and an optional symbol.
type Amount = {
    Value: decimal;
    Symbol: Symbol;
    Format: AmountFormat;
}

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Amount =

    let create quantity symbol format =
        {Value = quantity; Symbol = symbol; Format = format;}

    let serialize (amount : Amount) =
        match amount.Format with
        | SymbolLeftWithSpace  -> (Symbol.render amount.Symbol) + " " + amount.Value.ToString()
        | SymbolLeftNoSpace    -> (Symbol.render amount.Symbol) + amount.Value.ToString()
        | SymbolRightWithSpace -> amount.Value.ToString() + " " + (Symbol.render amount.Symbol)
        | SymbolRightNoSpace   -> amount.Value.ToString() + (Symbol.render amount.Symbol)


/// Symbol price as of a certain date.
type SymbolPrice = {
    LineNumber: int64
    Date: System.DateTime;
    Symbol: Symbol;
    Price: Amount;
}

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SymbolPrice =

    let create lineNum date symbol price =
        {LineNumber = lineNum; Date = date; Symbol = symbol; Price = price;}

    let render (sp : SymbolPrice) : string =
        let dateFormat = "yyyy-MM-dd"
        sprintf "P %s %s %s" (sp.Date.ToString(dateFormat)) (Symbol.render sp.Symbol) (Amount.serialize sp.Price)


/// A symbol price collection keeps all historical prices for a symbol, plus some metadata.
type SymbolPriceCollection = {
    Symbol:    Symbol;
    FirstDate: System.DateTime;
    LastDate:  System.DateTime;
    Prices:    SymbolPrice list;
}

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SymbolPriceCollection =

    let fromList (prices : seq<SymbolPrice>) =
        let sortedPrices = 
            prices
            |> Seq.toList
            |> List.sortBy (fun sp -> sp.Date)
        let symbol = (List.head sortedPrices).Symbol
        let firstDate = (List.head sortedPrices).Date
        let lastDate = (List.nth sortedPrices <| ((List.length sortedPrices) - 1)).Date
        {Symbol = symbol; FirstDate = firstDate; LastDate = lastDate; Prices = sortedPrices;}

    let prettyPrint spc =
        let dateFormat = "yyyy-MM-dd"
        let printPrice (price : SymbolPrice) =
            do printfn "%s - %s" (price.Date.ToString(dateFormat)) (Amount.serialize price.Price)
        do printfn "Symbol:  %s" spc.Symbol.Value
        do printfn "First Date: %s" (spc.FirstDate.ToString(dateFormat))
        do printfn "Last Date:  %s" (spc.LastDate.ToString(dateFormat))
        do printfn "Price History:"
        List.iter printPrice spc.Prices


/// Symbol Price DB is a map of symbols to symbol price collections
type SymbolPriceDB = Map<SymbolValue, SymbolPriceCollection>

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SymbolPriceDB =

    let fromList (prices : list<SymbolPrice>) : SymbolPriceDB =
        prices
        |> Seq.groupBy (fun sp -> sp.Symbol.Value)
        |> Seq.map (fun (symbolValue, symbolPrices) -> symbolValue, SymbolPriceCollection.fromList symbolPrices)
        |> Map.ofSeq

    let prettyPrint (priceDB : SymbolPriceDB) =
        let dateFormat = "yyyy-MM-dd"
        let printSymbolPrices _ (spc : SymbolPriceCollection) =
            do printfn "----"
            do SymbolPriceCollection.prettyPrint spc
        priceDB
        |> Map.iter printSymbolPrices


type SymbolConfig = {
    Symbol: Symbol;
    GoogleFinanceSearchSymbol: string;
}

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SymbolConfig =

    let create symbol googleFinanceSymbol =
        {Symbol = symbol; GoogleFinanceSearchSymbol = googleFinanceSymbol}

    let render (config : SymbolConfig) : string =
        sprintf "SC %s %s" (Symbol.render config.Symbol) config.GoogleFinanceSearchSymbol


type SymbolConfigCollection = Map<SymbolValue, SymbolConfig>

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SymbolConfigCollection =
    
    let fromList symbolConfigs =
        symbolConfigs
        |> List.map (fun sc -> sc.Symbol.Value, sc)
        |> Map.ofList

    let prettyPrint (configs : SymbolConfigCollection) : unit =
        printfn "Symbol Configs:"
        configs
        |> Map.iter (fun sym config -> printfn "%s" <| SymbolConfig.render config)


type Code = string
type Payee = string
type Comment = string

/// Transaction status.
type Status =
    | Cleared
    | Uncleared

/// Transaction header.
type Header = {
    LineNumber: int64;
    Date: System.DateTime;
    Status: Status;
    Code: Code option;
    Payee: Payee;
    Comment: Comment option
}

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Header =

    let create lineNum date status code payee comment =
        {LineNumber=lineNum; Date=date; Status=status; Code=code; Payee=payee; Comment=comment}


/// Transaction posting.
type Posting = {
    LineNumber: int64;
    Header: Header;
    Account: string;
    AccountLineage: string list;
    Amount: Amount;
    AmountSource: AmountSource;
    Comment: string option;
}

/// Journal with all postings and accounts.
type Journal = {
    Postings: Posting list;
    MainAccounts: Set<string>;
    AllAccounts: Set<string>;
    PriceDB: SymbolPriceDB;
    DownloadedPriceDB : SymbolPriceDB;
}

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Journal = 
    
    /// Given a list of journal postings, returns a Journal record
    let create postings priceDB downloadedPriceDB =
        let mainAccounts = Set.ofList <| List.map (fun (posting : Posting) -> posting.Account) postings
        let allAccounts = Set.ofList <| List.collect (fun posting -> posting.AccountLineage) postings
        {
            Postings = postings;
            MainAccounts = mainAccounts;
            AllAccounts = allAccounts;
            PriceDB = priceDB;
            DownloadedPriceDB = downloadedPriceDB;
        }


// Symbol Usage Types
    
/// Symbol Usage record.
type SymbolUsage = {
    Symbol: Symbol;
    FirstAppeared: DateTime;
    ZeroBalanceDate: DateTime option;
}