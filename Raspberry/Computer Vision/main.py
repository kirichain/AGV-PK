import imutils
import sys
import cv2
import numpy as np
import time
import os
import json
import paho.mqtt.client as mqtt
import concurrent.futures

from threading import Thread
from imutils.video import FPS
from tkinter import *
from tkinter import ttk
from PIL import ImageTk, Image
from datetime import datetime

start_time = time.time()

# MQTT variables
client = None
broker = "127.0.0.1"
port = 1883
command_topic = "ipc/camera/command"
data_topic = "ipc/camera/data"
data_msg = ""
command_msg = ""
isLoopStop = False
json_data = ""

# Dictionary to store frame data
bottom_cam_frame_data = {
    "camera-type": "bottom",
    "timestamp": "",
    "center-distance": "",
    "tilt-angle": ""
}

front_cam_frame_data = {
    "camera-type": "front",
    "timestamp": "",
    "center-distance": "",
    "tilt-angle": ""
}

# Video source
# video_source = 'video-4.mp4'
video_sources = [
    ('video-4.mp4', 'bottom'),
    ('video-5.mp4', 'bottom'),
    ('video-1.mp4', 'front'),
    ('video-2.mp4', 'front')
]

# RSTP link from camera
rstp_link = 'rtsp://admin:L220E1A5@192.168.227.145:554/cam/realmonitor?channel=1&subtype=0&unicast=true&proto=Onvif'

# Global variables
img, img_binary = None, None
centerDistance = 0
isStartingDetect = True
lastTimestamp = 0
lastCenterDistance = 0
lastTiltAngle = 0


def pre_processing(frame, arg, addedBorderColor):
    # Resize to 448x448
    img = cv2.resize(frame, (448, 448))

    if (addedBorderColor == "white"):
        # Add a white border line to 4 sides of the image, border size = 5
        img = cv2.copyMakeBorder(img, 2, 2, 2, 2, cv2.BORDER_CONSTANT, value=[255, 255, 255])
    else:
        # Add a black border line to 4 sides of the image, border size = 5
        img = cv2.copyMakeBorder(img, 2, 2, 2, 2, cv2.BORDER_CONSTANT, value=[0, 0, 0])

    # Convert to gray scale
    img_gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)

    # Convert to HSV
    img_hsv = cv2.cvtColor(img, cv2.COLOR_BGR2HSV)

    # Gaussian blur
    gaussian_blurframe = cv2.GaussianBlur(img_gray, (5, 5), 0);

    # Median blur
    median_blurframe = cv2.medianBlur(img_gray, 5)

    # Binary threshold
    ret, img_binary = cv2.threshold(gaussian_blurframe, 127, 255, cv2.THRESH_BINARY)

    # Adaptive threshold
    img_adaptive_threshold = cv2.adaptiveThreshold(gaussian_blurframe, 255, cv2.ADAPTIVE_THRESH_GAUSSIAN_C,
                                                   cv2.THRESH_BINARY, 11, 2)
    match arg:
        case 'none':
            return img
        case 'gray':
            return img_gray
        case 'hsv':
            return img_hsv
        case 'binary':
            return img_binary
        case 'adaptive':
            return img_adaptive_threshold


def connect_mqtt():
    # Global variables
    global client, isLoopStop

    client = mqtt.Client()
    client.on_connect = on_connect
    client.on_message = on_message

    client.connect(broker, port, 60)

    # Publish a message
    client.publish(command_topic, "Command topic connection established")
    client.publish(data_topic, "Data topic connection established")


def on_connect(client, userdata, flags, rc):
    print("Connected with result code " + str(rc))

    # Subscribing in on_connect() means that if we lose the connection and
    # reconnect then subscriptions will be renewed.
    client.subscribe(command_topic)
    client.subscribe(data_topic)


# The callback for when a PUBLISH message is received from the server.
def on_message(client, userdata, msg):
    global data_msg, command_msg, isLoopStop, isStartingDetect

    if msg.topic == data_topic:
        print("Data received: ", end="")
        data_msg = str(msg.payload.decode("utf-8"))
        print(data_msg)
    elif msg.topic == command_topic:
        print("Command received")
        command_msg = str(msg.payload.decode("utf-8"))
        if (command_msg == "loop_stop"):
            isLoopStop = True
            client.loop_stop(force=True)
            print("Loop stop received")
        elif (command_msg == "start_detect"):
            isStartingDetect = True
            print("Start detect received")
        elif (command_msg == "stop_detect"):
            isStartingDetect = False
            print("Stop detect received")
    else:
        print("Unknown topic")

    print(msg.topic + " " + str(msg.payload))


