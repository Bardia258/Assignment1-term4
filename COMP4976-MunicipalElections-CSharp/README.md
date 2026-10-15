# Municipal Elections Management System

**Current status: Phase 3 — Authentication and Authorization implemented.**
See [the Phase 3 report](PHASE3.md) for administrator login, Development accounts,
authorization rules, changed files, verification results, and browser testing.
Run the existing project with `dotnet run --project MunicipalElections.Web --launch-profile https`,
then visit `/Admin`. The required classroom accounts are seeded only in Development.

The sections below retain the Phase 2 implementation record. Counts and statements
about zero administrator accounts/roles describe the database at the end of Phase 2;
Phase 3 now adds two administrators and four roles without changing that schema.

## Phase 2 implementation record

The existing .NET 10 MVC project now uses one EF Core SQLite `ApplicationDbContext`
for Identity and election data. This context is also intended for the future iOS API.
Phase 2 does not add CRUD screens, roles, administrator accounts, JWTs, API endpoints,
or a new public frontend.

## Run locally

From this directory (`COMP4976-MunicipalElections-CSharp`) in PowerShell:

```powershell
# On a fresh clone only; keep your existing local configuration if present.
Copy-Item MunicipalElections.Web/appsettings.Development.example.json MunicipalElections.Web/appsettings.Development.json
dotnet tool restore
dotnet restore
dotnet build

$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet ef migrations list --project MunicipalElections.Web
dotnet ef database update --project MunicipalElections.Web
dotnet run --project MunicipalElections.Web --launch-profile https
```

Open the HTTPS URL printed by the app (normally `https://localhost:7105`).
The default home page and the template's `/Identity/Account/Login` and
`/Identity/Account/Register` pages are retained. The HTTP launch profile also works:
`dotnet run --project MunicipalElections.Web --launch-profile http`.

The local configuration contains `Data Source=municipal-local.db`. Relative paths
are resolved against the web project's content root, so EF commands from either
the web or solution directory use the same database. The database stays outside
`wwwroot`, and every application connection enables foreign-key enforcement.
Configuration pointing inside `wwwroot` is rejected. If you already have another
Identity database, retain its filename in your local connection string and apply
the migrations to that database; do not change filenames or delete it to upgrade.

Development startup applies pending **migrations** and runs the transactional
seeder. `dotnet ef database update` applies schema changes; start the app to seed
the sample data. `EnsureCreated` is not used. Outside Development, startup neither
migrates nor seeds; production connection configuration is deferred to Phase 6.
`appsettings.json` retains logging and allowed-host settings and has no database
connection string. Set a production connection externally when implementing that phase.

## Database

| Table | Relationships and constraints |
| --- | --- |
| Municipalities | Parent of positions and candidates; unique nullable SeedKey |
| Positions | MunicipalityId → Municipalities (RESTRICT); SeatsAvailable > 0 CHECK; unique (PositionId, MunicipalityId); unique nullable SeedKey |
| Candidates | MunicipalityId → Municipalities (RESTRICT); (PositionId, MunicipalityId) → Positions (RESTRICT); unique nullable SeedKey |
| AspNetUsers | ApplicationUser retains Identity columns and adds nullable FullName and MunicipalityId; municipality deletion sets MunicipalityId to NULL |
| SavedCandidates | UserId → AspNetUsers and CandidateId → Candidates (both CASCADE); unique (UserId, CandidateId) |

Identity also retains `AspNetRoles`, `AspNetRoleClaims`, `AspNetUserClaims`,
`AspNetUserLogins`, `AspNetUserRoles`, and `AspNetUserTokens`.
`__EFMigrationsHistory` records applied migrations; EF Core SQLite also creates
`__EFMigrationsLock`. SQLite maintains its internal `sqlite_sequence` table.

Deleting a user or candidate removes that record's saved links. Deleting a position
or municipality with election data is restricted to avoid accidental loss. The
composite candidate FK prevents cross-municipality assignments at database level,
including direct SQL when foreign keys are enabled.

Migration history:

