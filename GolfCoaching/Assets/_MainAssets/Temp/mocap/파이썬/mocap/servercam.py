import win32pipe, win32file, pywintypes
import threading
import json
import time

PIPE_NAME = r"\\.\pipe\UnityWebcamStream"


class WebcamServer:
    def __init__(self):
        self.pipe = None
        self.thread = None
        self.running = False

    def start(self):
        self.running = True
        self.thread = threading.Thread(target=self.run_server)
        self.thread.start()
        print("서버가 정상적으로 실행되었습니다.")

    def stop(self):
        self.running = False
        if self.thread:
            self.thread.join()

    def run_server(self):
        while self.running:
            try:
                print("클라이언트 연결을 대기 중입니다...")
                self.pipe = win32pipe.CreateNamedPipe(
                    PIPE_NAME,
                    win32pipe.PIPE_ACCESS_DUPLEX,
                    win32pipe.PIPE_TYPE_MESSAGE
                    | win32pipe.PIPE_READMODE_MESSAGE
                    | win32pipe.PIPE_WAIT,
                    1,
                    65536,
                    65536,
                    0,
                    None,
                )

                win32pipe.ConnectNamedPipe(self.pipe, None)
                print("클라이언트가 정상적으로 연결되었습니다.")

                while self.running:
                    try:
                        win32file.WriteFile(
                            self.pipe, b""
                        )  # 연결 유지를 위한 빈 메시지
                    except pywintypes.error:
                        print("클라이언트와의 연결이 끊겼습니다.")
                        break

            except pywintypes.error:
                print("파이프 연결 오류. 재연결 중...")
                time.sleep(1)
            finally:
                if self.pipe:
                    win32file.CloseHandle(self.pipe)

    def send_image_data(self, image_data):
        if self.pipe:
            try:
                data = json.dumps(image_data).encode("utf-8")
                win32file.WriteFile(self.pipe, data)
            except pywintypes.error:
                print("데이터 전송 오류")


webcam_server = WebcamServer()


def start_server():
    webcam_server.start()


def stop_server():
    webcam_server.stop()


def send_image(image_data):
    webcam_server.send_image_data(image_data)
