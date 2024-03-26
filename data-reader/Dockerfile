FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

# copy csproj different layers
COPY /src/WebHost/*.csproj .

#restore
RUN dotnet restore

# copy everything else and build app
COPY /src/WebHost .
RUN dotnet publish -c release -o /app --no-restore

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app
COPY --from=build /app ./

EXPOSE 80


ENTRYPOINT ["dotnet", "WebHost.dll"]