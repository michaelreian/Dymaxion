#!/usr/bin/env bash

abort()
{
  echo "An error has occurred. Exiting..." >&2
  exit 1
}

trap 'abort' 0

set -e

if [ -d artifacts ]
then
	echo "Removing artifacts..."
    rm -rf artifacts
fi

mkdir -p artifacts

echo "Building..."

docker build -t build-image -f Dockerfile.build .

echo "Creating container..."

BUILD_CONTAINER=$(docker create build-image)

echo "Copying artifacts..."

docker cp $BUILD_CONTAINER:/var/app/dist/ $(pwd)/artifacts/

echo "Removing build image..."

docker rmi -f build-image

trap : 0