1. `00000000000000_CreateIdentitySchema` — original migration, unchanged.
2. `20261015194951_AddElectionModels` — new tables, indexes, constraints, and nullable user fields.

The new migration's forward operations do not remove application columns or data.
SQLite implements the new AspNetUsers foreign key through a table rebuild that
copies all existing columns before replacing the old table. Regression checks
verify preservation of existing password hashes, users, claims, logins, tokens,
and role associations through this upgrade.

## Development sample data

All candidates and biographies are fictional classroom examples. Municipality
election dates are sample dates (`2026-10-17`), not an authoritative election feed.
Websites and email addresses use `example.com`; optional photo and logo filenames
are null so the dataset does not point to nonexistent image assets.

| Municipality | Mayor seats | Councillor seats | School Trustee seats | Candidates |
| --- | ---: | ---: | ---: | ---: |
| Pitt Meadows, BC | 1 | 6 | 2 | 4 |
| Maple Ridge, BC | 1 | 6 | 5 | 4 |
| Burnaby, BC | 1 | 8 | 7 | 4 |
| Total | | | | 12 |

There are **3 municipalities, 9 positions, and 12 candidates**, with one mayor,
two councillor candidates, and one trustee candidate per municipality. Candidate
creation timestamps use UTC. No saved links, users, or roles are seeded.

The seeder checks stable, meaningful `SeedKey` values such as `demo:pitt-meadows`
and `demo:avery-brookfield`, never fixed primary keys. These nullable unique keys
allow ordinary records without a key and preserve sample records across edits to
names, profiles, and relationships. Seeding inserts missing samples in one
transaction and never updates or deletes existing rows. Restarting does not
overwrite administrator edits. Deleting a keyed sample makes it a missing sample,
which will be inserted again on the next Development startup.

## Guidance for later CRUD/API phases

- Validate model annotations on every write: required fields, string lengths,
  positive seat counts, email, and URL. MVC can use ModelState; EF SaveChanges
  does not automatically run Data Annotations validation, and SQLite does not
  enforce declared maximum string lengths.
- Filter candidate position choices by municipality, then verify on the server
  that the selected position exists in that municipality. Return a field error
  for invalid choices and handle FK conflicts caused by concurrent changes.
  The composite FK supplies an independent database guarantee.
- Use dedicated input/view models. Do not bind SeedKey, creation timestamps,
  user ownership, or navigation properties from public input. Preserve SeedKey
  during edits. Read the saved-candidate UserId from the authenticated user and
  handle a duplicate save gracefully; the unique index enforces the rule.
- Treat a position's municipality as fixed: it participates in its alternate key.
  Create a new position and explicitly reassign candidates when moving positions
  between municipalities. Do not attempt to change a tracked EF key in place.
- Continue using UserManager, SignInManager, and Identity password hashing.
  Confirmed-account sign-in remains enabled, as in Phase 1. The default template's
  development confirmation link was used for HTTP testing; production email
  delivery is not implemented in this phase.

## Repeat verification

```powershell
dotnet build
dotnet run --project MunicipalElections.DatabaseTests --no-build

# Optional: Python 3, standard library only; build Debug first.
python MunicipalElections.DatabaseTests/verify_app.py

$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet ef migrations list --project MunicipalElections.Web
dotnet ef database update --project MunicipalElections.Web
dotnet ef dbcontext info --project MunicipalElections.Web
dotnet ef migrations has-pending-model-changes --project MunicipalElections.Web
git diff --check
```

The database regression project is a small executable with no additional test
framework packages. It applies the real migrations to isolated in-memory SQLite,
prints each check, and exits nonzero on failure. Use `dotnet run` above to execute
it; `dotnet test` does not execute this custom runner.

The optional HTTP runner starts and stops its own application instances, uses a
new SQLite database under ignored `MunicipalElections.DatabaseTests/TestResults`,
and retains test databases and logs for inspection. Its temporary normal user is
created only in that isolated database. It never edits the development database.
No debugger or IDE extension is required for any command.

Verification completed on 2026-10-15:

