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


#playwright need it
###################

RUN apt-get update -yq && apt-get upgrade -yq && apt-get install -yq curl git nano
RUN curl -sL https://deb.nodesource.com/setup_18.x | bash - && apt-get install -yq nodejs build-essential
RUN npm install -g npm

RUN apt-get install -y wget xvfb unzip

# Set up the Chrome PPA
RUN wget -q -O - https://dl-ssl.google.com/linux/linux_signing_key.pub | apt-key add -
RUN echo "deb http://dl.google.com/linux/chrome/deb/ stable main" >> /etc/apt/sources.list.d/google.list

# Update the package list and install chrome
RUN apt-get update -y
RUN apt-get install -y google-chrome-stable 

RUN npx playwright install-deps
RUN npx playwright install


######################




EXPOSE 80


ENTRYPOINT ["dotnet", "WebHost.dll"]