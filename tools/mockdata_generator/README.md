# Mockdata Generator

Deterministic Python CLI that generates related JSON mockdata for every MongoDB
collection currently modeled in this repository.

## Requirements

- Python 3.11+
- Windows example runtime in this repo: `py -3`

## Generate a dataset

```powershell
py -3 -m mockdata_generator generate --profile medium --seed 42 --out .\output\medium --pretty
```

## Launch desktop UI

```powershell
py -3 -m mockdata_generator ui
```

Desktop UI includes:

- a `Run all` button
- one button per collection target
- profile and seed controls
- output folder picker
- execution log area

When a single collection button is used, the generator also creates its required
dependency data so the output stays internally consistent.

## Import generated JSON into MongoDB

This tool exports Mongo Extended JSON, so `mongoimport` can preserve `_id` and
date fields correctly.

```powershell
cd "E:\Clone Repo\ASP.NET-HV-Travel\tools\mockdata_generator"
.\import-medium.ps1 -DataDir .\output\medium -Database HV-Travel -MongoUri "mongodb://localhost:27017" -DropCollections
```

Notes:

- `-DropCollections` replaces each collection before import
- omit `-DropCollections` if you intentionally want append behavior
- requires `mongoimport` from MongoDB Database Tools in `PATH`

Optional overrides:

```powershell
py -3 -m mockdata_generator generate --profile small --out .\output\small --count Bookings=12 --count chatMessages=50
```

## Output

Each run writes:

- one JSON array per collection, for example `Users.json`, `Bookings.json`,
  `chatMessages.json`
- `manifest.json` with profile, seed, counts, mapping, file list, and integrity
  summary

Default output location is `tools/mockdata_generator/output/<profile>/`.

## Clean output directory

```powershell
py -3 -m mockdata_generator generate --profile small --clean-output
```

`--clean-output` only clears the selected output directory before rewriting it.

## Run tests

```powershell
py -3 -m unittest discover -s tests -t .
```
