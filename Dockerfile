FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy all project files for restore
COPY DataAccessLayer/DataAccessLayer.csproj DataAccessLayer/
COPY BusinessLayer/BusinessLayer.csproj BusinessLayer/
COPY PitchProAPI/PitchProAPI.csproj PitchProAPI/

# Restore dependencies
RUN dotnet restore PitchProAPI/PitchProAPI.csproj

# Copy everything and build
COPY . .
RUN dotnet publish PitchProAPI/PitchProAPI.csproj -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

ARG PORT=8080
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}

ENTRYPOINT ["dotnet", "PitchProAPI.dll"]
