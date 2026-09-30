# ==========================================
# 1. SDK Image to build the application
# ==========================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy everything from your repository into the Docker container
COPY . .

# Run dotnet restore specifically pointing to your Web API project file
RUN dotnet restore "PitchProAPI/PitchProAPI.csproj"

# Compile and publish an optimized Release version of your app
RUN dotnet publish "PitchProAPI/PitchProAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ==========================================
# 2. Runtime Image to execute the app
# ==========================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copy the compiled files over from the build stage
COPY --from=build /app/publish .

# -------------------------------------------------------------
# 🔥 THE FIX FOR SILENT LOGS: Force .NET to flush logs instantly
# -------------------------------------------------------------
ENV DOTNET_SYSTEM_CONSOLE_ALLOW_BUFFERING=0
ENV ASPNETCORE_LOGGING__CONSOLE__DISABLECOLORS=true

# Tell .NET to listen on port 8080 (the default port Railway routes traffic to)
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# The startup command to run your web api assembly file
ENTRYPOINT ["dotnet", "PitchProAPI.dll"]