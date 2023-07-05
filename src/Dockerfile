FROM mcr.microsoft.com/dotnet/sdk:7.0-alpine AS build
WORKDIR /pt
COPY ./PrayerTracker.sln                              ./
COPY ./Directory.Build.props                          ./
COPY ./PrayerTracker/PrayerTracker.fsproj             ./PrayerTracker/
COPY ./PrayerTracker.Data/PrayerTracker.Data.fsproj   ./PrayerTracker.Data/
COPY ./PrayerTracker.Tests/PrayerTracker.Tests.fsproj ./PrayerTracker.Tests/
COPY ./PrayerTracker.UI/PrayerTracker.UI.fsproj       ./PrayerTracker.UI/
RUN dotnet restore

COPY . ./
WORKDIR /pt/PrayerTracker.Tests
RUN dotnet run

WORKDIR /pt/PrayerTracker
RUN dotnet publish -c Release -r linux-x64

FROM  mcr.microsoft.com/dotnet/aspnet:7.0-alpine as final
WORKDIR /app
RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
COPY --from=build /pt/PrayerTracker/bin/Release/net7.0/linux-x64/publish/ ./

EXPOSE 80
CMD [ "dotnet", "/app/PrayerTracker.dll" ]
