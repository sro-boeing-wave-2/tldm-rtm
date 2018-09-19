# FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
# WORKDIR /app
# EXPOSE 80


# FROM microsoft/dotnet:2.1-sdk AS build
# WORKDIR /RTMService
# COPY *.csproj ./
# RUN dotnet restore
# COPY . .

# WORKDIR /RTMService
# RUN dotnet build RTMService.csproj -c Release -o /app

# FROM build AS publish
# RUN dotnet publish RTMService.csproj -c Release -o /app

# FROM base AS final
# WORKDIR /app
# COPY --from=publish /app .
# ENTRYPOINT ["dotnet", "RTMService.dll"]
FROM microsoft/dotnet:2.1-sdk AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out

# Build runtime image
FROM microsoft/dotnet:aspnetcore-runtime
WORKDIR /app
EXPOSE 80
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "RTMService.dll"]