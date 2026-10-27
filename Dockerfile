FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Dashboard_server.csproj ./
RUN dotnet restore
COPY . ./
RUN dotnet publish Dashboard_server.csproj -c Release -o /out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out ./
EXPOSE 8080
ENTRYPOINT ["dotnet", "Dashboard_server.dll"]
