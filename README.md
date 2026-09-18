# AspireApp — API + Blazor demo

Resumen rápido:

- AspireApp.ApiService: API REST con Identity, JWT y refresh tokens. Usa SQLite por defecto en appsettings.json.
- AspireApp.Web: Frontend Blazor (Server) integrado que consume la API vía HttpClient.

Ejecución local (desarrollo):

1. Aplicar migraciones y crear la base de datos (desde la raíz del repo):

```powershell
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate -p AspireApp.ApiService -s AspireApp.ApiService
dotnet ef database update -p AspireApp.ApiService -s AspireApp.ApiService
```

2. Ejecutar la API y el frontend (desde Visual Studio o comandos dotnet):

```powershell
cd AspireApp.ApiService
dotnet run

cd ..\AspireApp.Web
dotnet run
```

3. Acceder al frontend (por defecto, el puerto lo asigna Kestrel/Visual Studio). El proyecto de ejemplo incluye una página de login y un dashboard de ejemplo.

Notas importantes:
- Reemplaza `Jwt:Key` en AspireApp.ApiService/appsettings.json por una clave segura antes de publicar.
- Si ejecutas frontend y API en hosts/puertos distintos, añade CORS en AspireApp.ApiService (Program.cs):
  builder.Services.AddCors(...) y app.UseCors(...)
- El Dockerfile para los proyectos está en AspireApp.ApiService/Dockerfile y AspireApp.Web/Dockerfile.

Próximos pasos sugeridos:
- Mejorar almacenamiento y renovación automática de tokens en Blazor.
- Añadir SignalR para colaboración en tiempo real.
- Añadir más pruebas de integración y E2E.
