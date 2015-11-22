FROM microsoft/aspnet:1.0.0-rc1-final

EXPOSE 5000

CMD ./web

WORKDIR /app/approot

ADD . /app