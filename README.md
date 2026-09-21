# Tjat – API

Backend för [Tjat](https://github.com/josefanderberg/nagster-web), en anti-prokrastineringsapp med rösttjat. Ett REST-API byggt med ASP.NET WebAPI och SQLite. Både webbappen och (senare i kursen) mobilappen hämtar sin data härifrån.

## Starta

Kräver [.NET SDK](https://dotnet.microsoft.com/download) 8 eller senare.

```bash
git clone https://github.com/josefanderberg/nagster-api.git
cd nagster-api
dotnet run
```

API:et startar på `http://localhost:5080`. SQLite-databasen (`nagster.db`) skapas automatiskt vid första starten.

## Endpoints

| Metod | URL | Beskrivning |
|---|---|---|
| GET | `/api/tasks` | Lista alla uppgifter |
| POST | `/api/tasks` | Skapa en uppgift |
| PUT | `/api/tasks/{id}` | Uppdatera titel, status eller snooze-räknare |
| POST | `/api/tasks/{id}/file` | Ladda upp fil (ljud/bild) till en uppgift, `multipart/form-data` |
| GET | `/uploads/{filnamn}` | Hämta en uppladdad fil |

## Tekniska val

- **SQLite**: hela databasen är en fil som skapas automatiskt – noll konfiguration för den som klonar repot.
- **EF Core**: etablerad ORM som ger datamodellen på ett ställe och slipper handskriven SQL.
- **CORS** är konfigurerat för webbappens dev-adress (`http://localhost:5173`) så klienten kan anropa API:et från en annan port.
- **Uppladdningar kontrolleras i flera steg**: vitlista på filändelser, att angiven Content-Type stämmer, och att filens första bytes ("magic bytes") matchar formatet. Filändelser går att ljuga om, innehållet gör det inte. Storleksgräns 10 MB.
- **Filer sparas på disk, bara sökvägen i databasen**: enklare än blobbar i databasen, och filerna kan serveras statiskt direkt av ASP.NET.
