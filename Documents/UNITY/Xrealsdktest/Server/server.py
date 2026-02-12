from flask import Flask, request, jsonify, send_from_directory
import os
from datetime import datetime, timedelta
import glob
import cv2
import numpy as np
import time
import threading
from ultralytics import YOLO
from face_recognition_module import FaceRecognizer
from object_recognition_module import ObjectRecognizer
import logging

app = Flask(__name__)

# /log エンドポイントのアクセスログを非表示にする
class LogFilter(logging.Filter):
    def filter(self, record):
        # /log へのリクエストを非表示
        return '/log' not in record.getMessage()

# Werkzeugのロガーにフィルタを適用
log = logging.getLogger('werkzeug')
log.addFilter(LogFilter())

# Config
BASE_DIR = os.path.dirname(os.path.abspath(__file__))
PROJECT_ROOT = os.path.dirname(BASE_DIR)
UPLOAD_FOLDER = os.path.join(PROJECT_ROOT, 'uploads')
FACE_DATA_FOLDER = os.path.join(BASE_DIR, 'face_data')

# クリーンアップ設定
IMAGE_MAX_AGE_HOURS = 24  # 画像の最大保持時間（時間）
CLEANUP_INTERVAL_SECONDS = 3600  # クリーンアップ間隔（1時間 = 3600秒）

if not os.path.exists(UPLOAD_FOLDER):
    os.makedirs(UPLOAD_FOLDER)

def cleanup_old_images():
    """
    24時間以上経過した画像を削除
    """
    while True:
        try:
            now = time.time()
            max_age_seconds = IMAGE_MAX_AGE_HOURS * 3600
            deleted_count = 0
            
            for filepath in glob.glob(os.path.join(UPLOAD_FOLDER, "*.jpg")):
                file_age = now - os.path.getmtime(filepath)
                if file_age > max_age_seconds:
                    os.remove(filepath)
                    deleted_count += 1
                    print(f"[CLEANUP] Deleted old image: {os.path.basename(filepath)}")
            
            if deleted_count > 0:
                print(f"[CLEANUP] Deleted {deleted_count} old images")
            else:
                print(f"[CLEANUP] No old images to delete (checked at {datetime.now().strftime('%H:%M:%S')})")
                
        except Exception as e:
            print(f"[CLEANUP] Error: {e}")
        
        # 次のクリーンアップまで待機
        time.sleep(CLEANUP_INTERVAL_SECONDS)

# クリーンアップスレッドを開始
cleanup_thread = threading.Thread(target=cleanup_old_images, daemon=True)
cleanup_thread.start()
print(f"[CLEANUP] Auto-cleanup thread started (every {CLEANUP_INTERVAL_SECONDS//3600}h, max age: {IMAGE_MAX_AGE_HOURS}h)")

# Load YOLOv8 model
print("Loading YOLOv8 model...")
model = YOLO('yolov8n.pt')
print("Model loaded!")

# Initialize Face Recognizer
print("Initializing Face Recognizer...")
face_recognizer = FaceRecognizer(data_dir=FACE_DATA_FOLDER)
print("Face Recognizer ready!")

# Initialize Object Recognizer
print("Initializing Object Recognizer...")
OBJECT_DATA_FOLDER = os.path.join(BASE_DIR, 'object_data')
object_recognizer = ObjectRecognizer(data_dir=OBJECT_DATA_FOLDER)
print(f"Object Recognizer ready! ({len(object_recognizer.get_registered_names())} objects registered)")

# シーン認識のルール（検出物体からシーンを推測）
SCENE_RULES = {
    'office': ['laptop', 'keyboard', 'mouse', 'monitor', 'book', 'chair', 'desk'],
    'kitchen': ['cup', 'bottle', 'bowl', 'fork', 'knife', 'spoon', 'microwave', 'oven', 'refrigerator', 'sink'],
    'livingroom': ['couch', 'tv', 'remote', 'potted plant', 'clock', 'vase'],
    'street': ['car', 'truck', 'bus', 'motorcycle', 'bicycle', 'traffic light', 'stop sign'],
    'outdoor': ['person', 'dog', 'cat', 'bird', 'tree', 'bench'],
    'dining': ['dining table', 'wine glass', 'cup', 'fork', 'knife', 'spoon'],
    'bedroom': ['bed', 'teddy bear', 'clock', 'lamp'],
}

