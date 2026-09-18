FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# copy csproj and restore
COPY ["AspireApp.ApiService/AspireApp.ApiService.csproj", "AspireApp.ApiService/"]
COPY ["AspireApp.ServiceDefaults/AspireApp.ServiceDefaults.csproj", "AspireApp.ServiceDefaults/"]
RUN dotnet restore "AspireApp.ApiService/AspireApp.ApiService.csproj"

# copy everything and publish
COPY . .
WORKDIR /src/AspireApp.ApiService
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80
ENTRYPOINT ["dotnet", "AspireApp.ApiService.dll"]
