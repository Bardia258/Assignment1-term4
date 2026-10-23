# API Endpoints

The C# API is hosted at [the Azure server](https://municipal-elections-d8ffazd0dfevdpc6.westus3-01.azurewebsites.net/). Every path below is relative to that address.

Requests and responses use JSON. Saved candidate requests require being logged in.

## Endpoint list

| Method | Path | Purpose |
| --- | --- | --- |
| POST | `/api/auth/register` | Create an account. |
| POST | `/api/auth/login` | Sign in with a username or email and password. |
| GET | `/api/municipalities` | Load all municipalities. |
| GET | `/api/municipalities/{id}` | Load one municipality by ID. |
| GET | `/api/positions` | Load positions, optionally filtered by `municipalityId`. |
| GET | `/api/positions/{id}` | Load one position by ID. |
| GET | `/api/candidates` | Load all candidates. |
| GET | `/api/candidates?municipalityId={id}` | Load candidates for one municipality. |
| GET | `/api/candidates/{id}` | Load one candidate by ID. |
| GET | `/api/candidates/search?name={name}` | Search candidates by name. |
| GET | `/api/savedcandidates` | Load the current user's voting guide. |
| POST | `/api/savedcandidates` | Add a candidate to the current user's guide. |
| DELETE | `/api/savedcandidates/{id}` | Remove a saved record belonging to the current user. |

## Backend source

- [Directory endpoints and response fields](../../../COMP4976-MunicipalElections-CSharp/MunicipalElections.Web/Controllers/Api/DirectoryController.cs)
- [Registration and login](../../../COMP4976-MunicipalElections-CSharp/MunicipalElections.Web/Controllers/Api/AuthController.cs)
- [Saved candidates](../../../COMP4976-MunicipalElections-CSharp/MunicipalElections.Web/Controllers/Api/SavedCandidatesController.cs)