- Restore and build succeeded with zero warnings and errors.
- Both migrations applied, including an idempotent database-update rerun.
- 29 database regression checks passed: migration upgrade preservation, all tables,
  relationships, constraints, annotations, seed reruns, edit preservation, and deletion behavior.
- 14 HTTP integration checks passed: home/login/register pages, registration,
  confirmation, wrong-password rejection, sign-in, profile, password hashing,
  restart without duplicates, and authentication-cookie survival.
- The actual development application returned HTTP 200 for home, login, and
  registration pages. Its database contains 3 municipalities, 9 positions,
  12 candidates, 0 saved links, 0 users, and 0 roles.
- The actual database's `PRAGMA integrity_check` returned `ok` and
  `PRAGMA foreign_key_check` returned no violations.
- EF tooling from the solution directory resolves the same project-root database
  and reports no pending model changes.

Initial sandbox runs could not access NuGet or Windows event logging/data-protection
services. Restoring dependencies and running the HTTP checks outside the sandbox
resolved those environment restrictions; the application did not need a workaround.

## Implementation command record

The following commands were executed during implementation (in the solution or
web directory as appropriate). This is a record, not a setup script: do not add
the migration or verification project a second time.

```powershell
dotnet --info
dotnet tool list --global
dotnet new tool-manifest
dotnet tool install dotnet-ef --version 10.0.12
dotnet tool restore
dotnet restore
dotnet build --no-restore
dotnet ef migrations list --no-build
dotnet ef migrations add AddElectionModels --no-build --output-dir Data/Migrations
dotnet ef database update
dotnet sln add MunicipalElections.DatabaseTests/MunicipalElections.DatabaseTests.csproj
dotnet build
dotnet run --project MunicipalElections.DatabaseTests --no-build
python MunicipalElections.DatabaseTests/verify_app.py
dotnet ef dbcontext info --project MunicipalElections.Web --no-build
dotnet ef migrations has-pending-model-changes --project MunicipalElections.Web --no-build
dotnet ef database update --project MunicipalElections.Web --no-build
dotnet ef migrations list --project MunicipalElections.Web --no-build
dotnet run --no-build --no-restore --no-launch-profile --urls http://127.0.0.1:5186
git diff --check
git status --short
```

The web launch and EF commands ran with `ASPNETCORE_ENVIRONMENT=Development`.
Python's standard-library SQLite driver also ran read-only table/count/relationship
queries and the integrity/foreign-key PRAGMAs against the development database.
PowerShell HTTP requests checked the actual development app's three public pages.

EF Core reference: [composite foreign and principal keys](https://learn.microsoft.com/en-us/ef/core/modeling/relationships/foreign-and-principal-keys)
and [applying migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying).

## Changed files

Created:

- `MunicipalElections.Web/Models/{Municipality,Position,Candidate,ApplicationUser,SavedCandidate}.cs`
- `MunicipalElections.Web/Data/{DatabaseConfiguration,DbSeeder}.cs`
- `MunicipalElections.Web/Data/Migrations/20261015194951_AddElectionModels.cs` and its `.Designer.cs`
- `MunicipalElections.Web/appsettings.Development.json` (local, ignored)
- `MunicipalElections.Web/appsettings.Development.example.json` (safe clone template)
- `MunicipalElections.DatabaseTests/{MunicipalElections.DatabaseTests.csproj,Program.cs,verify_app.py}`
- `dotnet-tools.json` (project-local dotnet-ef 10.0.12) and this README

Modified:

- Repository `.gitignore` (additional SQLite journal suffixes; existing local configuration/database exclusions retained)
- `MunicipalElections.slnx` (adds the verification project)
- `MunicipalElections.Web/Data/ApplicationDbContext.cs` and migration snapshot
- `MunicipalElections.Web/Program.cs` and `Views/Shared/_LoginPartial.cshtml`
- `MunicipalElections.Web/appsettings.json`
- `MunicipalElections.Web/MunicipalElections.Web.csproj` (removes template database copying to build output)

Local database files, test results, build output, and development configuration are
ignored by Git. Migration sources and the example configuration are included.
The original assignment PDF/DOCX files were left untouched. No changes were pushed.