def infer_scene(detected_objects):
    """
    検出された物体からシーンを推測
    Returns: (scene_name, confidence)
    """
    if not detected_objects:
        return ('unknown', 0.0)
    
    detected_set = set(obj.lower() for obj in detected_objects)
    
    scene_scores = {}
    for scene, keywords in SCENE_RULES.items():
        matches = detected_set.intersection(set(keywords))
        if matches:
            # スコアはマッチした数 / キーワード総数
            score = len(matches) / len(keywords)
            scene_scores[scene] = score
    
    if not scene_scores:
        return ('general', 0.0)
    
    # 最高スコアのシーンを返す
    best_scene = max(scene_scores, key=scene_scores.get)
    return (best_scene, scene_scores[best_scene])

# ============ Unity Log Forwarding Endpoint ============
@app.route('/log', methods=['POST'])
def receive_unity_log():
    """
    Unityからのコンソールログを受信してファイルに保存
    """
    try:
        data = request.get_json()
        if not data:
            return jsonify({'status': 'error', 'message': 'No JSON data'}), 400
        
        log_type = data.get('type', 'Log')
        message = data.get('message', '')
        stack = data.get('stack', '')
        
        timestamp = datetime.now().strftime('%Y-%m-%d %H:%M:%S')
        log_file = os.path.join(BASE_DIR, 'unity_logs.txt')
        
        with open(log_file, 'a', encoding='utf-8') as f:
            f.write(f"[{timestamp}] [{log_type}] {message}\n")
            if stack:
                f.write(f"    Stack: {stack[:200]}\n")  # スタックトレースは200文字まで
        
        # コンソールにも出力（ただしLogタイプは省略して重要なもののみ）
        if log_type in ['Error', 'Exception', 'Warning']:
            print(f"[UNITY-{log_type}] {message[:100]}")
        
        return jsonify({'status': 'ok'})
    except Exception as e:
        print(f"[ERROR] Log receive failed: {e}")
        return jsonify({'status': 'error', 'message': str(e)}), 500

