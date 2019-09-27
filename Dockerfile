FROM mcr.microsoft.com/dotnet/core/sdk:3.0-bionic AS build

WORKDIR /source

# Download & Unpack latest Starcounter 3.0.0
RUN apt-get update \
	&& apt-get install -y wget unzip

RUN mkdir artifacts
RUN wget https://starcounter.io/Starcounter/Starcounter.3.0.0-alpha-20190930.zip -O ./artifacts/Starcounter.zip
RUN unzip ./artifacts/Starcounter.zip -d ./artifacts

# Copy source files
COPY ./NuGet.Config ./app/NuGet.Config
COPY ./src/Starcounter.Techempower.csproj ./app/Starcounter.Techempower.csproj
COPY ./src/Fortune.cs ./app/Fortune.cs
COPY ./src/FortuneOrm.cs ./app/FortuneOrm.cs
COPY ./src/IFortune.cs ./app/IFortune.cs
COPY ./src/IRandom.cs ./app/IRandom.cs
COPY ./src/IWorld.cs ./app/IWorld.cs
COPY ./src/Startup.cs ./app/Startup.cs
COPY ./src/ThreadSafeRandom.cs ./app/ThreadSafeRandom.cs
COPY ./src/World.cs ./app/World.cs
COPY ./src/WorldOrm.cs ./app/WorldOrm.cs

WORKDIR /source/app
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/core/aspnet:3.0-bionic AS runtime

# Install Starcounter dependencies
RUN apt-get update \
	&& apt-get install -y wget unzip \
	&& apt-get install -y software-properties-common \
	&& apt-get install -y libaio1 \
	&& add-apt-repository -y ppa:swi-prolog/stable \
	&& apt-get update \
	&& apt-get install -y swi-prolog-nox=7.\*

ENV ASPNETCORE_URLS http://+:8080
WORKDIR /source/publish
COPY --from=build /source/app/out ./

ENTRYPOINT ["dotnet", "Starcounter.Techempower.dll"]
