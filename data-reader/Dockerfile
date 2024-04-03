FROM mcr.microsoft.com/dotnet/aspnet:7.0 as base


#Install powershell to playwright scripts
RUN apt-get update -yq \
    && apt-get install wget -yq \
    && wget -q https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb \
    && dpkg -i packages-microsoft-prod.deb \
    && apt-get update -yq \
    && apt-get install powershell -yq

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
FROM base
WORKDIR /app
COPY --from=build /app ./



# Install playwright dependencies and cleanup
RUN pwsh playwright.ps1 install chromium
RUN pwsh playwright.ps1 install-deps chromium
RUN rm -rf playwright.ps1
RUN apt remove wget powershell -yq


EXPOSE 80


ENTRYPOINT ["dotnet", "WebHost.dll"]