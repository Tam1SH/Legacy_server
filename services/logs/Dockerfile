FROM rust:1.70 as build
ENV PKG_CONFIG_ALLOW_CROSS=1

RUN apt-get update \
 && DEBIAN_FRONTEND=noninteractive \
    apt-get install --no-install-recommends --assume-yes \
      protobuf-compiler

WORKDIR /usr/src/logs
COPY . .



RUN cargo install --path .

FROM gcr.io/distroless/cc-debian12

COPY --from=build /usr/local/cargo/bin/logs /
CMD  ["./logs"]