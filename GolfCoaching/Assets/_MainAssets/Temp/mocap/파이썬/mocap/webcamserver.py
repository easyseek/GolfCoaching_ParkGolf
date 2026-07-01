import cv2
import numpy as np
import win32pipe, win32file, pywintypes
from multiprocessing import Process, Queue


def create_pipe(pipe_name):
    pipe = win32pipe.CreateNamedPipe(
        r"\\.\pipe\\" + pipe_name,
        win32pipe.PIPE_ACCESS_OUTBOUND,
        win32pipe.PIPE_TYPE_MESSAGE
        | win32pipe.PIPE_READMODE_MESSAGE
        | win32pipe.PIPE_WAIT,
        1,
        65536,
        65536,
        0,
        None,
    )
    return pipe


def send_image(pipe, image, pipe_name):
    success, encoded_image = cv2.imencode(".jpg", image)
    if not success:
        print("이미지 인코딩 실패")
        return

    try:
        # 이미지 데이터 전송
        image_bytes = encoded_image.tobytes()
        win32file.WriteFile(pipe, image_bytes)
        cv2.waitKey(1)
        # 보낸 바이트 크기 출력
        # print(f"{pipe_name} 보낸 바이트 크기: {len(image_bytes)}")
    except pywintypes.error as e:
        if e.winerror == 232:
            print("파이프가 닫혔습니다. 재연결을 시도합니다.")
            win32pipe.ConnectNamedPipe(pipe)
        else:
            print(f"이미지 전송 중 오류 발생: {e}")


def process_pipe(pipe_name, queue):
    while True:
        pipe = create_pipe(pipe_name)
        print(
            f"{pipe_name} 서버가 시작되었습니다. 유니티 클라이언트의 연결을 기다리는 중..."
        )
        try:
            win32pipe.ConnectNamedPipe(pipe)
            print(f"{pipe_name}에 유니티 클라이언트가 연결되었습니다.\n")

            while True:
                camera_id, image = queue.get()
                send_image(pipe, image, pipe_name)
        except pywintypes.error as e:
            if e.winerror == 232:
                print(f"{pipe_name} 연결이 끊겼습니다. 재연결을 시도합니다.")
                win32file.CloseHandle(pipe)
            else:
                print(f"{pipe_name}에서 예상치 못한 오류 발생: {e}")
                break
        except Exception as e:
            print(f"{pipe_name}에서 예상치 못한 오류 발생: {e}")
            break
        finally:
            win32file.CloseHandle(pipe)


def start_server(queue1, queue2):
    p1 = Process(target=process_pipe, args=("UnityWebcamStream1", queue1))
    p2 = Process(target=process_pipe, args=("UnityWebcamStream2", queue2))

    p1.start()
    p2.start()

    try:
        p1.join()
        p2.join()
    except KeyboardInterrupt:
        print("서버를 종료합니다.")
    finally:
        p1.terminate()
        p2.terminate()
