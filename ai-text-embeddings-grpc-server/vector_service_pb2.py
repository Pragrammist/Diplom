# -*- coding: utf-8 -*-
# Generated by the protocol buffer compiler.  DO NOT EDIT!
# source: vector-service.proto
"""Generated protocol buffer code."""
from google.protobuf import descriptor as _descriptor
from google.protobuf import descriptor_pool as _descriptor_pool
from google.protobuf import symbol_database as _symbol_database
from google.protobuf.internal import builder as _builder
# @@protoc_insertion_point(imports)

_sym_db = _symbol_database.Default()




DESCRIPTOR = _descriptor_pool.Default().AddSerializedFile(b'\n\x14vector-service.proto\"!\n\x11VectorTextRequest\x12\x0c\n\x04text\x18\x01 \x01(\t\" \n\x0eVectorResponse\x12\x0e\n\x06vector\x18\x01 \x03(\x02\x32\x46\n\rVectorService\x12\x35\n\nSendVector\x12\x12.VectorTextRequest\x1a\x0f.VectorResponse(\x01\x30\x01\x62\x06proto3')

_globals = globals()
_builder.BuildMessageAndEnumDescriptors(DESCRIPTOR, _globals)
_builder.BuildTopDescriptorsAndMessages(DESCRIPTOR, 'vector_service_pb2', _globals)
if _descriptor._USE_C_DESCRIPTORS == False:
  DESCRIPTOR._options = None
  _globals['_VECTORTEXTREQUEST']._serialized_start=24
  _globals['_VECTORTEXTREQUEST']._serialized_end=57
  _globals['_VECTORRESPONSE']._serialized_start=59
  _globals['_VECTORRESPONSE']._serialized_end=91
  _globals['_VECTORSERVICE']._serialized_start=93
  _globals['_VECTORSERVICE']._serialized_end=163
# @@protoc_insertion_point(module_scope)
