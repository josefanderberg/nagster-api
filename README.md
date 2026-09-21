# Tjat – API

REST-API i ASP.NET WebAPI med SQLite. Används av [webbappen](https://github.com/josefanderberg/nagster-web).

## Krav

- [.NET SDK 10](https://dotnet.microsoft.com/download/dotnet/10.0)

## Starta

```bash
git clone https://github.com/josefanderberg/nagster-api.git
cd nagster-api
dotnet run
```

API:et startar på `http://localhost:5080`. Databasen (`nagster.db`) skapas automatiskt.

## Endpoints

| Metod | URL | Beskrivning |
|---|---|---|
| GET | `/api/tasks` | Lista uppgifter |
| POST | `/api/tasks` | Skapa uppgift |
| PUT | `/api/tasks/{id}` | Uppdatera uppgift |
| POST | `/api/tasks/{id}/file` | Ladda upp fil till en uppgift (`multipart/form-data`) |

## Tekniska val

- **SQLite + EF Core** – databasen är en fil som skapas automatiskt, ingen installation behövs
- **CORS** öppnat för webbappens adress (`http://localhost:5173`)
- **Säker uppladdning** – vitlista på filtyper, kontroll av filens innehåll och storleksgräns på 10 MB
