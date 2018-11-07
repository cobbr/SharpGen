FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /app

COPY . ./
RUN dotnet restore 
RUN dotnet publish -c Release -o out

FROM microsoft/dotnet:2.1-aspnetcore-runtime AS runtime
WORKDIR /app
COPY --from=build /app/out .
COPY ./References ./References
COPY ./src ./src
ENTRYPOINT ["dotnet", "SharpGen.dll"]
