FROM microsoft/aspnetcore:1.1.2

ENV ASPNETCORE_ENVIRONMENT=Production

RUN mkdir -p /var/app

WORKDIR /var/app

COPY artifacts/ .

EXPOSE 80

ENTRYPOINT ["dotnet", "Dymaxion.dll"]