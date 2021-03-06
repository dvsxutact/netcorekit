#!/bin/bash
set -ex

readonly ROOT_DIR=`pwd`
readonly SERVICE_DIR=${ROOT_DIR}/samples/ExchangeService

readonly GRPC_PATH=${HOME}/.nuget/packages/grpc.tools/1.17.1/tools/linux_x64
readonly PROTO_PATH=${ROOT_DIR}/samples/_protos/v1
readonly OUTPUT_PATH=${SERVICE_DIR}/v1/Grpc
readonly PROTO_FILE=bimonetary.proto

cd `$SERVICE_DIR/App_Build`

$GRPC_PATH/protoc -I $PROTO_PATH -I /usr/local/include \
    --csharp_out $OUTPUT_PATH \
    --grpc_out $OUTPUT_PATH $PROTO_PATH/$PROTO_FILE \
    --plugin=protoc-gen-grpc=${GRPC_PATH}/grpc_csharp_plugin

cd -