@app.route('/stream', methods=['POST'])
def stream_frame():
    """
    Receives a frame from Unity, runs inference.
    If objects are detected, saves the image.
    Otherwise, discards it.
    """
    if 'image' not in request.files:
        return jsonify({"error": "No image part"}), 400
    
    file = request.files['image']
    if file.filename == '':
        return jsonify({"error": "No selected file"}), 400

    # 1. Save temporarily for inference
    temp_path = os.path.join(UPLOAD_FOLDER, "temp_stream.jpg")
    file.save(temp_path)

    try:
        # 2. Run Inference (YOLO)
        # conf=0.6: Higher threshold for precision (reduce false positives)
        results = model(temp_path, conf=0.6)
        
        detailed_objects = []
        detected_names = []
        
        # YOLO Detections
        for r in results:
            for box in r.boxes:
                cls_id = int(box.cls[0])
                name = model.names[cls_id]
                conf = float(box.conf[0])
                # Return normalized coordinates (0-1)
                xyxyn = box.xyxyn[0].tolist() # [x1, y1, x2, y2]
                
                detailed_objects.append({
                    "name": name,
                    "confidence": round(conf, 2),
                    "box": xyxyn,
                    "source": "yolo"
                })
                detected_names.append(name)
        
        # =================================================
        # Custom Object Recognition (SIFT/ORB)
        # =================================================
        img_cv2 = cv2.imread(temp_path)
        if img_cv2 is not None:
            # Check custom registered objects
            custom_result = object_recognizer.identify_objects(img_cv2, min_matches=10) # Lower matches for streaming
            
            if custom_result["success"] and len(custom_result["objects"]) > 0:
                for obj in custom_result["objects"]:
                    custom_name = obj["name"]
                    # confidence in this module is match_count, so we might need to normalize or just use it
                    # But the return format is {"name": name, "confidence": match_count}
                    custom_conf = float(obj["confidence"]) 
                    
                    # Prevent duplicates if YOLO also found it (unlikely for custom objects but possible)
                    if custom_name not in detected_names:
                        detailed_objects.append({
                            "name": custom_name,
                            "confidence": custom_conf, # This is match count, usually > 10
                            "box": [0.4, 0.4, 0.6, 0.6], # Dummy center box
                            "source": "custom"
                        })
                        detected_names.append(custom_name)
                        print(f"[Custom Object] Found: {custom_name} (Matches: {custom_conf})")
        # =================================================

        # Unique names for scene inference and summary
        detected_names_unique = list(set(detected_names))
        detected_objects = detected_names_unique # Restore variable for compatibility

        if len(detected_objects) > 0:
            # シーン認識
            scene, scene_confidence = infer_scene(detected_objects)
            
            # Create a filename that includes timestamp and detected objects
            # Format: YYYYMMDD_HHMMSS_scene_obj1_obj2.jpg
            timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
            objects_str = "_".join(detected_objects)
            save_filename = f"{timestamp}_{scene}_{objects_str}.jpg"
            save_path = os.path.join(UPLOAD_FOLDER, save_filename)
            
            # Rename temp file to permanent file
            if os.path.exists(save_path):
                os.remove(save_path)
            # Re-read image to save ensures we don't have file lock issues with rename
            # or just use rename if safer. Window os.rename has limitations if dest exists.
            if os.path.exists(temp_path): # Ensure temp path exists
                if os.path.exists(save_path): os.remove(save_path)
                os.rename(temp_path, save_path)
            
            print(f"[SAVED] {save_filename} (Scene: {scene}, Found: {objects_str})")
            return jsonify({
                "status": "saved",
                "objects": detected_objects,
                "detections": detailed_objects,
                "scene": scene,
                "scene_confidence": round(scene_confidence, 2),
                "filename": save_filename
            })
        else:
            # Debug: Save the latest failed image to check content
            # Nothing interesting, discard
            debug_path = os.path.join(UPLOAD_FOLDER, "debug_latest_no_detection.jpg")
            if os.path.exists(debug_path):
                os.remove(debug_path)
            if os.path.exists(temp_path):
                os.rename(temp_path, debug_path)
                
            # Log brightness
            if img_cv2 is not None:
                avg_brightness = np.mean(img_cv2)
                print(f"[DEBUG] No detection. Image Brightness: {avg_brightness:.2f} (saved to debug_latest_no_detection.jpg)")
            
            return jsonify({
                "status": "discarded",
                "objects": [],
                "scene": "unknown",
                "debug_info": "saved_to_debug_latest"
            })

    except Exception as e:
        print(f"Error: {e}")
        return jsonify({"error": str(e)}), 500

# ============ FACE RECOGNITION ENDPOINTS ============

@app.route('/register_face', methods=['POST'])
def register_face():
    """
    Register a face with a name.
    Expects: multipart form with 'image' and 'name'
    """
    if 'image' not in request.files:
        return jsonify({"error": "No image part"}), 400
    
    name = request.form.get('name', '').strip()
    if not name:
        return jsonify({"error": "No name provided"}), 400
    
    file = request.files['image']
    if file.filename == '':
        return jsonify({"error": "No selected file"}), 400
    
    # Read image from file
    file_bytes = np.frombuffer(file.read(), np.uint8)
    image = cv2.imdecode(file_bytes, cv2.IMREAD_COLOR)
    
    if image is None:
        return jsonify({"error": "Could not read image"}), 400
    
    # Register face
    result = face_recognizer.register_face(image, name)
    
    return jsonify(result)

