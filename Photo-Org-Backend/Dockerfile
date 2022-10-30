## Based on the dotnet code 3.1 alpine image
FROM mcr.microsoft.com/dotnet/sdk:3.1.424-alpine3.16

## Set the working directory to app and copy the code files into it
WORKDIR /app
COPY Source .

## Set up the dotnet environment and build the code
RUN dotnet restore
RUN dotnet run

## Set up the cron job to run the code every minuite
COPY Source/CronFile /etc/crontab
RUN crontab /etc/crontab
RUN touch /var/log/cron.log
CMD crond -f

## Image Info
MAINTAINER James Ashwood