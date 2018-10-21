FROM alpine

RUN echo "http://dl-4.alpinelinux.org/alpine/edge/testing" | cat - /etc/apk/repositories > repositories \
	&& mv -f repositories /etc/apk

RUN apk add --no-cache mono mono-dev git bash automake autoconf findutils make pkgconf libtool g++ \
	&& mkdir /opt \
	&& git clone https://github.com/mono/xsp.git /opt/xsp \
	&& cd /opt/xsp \
	&& ./autogen.sh \
	&& ./configure --prefix=/usr \
	&& make -j3 \
	&& make install \
	&& rm -rf /opt/xsp \
	&& apk del mono-dev git bash automake autoconf findutils make pkgconf libtool g++

RUN apk add --no-cache ca-certificates \
	&& cert-sync /etc/ssl/certs/ca-certificates.crt

WORKDIR /usr/src/app

ADD . /usr/src/app

ENTRYPOINT ["/usr/bin/xsp4", "--address=0.0.0.0", "--port=8080"]