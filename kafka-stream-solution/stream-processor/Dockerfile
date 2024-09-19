# Use .NET SDK to build the project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app
COPY stream-processor.csproj ./
RUN dotnet restore
COPY . ./
RUN dotnet publish -c Release -o out

# Use .NET runtime to run the app
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "stream-processor.dll"]
