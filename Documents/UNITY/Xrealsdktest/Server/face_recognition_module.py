"""
Face Recognition Module
Uses OpenCV's LBPH (Local Binary Patterns Histograms) for face recognition.
"""

import cv2
import numpy as np
import os
import json
from datetime import datetime

class FaceRecognizer:
    def __init__(self, data_dir="face_data"):
        self.data_dir = data_dir
        self.faces_json = os.path.join(data_dir, "faces.json")
        
        # Create data directory if not exists
        if not os.path.exists(data_dir):
            os.makedirs(data_dir)
        
        # Load Haar cascade for face detection (built into OpenCV)
        cascade_path = cv2.data.haarcascades + 'haarcascade_frontalface_default.xml'
        self.face_cascade = cv2.CascadeClassifier(cascade_path)
        
        # LBPH Face Recognizer
        self.recognizer = cv2.face.LBPHFaceRecognizer_create()
        
        # Person data: {id: {"name": "Name", "samples": count}}
        self.persons = {}
        self.label_to_name = {}
        
        # Load existing data
        self._load_data()
    
    def _load_data(self):
        """Load saved face data"""
        if os.path.exists(self.faces_json):
            with open(self.faces_json, 'r', encoding='utf-8') as f:
                data = json.load(f)
                self.persons = data.get('persons', {})
                self.label_to_name = {int(k): v for k, v in data.get('label_to_name', {}).items()}
        
        # Load trained model if exists
        model_path = os.path.join(self.data_dir, "face_model.yml")
        if os.path.exists(model_path):
            try:
                self.recognizer.read(model_path)
                print(f"[FaceRecognizer] Loaded model with {len(self.persons)} persons")
            except Exception as e:
                print(f"[FaceRecognizer] Could not load model: {e}")
    
    def _save_data(self):
        """Save face data"""
        data = {
            'persons': self.persons,
            'label_to_name': {str(k): v for k, v in self.label_to_name.items()}
        }
        with open(self.faces_json, 'w', encoding='utf-8') as f:
            json.dump(data, f, ensure_ascii=False, indent=2)
    
    def detect_faces(self, image):
        """
        Detect faces in an image.
        Returns: list of (x, y, w, h) tuples and grayscale face images
        """
        if image is None:
            return [], []
        
        gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)
        # CLAHE for better contrast
        clahe = cv2.createCLAHE(clipLimit=2.0, tileGridSize=(8, 8))
        gray = clahe.apply(gray)
        
        faces = self.face_cascade.detectMultiScale(
            gray,
            scaleFactor=1.05,  # より細かいスケール（感度アップ）
            minNeighbors=3,    # 検出しやすく（3が一般的）
            minSize=(40, 40),  # より小さい顔も検出
            flags=cv2.CASCADE_SCALE_IMAGE
        )
        
        face_images = []
        for (x, y, w, h) in faces:
            face_img = gray[y:y+h, x:x+w]
            # Resize to standard size
            face_img = cv2.resize(face_img, (100, 100))
            face_images.append(face_img)
        
        return list(faces), face_images
    
    def register_face(self, image, name):
        """
        Register a face with a name.
        Returns: {"success": True/False, "message": "..."}
        """
        faces, face_images = self.detect_faces(image)
        
        if len(faces) == 0:
            return {"success": False, "message": "No face detected"}
        
        # 複数の顔が検出された場合は最大の顔を選択
        if len(faces) > 1:
            # 顔のサイズ（面積）で最大のものを選択
            largest_idx = 0
            largest_area = 0
            for idx, (x, y, w, h) in enumerate(faces):
                area = w * h
                if area > largest_area:
                    largest_area = area
                    largest_idx = idx
            face_images = [face_images[largest_idx]]
            faces = [faces[largest_idx]]
            print(f"[FaceRecognizer] Multiple faces detected, using largest one")
        
        # Get or create label for this person
        label = None
        for lbl, n in self.label_to_name.items():
            if n == name:
                label = lbl
                break
        
        if label is None:
            label = len(self.label_to_name)
            self.label_to_name[label] = name
            self.persons[name] = {"samples": 0}
        
        # Save face sample
        sample_dir = os.path.join(self.data_dir, f"person_{label}")
        if not os.path.exists(sample_dir):
            os.makedirs(sample_dir)
        
        sample_count = self.persons[name]["samples"]
        sample_path = os.path.join(sample_dir, f"sample_{sample_count}.jpg")
        cv2.imwrite(sample_path, face_images[0])
        
        self.persons[name]["samples"] = sample_count + 1
        self._save_data()
        
        # Retrain model
        self._train_model()
        
        return {
            "success": True, 
            "message": f"Registered face for '{name}' (sample #{sample_count + 1})"
        }
    
    def _train_model(self):
        """Train the LBPH model with all saved faces"""
        faces = []
        labels = []
        
        for label, name in self.label_to_name.items():
            sample_dir = os.path.join(self.data_dir, f"person_{label}")
            if not os.path.exists(sample_dir):
                continue
            
            for sample_file in os.listdir(sample_dir):
                if sample_file.endswith('.jpg'):
                    sample_path = os.path.join(sample_dir, sample_file)
                    face_img = cv2.imread(sample_path, cv2.IMREAD_GRAYSCALE)
                    if face_img is not None:
                        faces.append(face_img)
                        labels.append(label)
        
        if len(faces) > 0:
            self.recognizer.train(faces, np.array(labels))
            model_path = os.path.join(self.data_dir, "face_model.yml")
            self.recognizer.write(model_path)
            print(f"[FaceRecognizer] Trained model with {len(faces)} samples from {len(self.label_to_name)} persons")
    
    def identify_faces(self, image):
        """
        Identify faces in an image.
        Returns: list of {"name": "Name", "confidence": 0.0-1.0, "bbox": [x,y,w,h]}
        """
        faces, face_images = self.detect_faces(image)
        
        if len(faces) == 0:
            return []
        
        if len(self.label_to_name) == 0:
            # No trained faces yet
            return [{"name": "Unknown", "confidence": 0.0, "bbox": list(map(int, face))} for face in faces]
        
        results = []
        for i, (face_bbox, face_img) in enumerate(zip(faces, face_images)):
            try:
                label, confidence = self.recognizer.predict(face_img)
                
                # LBPH confidence is distance (lower = better)
                # Convert to 0-1 scale (higher = better)
                # Typical threshold is around 50-80
                confidence_normalized = max(0, (100 - confidence) / 100)
                
                if confidence < 70 and label in self.label_to_name:
                    name = self.label_to_name[label]
                else:
                    name = "Unknown"
                    confidence_normalized = 0.0
                
                results.append({
                    "name": name,
                    "confidence": round(confidence_normalized, 2),
                    "bbox": list(map(int, face_bbox))
                })
            except Exception as e:
                print(f"[FaceRecognizer] Error identifying face: {e}")
                results.append({
                    "name": "Unknown",
                    "confidence": 0.0,
                    "bbox": list(map(int, face_bbox))
                })
        
        return results
    
    def get_registered_persons(self):
        """Get list of registered persons"""
        return [
            {"name": name, "samples": info["samples"]}
            for name, info in self.persons.items()
        ]
    
    def delete_person(self, name):
        """Delete a person from the database"""
        label = None
        for lbl, n in self.label_to_name.items():
            if n == name:
                label = lbl
                break
        
        if label is None:
            return {"success": False, "message": f"Person '{name}' not found"}
        
        # Delete samples
        sample_dir = os.path.join(self.data_dir, f"person_{label}")
        if os.path.exists(sample_dir):
            import shutil
            shutil.rmtree(sample_dir)
        
        # Remove from data
        del self.label_to_name[label]
        if name in self.persons:
            del self.persons[name]
        
        self._save_data()
        
        # Retrain model
        if len(self.label_to_name) > 0:
            self._train_model()
        
        return {"success": True, "message": f"Deleted person '{name}'"}


# Test
if __name__ == "__main__":
    recognizer = FaceRecognizer()
    print(f"Registered persons: {recognizer.get_registered_persons()}")
