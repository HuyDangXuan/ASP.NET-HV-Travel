# Docker Commands

Tai lieu nay tong hop cac lenh Docker Compose hay dung cho du an `ASP.NET-HV-Travel`.

## Luu y

- File `docker-compose.yml` hien dang de `restart: "no"`, nen container se khong tu dong start lai.
- App web doc `HVTravelDatabase__ConnectionString` tu file `.env`.
- Web trong Docker duoc publish tai `http://localhost:5028`.
- Mongo local neu duoc start se map ra `localhost:27018`.
- Muon hot reload trong Docker thi dung them file `docker-compose.dev.yml`.

## Build

Build toan bo image:

```powershell
docker compose build
```

Build lai va start ngay:

```powershell
docker compose up -d --build
```

Build lai image dev cho hot reload:

```powershell
docker compose -f docker-compose.yml -f docker-compose.dev.yml build hv-travel-web
```

## Start

Start toan bo service:

```powershell
docker compose up -d
```

Start rieng web:

```powershell
docker compose up -d hv-travel-web
```

Start web va Mongo:

```powershell
docker compose up -d mongodb hv-travel-web
```

Start tunnel khi can:

```powershell
docker compose up -d tunnel
```

## Hot reload trong Docker

Chay web bang `dotnet watch` ben trong container, co mount source code tu may local:

```powershell
docker compose -f docker-compose.yml -f docker-compose.dev.yml up -d hv-travel-web
```

Chay web + Mongo local voi hot reload:

```powershell
docker compose -f docker-compose.yml -f docker-compose.dev.yml --profile local-db up -d mongodb hv-travel-web
```

Luu y:

- Source code local duoc mount vao container, sua file se duoc `dotnet watch` tu restart.
- Service van publish ra `http://localhost:5028`.
- Neu stack thuong dang chay truoc do, nen `docker compose stop hv-travel-web` truoc khi bat che do dev.

## Status

Xem trang thai container:

```powershell
docker compose ps
```

## Logs

Xem log tat ca service:

```powershell
docker compose logs -f
```

Xem log rieng web:

```powershell
docker compose logs -f hv-travel-web
```

Xem log web trong che do hot reload:

```powershell
docker compose -f docker-compose.yml -f docker-compose.dev.yml logs -f hv-travel-web
```

Xem log rieng Mongo:

```powershell
docker compose logs -f mongodb
```

Xem log rieng tunnel:

```powershell
docker compose logs -f tunnel
```

## Stop

Stop tat ca container nhung giu nguyen:

```powershell
docker compose stop
```

Stop rieng web:

```powershell
docker compose stop hv-travel-web
```

## Down

Tat va go container/network:

```powershell
docker compose down
```

Tat va xoa ca volume Mongo local:

```powershell
docker compose down -v
```

## Goi y thao tac nhanh

Lan dau hoac sau khi doi code:

```powershell
docker compose build
docker compose up -d mongodb hv-travel-web
```

Khi can xem log app:

```powershell
docker compose logs -f hv-travel-web
```

Khi xong viec:

```powershell
docker compose down
```