@app.route('/identify_faces', methods=['POST'])
def identify_faces():
    """
    Identify faces in an image.
    Returns list of detected faces with names and bounding boxes.
    """
    if 'image' not in request.files:
        return jsonify({"error": "No image part"}), 400
    
    file = request.files['image']
    if file.filename == '':
        return jsonify({"error": "No selected file"}), 400
    
    # Read image from file
    file_bytes = np.frombuffer(file.read(), np.uint8)
    image = cv2.imdecode(file_bytes, cv2.IMREAD_COLOR)
    
    if image is None:
        return jsonify({"error": "Could not read image"}), 400
    
    # Identify faces
    faces = face_recognizer.identify_faces(image)
    
    return jsonify({
        "faces": faces,
        "count": len(faces)
    })

@app.route('/list_persons', methods=['GET'])
def list_persons():
    """
    Get list of registered persons.
    """
    persons = face_recognizer.get_registered_persons()
    return jsonify({
        "persons": persons,
        "count": len(persons)
    })

@app.route('/delete_person', methods=['POST'])
def delete_person():
    """
    Delete a registered person.
    """
    name = request.json.get('name', '') if request.is_json else request.form.get('name', '')
    if not name:
        return jsonify({"error": "No name provided"}), 400
    
    result = face_recognizer.delete_person(name)
    return jsonify(result)

# ============ OBJECT RECOGNITION ENDPOINTS ============

@app.route('/objects/register', methods=['POST'])
def register_object():
    """
    Register an object from the current camera frame.
    Expects: multipart form with 'image' and 'name'
    """
    if 'image' not in request.files:
        return jsonify({"error": "No image provided"}), 400
    
    name = request.form.get('name', '')
    if not name:
        return jsonify({"error": "No name provided"}), 400
    
    file = request.files['image']
    np_arr = np.frombuffer(file.read(), np.uint8)
    image = cv2.imdecode(np_arr, cv2.IMREAD_COLOR)
    
    if image is None:
        return jsonify({"error": "Invalid image"}), 400
    
    result = object_recognizer.register_object(image, name)
    return jsonify(result)

@app.route('/objects/add_sample', methods=['POST'])
def add_object_sample():
    """
    Add a sample image to an object (for multi-image registration).
    Call this multiple times during registration to build up features.
    """
    print("[DEBUG] /objects/add_sample called!")  # デバッグログ
    
    if 'image' not in request.files:
        print("[DEBUG] No image in request!")
        return jsonify({"error": "No image provided"}), 400
    
    name = request.form.get('name', '')
    if not name:
        print("[DEBUG] No name provided!")
        return jsonify({"error": "No name provided"}), 400
    
    print(f"[DEBUG] Registering object: {name}")
    
    file = request.files['image']
    np_arr = np.frombuffer(file.read(), np.uint8)
    image = cv2.imdecode(np_arr, cv2.IMREAD_COLOR)
    
    if image is None:
        return jsonify({"error": "Invalid image"}), 400
    
    result = object_recognizer.add_sample_to_object(image, name)
    return jsonify(result)

@app.route('/objects/identify', methods=['POST'])
def identify_objects():
    """
    Identify registered objects in an image.
    """
    if 'image' not in request.files:
        return jsonify({"error": "No image provided"}), 400
    
    file = request.files['image']
    np_arr = np.frombuffer(file.read(), np.uint8)
    image = cv2.imdecode(np_arr, cv2.IMREAD_COLOR)
    
    if image is None:
        return jsonify({"error": "Invalid image"}), 400
    
    result = object_recognizer.identify_objects(image)
    
    # Also run YOLO for common objects (Pre-registered)
    try:
        yolo_results = model(image, conf=0.5, verbose=False)
        common_target = [
            "person", "bicycle", "car", "dog", "cat", 
            "backpack", "umbrella", "bottle", "cup", "fork", 
            "spoon", "bowl", "chair", "laptop", "cell phone"
        ]
        
        yolo_found = []
        for r in yolo_results:
            for box in r.boxes:
                cls_id = int(box.cls[0])
                name = model.names[cls_id]
                conf = float(box.conf[0])
                
                if name in common_target:
                    # Get normalized box coordinates [x1, y1, x2, y2]
                    xyxyn = box.xyxyn[0].tolist()
                    
                    # Add to results
                    result['objects'].append({
                        "name": name,
                        "confidence": round(conf, 2),
                        "matches": 999,
                        "box": xyxyn
                    })
                    yolo_found.append(name)
        
        # Sort combined results by confidence
        result['objects'].sort(key=lambda x: x['confidence'], reverse=True)
        if yolo_found:
            result['message'] += f" (+{len(yolo_found)} common items)"
            
    except Exception as e:
        print(f"YOLO Identify Error: {e}")

    return jsonify(result)

