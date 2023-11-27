FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build-env
WORKDIR /App

COPY . ./
RUN dotnet restore
RUN dotnet publish -c Release -o out


FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /App
COPY --from=build-env /App/out .

#ENV 1001__smtpkeyid
#ENV 1001__smtpkeysecret
#ENV 1001__authtokensecret
#ENV 1001__databasekeyid
#ENV 1001__databasekeysecret

EXPOSE 80
ENTRYPOINT ["dotnet", "Server1001.dll"]