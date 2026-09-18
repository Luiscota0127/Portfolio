FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["AspireApp.Web/AspireApp.Web.csproj", "AspireApp.Web/"]
COPY ["AspireApp.ServiceDefaults/AspireApp.ServiceDefaults.csproj", "AspireApp.ServiceDefaults/"]
RUN dotnet restore "AspireApp.Web/AspireApp.Web.csproj"

COPY . .
WORKDIR /src/AspireApp.Web
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80
ENTRYPOINT ["dotnet", "AspireApp.Web.dll"]