@app.route('/objects/list', methods=['GET'])
def list_objects():
    """
    List all registered objects.
    """
    print("[API] /objects/list called!")
    result = object_recognizer.list_objects()
    print(f"[API] Returning {len(result.get('objects', []))} objects")
    return jsonify(result)

@app.route('/objects/delete', methods=['POST'])
def delete_object():
    """
    Delete a registered object.
    """
    name = request.json.get('name', '') if request.is_json else request.form.get('name', '')
    if not name:
        return jsonify({"error": "No name provided"}), 400
    
    result = object_recognizer.delete_object(name)
    return jsonify(result)

# ============ EXISTING ENDPOINTS ============

@app.route('/search', methods=['GET'])
def search_history():
    """
    Searches saved images for a specific object.
    Query param: ?q=bottle or ?q=person,bottle (comma separated for multi-category OR search)
    """
    query = request.args.get('q', '').lower()
    if not query:
        return jsonify({"error": "No query provided"}), 400

    # 複数カテゴリ対応（カンマ区切り）
    categories = [c.strip() for c in query.split(',') if c.strip()]
    print(f"Searching for: {categories}")
    
    # Simple search: look at filenames
    # Filename format: YYYYMMDD_HHMMSS_obj1_obj2.jpg
    all_files = sorted(glob.glob(os.path.join(UPLOAD_FOLDER, "*.jpg")), reverse=True)
    
    matches = []
    for filepath in all_files:
        filename = os.path.basename(filepath)
        filename_lower = filename.lower()
        
        # "all" を含む場合はすべてマッチ
        is_all = "all" in categories
        
        # いずれかのカテゴリがファイル名に含まれるか（OR検索）
        matches_category = is_all or any(cat in filename_lower for cat in categories)
        
        if matches_category:
            # Extract timestamp and objects
            parts = filename.replace('.jpg', '').split('_')
            time_str = "Unknown"
            objects_list = []
            
            if len(parts) >= 2:
                # Format: YYYY/MM/DD HH:MM:SS
                d = parts[0]
                t = parts[1]
                if len(d) == 8 and len(t) == 6:
                    time_str = f"{d[4:6]}/{d[6:8]} {t[0:2]}:{t[2:4]}:{t[4:6]}"
                
                # Objects are the rest of the parts
                if len(parts) > 2:
                    objects_list = parts[2:]
            
            matches.append({
                "filename": filename,
                "url": f"/uploads/{filename}",
                "timestamp": time_str,
                "objects": objects_list
            })
    
    return jsonify({
        "count": len(matches),
        "results": matches
    })

@app.route('/ping', methods=['GET'])
def ping():
    """Simple heartbeat"""
    return "pong", 200

@app.route('/uploads/<path:filename>')
def serve_file(filename):
    """Serve the image file"""
    return send_from_directory(UPLOAD_FOLDER, filename)

if __name__ == '__main__':
    print("Starting Streaming Server with Face Recognition and Object Recognition...")
    print(f"Registered persons: {[p['name'] for p in face_recognizer.get_registered_persons()]}")
    print(f"Registered objects: {object_recognizer.get_registered_names()}")
    # Listen on all interfaces
    app.run(host='0.0.0.0', port=5000, debug=True)
