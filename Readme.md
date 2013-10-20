Webledger
=========

Webledger is web frontend for a ledger journal file. The ledger journal file is
based on the command line [Ledger] [1] journal file format and features double-entry 
accounting for personal finance tracking.


Objective
---------

Short-term, the focus is on what ledger cannot do right now: tables and charts.

Medium-term, I may deviate from the ledger file format to my own file format,
so that I can handle investments better.

Long-term, the idea is to replace the command line ledger with my own tool that
does all reporting via a web interface. Editing will still be done by text file,
though in the long-long term, perhaps a front end for adding/editing 
transactions would be a possibility.


Dependencies
------------

*	FParsec 1.0.1
*	FsUnit.xUnit.1.2.1.2
*	xunit 1.9.1
*	xunit.runners 1.9.1



How to Run
----------

*	Setup LEDGER_FILE environment variable to point to your ledger file



Command Bar Supported Commands
------------------------------
[NOTE: Not implemented yet]

Commands:

	balance [accounts-to-include] [parameters]

	register [accounts-to-include] [parameters]

Parameters:

	:excluding [accounts-to-exclude]

	:period [this month|last month]

	:since [yyyy/mm/dd]

	:upto [yyyy/mm/dd]

	:title [report title]




Implementation Notes
--------------------

Investments & Commodities:
*	I'm basically ignoring these for the moment. The parser will parse them,
but all processing after that point assumes one commodity and basically assumes
only the "amount" field is used. I'll need to revisit this once I get around
to adding investment/commodity support.



Phase 1 Implementation (Reporting)
----------------------

### Objective

*	Replace the ledger bal and reg commandline options with a web interface.
*	Provide some basic reporting like net worth, income vs expenses, ...
*	See http://bugsplat.info/static/stan-demo-report.html for some examples

### Main Tasks

Parsing Ledger File
- [] Read/Parse ledger file
- [] Autobalance transactions
- [] Ensure transactions balance

Initial Static Balance Reports:
- [] Assets vs Liabilities, ie Net Worth
- [] Income Statement (current & previous month)
- [] Net Worth chart

Dynamic Website:
- [] Convert all existing reports to render dynamically instead of a static page
	- [] Get barebones nancy working
	- [] map /, /balancesheet, /currentincomestatement, /previousincomestatement to current pages
	- [] fix html/css (use proper elements ie h1, ul, etc...)
	- [] start using bootstrap css
	- [] turn into "one page" app that takes GET parameters for what to show (with command bar)
	- [] watch ledger file and reload on change
		1) On initial load, note last modified time of file
		2) On every request, compare last modified time of file to noted time
			If newer, (attempt to) reload journal

Register Report
- [] Register report with parameters (ie accounts, date range)
	- [] build register report generator function
	- [] create register report template
	- [] link up to command bar
	- [] link to from balance reports
- [] Sorting:
	- [] Preserve file order for transactions and entries within transactions but output in reverse so most recent is on top
		- Need to do sorting at the end so that running total makes sense
- [] Accounts Payable vs Accounts Receivable
	- Dynamically list non-zero accounts with balance in navlist. Link to register report

All Reports
- [] Refactoring/clean up of all reports

Command Bar Enhancements
- [] Clean up and improve date/period parsing
	Additions for period: yyyy, last year, this year
- [] Generate "networth" chart from the command bar
- [] Autocomplete hints (bootstrap typeahead)

Charts
- [] Income Statement chart (monthly, over time)

Nav
- [] Configurable nav list
- [] Combine reports and payables / receivables into one dict?
- [] Default report?

Expenses
- [] Average in last 3 months, in last year
- [] Burn rate - using last 3 months expenses average, how long until savings is gone?
- [] Top Expenses over last period

Documentation
- [] github wiki
	- [] how to use / setup


Phase 2 Implementation (Commodities)
----------------------

Commodity Prices
[] Update to handle commodities
[] (While continuing to use ledger file format) Detect investment transactions and merge transaction lines
[] Identify commodities from ledger file
[] Fetch prices from internet and add to cache
	[] Store commodity prices in a local cache
	[] Prices should go from first date in ledger file to today

Net Worth
[] Update chart with book value line and actual line

Balance Sheet
[] Update Net Worth sheet with actual vs book value columns

Portfolio
[] Overall portfolio return and per investment
[] Expected T3s/T5s to receive for last year (ie had distribution)
[] Rebalancing calculator - for rebalancing investments to proper allocation



Phase 3 Ideas (Entry)
-------------

- Replace bal and reg functions from ledger command line
- Move away from ledger file format
	- Define my own that handles investments better
- Entry/editing of transactions


[1]: http://www.ledger-cli.org/			"Ledger command-line accounting system"