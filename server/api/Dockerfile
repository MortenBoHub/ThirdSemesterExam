FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files first for efficient restore layers.
# IMPORTANT: This Dockerfile assumes the build context is the repository root.
COPY ProjectSolution.sln ./
COPY server/api/api.csproj server/api/
COPY server/dataccess/dataccess.csproj server/dataccess/

# Restore API project (pulls transitive refs including dataccess)
RUN dotnet restore server/api/api.csproj

# Copy the full source tree
COPY . .

# Publish the API project
RUN dotnet publish server/api/api.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Fly.io typical port
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["dotnet", "api.dll"]
