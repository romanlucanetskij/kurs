# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["DripCube.csproj", "./"]
RUN dotnet restore "DripCube.csproj"

# Copy everything else and build
COPY . .
RUN dotnet publish "DripCube.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy published files from build stage
COPY --from=build /app/publish .

# Expose the port that Render will use (Render typically uses 10000)
EXPOSE 10000

# Set environment variables - PORT will be provided by Render
ENV ASPNETCORE_ENVIRONMENT=Production

# Run the application
ENTRYPOINT ["dotnet", "DripCube.dll"]
