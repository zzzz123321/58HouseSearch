FROM microsoft/dotnet:2.1-sdk AS build-env
WORKDIR /app

# copy csproj and restore as distinct layers
COPY src/*.csproj ./
COPY HouseMap.Dao/*.csproj /HouseMap.Dao/
RUN dotnet restore

# copy everything else and build
COPY . .
RUN dotnet publish src -c Release -o out

# build runtime image
FROM microsoft/dotnet:2.1.3-aspnetcore-runtime
WORKDIR /app
COPY --from=build-env /app/src/out .
ENTRYPOINT ["dotnet", "HouseMapAPI.dll"]