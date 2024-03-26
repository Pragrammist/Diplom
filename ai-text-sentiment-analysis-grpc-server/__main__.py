import grpc
import concurrent.futures
from transformers import pipeline
import os
import lable_service_pb2_grpc
from lable_service_pb2 import LableTextRequest, LableResponse


pipe = pipeline("text-classification", model="rubert-ru-sentiment-rusentiment")
port = os.environ.get('PORT', '50052')

class LableService(
    lable_service_pb2_grpc.LableServiceServicer
):

    def SendLable(self, request_iterator, context):
        for LableTextRequest in request_iterator:
            text = LableTextRequest.text
            lable = self.get_lable(text)
            response = LableResponse(label=lable['label'], score=lable['score'])
            yield response
    def get_lable(self, input):
        return pipe(input)[0]    




def serve():
    server = grpc.server(concurrent.futures.ThreadPoolExecutor(max_workers=10))
    lable_service_pb2_grpc.add_LableServiceServicer_to_server(
        LableService(), server
    )
    server.add_insecure_port("[::]:"+port)
    server.start()
    print("all ok")
    server.wait_for_termination()



if __name__ == "__main__":
    serve()