def detect_lane_front_cam(frame):
    print("Detecting lane front cam")

    img_binary = pre_processing(frame, 'binary', "black")

    # Detect edges using canny
    img_canny = cv2.Canny(img_binary, 100, 200)

    # Find contours
    contours, hierarchy = cv2.findContours(img_canny, cv2.RETR_TREE, cv2.CHAIN_APPROX_SIMPLE)

    # Remove small contours
    contours = [cnt for cnt in contours if cv2.contourArea(cnt) > 10]

    # If no contours are found
    if len(contours) == 0:
        print('No contours found')
    #    return
    else:
        # Get rectangles for each contour
        rects = [cv2.boundingRect(cnt) for cnt in contours]

        # Get min area rectangles
        min_area_rects = [cv2.minAreaRect(cnt) for cnt in contours]

        # On the left side of center vertical line, keep only tilted rectangles that are tilted to the right
        min_area_rects_left = [rect for rect in min_area_rects if rect[0][0] < 224]

        # Filter out rectangles that has width > hegith
        min_area_rects_left = [rect for rect in min_area_rects_left if rect[1][0] < rect[1][1]]

        # Filter out rectangles that has angle > 30 degree and angle < 60 degree
        min_area_rects_left = [rect for rect in min_area_rects_left if rect[2] > 5 and rect[2] < 45]

        # Draw min area rectangle left
        for rect in min_area_rects_left:
            box = cv2.boxPoints(rect)
            box = np.int0(box)
            # Skip drawing if any of the 4 points has x coordinate >= 224
            if (box[0][0] >= 224 or box[1][0] >= 224 or box[2][0] >= 224 or box[3][0] >= 224):
                continue
            else:
                cv2.drawContours(frame, [box], 0, (0, 255, 255), 2)

        # On the right side of center vertical line, keep only -45 degree tilted rectangles
        min_area_rects_right = [rect for rect in min_area_rects if rect[0][0] > 224]

        # Filter out rectangles that has width > hegith
        min_area_rects_right = [rect for rect in min_area_rects_right if rect[1][0] > rect[1][1]]

        # Filter rectangles that has angle > 30 degree and angle < 60 degree
        min_area_rects_right = [rect for rect in min_area_rects_right if rect[2] > 45 and rect[2] < 90]

        # Draw min area rectangle right
        for rect in min_area_rects_right:
            box = cv2.boxPoints(rect)
            box = np.int0(box)
            # Skip drawing if any of 4 points has x coordinate <= 224
            if (box[0][0] <= 224 or box[1][0] <= 224 or box[2][0] <= 224 or box[3][0] <= 224):
                continue
            else:
                cv2.drawContours(frame, [box], 0, (0, 255, 255), 2)

        # Draw detected contours
        cv2.drawContours(frame, contours, -1, (0, 255, 0), 5)

        # Draw a vertical line in the middle of the image, color red
        cv2.line(frame, (224, 0), (224, 448), (0, 0, 255), 2)

        # Draw a horizontal line in the middle of the image, color red
        cv2.line(frame, (0, 224), (448, 224), (0, 0, 255), 2)

        cv2.imshow('Contours', frame)
        cv2.imshow('Canny', img_canny)

        print("Detecting lane front cam done")


