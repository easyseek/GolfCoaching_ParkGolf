import cv2
import mediapipe as mp
import numpy as np
import server
#import handserver
import time

mp_hands = mp.solutions.hands

mp_pose = mp.solutions.pose
mp_drawing = mp.solutions.drawing_utils
pose = mp_pose.Pose(
    static_image_mode=False,
    enable_segmentation=False,
    smooth_landmarks=True,
    min_detection_confidence=0.5,
    min_tracking_confidence=0.5,
)
hands = mp_hands.Hands(
    static_image_mode=False,
    max_num_hands=2,
    min_detection_confidence=0.5,
    min_tracking_confidence=0.5,
)

video_paths = "D:\\20.Projects\\22.Python\\PosEstimationModule\\Videos\\fullswing.mp4"
#video_paths = "D:\\20.Projects\\22.Python\\PosEstimationModule\\Videos\\swing.mp4"
#video_paths = "D:\\20.Projects\\22.Python\\PosEstimationModule\\Videos\\swing2.mp4"
#video_paths = "D:\\20.Projects\\22.Python\\PosEstimationModule\\Videos\\test3.mp4"
#video_paths = "D:\\20.Projects\\22.Python\\PosEstimationModule\\Videos\\Side.mp4"
cap = cv2.VideoCapture(video_paths)

#cap = cv2.VideoCapture(0)

server.start_server()
#handserver.start_server()
first_frame = True

try:
    while cap.isOpened():
        #print("while start")
        success, image = cap.read()
        if not success:
            #print("카메라를 읽을 수 없습니다.")
            
            print("End of video, looping...")
            cap.set(cv2.CAP_PROP_POS_FRAMES, 0)
            first_frame = True  # 다시 첫 프레임에서 1초 유지하도록 설정
            continue
            
            
        #time.sleep(1 / 30)  # 초당 30 프레임 기준으로 재생 속도 조절
        #image = cv2.flip(image, 1)  # 좌우반전
        image = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)
        image.flags.writeable = False

        results = pose.process(image)
        hand_results = hands.process(image)
        
        image.flags.writeable = True
        image = cv2.cvtColor(image, cv2.COLOR_RGB2BGR)

        if results.pose_landmarks:
            #print("results")
            mp_drawing.draw_landmarks(
                image,
                results.pose_landmarks,
                mp_pose.POSE_CONNECTIONS,
                mp_drawing.DrawingSpec(
                    color=(245, 117, 66), thickness=2, circle_radius=2
                ),
                mp_drawing.DrawingSpec(
                    color=(245, 66, 230), thickness=2, circle_radius=2
                ),
            )

            pose_data = {}
            if results.pose_world_landmarks:
                for idx, landmark in enumerate(results.pose_world_landmarks.landmark):
                    pose_data[f"landmark_{idx}"] = {
                        "x": landmark.x,
                        "y": landmark.y,
                        "z": landmark.z,
                        "visibility": landmark.visibility,
                    }

            server.send_pose(pose_data)

            print("results send")
            #_, encoded_image = cv2.imencode('.jpg', image)
            #image_data = encoded_image.tobytes()

            #cv2.imshow("MediaPipe Pose", image)
            #servercam.send_image(image_data)
            
        #if hand_results.multi_hand_landmarks:
        #    print("hand_results")
        #    for hand_landmarks in hand_results.multi_hand_landmarks:
        #        mp_drawing.draw_landmarks(
        #            image,
        #            hand_landmarks,
        #            mp_hands.HAND_CONNECTIONS,
        #            mp_drawing.DrawingSpec(
        #                color=(0, 255, 0), thickness=2, circle_radius=2
        #            ),
        #            mp_drawing.DrawingSpec(
        #                color=(0, 0, 255), thickness=2, circle_radius=2
        #            ),
        #        )

        #    hands_data = []
        #    for hand_landmarks in hand_results.multi_hand_landmarks:
        #        hand_data = {}
        #        for landmark_idx, landmark in enumerate(hand_landmarks.landmark):
        #            hand_data[f"landmark_{landmark_idx}"] = {
        #                "x": landmark.x,
        #                "y": landmark.y,
        #                "z": landmark.z,
        #            }
        #        hands_data.append(hand_data)

        #    handserver.send_hands(hands_data)
        #    print("hand_results send")
        
        # 첫 프레임일 경우 1초 동안 유지
        #print(f"first_frame:{first_frame}")
        if first_frame:
            cv2.imshow("MediaPipe Pose", image)
            cv2.waitKey(1000)  # 1초 대기
            first_frame = False
        else:
            cv2.imshow("MediaPipe Pose", image)
    
        #cv2.imshow("MediaPipe Pose", image)
        if cv2.waitKey(5) & 0xFF == 27:  # ESC 키로 종료
            break
            
        #print("while end")
finally:
    cap.release()
    cv2.destroyAllWindows()
    server.stop_server()
    handserver.stop_server()
