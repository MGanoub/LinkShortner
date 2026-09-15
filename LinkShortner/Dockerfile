# --- Build stage ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 As build
WORKDIR /src

# Copy csproj files
COPY  LinkShortner/LinkShortner.csproj LinkShortner/
RUN dotnet restore LinkShortner/LinkShortner.csproj

# copy everything else
COPY LinkShortner/ LinkShortner/
WORKDIR /src/LinkShortner
RUN dotnet publish -c Release -o /app/publish --no-restore

# --- Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URL=http://+:8080

ENTRYPOINT ["dotnet", "LinkShortner.dll"]