def detect_lane_bottom_cam(frame):
    # Global variables
    global centerDistance, json_data, lastTimestamp, lastCenterDistance, lastTiltAngle

    img_binary = pre_processing(frame, 'binary', "white")
    img_rbg = pre_processing(frame, 'none', "white")

    # Detect edges using canny
    img_canny = cv2.Canny(img_binary, 100, 200)

    # Find contours
    contours, hierarchy = cv2.findContours(img_canny, cv2.RETR_TREE, cv2.CHAIN_APPROX_SIMPLE)

    # Draw contours
    cv2.drawContours(frame, contours, -1, (0, 255, 0), 5)

    # Filter contours by size from max to min
    contours = sorted(contours, key=cv2.contourArea, reverse=True)[:5]

    # Get the biggest contour
    biggest_contour = contours[0]

    # Find moment of the biggest contour
    M = cv2.moments(biggest_contour)

    # Get the center of the biggest contour
    cx = int(M['m10'] / M['m00'])
    cy = int(M['m01'] / M['m00'])

    # Get bounding rectangles for each contour
    rects = [cv2.boundingRect(cnt) for cnt in contours]

    # Draw the center of the biggest contour
    cv2.circle(frame, (cx, cy), 5, (0, 0, 255), -1)

    # Draw the bounding rectangles, color blue
    for rect in rects:
        x, y, w, h = rect
        cv2.rectangle(frame, (x, y), (x + w, y + h), (255, 0, 0), 2)

    # Draw min area rectangles, color yellow
    for cnt in contours:
        min_area_rect = cv2.minAreaRect(cnt)
        box = cv2.boxPoints(min_area_rect)
        box = np.int0(box)
        cv2.drawContours(frame, [box], 0, (0, 255, 255), 2)

    # Draw a vertical line in the middle of the image, color red
    cv2.line(frame, (224, 0), (224, 448), (0, 0, 255), 2)

    # Draw a horizontal line in the middle of the image, color red
    cv2.line(frame, (0, 224), (448, 224), (0, 0, 255), 2)

    # Calculate the distance between the center of the biggest contour and the center vertical line, with cases of the
    # center point is on the left or right of the center vertical line, always return distance in positive value
    if cx < 224:
        centerDistance = 224 - cx
    else:
        centerDistance = cx - 224

    # Draw a connecting line between the center of the biggest contour and the vertical line, put the distance value, text color
    # and line color depend on the distance value
    if centerDistance < 50:
        cv2.line(frame, (cx, cy), (224, cy), (0, 255, 0), 2)
        cv2.putText(frame, str(centerDistance), (cx, cy), cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 255, 0), 2)
    elif centerDistance < 100:
        cv2.line(frame, (cx, cy), (224, cy), (0, 255, 255), 2)
        cv2.putText(frame, str(centerDistance), (cx, cy), cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 255, 255), 2)
    else:
        cv2.line(frame, (cx, cy), (224, cy), (0, 0, 255), 2)
        cv2.putText(frame, str(centerDistance), (cx, cy), cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 0, 255), 2)

    # Get time stamp at format linux time, to nano second
    timestamp = time.time_ns()

    # Check if last timestamp is different from current timestamp, if not, do not publish data to MQTT broker
    if timestamp != lastTimestamp:
        print("Publishing data to MQTT broker")
        bottom_cam_frame_data["timestamp"] = timestamp
        # If center distance is different from last center distance, publish data to MQTT broker
        if centerDistance != lastCenterDistance:
            bottom_cam_frame_data["center-distance"] = centerDistance
            # Serialize data to json format
            json_data = json.dumps(bottom_cam_frame_data)
            # Publish the json data to MQTT broker
            # client.publish(data_topic, json_data)
            # Update last timestamp and last center distance
            lastTimestamp = timestamp
            lastCenterDistance = centerDistance
    else:
        pass

    # Print the json data
    print(json_data)

    # Dislay the image
    cv2.imshow('Contours', frame)
    cv2.imshow('Canny', img_canny)


def main(camera_position, video_source):
    # Capture video from RTSP stream and display frame
    cap = cv2.VideoCapture(video_source)

    # Read frame and process
    while True:
        # Read the frame
        ret, frame = cap.read()
        # If frame is read correctly ret is True
        if not ret:
            print("Can't receive frame (stream end?). Exiting ...")
            # Release the video capture object
            cap.release()
            # Close all windows
            cv2.destroyAllWindows()
            break
        frame = cv2.resize(frame, (448, 448))
        if camera_position == 'front':
            detect_lane_front_cam(frame)
        else:
            detect_lane_bottom_cam(frame)
        # Press Q on keyboard to exit
        if cv2.waitKey(1) == ord('q'):
            break


def run_single_detection(video_index):
    video_source, camera_position = video_sources[video_index]
    main(camera_position, video_source)


def run_all_detections():
    with concurrent.futures.ThreadPoolExecutor(max_workers=4) as executor:
        futures = [executor.submit(main, vs[1], vs[0]) for vs in video_sources]
        concurrent.futures.wait(futures)


if __name__ == "__main__":
    execute_all = False  # Set this flag to True to run all detections, False to run a single detection
    single_video_index = 0  # Set this to the index of the video to run if execute_all is False

    while True:
        if execute_all:
            run_all_detections()
        else:
            if 0 <= single_video_index < len(video_sources):
                run_single_detection(single_video_index)
            else:
                print("Invalid video index.")
                break
        break  # Exit the loop after one execution

# Start MQTT connection
# connect_mqtt()
# Start main function and repeat when it ends

# while True:
#     if (isLoopStop == False):
#        client.loop_start()
#     if (isStartingDetect == True):
#         video_source = 'video-4.mp4'
#         main('bottom')
#         video_source = 'video-5.mp4'
#         main('bottom')
#         video_source = 'video-1.mp4'
#         main('front')
#         video_source = 'video-2.mp4'
#         main('front')
