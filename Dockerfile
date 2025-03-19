# Use the official .NET 8 SDK image as a build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Set the working directory
WORKDIR /app

# Copy the project files
COPY src/Endpoint/*.csproj ./src/Endpoint/
RUN dotnet restore ./src/Endpoint

# Copy the remaining files and build the project
COPY src /app/src/
RUN dotnet publish ./src/Endpoint -c Release -o out

# Use the official .NET 8 runtime image as a runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# Set the working directory
WORKDIR /app

# Copy the build output from the build stage
COPY --from=build /app/out .

# Expose the port the app runs on
EXPOSE 8080

# Set the entry point for the application
ENTRYPOINT ["dotnet", "Endpoint.dll"]